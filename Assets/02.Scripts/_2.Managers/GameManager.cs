using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.InputSystem;

namespace WildTamer
{
    /// <summary>
    /// 테이밍한 개체 1기의 저장 데이터입니다.
    /// </summary>
    [Serializable]
    public class TamedUnitData
    {
        public string monsterName;
        public float  currentHp;
        public float  maxHp;
        public Vector3 position;
    }

    /// <summary>
    /// 게임 전체 저장 데이터입니다.
    /// JsonUtility로 직렬화하여 persistentDataPath에 저장합니다.
    /// </summary>
    [Serializable]
    public class GameData
    {
        public int               currency;
        public float             playerCurrentHp;
        public float             playerMaxHp;
        public Vector3           playerPosition;
        public List<TamedUnitData> tamedUnits = new List<TamedUnitData>();
    }

    /// <summary>
    /// 게임 전체를 총괄하는 매니저 싱글턴입니다.
    /// 씬 시작 시 아군·적군 스쿼드를 스폰하고, 단축키로 적군을 추가 스폰할 수 있습니다.
    ///
    /// 아군 Squad: 씬에 배치된 Squad 오브젝트를 Inspector에서 직접 연결합니다.
    /// 적군 Squad: enemySquadPrefab을 런타임에 Instantiate하여 플레이어 주변에 생성합니다.
    /// </summary>
    public class GameManager : MonoBehaviour
    {
        #region Public 필드 및 프로퍼티

        public static GameManager Instance { get; private set; }

        #endregion

        #region SerializeField 필드

        [Header("게임 데이터")]
        [SerializeField, Tooltip("게임 내 재화")]
        private int currency = 0;

        [Header("아군 스쿼드")]
        [SerializeField, Tooltip("씬에 배치된 아군 Squad 오브젝트 (PlayerController와 동일한 Squad)")]
        private Squad allySquad;

        [SerializeField, Tooltip("아군으로 스폰할 몬스터 데이터")]
        private MonsterData allyMonsterData;

        [SerializeField, Tooltip("스폰할 아군 수")]
        private int allyCount = 3;

        [SerializeField, Tooltip("아군이 스폰되는 반경 (allySquad 중심 기준)")]
        private float allySpawnRadius = 1.5f;

        [Header("적군 스쿼드")]
        [SerializeField, Tooltip("Squad 컴포넌트가 붙어있는 적군 Squad 프리팹")]
        private GameObject enemySquadPrefab;

        [SerializeField, Tooltip("적으로 스폰할 몬스터 데이터")]
        private MonsterData enemyMonsterData;

        [SerializeField, Tooltip("스폰할 적군 수")]
        private int enemyCount = 5;

        [SerializeField, Tooltip("플레이어로부터 적군 스쿼드가 스폰되는 거리")]
        private float enemySpawnDistance = 15f;

        [SerializeField, Tooltip("스쿼드 내 개체들이 퍼지는 반경")]
        private float enemySpawnRadius = 2f;

        [Header("디버그")]
        [SerializeField, Tooltip("적군 스쿼드를 스폰하는 빈도 (초)")]
        private float spawnEnemyFrequency = 10f;

        #endregion

        #region Private 필드

        private const string SaveFileName = "gamedata.json";

        private Transform        _playerTransform;
        private PlayerController _player;
        private GameData         _currentData = new GameData();

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
            PlayerController player = FindAnyObjectByType<PlayerController>();

            if (player != null)
            {
                _player          = player;
                _playerTransform = player.transform;
                _player.OnHpChanged += OnPlayerHpChanged;
            }
            else
            {
                Debug.LogWarning("[GameManager] 씬에서 PlayerController를 찾을 수 없습니다.");
            }

            SpawnAllySquad();
            StartCoroutine(SpawnEnemyRoutine());
        }

        private void OnDestroy()
        {
            if (_player != null)
            {
                _player.OnHpChanged -= OnPlayerHpChanged;
            }
        }

        #endregion

        #region 게임 데이터

        /// <summary>
        /// 플레이어 HP가 변경될 때 호출됩니다.
        /// 변경 즉시 _currentData에 반영합니다.
        /// </summary>
        private void OnPlayerHpChanged(float current, float max)
        {
            _currentData.playerCurrentHp = current;
            _currentData.playerMaxHp     = max;
        }

        /// <summary>
        /// 현재 게임 상태를 _currentData에 스냅샷합니다.
        /// 테이밍 개체 목록을 플레이어 스쿼드에서 수집합니다.
        /// </summary>
        private void CollectCurrentData()
        {
            // TODO: ResourceManager 연동 시 실제 재화 값으로 교체
            _currentData.currency = 0;

            // 플레이어 위치 수집
            if (_playerTransform != null)
            {
                _currentData.playerPosition = _playerTransform.position;
            }

            _currentData.tamedUnits.Clear();

            Squad playerSquad = Squad.GetPlayerSquad();

            if (playerSquad == null)
            {
                return;
            }

            foreach (Monster member in playerSquad.GetMembers())
            {
                if (member == null || member.Data == null)
                {
                    continue;
                }

                _currentData.tamedUnits.Add(new TamedUnitData
                {
                    monsterName = member.Data.monsterName,
                    currentHp   = member.CurrentHp,
                    maxHp       = member.Data.stat.maxHp,
                    position    = member.Transform.position
                });
            }
        }

