using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Pool;

namespace WildTamer
{
    /// <summary>
    /// Inspector에서 사전 등록할 풀 항목입니다.
    /// </summary>
    [Serializable]
    public struct PoolEntry
    {
        [Tooltip("풀링할 프리팹")]
        public GameObject prefab;

        [Tooltip("초기 생성 수")]
        public int defaultCapacity;

        [Tooltip("풀 최대 크기")]
        public int maxSize;
    }

    /// <summary>
    /// 오브젝트 풀링을 총괄하는 매니저 싱글턴입니다.
    /// 적·아군 개체를 포함한 모든 동적 오브젝트는 이 매니저를 통해 생성·반환합니다.
    /// </summary>
    public class PoolManager : Singleton<PoolManager>
    {
        #region SerializeField 필드

        [Header("사전 등록 풀 목록")]
        [SerializeField, Tooltip("씬 시작 시 미리 생성할 풀 항목 목록")]
        private List<PoolEntry> poolEntries = new List<PoolEntry>();

        #endregion

        #region Private 필드

        private readonly Dictionary<GameObject, ObjectPool<GameObject>> _pools = new();

        // 프리팹별 계층 컨테이너 — 하이어라키에서 "[Pool] 프리팹명" 으로 묶임
        private readonly Dictionary<GameObject, Transform> _containers = new();

        #endregion

        #region Unity 메소드

        protected override void Awake()
        {
            base.Awake();
            DontDestroyOnLoad(gameObject);
            InitializePools();
        }

        #endregion

        #region 초기화

        /// <summary>
        /// Inspector에 등록된 풀 항목을 미리 생성하고 워밍업합니다.
        /// Unity ObjectPool의 defaultCapacity는 내부 컬렉션 용량 힌트일 뿐
        /// 실제 오브젝트를 사전 생성하지 않으므로, Get→Release 반복으로 직접 워밍업합니다.
        /// </summary>
        private void InitializePools()
        {
            foreach (PoolEntry entry in poolEntries)
            {
                if (entry.prefab == null)
                {
                    continue;
                }

                ObjectPool<GameObject> pool = CreatePool(entry.prefab, entry.defaultCapacity, entry.maxSize);
                WarmUpPool(pool, entry.defaultCapacity);
            }
        }

        /// <summary>
        /// 풀에서 count만큼 오브젝트를 꺼냈다가 즉시 반환하여 사전 생성합니다.
        /// </summary>
        /// <param name="pool">워밍업할 풀</param>
        /// <param name="count">사전 생성할 오브젝트 수</param>
        private void WarmUpPool(ObjectPool<GameObject> pool, int count)
        {
            if (count <= 0)
            {
                return;
            }

            GameObject[] buffer = new GameObject[count];

            for (int i = 0; i < count; i++)
            {
                buffer[i] = pool.Get();
            }

            for (int i = 0; i < count; i++)
            {
                pool.Release(buffer[i]);
            }
        }

        /// <summary>
        /// 프리팹에 대한 ObjectPool을 생성하고 등록합니다.
        /// 이미 등록된 경우 기존 풀을 반환합니다.
        /// </summary>
        /// <param name="prefab">풀링할 프리팹</param>
        /// <param name="defaultCapacity">초기 생성 수</param>
        /// <param name="maxSize">풀 최대 크기</param>
        private ObjectPool<GameObject> CreatePool(GameObject prefab, int defaultCapacity = 10, int maxSize = 100)
        {
            if (_pools.TryGetValue(prefab, out ObjectPool<GameObject> existingPool))
            {
                return existingPool;
            }

            // 프리팹별 부모 컨테이너 생성 — 하이어라키에서 "[Pool] 이름" 으로 묶임
            GameObject containerObj = new GameObject($"[Pool] {prefab.name}");
            containerObj.transform.SetParent(transform);
            Transform container = containerObj.transform;
            _containers[prefab] = container;

            GameObject capturedPrefab = prefab;
            ObjectPool<GameObject> pool = new ObjectPool<GameObject>(
                createFunc: () => Instantiate(capturedPrefab, container),
                actionOnGet: obj => obj.SetActive(true),
                actionOnRelease: obj => obj.SetActive(false),
                actionOnDestroy: obj => Destroy(obj),
                defaultCapacity: defaultCapacity,
                maxSize: maxSize
            );

            _pools[prefab] = pool;
            return pool;
        }

        #endregion

        #region 풀 Get / Release

        /// <summary>
        /// 해당 프리팹의 풀에서 오브젝트를 꺼냅니다.
        /// 등록되지 않은 프리팹이면 풀을 자동 생성합니다.
        /// </summary>
        /// <param name="prefab">꺼낼 프리팹</param>
        public GameObject Get(GameObject prefab)
        {
            if (!_pools.TryGetValue(prefab, out ObjectPool<GameObject> pool))
            {
                pool = CreatePool(prefab);
            }

            return pool.Get();
        }

        /// <summary>
        /// 오브젝트를 해당 프리팹의 풀로 반환합니다.
        /// 풀이 존재하지 않으면 즉시 파괴합니다.
        /// </summary>
        /// <param name="prefab">원본 프리팹</param>
        /// <param name="instance">반환할 인스턴스</param>
        public void Release(GameObject prefab, GameObject instance)
        {
            if (!_pools.TryGetValue(prefab, out ObjectPool<GameObject> pool))
            {
                Destroy(instance);
                return;
            }

            pool.Release(instance);
        }

        #endregion

        #region MonsterData 편의 메서드

        /// <summary>
        /// MonsterData를 기반으로 적 상태 오브젝트를 풀에서 꺼냅니다.
        /// </summary>
        public GameObject GetEnemy(MonsterData data)
        {
            if (data.enemyPrefab == null)
            {
                Debug.LogError($"[PoolManager] {data.monsterName}의 enemyPrefab이 설정되지 않았습니다.");
                return null;
            }

            return Get(data.enemyPrefab);
        }

        /// <summary>
        /// MonsterData를 기반으로 아군(테이밍된) 상태 오브젝트를 풀에서 꺼냅니다.
        /// </summary>
        public GameObject GetAlly(MonsterData data)
        {
            if (data.allyPrefab == null)
            {
                Debug.LogError($"[PoolManager] {data.monsterName}의 allyPrefab이 설정되지 않았습니다.");
                return null;
            }

            return Get(data.allyPrefab);
        }

        /// <summary>
        /// 적 상태 인스턴스를 풀로 반환합니다.
        /// </summary>
        public void ReleaseEnemy(MonsterData data, GameObject instance)
        {
            if (data.enemyPrefab == null)
            {
                Destroy(instance);
                return;
            }

            Release(data.enemyPrefab, instance);
        }

        /// <summary>
        /// 아군(테이밍된) 상태 인스턴스를 풀로 반환합니다.
        /// </summary>
        public void ReleaseAlly(MonsterData data, GameObject instance)
        {
            if (data.allyPrefab == null)
            {
                Destroy(instance);
                return;
            }

            Release(data.allyPrefab, instance);
        }

        #endregion
    }
}
