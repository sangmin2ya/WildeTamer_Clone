using UnityEngine;

namespace WildTamer
{
    /// <summary>
    /// 타겟과 일정 거리를 유지하며 원거리 공격을 수행하는 몬스터입니다.
    ///
    /// 이동 패턴 — BasicMonster(직선 추격), RangedMonster(직선 추격)와 완전히 다릅니다:
    ///   dist &lt; PreferredMinRange → 후퇴 (타겟 반대 방향으로 이동, 타겟은 계속 바라봄)
    ///   dist &gt; PreferredMaxRange → 접근
    ///   그 사이                   → 정지 + 투척물 발사
    ///
    /// 공격 사거리 — 적정 거리(MinRange ~ MaxRange) 안에서만 공격 가능합니다.
    ///   너무 가깝거나 너무 멀면 공격 불가 → 적극적으로 거리를 조정합니다.
    ///
    /// 공격 흐름:
    ///   KeepDistanceCombatState → Attack() → 공격 애니메이션 재생
    ///   → Animation Event → OnAttackHit() → 투척물 발사
    ///   타겟이 이미 사망했다면 투척물을 발사하지 않습니다.
    /// </summary>
    public class KeepDistanceMonster : Monster
    {
        #region SerializeField 필드

        [Header("거리 유지 설정")]
        [SerializeField, Tooltip("이 거리 이하로 타겟이 접근하면 후퇴를 시작합니다")]
        private float preferredMinRange = 2f;

        [SerializeField, Tooltip("이 거리 이상으로 타겟이 멀어지면 접근합니다. 이 범위 안에서만 공격 가능합니다")]
        private float preferredMaxRange = 5f;

        [Header("투척 오브젝트 설정")]
        [SerializeField, Tooltip("PoolManager에서 관리할 투척 오브젝트 프리팹 — ThrowableProjectile 컴포넌트 필수")]
        private GameObject projectilePrefab;

        [SerializeField, Tooltip("투척 오브젝트 비행 속도 (m/s)")]
        private float projectileSpeed = 6f;

        [SerializeField, Tooltip("투척 오브젝트 적중 판정 반경 (m)")]
        private float projectileHitRadius = 0.4f;

        [SerializeField, Tooltip("투척 오브젝트 최대 비행 시간 (초) — 초과 시 데미지 없이 소멸")]
        private float projectileLifetime = 4f;

        #endregion

        #region Public 프로퍼티

        /// <summary>후퇴 기준 최소 거리 — KeepDistanceCombatState에서 참조합니다</summary>
        public float PreferredMinRange => preferredMinRange;

        /// <summary>접근 기준 최대 거리 (= 유효 공격 사거리) — KeepDistanceCombatState에서 참조합니다</summary>
        public float PreferredMaxRange => preferredMaxRange;

        #endregion

        #region Unity 메소드

        protected override void Awake()
        {
            base.Awake();
            // 기본 MonsterCombatState를 거리 유지 전용 상태로 교체합니다
            _combatState = new KeepDistanceCombatState(this);
        }

        #endregion

        #region IFightable

        /// <summary>
        /// 공격 애니메이션의 투척 타이밍에 Animation Event로 호출됩니다.
        /// 타겟이 이미 사망했다면 투척물을 발사하지 않습니다.
        /// </summary>
        public override void OnAttackHit()
        {
            if (_attackTarget == null || !_attackTarget.IsAlive)
            {
                return;
            }

            if (projectilePrefab == null)
            {
                Debug.LogWarning($"[KeepDistanceMonster] {name}: projectilePrefab이 설정되지 않았습니다.");
                return;
            }

            GameObject obj = PoolManager.Instance.Get(projectilePrefab);
            obj.transform.SetPositionAndRotation(transform.position, Quaternion.identity);

            ThrowableProjectile projectile = obj.GetComponent<ThrowableProjectile>();

            if (projectile != null)
            {
                projectile.Initialize(
                    sourcePrefab: projectilePrefab,
                    target:       _attackTarget,
                    damage:       monsterData.stat.attackDamage,
                    speed:        projectileSpeed,
                    hitRadius:    projectileHitRadius,
                    lifetime:     projectileLifetime
                );
            }
            else
            {
                Debug.LogWarning($"[KeepDistanceMonster] {name}: projectilePrefab에 ThrowableProjectile 컴포넌트가 없습니다.");
                PoolManager.Instance.Release(projectilePrefab, obj);
            }
        }

        #endregion

#if UNITY_EDITOR
        #region 디버그 기즈모

        private void OnDrawGizmosSelected()
        {
            // 최소 거리 — 빨강 (이 안에 들어오면 후퇴)
            UnityEditor.Handles.color = new Color(1f, 0.2f, 0.2f, 0.4f);
            UnityEditor.Handles.DrawWireDisc(transform.position, Vector3.forward, preferredMinRange);

            // 최대 거리 — 파랑 (이 밖으로 나가면 접근 / 이 안에서만 공격 가능)
            UnityEditor.Handles.color = new Color(0.2f, 0.5f, 1f, 0.4f);
            UnityEditor.Handles.DrawWireDisc(transform.position, Vector3.forward, preferredMaxRange);
        }

        #endregion
#endif
    }
}
