using UnityEngine;

namespace WildTamer
{
    /// <summary>
    /// KeepDistanceMonster 전용 전투 상태입니다.
    /// 타겟과의 거리를 preferredMinRange ~ preferredMaxRange 사이로 유지합니다.
    ///
    /// 거리에 따른 행동:
    ///   dist &lt; minRange  → 타겟 반대 방향으로 후퇴 (타겟은 계속 바라봄)
    ///   dist &gt; maxRange  → 타겟에게 접근
    ///   그 사이          → 정지 + 쿨타임마다 원거리 공격
    /// </summary>
    public class KeepDistanceCombatState : UnitState<Monster>
    {
        #region 상수

        private const float RetargetInterval = 0.5f;

        // 후퇴 시 목적지 계산에 사용하는 임의 거리 — 실제 이동 속도는 monsterData.stat.moveSpeed
        private const float RetreatDestOffset = 6f;

        #endregion

        #region Private 필드

        private readonly KeepDistanceMonster _keeper;

        private IFightable _target;
        private float _attackTimer;
        private float _retargetTimer;

        #endregion

        #region 생성자

        public KeepDistanceCombatState(KeepDistanceMonster owner) : base(owner)
        {
            _keeper = owner;
        }

        #endregion

        #region 상태 생명주기

        public override void Enter()
        {
            Owner.StopMovement();
            _attackTimer  = 0f;
            _retargetTimer = 0f;
            FindNewTarget();
        }

        public override void Exit()
        {
            _target = null;
        }

        public override void FixedUpdate()
        {
            _attackTimer   -= Time.fixedDeltaTime;
            _retargetTimer -= Time.fixedDeltaTime;

            // 타겟 사망·소멸 시 즉시 재탐색
            if (_target == null || !_target.IsAlive)
            {
                FindNewTarget();
                _retargetTimer = RetargetInterval;
            }
            else if (_retargetTimer <= 0f)
            {
                FindNewTarget();
                _retargetTimer = RetargetInterval;
            }

            if (_target == null)
            {
                Owner.StopMovement();
                return;
            }

            Vector2 toTarget = (Vector2)_target.Transform.position - (Vector2)Owner.transform.position;
            float distSqr    = toTarget.sqrMagnitude;
            float minSqr     = _keeper.PreferredMinRange * _keeper.PreferredMinRange;
            float maxSqr     = _keeper.PreferredMaxRange * _keeper.PreferredMaxRange;

            // 후퇴 중에도 타겟 방향을 바라봅니다
            Owner.UpdateFacing(toTarget);

            ExecuteCombatBehavior(toTarget, distSqr, minSqr, maxSqr);
        }

        public override void Update()
        {
            Owner.UpdateMoveAnimation();
        }

        #endregion

        #region 전투 행동

        /// <summary>
        /// 거리 조건에 따라 후퇴·접근·정지 공격 중 하나를 수행합니다.
        /// </summary>
        private void ExecuteCombatBehavior(Vector2 toTarget, float distSqr, float minSqr, float maxSqr)
        {
            if (distSqr < minSqr)
            {
                // ── 너무 가까움 → 타겟 반대 방향으로 후퇴 ──────────────
                Vector2 retreatDest = (Vector2)Owner.transform.position
                                      - toTarget.normalized * RetreatDestOffset;
                Owner.Move(retreatDest);
            }
            else if (distSqr > maxSqr)
            {
                // ── 너무 멂 → 타겟에게 접근 ─────────────────────────────
                Owner.Move((Vector2)_target.Transform.position);
            }
            else
            {
                // ── 적정 거리 → 정지 후 쿨타임마다 공격 ─────────────────
                Owner.StopMovement();

                if (_attackTimer <= 0f)
                {
                    Owner.Attack(_target);
                    _attackTimer = Owner.Data.stat.attackCooldown;
                }
            }
        }

        #endregion

        #region 타겟 탐색

        /// <summary>
        /// 소속 Squad의 적 목록에서 가장 가까운 살아있는 타겟을 설정합니다.
        /// </summary>
        private void FindNewTarget()
        {
            _target = Owner.Squad?.GetNearestEnemyTarget(Owner.transform.position);
        }

        #endregion
    }
}
