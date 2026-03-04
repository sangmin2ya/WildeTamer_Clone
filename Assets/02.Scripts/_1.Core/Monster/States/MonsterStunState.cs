using UnityEngine;

namespace WildTamer
{
    /// <summary>
    /// 몬스터의 기절 상태입니다.
    /// HP가 0이 되어 stunChance 판정을 통과하면 진입하며,
    /// 이동·공격을 완전히 정지하고 TameController에 테이밍 UI 표시를 요청합니다.
    /// MonsterData.stunDuration 초가 경과하면 자동으로 Die()를 호출합니다.
    /// TameController가 없는 몬스터에서도 null 조건 연산자로 안전하게 동작합니다.
    /// </summary>
    public class MonsterStunState : UnitState<Monster>
    {
        #region Private 필드

        private TameController _tameController;

        #endregion

        #region 생성자

        public MonsterStunState(Monster owner) : base(owner)
        {
            _tameController = owner.GetComponent<TameController>();
        }

        #endregion

        #region 상태 생명주기

        public override void Enter()
        {
            Owner.StopMovement();
            _tameController?.ShowTameUI();
        }

        public override void Exit()
        {
            _tameController?.HideTameUI();
        }

        #endregion
    }
}
