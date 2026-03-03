using UnityEngine;

namespace WildTamer
{
    /// <summary>
    /// 이동 가능한 모든 객체가 구현해야 하는 인터페이스입니다.
    /// 이동, 방향 전환, 이동 애니메이션 관련 기능을 선언합니다.
    /// </summary>
    public interface IMovable
    {
        /// <summary>
        /// 유닛을 이동시킵니다.
        /// FixedUpdate에서 호출되어야 합니다.
        /// </summary>
        void Move();

        /// <summary>
        /// 이동 방향에 따라 스프라이트 방향(좌/우)을 갱신합니다.
        /// </summary>
        /// <param name="direction">이동 방향 벡터</param>
        void UpdateFacing(Vector2 direction);

        /// <summary>
        /// 실제 이동 속도를 기반으로 이동 애니메이션 파라미터를 갱신합니다.
        /// </summary>
        void UpdateMoveAnimation();
    }
}
