namespace WildTamer
{
    /// <summary>
    /// 몬스터 정지 상태입니다.
    /// 이동하지 않고 대기하며, 아무 행동도 수행하지 않습니다.
    /// </summary>
    public class MonsterIdleState : UnitState<Monster>
    {
        public MonsterIdleState(Monster owner) : base(owner) { }

        public override void Enter()
        {
            Owner.StopMovement();
        }

        public override void FixedUpdate()
        {
            Owner.StopMovement();
        }

        public override void Update()
        {
            Owner.UpdateMoveAnimation();
        }
    }
}
