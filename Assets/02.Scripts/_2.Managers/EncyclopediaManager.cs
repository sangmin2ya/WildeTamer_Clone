using System;
using System.Collections.Generic;
using UnityEngine;

namespace WildTamer
{
    /// <summary>
    /// 도감 항목 1개의 정의 데이터입니다.
    /// EncyclopediaManager Inspector에서 5~6종을 등록합니다.
    /// </summary>
    [Serializable]
    public class EncyclopediaEntry
    {
        [Tooltip("몬스터 스탯·이름 참조용 데이터")]
        public MonsterData monsterData;

        [Tooltip("해금 시 표시할 몬스터 초상화 스프라이트")]
        public Sprite portrait;
    }

    /// <summary>
    /// 몬스터 도감의 해금 상태를 관리하는 싱글턴 매니저입니다.
    /// 몬스터를 처치하거나 테이밍하면 Unlock()을 호출하여 항목을 해금합니다.
    /// </summary>
    public class EncyclopediaManager : MonoBehaviour
    {
        #region Public 프로퍼티

        public static EncyclopediaManager Instance { get; private set; }

        /// <summary>해금되지 않은 항목에 표시할 잠금 스프라이트</summary>
        public Sprite LockedSprite => lockedSprite;

        /// <summary>새 항목이 해금되거나 저장 데이터에서 복원되면 발행됩니다.</summary>
        public event Action OnEncyclopediaUpdated;

        #endregion

        #region SerializeField 필드

        [Header("도감 항목")]
        [SerializeField, Tooltip("도감에 등록할 몬스터 목록 — 표시 순서대로 배치")]
        private List<EncyclopediaEntry> entries = new List<EncyclopediaEntry>();

        [Header("잠금 스프라이트")]
        [SerializeField, Tooltip("해금되지 않은 항목에 표시할 잠금 스프라이트")]
        private Sprite lockedSprite;

        #endregion

        #region Private 필드

        private readonly HashSet<string> _unlockedNames = new HashSet<string>();

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

        #endregion

        #region 해금 관리

        /// <summary>
        /// 지정한 몬스터 이름의 도감 항목을 해금합니다.
        /// 이미 해금된 경우 이벤트를 발행하지 않고 무시합니다.
        /// </summary>
        /// <param name="monsterName">MonsterData.monsterName 값</param>
        public void Unlock(string monsterName)
        {
            if (_unlockedNames.Add(monsterName))
            {
                OnEncyclopediaUpdated?.Invoke();
            }
        }

        /// <summary>
        /// 지정한 몬스터 이름이 해금되어 있는지 반환합니다.
        /// </summary>
        public bool IsUnlocked(string monsterName)
        {
            return _unlockedNames.Contains(monsterName);
        }

        /// <summary>
        /// 등록된 전체 도감 항목 목록을 반환합니다.
        /// </summary>
        public IReadOnlyList<EncyclopediaEntry> GetAllEntries()
        {
            return entries;
        }

        #endregion

        #region 저장 데이터 연동

        /// <summary>
        /// 현재 해금 목록을 저장용 리스트로 반환합니다.
        /// </summary>
        public List<string> GetUnlockedList()
        {
            return new List<string>(_unlockedNames);
        }

        /// <summary>
        /// 저장 데이터에서 해금 목록을 복원합니다.
        /// </summary>
        /// <param name="names">저장된 해금 몬스터 이름 목록</param>
        public void RestoreFromList(List<string> names)
        {
            _unlockedNames.Clear();

            foreach (string name in names)
            {
                _unlockedNames.Add(name);
            }

            OnEncyclopediaUpdated?.Invoke();
        }

        #endregion
    }
}
