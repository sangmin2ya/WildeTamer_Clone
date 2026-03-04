namespace WildTamer
{
    /// <summary>
    /// 테이밍 가능한 모든 객체가 구현해야 하는 인터페이스입니다.
    /// 기절, 테이밍(적→아군), 아군 해제(아군→적) 관련 기능을 선언합니다.
    /// </summary>
    public interface ITameable
    {
        /// <summary>현재 기절 상태 여부</summary>
        bool IsStunned { get; }

        /// <summary>테이밍(아군 편입) 상태 여부</summary>
        bool IsTamed { get; }

        /// <summary>
        /// 유닛을 기절 상태로 만듭니다.
        /// HP가 0이 되어 아군에게 처치될 때 stunChance 판정으로 호출됩니다.
        /// </summary>
        void Stun();

        /// <summary>
        /// 기절한 적 유닛을 아군으로 테이밍합니다.
        /// enemyPrefab을 비활성화하고 allyPrefab을 활성화합니다.
        /// </summary>
        void Tame();
    }
}
