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
        /// 유닛을 이동시킵니다. 목표지점 이동이라면 destination을 사용하고, 입력 기반 이동이라면 입력 벡터를 사용합니다.
        /// FixedUpdate에서 호출되어야 합니다.
        /// </summary>
        void Move(Vector2 destination);

    }
}
