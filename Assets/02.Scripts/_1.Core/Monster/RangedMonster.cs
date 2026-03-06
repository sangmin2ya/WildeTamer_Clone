using UnityEngine;

namespace WildTamer
{
    /// <summary>
    /// 원거리 공격을 수행하는 몬스터입니다.
    ///
    /// 공격 흐름:
    ///   MonsterCombatState → Attack() → 대상 저장 + 공격 애니메이션 재생
    ///   → 투척 타이밍에 Animation Event → OnAttackHit() → 투척물 발사
    ///   타겟이 이미 사망했다면 OnAttackHit()는 투척물을 발사하지 않습니다.
    /// </summary>
    public class RangedMonster : Monster
    {
        #region SerializeField 필드

        [Header("투척 오브젝트 설정")]
        [SerializeField, Tooltip("PoolManager에서 관리할 투척 오브젝트 프리팹 — ThrowableProjectile 컴포넌트 필수")]
        private GameObject projectilePrefab;

        [SerializeField, Tooltip("투척 오브젝트 비행 속도 (m/s)")]
        private float projectileSpeed = 8f;

        [SerializeField, Tooltip("투척 오브젝트 적중 판정 반경 (m) — 타겟 위치와의 거리가 이 값 이하면 적중")]
        private float projectileHitRadius = 0.4f;

        [SerializeField, Tooltip("투척 오브젝트 최대 비행 시간 (초) — 초과 시 데미지 없이 소멸")]
        private float projectileLifetime = 3f;

        #endregion

        #region IFightable

        /// <summary>
        /// 대상을 저장하고 공격 애니메이션을 재생합니다.
        /// 실제 투척물 발사는 Animation Event가 OnAttackHit()을 호출할 때 수행됩니다.
        /// </summary>
        public override void Attack(IFightable target)
        {
            base.Attack(target); // _attackTarget 저장 + 애니메이션 재생
        }

        /// <summary>
        /// 공격 애니메이션의 투척 타이밍에 Animation Event로 호출됩니다.
        /// 대상이 이미 사망한 경우 투척물을 발사하지 않습니다.
        /// </summary>
        public override void OnAttackHit()
        {
            if (_attackTarget == null || !_attackTarget.IsAlive)
            {
                return;
            }

            if (projectilePrefab == null)
            {
                Debug.LogWarning($"[RangedMonster] {name}: projectilePrefab이 설정되지 않았습니다.");
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
                Debug.LogWarning($"[RangedMonster] {name}: projectilePrefab에 ThrowableProjectile 컴포넌트가 없습니다.");
                PoolManager.Instance.Release(projectilePrefab, obj);
            }
        }

        #endregion
    }
}
