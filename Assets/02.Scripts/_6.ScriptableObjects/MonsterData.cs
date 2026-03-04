using System;
using UnityEngine;

namespace WildTamer
{
    /// <summary>
    /// 유닛(적·아군 공통) 스탯 데이터입니다.
    /// </summary>
    [Serializable]
    public struct UnitStat
    {
        [Tooltip("최대 체력")]
        public float maxHp;

        [Tooltip("공격력")]
        public float attackDamage;

        [Tooltip("이동 속도")]
        public float moveSpeed;

        [Tooltip("공격 쿨타임 (초)")]
        public float attackCooldown;

        [Tooltip("공격 사정거리")]
        public float attackRange;
    }

    /// <summary>
    /// 몬스터의 기본 정보, 스탯, 프리팹, 타입별 설정을 정의하는 ScriptableObject입니다.
    /// </summary>
    [CreateAssetMenu(fileName = "MonsterData", menuName = "WildTamer/Data/MonsterData")]
    public class MonsterData : ScriptableObject
    {
        #region SerializeField 필드

        [Header("기본 정보")]
        [SerializeField, Tooltip("몬스터 이름")]
        public string monsterName;

        [Header("스탯")]
        [SerializeField, Tooltip("유닛 스탯 (적·아군 공통)")]
        public UnitStat stat;

        [SerializeField, Tooltip("기절 확률 (0~1) — 아군에게 HP 0으로 처치될 때 적용")]
        [Range(0f, 1f)]
        public float stunChance = 0.3f;

        [Header("물리 설정")]
        [SerializeField, Tooltip("Rigidbody2D 무게 — 클수록 다른 유닛에게 덜 밀림")]
        public float mass = 1f;

        [Header("프리팹 참조")]
        [SerializeField, Tooltip("적 상태일 때 사용할 프리팹")]
        public GameObject enemyPrefab;

        [SerializeField, Tooltip("아군 상태(테이밍 후)일 때 사용할 프리팹")]
        public GameObject allyPrefab;

        #endregion
    }
}
