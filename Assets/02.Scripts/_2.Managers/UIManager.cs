using System.Collections.Generic;
using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace WildTamer
{
    /// <summary>
    /// 전체 UI를 중앙에서 관리하는 싱글턴 매니저입니다.
    /// 팝업 열기·닫기, 팝업 중 게임 일시 정지(TimeScale=0),
    /// 플레이어 HP 및 부대 개체수 HUD 표시를 담당합니다.
    /// </summary>
    public class UIManager : Singleton<UIManager>
    {
        #region SerializeField 필드 - HUD

        [Header("플레이어 HUD")]
        [SerializeField, Tooltip("HP 바의 fillAmount를 제어할 Image 컴포넌트")]
        private Image hpFillImage;

        [SerializeField, Tooltip("플레이어 HP 텍스트 (현재 / 최대)")]
        private TMP_Text hpText;

        [Header("부대 HUD")]
        [SerializeField, Tooltip("길들인 몬스터 수 텍스트 (현재 / 최대)")]
        private TMP_Text squadCountText;

        #endregion

        #region SerializeField 필드 - 팝업 패널

        [Header("팝업 패널")]
        [SerializeField, Tooltip("메뉴 팝업 패널 (다시하기·불러오기·종료하기)")]
        private GameObject menuPanel;

        [SerializeField, Tooltip("몬스터 도감 팝업 패널")]
        private GameObject collectionPanel;

        [SerializeField, Tooltip("전체맵 팝업 패널")]
        private GameObject fullmapPanel;

        [SerializeField, Tooltip("게임오버 팝업 패널 (재시작·종료 버튼 포함)")]
        private GameObject gameOverPanel;

        [Header("공용 팝업 배경")]
        [SerializeField, Tooltip("팝업이 열릴 때 표시할 반투명 오버레이 패널 — 팝업이 하나라도 열리면 활성화됩니다.")]
        private GameObject overlayPanel;

        [Header("팝업 등장 애니메이션")]
        [SerializeField, Tooltip("팝업이 열릴 때 scale 0→1 애니메이션 재생 시간 (초)")]
        private float popupOpenDuration = 0.25f;

        [SerializeField, Tooltip("팝업 등장 이징 모드")]
        private Ease popupOpenEase = Ease.OutBack;

        #endregion

        #region SerializeField 필드 - HUD 버튼

        [Header("HUD 버튼")]
        [SerializeField, Tooltip("메뉴 팝업 열기 버튼")]
        private Button menuButton;

        [SerializeField, Tooltip("도감 팝업 열기 버튼")]
        private Button collectionButton;

        [SerializeField, Tooltip("미니맵 클릭 시 전체맵 열기 버튼")]
        private Button minimapButton;

        #endregion

        #region SerializeField 필드 - 메뉴 패널 버튼

        [Header("메뉴 패널 버튼")]
        [SerializeField, Tooltip("처음부터 다시 시작하는 버튼")]
        private Button restartButton;

        [SerializeField, Tooltip("저장된 데이터를 불러오는 버튼")]
        private Button loadButton;

        [SerializeField, Tooltip("게임을 종료하는 버튼")]
        private Button quitButton;

        [SerializeField, Tooltip("메뉴 패널 닫기 버튼")]
        private Button menuCloseButton;

        #endregion

        #region SerializeField 필드 - 팝업 닫기 버튼

        [Header("팝업 닫기 버튼")]
        [SerializeField, Tooltip("도감 패널 닫기 버튼")]
        private Button collectionCloseButton;

        [SerializeField, Tooltip("전체맵 패널 닫기 버튼")]
        private Button fullmapCloseButton;

        [Header("게임오버 패널 버튼")]
        [SerializeField, Tooltip("게임오버 후 처음부터 다시 시작하는 버튼")]
        private Button gameOverRestartButton;

        [SerializeField, Tooltip("게임오버 후 게임을 종료하는 버튼")]
        private Button gameOverQuitButton;

        #endregion

        #region Private 필드

        private readonly HashSet<GameObject> _openPanels = new HashSet<GameObject>();
        private PlayerController _player;
        private Squad _playerSquad;

        #endregion

        #region Unity 메소드

        private void Start()
        {
            InitButtonListeners();
            FindPlayerAndSubscribe();
            FindSquadAndSubscribe();
            CloseAllPopups();
        }

        private void OnDestroy()
        {
            UnsubscribeEvents();
        }

        #endregion

        #region 초기화

        /// <summary>
        /// Inspector에서 연결된 버튼에 이벤트 리스너를 등록합니다.
        /// </summary>
        private void InitButtonListeners()
        {
            // HUD 버튼
            menuButton?.onClick.AddListener(OpenMenu);
            collectionButton?.onClick.AddListener(OpenCollection);
            minimapButton?.onClick.AddListener(OpenFullMap);

            // 메뉴 패널
            restartButton?.onClick.AddListener(OnRestartClicked);
            loadButton?.onClick.AddListener(OnLoadClicked);
            quitButton?.onClick.AddListener(OnQuitClicked);
            menuCloseButton?.onClick.AddListener(CloseMenu);

            // 팝업 닫기
            collectionCloseButton?.onClick.AddListener(CloseCollection);
            fullmapCloseButton?.onClick.AddListener(CloseFullMap);

            // 게임오버 패널
            gameOverRestartButton?.onClick.AddListener(OnGameOverRestartClicked);
            gameOverQuitButton?.onClick.AddListener(OnQuitClicked);
        }

        /// <summary>
        /// PlayerController를 탐색하고 HP 변경 이벤트를 구독합니다.
        /// </summary>
        private void FindPlayerAndSubscribe()
        {
            _player = FindFirstObjectByType<PlayerController>();

            if (_player != null)
            {
                _player.OnHpChanged += OnPlayerHpChanged;
                OnPlayerHpChanged(_player.CurrentHp, _player.MaxHp);
            }
        }

        /// <summary>
        /// 플레이어 스쿼드를 탐색하고 멤버 수 변경 이벤트를 구독합니다.
        /// </summary>
        private void FindSquadAndSubscribe()
        {
            _playerSquad = Squad.GetPlayerSquad();

            if (_playerSquad != null)
            {
                _playerSquad.OnMemberCountChanged += OnSquadCountChanged;
                OnSquadCountChanged(_playerSquad.MemberCount, _playerSquad.MaxMembers);
            }
        }

        /// <summary>
        /// 구독한 이벤트를 전부 해제합니다.
        /// </summary>
        private void UnsubscribeEvents()
        {
            if (_player != null)
            {
                _player.OnHpChanged -= OnPlayerHpChanged;
            }

            if (_playerSquad != null)
            {
                _playerSquad.OnMemberCountChanged -= OnSquadCountChanged;
            }
        }

        #endregion

        #region HUD 업데이트

        /// <summary>
        /// 플레이어 HP가 변경될 때 슬라이더와 텍스트를 갱신합니다.
        /// </summary>
        private void OnPlayerHpChanged(float current, float max)
        {
            if (hpFillImage != null)
            {
                hpFillImage.fillAmount = max > 0f ? current / max : 0f;
            }

            if (hpText != null)
            {
                hpText.text = $"{Mathf.CeilToInt(current)} / {Mathf.CeilToInt(max)}";
            }
        }

        /// <summary>
        /// 부대 멤버 수가 변경될 때 텍스트를 갱신합니다.
        /// </summary>
        private void OnSquadCountChanged(int current, int max)
        {
            if (squadCountText != null)
            {
                squadCountText.text = $"{current} / {max}";
            }
        }

        #endregion

        #region 팝업 제어

        /// <summary>
        /// 메뉴 팝업을 엽니다.
        /// </summary>
        public void OpenMenu() => OpenPopup(menuPanel);

        /// <summary>
        /// 메뉴 팝업을 닫습니다.
        /// </summary>
        public void CloseMenu() => ClosePopup(menuPanel);

        /// <summary>
        /// 도감 팝업을 엽니다.
        /// </summary>
        public void OpenCollection() => OpenPopup(collectionPanel);

        /// <summary>
        /// 도감 팝업을 닫습니다.
        /// </summary>
        public void CloseCollection() => ClosePopup(collectionPanel);

        /// <summary>
        /// 전체맵 팝업을 열고 카메라를 전체맵 모드로 전환합니다.
        /// </summary>
        public void OpenFullMap()
        {
            OpenPopup(fullmapPanel);
            MinimapCamera.Instance?.SetFullMapMode(true);
        }

        /// <summary>
        /// 게임오버 팝업을 엽니다. TimeScale이 0으로 설정됩니다.
        /// </summary>
        public void OpenGameOver() => OpenPopup(gameOverPanel);

        /// <summary>
        /// 전체맵 팝업을 닫고 카메라를 미니맵 모드로 복귀시킵니다.
        /// </summary>
        public void CloseFullMap()
        {
            ClosePopup(fullmapPanel);
            MinimapCamera.Instance?.SetFullMapMode(false);
        }

        /// <summary>
        /// 열려 있는 모든 팝업을 즉시 닫고 TimeScale·오버레이를 초기화합니다.
        /// Start 호출 등 애니메이션 없이 초기화할 때 사용합니다.
        /// </summary>
        public void CloseAllPopups()
        {
            SetPanelActive(menuPanel, false);
            SetPanelActive(collectionPanel, false);
            SetPanelActive(fullmapPanel, false);
            SetPanelActive(gameOverPanel, false);
            _openPanels.Clear();
            UpdateTimeScale();
            UpdateOverlay();
        }

        /// <summary>
        /// 지정 패널을 열고 scale 0→1 등장 애니메이션을 재생합니다.
        /// </summary>
        private void OpenPopup(GameObject panel)
        {
            if (panel == null)
            {
                return;
            }

            panel.SetActive(true);
            _openPanels.Add(panel);
            UpdateTimeScale();
            UpdateOverlay();

            // scale 0 → 1 등장 애니메이션 (TimeScale=0에서도 동작하도록 SetUpdate)
            RectTransform rt = panel.GetComponent<RectTransform>();
            if (rt != null)
            {
                rt.DOKill();
                rt.localScale = Vector3.zero;
                rt.DOScale(Vector3.one, popupOpenDuration)
                    .SetEase(popupOpenEase)
                    .SetUpdate(true);
            }
        }

        /// <summary>
        /// 지정 패널을 닫고 팝업 목록에서 제거합니다.
        /// </summary>
        private void ClosePopup(GameObject panel)
        {
            if (panel == null)
            {
                return;
            }

            // 진행 중인 등장 애니메이션이 있으면 즉시 종료
            RectTransform rt = panel.GetComponent<RectTransform>();
            rt?.DOKill();

            panel.SetActive(false);
            _openPanels.Remove(panel);
            UpdateTimeScale();
            UpdateOverlay();
        }

        private void SetPanelActive(GameObject panel, bool active)
        {
            if (panel != null)
            {
                panel.SetActive(active);
            }
        }

        /// <summary>
        /// 열린 팝업이 하나라도 있으면 TimeScale을 0으로, 없으면 1로 설정합니다.
        /// </summary>
        private void UpdateTimeScale()
        {
            Time.timeScale = _openPanels.Count > 0 ? 0f : 1f;
        }

        /// <summary>
        /// 열린 팝업이 하나라도 있으면 오버레이를 켜고, 없으면 끕니다.
        /// </summary>
        private void UpdateOverlay()
        {
            if (overlayPanel != null)
            {
                overlayPanel.SetActive(_openPanels.Count > 0);
            }
        }

        #endregion

        #region 메뉴 동작

        /// <summary>
        /// 세이브 데이터를 삭제하고 현재 씬을 처음부터 다시 시작합니다.
        /// </summary>
        private void OnRestartClicked()
        {
            SaveManager.Instance?.DeleteSaveData();
            Time.timeScale = 1f;
            SceneManager.LoadScene(SceneManager.GetActiveScene().name);
        }

        /// <summary>
        /// 저장된 데이터를 불러와 게임을 복원합니다.
        /// </summary>
        private void OnLoadClicked()
        {
            CloseAllPopups();
            GameManager.Instance?.LoadAndApply();
        }

        /// <summary>
        /// 게임오버 패널에서 재시작 버튼을 눌렀을 때 호출됩니다.
        /// 세이브 데이터를 삭제하고 현재 씬을 처음부터 다시 시작합니다.
        /// </summary>
        private void OnGameOverRestartClicked()
        {
            SaveManager.Instance?.DeleteSaveData();
            Time.timeScale = 1f;
            SceneManager.LoadScene(SceneManager.GetActiveScene().name);
        }

        /// <summary>
        /// 게임을 종료합니다.
        /// </summary>
        private void OnQuitClicked()
        {
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }

        #endregion
    }
}
