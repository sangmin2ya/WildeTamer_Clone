namespace WildTamer
{
    /// <summary>
    /// 플레이어 전투 상태입니다.
    /// 이동 입력이 없을 때 스쿼드 전투 돌입에 따라 진입합니다.
    /// IsMoving이면 PlayerController.Update()에서 즉시 이동 상태로 덮어씌워집니다.
    /// 전투 시스템 구현 후 세부 로직을 채워 넣을 예정입니다.
    /// </summary>
    public class PlayerCombatState : UnitState<PlayerController>
    {
        public PlayerCombatState(PlayerController owner) : base(owner) { }

        public override void Enter()
        {
            Owner.StopMovement();
        }

        public override void FixedUpdate()
        {
            // TODO: 자동 공격 또는 타겟 추적
            Owner.StopMovement();
        }

        public override void Update()
        {
            // TODO: UpdateFacing(타겟 방향)
            Owner.UpdateMoveAnimation();
        }
    }
}
