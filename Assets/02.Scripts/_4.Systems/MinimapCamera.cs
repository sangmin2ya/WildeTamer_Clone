using UnityEngine;

namespace WildTamer
{
    /// <summary>
    /// 미니맵 전용 카메라를 제어하는 싱글턴입니다.
    /// 미니맵 모드에서는 플레이어를 추적하고,
    /// 전체맵 모드에서는 마우스 드래그로 팬 이동을 지원합니다.
    /// </summary>
    public class MinimapCamera : MonoBehaviour
    {
        #region 공개 프로퍼티

        public static MinimapCamera Instance { get; private set; }

        #endregion

        #region SerializeField 필드

        [Header("추적 대상")]
        [SerializeField, Tooltip("미니맵 모드에서 추적할 플레이어 트랜스폼")]
        private Transform playerTransform;

        [Header("카메라 설정")]
        [SerializeField, Tooltip("미니맵 모드의 OrthographicSize")]
        private float minimapSize = 20f;

        [SerializeField, Tooltip("전체맵 모드의 OrthographicSize")]
        private float fullmapSize = 80f;

        [SerializeField, Tooltip("전체맵 팬 이동 감도 (픽셀 → 월드 변환 배율)")]
        private float panSensitivity = 0.1f;

        [SerializeField, Tooltip("전체맵 카메라가 이동 가능한 월드 경계 (Rect)")]
        private Rect worldBounds = new Rect(-100f, -100f, 200f, 200f);

        #endregion

        #region Private 필드

        private Camera _minimapCamera;
        private bool _isFullMapMode;

        #endregion

        #region Unity 메소드

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            _minimapCamera = GetComponent<Camera>();
        }

        private void LateUpdate()
        {
            if (!_isFullMapMode && playerTransform != null)
            {
                Vector3 pos = playerTransform.position;
                pos.z = transform.position.z;
                transform.position = pos;
            }
        }

        #endregion

        #region 모드 전환

        /// <summary>
        /// 전체맵 모드 활성·비활성을 전환합니다.
        /// </summary>
        /// <param name="enable">true면 전체맵, false면 미니맵</param>
        public void SetFullMapMode(bool enable)
        {
            _isFullMapMode = enable;

            if (_minimapCamera != null)
            {
                _minimapCamera.orthographicSize = enable ? fullmapSize : minimapSize;
            }

            // 전체맵 전환 시 현재 플레이어 위치 중심으로 이동
            if (enable && playerTransform != null)
            {
                Vector3 pos = playerTransform.position;
                pos.z = transform.position.z;
                transform.position = pos;
            }
        }

        #endregion

        #region 팬 이동

        /// <summary>
        /// 전체맵 모드에서 스크린 델타만큼 카메라를 이동시킵니다.
        /// 카메라의 가시 영역(상하좌우 끝)이 worldBounds를 벗어나지 않도록 클램프합니다.
        /// </summary>
        /// <param name="screenDelta">마우스 드래그 델타 (스크린 픽셀 단위)</param>
        public void Pan(Vector2 screenDelta)
        {
            if (!_isFullMapMode)
            {
                return;
            }

            // 스크린 델타를 월드 델타로 변환 (드래그 방향 반전으로 자연스러운 팬)
            Vector3 worldDelta = new Vector3(-screenDelta.x, -screenDelta.y, 0f) * panSensitivity;
            Vector3 newPosition = transform.position + worldDelta;

            // 카메라 가시 영역의 절반 크기 계산
            float halfHeight = _minimapCamera.orthographicSize;
            float halfWidth = halfHeight * _minimapCamera.aspect;

            // 카메라 가시 영역 끝이 worldBounds를 넘지 않도록 클램프
            newPosition.x = Mathf.Clamp(newPosition.x, worldBounds.xMin + halfWidth, worldBounds.xMax - halfWidth);
            newPosition.y = Mathf.Clamp(newPosition.y, worldBounds.yMin + halfHeight, worldBounds.yMax - halfHeight);

            transform.position = newPosition;
        }

        #endregion
    }
}
