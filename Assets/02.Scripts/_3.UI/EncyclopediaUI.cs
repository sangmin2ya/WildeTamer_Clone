using System.Collections.Generic;
using UnityEngine;

namespace WildTamer
{
    /// <summary>
    /// 몬스터 도감 패널 전체를 관리하는 UI 컨트롤러입니다.
    /// EncyclopediaManager의 OnEncyclopediaUpdated 이벤트를 구독하여
    /// 새 항목이 해금될 때 자동으로 UI를 갱신합니다.
    /// </summary>
    public class EncyclopediaUI : MonoBehaviour
    {
        #region SerializeField 필드

        [Header("UI 참조")]
        [SerializeField, Tooltip("도감 패널 루트 오브젝트 — Toggle() 시 활성·비활성됩니다")]
        private GameObject panel;

        [SerializeField, Tooltip("항목 UI 프리팹 — EncyclopediaEntryUI 컴포넌트 포함")]
        private GameObject entryPrefab;

        [SerializeField, Tooltip("항목들이 배치될 컨테이너 — GridLayoutGroup 또는 VerticalLayoutGroup 권장")]
        private Transform entryContainer;

        #endregion

        #region Private 필드

        private readonly List<EncyclopediaEntryUI> _entryUIs = new List<EncyclopediaEntryUI>();

        #endregion

        #region Unity 메소드

        private void Start()
        {
            if (EncyclopediaManager.Instance == null)
            {
                Debug.LogWarning("[EncyclopediaUI] EncyclopediaManager가 없습니다.");
                return;
            }

            InitEntries();
            EncyclopediaManager.Instance.OnEncyclopediaUpdated += RefreshAll;

            // 시작 시 패널 닫힌 상태
            panel.SetActive(false);
        }

        private void OnDestroy()
        {
            if (EncyclopediaManager.Instance != null)
            {
                EncyclopediaManager.Instance.OnEncyclopediaUpdated -= RefreshAll;
            }
        }

        #endregion

        #region 초기화

        /// <summary>
        /// EncyclopediaManager의 항목 수에 맞춰 UI 항목을 생성합니다.
        /// </summary>
        private void InitEntries()
        {
            IReadOnlyList<EncyclopediaEntry> entries = EncyclopediaManager.Instance.GetAllEntries();

            foreach (EncyclopediaEntry entry in entries)
            {
                GameObject obj = Instantiate(entryPrefab, entryContainer);
                EncyclopediaEntryUI ui = obj.GetComponent<EncyclopediaEntryUI>();

                if (ui != null)
                {
                    _entryUIs.Add(ui);
                }
            }

            RefreshAll();
        }

        #endregion

        #region 갱신

        /// <summary>
        /// 모든 항목 UI를 현재 해금 상태로 갱신합니다.
        /// </summary>
        private void RefreshAll()
        {
            IReadOnlyList<EncyclopediaEntry> entries = EncyclopediaManager.Instance.GetAllEntries();

            for (int i = 0; i < _entryUIs.Count && i < entries.Count; i++)
            {
                bool unlocked = EncyclopediaManager.Instance.IsUnlocked(entries[i].monsterData.monsterName);
                _entryUIs[i].Refresh(entries[i], unlocked);
            }
        }

        #endregion

        #region 패널 제어

        /// <summary>
        /// 도감 패널을 열거나 닫습니다. 열 때 UI를 갱신합니다.
        /// </summary>
        public void Toggle()
        {
            bool next = !panel.activeSelf;
            panel.SetActive(next);

            if (next)
            {
                RefreshAll();
            }
        }

        /// <summary>
        /// 도감 패널을 엽니다.
        /// </summary>
        public void Open()
        {
            panel.SetActive(true);
            RefreshAll();
        }

        /// <summary>
        /// 도감 패널을 닫습니다.
        /// </summary>
        public void Close()
        {
            panel.SetActive(false);
        }

        #endregion
    }
}
