using UnityEngine;

namespace WildTamer
{
    /// <summary>
    /// 적 군중(Enemy Squad) 하나의 생성·이동·구성을 제어하는 ScriptableObject
    /// 개체 AI 스탯은 MonsterData에서 관리하고,
    /// 군중 단위의 거시적 행동 파라미터만 이 데이터에서 정의합니다.
    /// </summary>
    [CreateAssetMenu(fileName = "EnemySquadData", menuName = "WildTamer/Data/EnemySquadData")]
    public class EnemySquadData : ScriptableObject
    {
        #region SerializeField 필드
        /* 일단 모든 몬스터는 하나의 군중 데이터 사용

        [Header("기본 정보")]
        [SerializeField, Tooltip("군중 이름 (식별용)")]
        public string squadName;

        [SerializeField, Tooltip("이 군중을 구성하는 몬스터 종류")]
        public MonsterData monsterData;

        */
        [Header("스폰 / 디스폰")]
        [SerializeField, Tooltip("플레이어로부터 이 거리 이내에 들어오면 군중 스폰")]
        public float spawnDistance = 20f;

        [SerializeField, Tooltip("플레이어로부터 이 거리 이상 멀어지면 디스폰 대기 시작 (이동 중에만 적용)")]
        public float despawnDistance = 30f;

        [SerializeField, Tooltip("디스폰 거리 초과 후 실제 디스폰까지 유예 시간 (초)")]
        public float despawnDelay = 5f;

        [Header("군중 구성")]
        [SerializeField, Tooltip("군중을 구성하는 개체 최소 수")]
        public int minUnitCount = 3;

        [SerializeField, Tooltip("군중을 구성하는 개체 최대 수")]
        public int maxUnitCount = 6;

        [SerializeField, Tooltip("스쿼드 중심 기준 개체들이 퍼지는 반경 (m)")]
        public float spawnRadius = 2f;

        #endregion

        #region 유효성 검증

#if UNITY_EDITOR
        private void OnValidate()
        {
            if (minUnitCount < 1)
            {
                minUnitCount = 1;
            }

            if (maxUnitCount < minUnitCount)
            {
                maxUnitCount = minUnitCount;
            }

            if (spawnRadius < 0f)
            {
                spawnRadius = 0f;
            }

            if (despawnDistance <= spawnDistance)
            {
                despawnDistance = spawnDistance + 5f;
            }
        }
#endif

        #endregion
    }
}
