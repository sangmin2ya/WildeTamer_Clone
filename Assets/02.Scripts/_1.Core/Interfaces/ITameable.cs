namespace WildTamer
{
    /// <summary>
    /// 테이밍 가능한 모든 객체가 구현해야 하는 인터페이스입니다.
    /// 기절, 테이밍(적→아군), 수확 관련 기능을 선언합니다.
    /// </summary>
    public interface ITameable
    {
        /// <summary>현재 기절 상태 여부</summary>
        bool IsStunned { get; }

        /// <summary>
        /// 유닛을 기절 상태로 만듭니다.
        /// HP가 0이 되어 stunChance 판정을 통과하면 호출됩니다.
        /// </summary>
        void Stun();

        /// <summary>
        /// 기절한 적 유닛을 아군으로 테이밍합니다.
        /// enemyPrefab을 풀에 반환하고 allyPrefab을 플레이어 스쿼드에 편입합니다.
        /// </summary>
        void Tame();
    }
}
