using System.Collections.Generic;
using UnityEngine;

namespace WildTamer
{
    /// <summary>
    /// 몬스터 도감 패널 전체를 관리하는 UI 컨트롤러입니다.
    /// CollectionManager의 OnCollectionUpdated 이벤트를 구독하여
    /// 새 항목이 해금될 때 자동으로 UI를 갱신합니다.
    /// </summary>
    public class CollectionUI : MonoBehaviour
    {
        #region SerializeField 필드

        [SerializeField, Tooltip("항목 UI 프리팹 — CollectionEntryUI 컴포넌트 포함")]
        private GameObject entryPrefab;

        [SerializeField, Tooltip("항목들이 배치될 컨테이너 — GridLayoutGroup 또는 VerticalLayoutGroup 권장")]
        private Transform entryContainer;

        #endregion

        #region Private 필드

        private readonly List<CollectionEntryUI> _entryUIs = new List<CollectionEntryUI>();

        #endregion

        #region Unity 메소드

        private void Start()
        {
            if (CollectionManager.Instance == null)
            {
                Debug.LogWarning("[CollectionUI] CollectionManager가 없습니다.");
                return;
            }

            InitEntries();
            CollectionManager.Instance.OnCollectionUpdated += RefreshAll;
        }

        private void OnDestroy()
        {
            if (CollectionManager.Instance != null)
            {
                CollectionManager.Instance.OnCollectionUpdated -= RefreshAll;
            }
        }

        #endregion

        #region 초기화

        /// <summary>
        /// CollectionManager의 항목 수에 맞춰 UI 항목을 생성합니다.
        /// </summary>
        private void InitEntries()
        {
            IReadOnlyList<CollectionEntry> entries = CollectionManager.Instance.GetAllEntries();

            foreach (CollectionEntry entry in entries)
            {
                GameObject obj = Instantiate(entryPrefab, entryContainer);
                CollectionEntryUI ui = obj.GetComponent<CollectionEntryUI>();

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
            IReadOnlyList<CollectionEntry> entries = CollectionManager.Instance.GetAllEntries();

            for (int i = 0; i < _entryUIs.Count && i < entries.Count; i++)
            {
                bool unlocked = CollectionManager.Instance.IsUnlocked(entries[i].monsterData.monsterName);
                _entryUIs[i].Refresh(entries[i], unlocked);
            }
        }

        #endregion
    }
}
