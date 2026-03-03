namespace WildTamer
{
    /// <summary>
    /// 전투 가능한 모든 객체가 구현해야 하는 인터페이스입니다.
    /// 공격, 공격 애니메이션, 피격 관련 기능을 선언합니다.
    /// </summary>
    public interface IFightable
    {
        /// <summary>현재 체력</summary>
        float CurrentHp { get; }

        /// <summary>생존 여부 (CurrentHp > 0)</summary>
        bool IsAlive { get; }

        /// <summary>
        /// 대상을 공격합니다.
        /// </summary>
        /// <param name="target">공격 대상</param>
        void Attack(IFightable target);

        /// <summary>
        /// 데미지를 받아 체력을 감소시킵니다.
        /// </summary>
        /// <param name="damage">받는 데미지 양</param>
        void TakeDamage(float damage);

        /// <summary>
        /// 사망 처리를 수행합니다. (풀 반환, 아군화 등)
        /// </summary>
        void Die();

        /// <summary>
        /// 공격 애니메이션을 재생합니다.
        /// </summary>
        void PlayAttackAnimation();

        /// <summary>
        /// 피격 애니메이션을 재생합니다.
        /// </summary>
        void PlayHitAnimation();
    }
}
