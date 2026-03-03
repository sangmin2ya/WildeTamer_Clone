using UnityEngine;
using UnityEngine.InputSystem;

namespace WildTamer
{
    /// <summary>
    /// 플레이어 입력을 수신하고 다른 컴포넌트에 노출하는 입력 전담 클래스
    /// 이동·전투 등 실제 처리 로직은 각 담당 컴포넌트에서 이 클래스의 프로퍼티를 읽어 사용합니다.
    /// </summary>
    [RequireComponent(typeof(PlayerInput))]
    public class PlayerInputController : MonoBehaviour
    {
        #region Public 프로퍼티

        /// <summary>이동 입력 벡터</summary>
        public Vector2 MoveInput { get; private set; }

        /// <summary>이동 입력이 존재하는지 여부</summary>
        public bool IsMoving => MoveInput.sqrMagnitude > 0.01f;

        #endregion

        #region 입력 처리

        /// <summary>
        /// PlayerInput(Send Messages)이 Move 액션 발생 시 자동 호출합니다.
        /// </summary>
        private void OnMove(InputValue value)
        {
            MoveInput = value.Get<Vector2>();
        }

        #endregion
    }
}
