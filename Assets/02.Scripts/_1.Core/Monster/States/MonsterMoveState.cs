using UnityEngine;

namespace WildTamer
{
    /// <summary>
    /// 몬스터 이동 상태입니다.
    /// SteeringAgent를 통해 장애물을 회피하는 방향을 계산하고, 해당 위치로 이동합니다.
    /// SteeringAgent 컴포넌트가 없으면 스쿼드 목표 위치로 직접 이동합니다 (폴백).
    /// </summary>
    public class MonsterMoveState : UnitState<Monster>
    {
        #region Private 필드

        // SteeringAgent는 선택 사항 — 없으면 직접 이동 폴백
        private SteeringAgent _steeringAgent;

        #endregion

        #region 생성자

        public MonsterMoveState(Monster owner) : base(owner)
        {
            _steeringAgent = owner.GetComponent<SteeringAgent>();
        }

        #endregion

        #region 상태 생명주기

        public override void FixedUpdate()
        {
            if (Owner.Squad == null)
            {
                return;
            }

            Vector2 destination = GetDestination();
            Owner.Move(destination);
        }

        public override void Update()
        {
            Owner.UpdateMoveAnimation();
        }

        #endregion

        #region 내부 유틸리티

        /// <summary>
        /// SteeringAgent가 있으면 장애물 회피 목표점을, 없으면 Squad 목표 위치를 반환합니다.
        /// </summary>
        private Vector2 GetDestination()
        {
            if (_steeringAgent != null)
            {
                return _steeringAgent.ComputeSteeringTarget(Owner.Squad.TargetPosition);
            }

            // 폴백: 장애물 무시 직접 이동
            return Owner.Squad.TargetPosition;
        }

        #endregion
    }
}
