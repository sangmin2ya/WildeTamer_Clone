using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Tilemaps;

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

        [SerializeField, Tooltip("적군 스쿼드 스폰·디스폰 설정 데이터")]
        private EnemySquadData enemySquadData;

        [Header("디버그")]
        [SerializeField, Tooltip("적군 스쿼드를 스폰하는 빈도 (초)")]
        private float spawnEnemyFrequency = 10f;

        [Header("디스폰 설정")]
        [SerializeField, Tooltip("디스폰 거리 체크 주기 (초) — 낮을수록 정밀하나 CPU 비용 증가")]
        private float despawnCheckInterval = 1f;

        [Header("맵 설정")]
        [SerializeField, Tooltip("땅 타일맵 — 적·아군이 이 지역 위에만 배치됩니다. 미설정 시 위치 제한 없음")]
        private Tilemap groundGrid;

        [Header("시작 지점")]
        [SerializeField, Tooltip("플레이어 시작 위치 — 씬 시작 시 플레이어를 이 위치로 이동시킵니다. 미설정 시 현재 위치 유지")]
        private Transform playerSpawnPoint;

        [SerializeField, Tooltip("아군 스쿼드 시작 위치 — 미설정 시 allySquad 오브젝트의 현재 위치를 사용합니다")]
        private Transform allySquadSpawnPoint;

        #endregion

        #region Private 필드

        private const string SaveFileName = "gamedata.json";

        private Transform        _playerTransform;
        private PlayerController _player;
        private GameData         _currentData = new GameData();

        private readonly List<Squad> _activeEnemySquads = new List<Squad>();
        private WaitForSeconds       _despawnCheckWait;

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
            _despawnCheckWait = new WaitForSeconds(despawnCheckInterval);
        }

        private void Start()
        {
            PlayerController player = FindAnyObjectByType<PlayerController>();

            if (player != null)
            {
                _player          = player;
                _playerTransform = player.transform;
                _player.OnHpChanged += OnPlayerHpChanged;

                if (playerSpawnPoint != null)
                {
                    _playerTransform.position = playerSpawnPoint.position;
                }
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

            Vector3 allyCenter = allySquadSpawnPoint != null
                ? allySquadSpawnPoint.position
                : allySquad.transform.position;

            if (allySquadSpawnPoint != null)
            {
                allySquad.transform.position = allyCenter;
            }

            SpawnMembersIntoSquad(
                squad: allySquad,
                data: allyMonsterData,
                isEnemy: false,
                count: allyCount,
                center: allyCenter,
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
            _activeEnemySquads.Add(enemySquad);
            StartCoroutine(DespawnWatchRoutine(enemySquad));

            int   count  = enemySquadData != null
                ? UnityEngine.Random.Range(enemySquadData.minUnitCount, enemySquadData.maxUnitCount + 1)
                : 5;
            float radius = enemySquadData != null ? enemySquadData.spawnRadius : 2f;

            SpawnMembersIntoSquad(
                squad: enemySquad,
                data: enemyMonsterData,
                isEnemy: true,
                count: count,
                center: center,
                radius: radius
            );
        }

        /// <summary>
        /// 적 스쿼드 멤버가 전부 사라졌을 때 호출됩니다.
        /// 스쿼드 오브젝트를 파괴하여 AllSquads에서 제거합니다.
        /// </summary>
        private void OnEnemySquadEmpty(Squad squad)
        {
            squad.OnEmpty -= OnEnemySquadEmpty;
            _activeEnemySquads.Remove(squad);
            Destroy(squad.gameObject);
        }

        /// <summary>
        /// 스폰된 적 스쿼드를 주기적으로 감시하여 거리·상태 조건이 충족되면 디스폰합니다.
        /// 전투 중인 스쿼드는 체크를 건너뛰며, 범위를 벗어나도 즉시 디스폰하지 않고
        /// despawnDelay 동안 조건이 유지된 경우에만 디스폰합니다.
        /// </summary>
        private IEnumerator DespawnWatchRoutine(Squad squad)
        {
            float outOfRangeTime = -1f;

            while (squad != null && squad.gameObject != null)
            {
                yield return _despawnCheckWait;

                if (squad == null || _playerTransform == null)
                {
                    yield break;
                }

                // 전투 중인 스쿼드는 디스폰 체크 건너뜀 — 전투 종료 후 타이머도 리셋
                if (squad.CurrentState == SquadState.전투)
                {
                    outOfRangeTime = -1f;
                    continue;
                }

                float despawnDist  = enemySquadData != null ? enemySquadData.despawnDistance : 30f;
                float delay        = enemySquadData != null ? enemySquadData.despawnDelay    : 5f;
                float distSqr      = ((Vector2)squad.transform.position - (Vector2)_playerTransform.position).sqrMagnitude;
                float thresholdSqr = despawnDist * despawnDist;

                if (distSqr >= thresholdSqr)
                {
                    if (outOfRangeTime < 0f)
                    {
                        // 범위 초과 시작 — 타이머 시작
                        outOfRangeTime = Time.time;
                    }
                    else if (Time.time - outOfRangeTime >= delay)
                    {
                        // 유예 시간 초과 — 디스폰
                        DespawnEnemySquad(squad);
                        yield break;
                    }
                }
                else
                {
                    // 범위 내로 돌아오면 타이머 리셋
                    outOfRangeTime = -1f;
                }
            }
        }

        /// <summary>
        /// 지정된 적 스쿼드의 멤버를 풀에 반환하고 스쿼드 오브젝트를 파괴합니다.
        /// </summary>
        private void DespawnEnemySquad(Squad squad)
        {
            if (squad == null)
            {
                return;
            }

            squad.OnEmpty -= OnEnemySquadEmpty;
            _activeEnemySquads.Remove(squad);

            // 멤버 목록을 복사한 뒤 각각 풀에 반환 (Release 중 목록 변경 방지)
            List<Monster> membersToRelease = new List<Monster>(squad.GetMembers());

            foreach (Monster member in membersToRelease)
            {
                if (member == null || member.Data == null)
                {
                    continue;
                }

                PoolManager.Instance.ReleaseEnemy(member.Data, member.gameObject);
            }

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

            float spawnDist = enemySquadData != null ? enemySquadData.spawnDistance : 15f;

            // groundGrid가 설정된 경우 유효 지형 위에 스폰되도록 최대 10회 시도
            const int maxAttempts = 10;

            for (int i = 0; i < maxAttempts; i++)
            {
                Vector2 dir       = UnityEngine.Random.insideUnitCircle.normalized;
                Vector3 candidate = _playerTransform.position + new Vector3(dir.x, dir.y, 0f) * spawnDist;

                if (IsValidGroundPosition(candidate))
                {
                    return candidate;
                }
            }

            // 유효 위치를 찾지 못한 경우 — 마지막 임의 방향을 그대로 반환
            Vector2 fallback = UnityEngine.Random.insideUnitCircle.normalized;
            return _playerTransform.position + new Vector3(fallback.x, fallback.y, 0f) * spawnDist;
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

        #region 맵 유효성 검사

        /// <summary>
        /// 주어진 월드 좌표가 groundGrid 위에 유효한 타일인지 반환합니다.
        /// groundGrid가 설정되지 않은 경우 항상 true를 반환하여 제한 없이 허용합니다.
        /// </summary>
        /// <param name="worldPos">검사할 월드 좌표</param>
        public bool IsValidGroundPosition(Vector2 worldPos)
        {
            if (groundGrid == null)
            {
                return true;
            }

            Vector3Int cellPos = groundGrid.WorldToCell(new Vector3(worldPos.x, worldPos.y, 0f));
            return groundGrid.HasTile(cellPos);
        }

        #endregion
    }
}
