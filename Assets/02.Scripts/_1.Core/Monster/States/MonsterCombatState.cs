namespace WildTamer
{
    /// <summary>
    /// 몬스터 전투 상태입니다.
    /// 가장 가까운 적을 감지하여 추적하고 공격합니다.
    /// 전투 시스템 구현 후 FixedUpdate/Update 내용을 채워 넣을 예정입니다.
    /// </summary>
    public class MonsterCombatState : UnitState<Monster>
    {
        public MonsterCombatState(Monster owner) : base(owner) { }

        public override void Enter()
        {
            Owner.StopMovement();
        }

        public override void FixedUpdate()
        {
            // TODO: 가장 가까운 적 감지
            // - 공격 사거리 내: Owner.Attack(target)
            // - 사거리 외: Owner.Move(target.position)
            Owner.StopMovement();
        }

        public override void Update()
        {
            // TODO: Owner.UpdateFacing(적 방향)
            Owner.UpdateMoveAnimation();
        }
    }
}
