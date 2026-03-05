using UnityEngine;

namespace WildTamer
{
    /// <summary>
    /// 카메라 상대 Y 위치를 기반으로 SpriteRenderer의 sortingOrder를 갱신합니다.
    /// spriteRenderer.isVisible로 카메라에 보일 때만 LateUpdate를 실행합니다.
    /// OnBecameVisible과 달리 이 컴포넌트가 SpriteRenderer와 다른 GameObject에 있어도 동작합니다.
    ///
    /// sortingOrder = Round(-(오브젝트Y - 카메라Y) * scale)
    ///   카메라 기준 상대값을 사용하여 월드가 커져도 sortingOrder가 short 범위를 초과하지 않습니다.
    /// </summary>
    public class DepthSortByY : MonoBehaviour
    {
        #region SerializeField 필드

        [Header("정렬 설정")]
        [SerializeField, Tooltip("Y 좌표를 정수 sortingOrder로 변환하는 배율. 클수록 세밀한 정렬.")]
        private float scale = 100f;

        [SerializeField, Tooltip("sortingOrder를 적용할 SpriteRenderer")]
        private SpriteRenderer spriteRenderer;

        #endregion

        #region Private 필드

        private Camera _mainCamera;

        #endregion

        #region Unity 메소드

        private void Awake()
        {
            _mainCamera = Camera.main;
        }

        private void LateUpdate()
        {
            if (spriteRenderer == null || _mainCamera == null || !spriteRenderer.isVisible)
            {
                return;
            }

            float relativeY = transform.position.y - _mainCamera.transform.position.y;
            spriteRenderer.sortingOrder = Mathf.RoundToInt(-relativeY * scale);
        }

        #endregion
    }
}
