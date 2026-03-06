using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace WildTamer
{
    /// <summary>
    /// 스쿼드 멤버 1기의 저장 데이터입니다.
    /// </summary>
    [Serializable]
    public class MemberSaveData
    {
        public string monsterDataName;
        public float  currentHp;
    }

    /// <summary>
    /// 스쿼드 1기의 저장 데이터입니다. 위치와 멤버 목록을 포함합니다.
    /// </summary>
    [Serializable]
    public class SquadSaveData
    {
        public Vector3              position;
        public List<MemberSaveData> members = new List<MemberSaveData>();
    }

    /// <summary>
    /// 게임 전체 저장 데이터입니다.
    /// JsonUtility로 직렬화하여 persistentDataPath에 저장합니다.
    /// </summary>
    [Serializable]
    public class GameData
    {
        public int                 currency;
        public float               playerCurrentHp;
        public float               playerMaxHp;
        public Vector3             playerPosition;
        public SquadSaveData       allySquadData        = new SquadSaveData();
        public List<SquadSaveData> enemySquads          = new List<SquadSaveData>();
        public List<string>        unlockedMonsterNames = new List<string>();
    }

    /// <summary>
    /// 게임 데이터의 직렬화·파일 IO를 전담하는 싱글턴 매니저입니다.
    /// 씬 복원·스폰 로직은 GameManager가 담당하며,
    /// 이 클래스는 순수 데이터 레이어로서 Unity 씬에 종속되지 않습니다.
    /// </summary>
    public class SaveManager : Singleton<SaveManager>
    {
        #region SerializeField 필드

        [Header("자동 저장 설정")]
        [SerializeField, Tooltip("자동 저장 주기 (초) — GameManager가 이 값으로 저장 코루틴 간격을 설정합니다")]
        private float saveInterval = 30f;

        [Header("데이터 레지스트리")]
        [SerializeField, Tooltip("이름으로 MonsterData를 조회하기 위한 등록 목록 — 저장 복원 시 사용됩니다")]
        private List<MonsterData> monsterDataRegistry = new List<MonsterData>();

        #endregion

        #region Private 필드

        private const string SaveFileName = "gamedata.json";

        #endregion

        #region Public 프로퍼티

        /// <summary>자동 저장 주기 (초) — GameManager 자동저장 코루틴에서 참조합니다</summary>
        public float SaveInterval => saveInterval;

        #endregion

        #region 저장 / 불러오기

        /// <summary>
        /// GameData를 JSON 파일로 저장합니다.
        /// </summary>
        /// <param name="data">저장할 게임 데이터</param>
        public void SaveGame(GameData data)
        {
            string json = JsonUtility.ToJson(data, true);
            string path = Path.Combine(Application.persistentDataPath, SaveFileName);
            File.WriteAllText(path, json);
            Debug.Log($"[SaveManager] 게임 저장 완료: {path}");
        }

        /// <summary>
        /// 저장 파일을 읽어 GameData를 반환합니다.
        /// 파일이 없으면 null을 반환합니다.
        /// </summary>
        public GameData LoadGame()
        {
            string path = Path.Combine(Application.persistentDataPath, SaveFileName);

            if (!File.Exists(path))
            {
                Debug.Log("[SaveManager] 저장 파일이 없습니다.");
                return null;
            }

            string   json = File.ReadAllText(path);
            GameData data = JsonUtility.FromJson<GameData>(json);
            Debug.Log("[SaveManager] 게임 불러오기 완료");
            return data;
        }

        /// <summary>
        /// 저장 파일이 존재하는지 여부를 반환합니다.
        /// </summary>
        public bool HasSaveData()
        {
            string path = Path.Combine(Application.persistentDataPath, SaveFileName);
            return File.Exists(path);
        }

        /// <summary>
        /// 저장 파일을 삭제합니다.
        /// 플레이어 사망 시 호출하여 세이브 데이터를 초기화합니다.
        /// </summary>
        public void DeleteSaveData()
        {
            string path = Path.Combine(Application.persistentDataPath, SaveFileName);

            if (File.Exists(path))
            {
                File.Delete(path);
                Debug.Log("[SaveManager] 저장 파일 삭제 완료");
            }
        }

        #endregion

        #region 데이터 조회

        /// <summary>
        /// monsterDataRegistry에서 이름으로 MonsterData를 검색합니다.
        /// </summary>
        /// <param name="name">검색할 monsterName 값</param>
        public MonsterData FindMonsterDataByName(string name)
        {
            foreach (MonsterData data in monsterDataRegistry)
            {
                if (data != null && data.monsterName == name)
                {
                    return data;
                }
            }

            Debug.LogWarning($"[SaveManager] MonsterData '{name}'를 레지스트리에서 찾을 수 없습니다.");
            return null;
        }

        #endregion
    }
}
