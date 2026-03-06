using UnityEngine;

namespace WildTamer
{
    /// <summary>
    /// 카메라 상대 Y 위치를 기반으로 World Space Canvas의 sortingOrder를 갱신합니다.
    /// DepthSortByY와 동일한 공식을 사용하여 스프라이트와 같은 깊이로 렌더링됩니다.
    ///
    /// sortingOrder = Round(-(오브젝트Y - 카메라Y) * scale)
    ///
    /// ■ 사용법
    ///   Monster 하위 World Space Canvas 오브젝트에 이 컴포넌트를 부착합니다.
    ///   scale은 DepthSortByY와 동일한 값으로 맞춰야 렌더 순서가 일치합니다.
    /// </summary>
    public class CanvasDepthSortByY : MonoBehaviour
    {
        #region SerializeField 필드

        [Header("정렬 설정")]
        [SerializeField, Tooltip("Y 좌표를 정수 sortingOrder로 변환하는 배율. DepthSortByY와 동일한 값을 사용하세요.")]
        private float scale = 100f;

        [SerializeField, Tooltip("sortingOrder를 적용할 Canvas. 비워두면 자신의 Canvas를 자동으로 사용합니다.")]
        private Canvas targetCanvas;

        #endregion

        #region Private 필드

        private Camera _mainCamera;

        #endregion

        #region Unity 메소드

        private void Awake()
        {
            _mainCamera = Camera.main;

            if (targetCanvas == null)
            {
                targetCanvas = GetComponent<Canvas>();
            }
        }

        private void LateUpdate()
        {
            if (targetCanvas == null || _mainCamera == null)
            {
                return;
            }

            float relativeY = transform.position.y - _mainCamera.transform.position.y;
            targetCanvas.sortingOrder = Mathf.RoundToInt(-relativeY * scale);
        }

        #endregion
    }
}
