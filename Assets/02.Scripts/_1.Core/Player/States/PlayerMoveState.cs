namespace WildTamer
{
    /// <summary>
    /// 플레이어 이동 상태입니다.
    /// IsMoving 입력이 감지되면 진입하며, 소속 스쿼드를 이동(Retreat) 상태로 전환합니다.
    /// </summary>
    public class PlayerMoveState : UnitState<PlayerController>
    {
        public PlayerMoveState(PlayerController owner) : base(owner) { }

        public override void Enter()
        {
            
        }

        public override void FixedUpdate()
        {
            Owner.Move(Owner.Input.MoveInput);
        }

        public override void Update()
        {
            Owner.UpdateFacing(Owner.Input.MoveInput);
            Owner.UpdateMoveAnimation();
            Owner.Squad.ForceFaceDirection(Owner.Input.MoveInput);
        }
    }
}
