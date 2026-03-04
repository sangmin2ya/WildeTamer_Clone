using UnityEngine;

namespace WildTamer
{
    /// <summary>
    /// 게임 전체를 총괄하는 매니저 싱글턴입니다.
    /// 씬 시작 시 아군 스쿼드와 적군 스쿼드의 몬스터를 스폰하고
    /// 각 Squad에 멤버로 등록합니다.
    ///
    /// 아군 Squad: 씬에 배치된 Squad 오브젝트를 Inspector에서 직접 연결합니다.
    /// 적군 Squad: enemySquadPrefab을 런타임에 Instantiate하여 지정 위치에 생성합니다.
    /// </summary>
    public class GameManager : MonoBehaviour
    {
        #region Public 필드 및 프로퍼티

        public static GameManager Instance { get; private set; }

        #endregion

        #region SerializeField 필드

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

        [SerializeField, Tooltip("적군 스쿼드가 생성될 위치")]
        private Transform enemySpawnCenter;

        [SerializeField, Tooltip("적군이 스폰되는 반경 (enemySpawnCenter 기준)")]
        private float enemySpawnRadius = 2f;

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
            SpawnAllySquad();
            SpawnEnemySquad();
        }

        #endregion

        #region 스쿼드 스폰

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
        /// enemySquadPrefab을 Instantiate하고 지정 위치에 적군 몬스터들을 스폰합니다.
        /// 적군 Squad는 기본 정지 상태(SquadState.정지)로 시작하며,
        /// 플레이어 Squad가 탐지 반경 내에 들어오면 자동으로 전투 상태로 전환됩니다.
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

            Vector3 center = enemySpawnCenter != null ? enemySpawnCenter.position : Vector3.zero;

            GameObject squadObj = Instantiate(enemySquadPrefab, center, Quaternion.identity);
            Squad enemySquad = squadObj.GetComponent<Squad>();

            if (enemySquad == null)
            {
                Debug.LogError("[GameManager] enemySquadPrefab에 Squad 컴포넌트가 없습니다.");
                Destroy(squadObj);
                return;
            }

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
        /// 지정된 Squad에 몬스터들을 스폰하고 멤버로 등록합니다.
        /// 각 몬스터는 center 주변의 원 안에 랜덤 배치됩니다.
        /// </summary>
        /// <param name="squad">멤버를 추가할 스쿼드</param>
        /// <param name="data">스폰할 몬스터 데이터</param>
        /// <param name="isEnemy">적 프리팹 사용 여부 (false면 아군 프리팹 사용)</param>
        /// <param name="count">스폰할 수</param>
        /// <param name="center">스폰 중심 위치</param>
        /// <param name="radius">스폰 반경</param>
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

                Vector2 offset = Random.insideUnitCircle * radius;
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
