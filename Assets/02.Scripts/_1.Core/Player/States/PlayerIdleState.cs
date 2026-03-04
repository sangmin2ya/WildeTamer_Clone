namespace WildTamer
{
    /// <summary>
    /// 플레이어 정지 상태입니다.
    /// 이동 입력이 없을 때 유지되며, 소속 스쿼드를 정지시킵니다.
    /// </summary>
    public class PlayerIdleState : UnitState<PlayerController>
    {
        public PlayerIdleState(PlayerController owner) : base(owner) { }

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
