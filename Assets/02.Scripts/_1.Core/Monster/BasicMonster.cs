namespace WildTamer
{
    /// <summary>
    /// Monster 추상 클래스의 기본 근접 공격 구현체입니다.
    /// 이동·애니메이션·상태 전환은 Monster 기반 클래스의 상태 머신이 처리합니다.
    ///
    /// 공격 흐름:
    ///   MonsterCombatState → Attack() → 공격 애니메이션 재생
    ///   → 타격 타이밍에 Animation Event → OnAttackHit() → 타겟에 데미지 적용
    ///   타겟이 이미 사망했다면 OnAttackHit()는 아무 동작도 하지 않습니다.
    /// </summary>
    public class BasicMonster : Monster
    {
        // Attack()과 OnAttackHit()은 Monster 기반 클래스의 구현을 그대로 사용합니다.
        // • Attack()     : 대상 저장 + 공격 애니메이션 재생
        // • OnAttackHit(): 대상 생존 확인 후 데미지 적용 (Animation Event 호출)
    }
}
