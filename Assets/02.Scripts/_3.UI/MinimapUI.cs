using UnityEngine;
using UnityEngine.UI;

namespace WildTamer
{
    /// <summary>
    /// 미니맵·전체맵 패널 전환을 담당하는 UI 컨트롤러입니다.
    /// 미니맵 패널 클릭 → 전체맵 패널 열기,
    /// 전체맵 닫기 버튼 → 미니맵 패널로 복귀합니다.
    /// </summary>
    public class MinimapUI : MonoBehaviour
    {
        #region 공개 프로퍼티

        public static MinimapUI Instance { get; private set; }

        #endregion

        #region SerializeField 필드

        [Header("패널 참조")]
        [SerializeField, Tooltip("화면 구석에 항상 표시되는 미니맵 패널")]
        private GameObject minimapPanel;

        [SerializeField, Tooltip("클릭 시 열리는 전체맵 패널")]
        private GameObject fullmapPanel;

        [SerializeField, Tooltip("전체맵 패널 열기 버튼")]
        private GameObject openFullMapButton;

        [SerializeField, Tooltip("전체맵 패널 닫기 버튼")]
        private GameObject closeFullMapButton;

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
        }

        private void Start()
        {
            // 초기 상태: 미니맵만 표시
            if (minimapPanel != null)
            {
                minimapPanel.SetActive(true);
            }

            if (fullmapPanel != null)
            {
                fullmapPanel.SetActive(false);
            }

            // 버튼 이벤트 연결
            if (openFullMapButton != null)
            {
                openFullMapButton.GetComponent<Button>().onClick.AddListener(OpenFullMap);
            }

            if (closeFullMapButton != null)
            {
                closeFullMapButton.GetComponent<Button>().onClick.AddListener(CloseFullMap);
            }
        }

        #endregion

        #region 패널 제어

        /// <summary>
        /// 전체맵 패널 열기
        /// </summary>
        public void OpenFullMap()
        {
            if (fullmapPanel != null)
            {
                fullmapPanel.SetActive(true);
            }

            MinimapCamera.Instance?.SetFullMapMode(true);
        }

        /// <summary>
        /// 전체맵 패널 닫기
        /// </summary>
        public void CloseFullMap()
        {
            if (fullmapPanel != null)
            {
                fullmapPanel.SetActive(false);
            }

            MinimapCamera.Instance?.SetFullMapMode(false);
        }

        /// <summary>
        /// 전체맵 패널 열기·닫기를 토글합니다.
        /// </summary>
        public void Toggle()
        {
            if (fullmapPanel != null && fullmapPanel.activeSelf)
            {
                CloseFullMap();
            }
            else
            {
                OpenFullMap();
            }
        }

        #endregion
    }
}