        /// <summary>
        /// 현재 게임 데이터를 JSON 파일로 저장합니다.
        /// </summary>
        public void SaveGame()
        {
            CollectCurrentData();

            string json = JsonUtility.ToJson(_currentData, true);
            string path = Path.Combine(Application.persistentDataPath, SaveFileName);

            File.WriteAllText(path, json);
            Debug.Log($"[GameManager] 게임 저장 완료: {path}");
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
                Debug.Log("[GameManager] 저장 파일이 없습니다.");
                return null;
            }

            string json = File.ReadAllText(path);
            GameData data = JsonUtility.FromJson<GameData>(json);
            Debug.Log("[GameManager] 게임 불러오기 완료");
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

        #endregion

        #region 스쿼드 스폰

        private IEnumerator SpawnEnemyRoutine()
        {
            while (true)
            {
                yield return new WaitForSeconds(spawnEnemyFrequency);
                SpawnEnemySquad();
            }
        }

        /// <summary>
        /// 씬에 배치된 allySquad에 아군 몬스터들을 스폰하여 멤버로 등록합니다.
        /// </summary>
        private void SpawnAllySquad()
        {
            if (allySquad == null)
            {
                Debug.LogWarning("[GameManager] allySquad가 설정되지 않았습니다.");
                return;
            }

            if (allyMonsterData == null)
            {
                Debug.LogWarning("[GameManager] allyMonsterData가 설정되지 않았습니다.");
                return;
            }

            SpawnMembersIntoSquad(
                squad: allySquad,
                data: allyMonsterData,
                isEnemy: false,
                count: allyCount,
                center: allySquad.transform.position,
                radius: allySpawnRadius
            );
        }

        /// <summary>
        /// enemySquadPrefab을 Instantiate하고 플레이어 주변 임의 방향에 적군 몬스터들을 스폰합니다.
        /// </summary>
        private void SpawnEnemySquad()
        {
            if (enemySquadPrefab == null)
            {
                Debug.LogWarning("[GameManager] enemySquadPrefab이 설정되지 않았습니다.");
                return;
            }

            if (enemyMonsterData == null)
            {
                Debug.LogWarning("[GameManager] enemyMonsterData가 설정되지 않았습니다.");
                return;
            }

            Vector3 center = GetEnemySpawnCenter();

            GameObject squadObj = Instantiate(enemySquadPrefab, center, Quaternion.identity);
            Squad enemySquad = squadObj.GetComponent<Squad>();

            if (enemySquad == null)
            {
                Debug.LogError("[GameManager] enemySquadPrefab에 Squad 컴포넌트가 없습니다.");
                Destroy(squadObj);
                return;
            }

            enemySquad.OnEmpty += OnEnemySquadEmpty;

            SpawnMembersIntoSquad(
                squad: enemySquad,
                data: enemyMonsterData,
                isEnemy: true,
                count: enemyCount,
                center: center,
                radius: enemySpawnRadius
            );
        }

        /// <summary>
        /// 적 스쿼드 멤버가 전부 사라졌을 때 호출됩니다.
        /// 스쿼드 오브젝트를 파괴하여 AllSquads에서 제거합니다.
        /// </summary>
        private void OnEnemySquadEmpty(Squad squad)
        {
            squad.OnEmpty -= OnEnemySquadEmpty;
            Destroy(squad.gameObject);
        }

        /// <summary>
        /// 플레이어 주변 임의 방향으로 enemySpawnDistance만큼 떨어진 위치를 반환합니다.
        /// 플레이어를 찾을 수 없으면 원점을 반환합니다.
        /// </summary>
        private Vector3 GetEnemySpawnCenter()
        {
            if (_playerTransform == null)
            {
                return Vector3.zero;
            }

            Vector2 dir = UnityEngine.Random.insideUnitCircle.normalized;
            return _playerTransform.position + new Vector3(dir.x, dir.y, 0f) * enemySpawnDistance;
        }

        /// <summary>
        /// 지정된 Squad에 몬스터들을 스폰하고 멤버로 등록합니다.
        /// 각 몬스터는 center 주변의 원 안에 랜덤 배치됩니다.
        /// </summary>
        private void SpawnMembersIntoSquad(Squad squad, MonsterData data, bool isEnemy, int count, Vector3 center, float radius)
        {
            for (int i = 0; i < count; i++)
            {
                GameObject obj = isEnemy
                    ? PoolManager.Instance.GetEnemy(data)
                    : PoolManager.Instance.GetAlly(data);

                if (obj == null)
                {
                    continue;
                }

                Vector2 offset = UnityEngine.Random.insideUnitCircle * radius;
                obj.transform.position = center + new Vector3(offset.x, offset.y, 0f);

                Monster monster = obj.GetComponent<Monster>();

                if (monster == null)
                {
                    Debug.LogWarning($"[GameManager] 스폰된 오브젝트 '{obj.name}'에 Monster 컴포넌트가 없습니다.");
                    continue;
                }

                monster.SetSquad(squad);
                squad.AddMember(monster);
            }
        }

        #endregion
    }
}
