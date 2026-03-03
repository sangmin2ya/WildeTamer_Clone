using UnityEngine;

namespace WildTamer
{
    /// <summary>
    /// 플레이어의 기본 스탯을 정의하는 ScriptableObject입니다.
    /// </summary>
    [CreateAssetMenu(fileName = "PlayerData", menuName = "WildTamer/Data/PlayerData")]
    public class PlayerData : ScriptableObject
    {
        #region SerializeField 필드

        [Header("기본 스탯")]
        [SerializeField, Tooltip("플레이어 최대 체력")]
        public float maxHp = 100f;

        [SerializeField, Tooltip("플레이어 공격력")]
        public float attackDamage = 15f;

        [SerializeField, Tooltip("플레이어 이동 속도")]
        public float moveSpeed = 5f;

        [SerializeField, Tooltip("플레이어 공격 사정거리")]
        public float attackRange = 3f;

        [SerializeField, Tooltip("공격 쿨타임 (초)")]
        public float attackCooldown = 1f;

        [Header("물리 설정")]
        [SerializeField, Tooltip("Rigidbody2D 무게 — 클수록 유닛에게 덜 밀림")]
        public float mass = 10f;

        #endregion
    }
}
