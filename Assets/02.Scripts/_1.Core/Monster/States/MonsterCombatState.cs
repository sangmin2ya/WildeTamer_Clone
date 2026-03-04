using UnityEngine;

namespace WildTamer
{
    /// <summary>
    /// 몬스터 전투 상태입니다.
    /// Squad가 제공하는 적 목록에서 가장 가까운 타겟(Monster 또는 Player)을 선택하고,
    /// 공격 사정거리 내외에 따라 이동 또는 공격을 수행합니다.
    /// SteeringAgent가 있으면 장애물을 우회하며 타겟에게 접근합니다.
    ///
    /// 공격 사정거리 내 → 공격 / 외 → SteeringAgent 경유 접근 (없으면 직선 이동)
    /// </summary>
    public class MonsterCombatState : UnitState<Monster>
    {
        #region 상수

        // 주기적으로 더 가까운 타겟을 재탐색하는 간격 (초)
        private const float RetargetInterval = 0.5f;

        #endregion

        #region Private 필드

        // SteeringAgent는 선택 사항 — 없으면 타겟 위치로 직접 이동 폴백
        private SteeringAgent _steeringAgent;

        // IFightable로 선언하여 Monster와 PlayerController 모두 타겟으로 수용
        private IFightable _target;

        // 공격 쿨타임 카운터 — 0 이하일 때 공격 가능
        private float _attackTimer;

        // 재탐색 타이머 — 0 이하가 되면 더 가까운 타겟으로 갱신
        private float _retargetTimer;

        #endregion

        #region 생성자

        public MonsterCombatState(Monster owner) : base(owner)
        {
            _steeringAgent = owner.GetComponent<SteeringAgent>();
        }

        #endregion

        #region 상태 생명주기

        public override void Enter()
        {
            Owner.StopMovement();
            _attackTimer = 0f;
            _retargetTimer = 0f;
            FindNewTarget();
        }

        public override void Exit()
        {
            _target = null;
        }

        public override void FixedUpdate()
        {
            _attackTimer -= Time.fixedDeltaTime;
            _retargetTimer -= Time.fixedDeltaTime;

            // 타겟이 없거나 사망 시 즉시 재탐색
            if (_target == null || !_target.IsAlive)
            {
                FindNewTarget();
                _retargetTimer = RetargetInterval;
            }
            // 주기적으로 더 가까운 타겟 갱신
            else if (_retargetTimer <= 0f)
            {
                FindNewTarget();
                _retargetTimer = RetargetInterval;
            }

            // 타겟 없음 — 정지 대기
            if (_target == null)
            {
                Owner.StopMovement();
                return;
            }

            float distSqr = ((Vector2)_target.Transform.position - (Vector2)Owner.transform.position).sqrMagnitude;

            ExecuteCombatBehavior(distSqr);
        }

        public override void Update()
        {
            Owner.UpdateMoveAnimation();
        }

        #endregion

        #region 전투 행동

        /// <summary>
        /// 거리에 따라 이동·공격 행동을 결정합니다.
        /// </summary>
        /// <param name="distSqr">현재 타겟과의 거리 제곱값</param>
        private void ExecuteCombatBehavior(float distSqr)
        {
            MonsterData data = Owner.Data;
            float attackRangeSqr = data.stat.attackRange * data.stat.attackRange;

            Vector2 toTarget = (Vector2)_target.Transform.position - (Vector2)Owner.transform.position;

            // 공격 사정거리 내 — 이동 멈추고 타겟 방향으로 facing 후 쿨타임 체크 공격
            if (distSqr <= attackRangeSqr)
            {
                Owner.StopMovement();
                Owner.UpdateFacing(toTarget);

                if (_attackTimer <= 0f)
                {
                    Owner.Attack(_target);
                    _attackTimer = data.stat.attackCooldown;
                }
            }
            // 공격 사정거리 밖 — SteeringAgent 경유로 타겟에게 접근, 실제 이동 방향으로 facing
            else
            {
                Vector2 destination = GetMoveDestination();
                Owner.Move(destination);

                Vector2 moveDir = destination - (Vector2)Owner.transform.position;
                Owner.UpdateFacing(moveDir);
            }
        }

        #endregion

        #region 이동 목표 계산

        /// <summary>
        /// SteeringAgent가 있으면 장애물 우회 목표점을, 없으면 타겟 위치를 직접 반환합니다.
        /// </summary>
        private Vector2 GetMoveDestination()
        {
            Vector2 targetPos = _target.Transform.position;

            if (_steeringAgent != null)
            {
                return _steeringAgent.ComputeSteeringTarget(targetPos);
            }

            return targetPos;
        }

        #endregion

        #region 타겟 탐색

        /// <summary>
        /// 소속 Squad가 감지한 적 군중의 리더(플레이어) 및 멤버 중
        /// 가장 가까운 살아있는 IFightable을 타겟으로 설정합니다.
        /// </summary>
        private void FindNewTarget()
        {
            if (Owner.Squad == null)
            {
                _target = null;
                return;
            }

            _target = Owner.Squad.GetNearestEnemyTarget(Owner.transform.position);
        }

        #endregion
    }
}
