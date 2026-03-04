using UnityEngine;

namespace WildTamer
{
    /// <summary>
    /// Monster 추상 클래스의 기본 구현체입니다.
    /// 이동·애니메이션·상태 전환은 Monster 기반 클래스의 상태 머신이 처리하며,
    /// 이 클래스는 개체별 동작(전투, 사망, 테이밍)과 플레이어 방향 동기화를 담당합니다.
    /// </summary>
    public class BasicMonster : Monster
    {
        #region IFightable

        /// <inheritdoc/>
        public override void Attack(IFightable target)
        {
            PlayAttackAnimation();
            target.TakeDamage(monsterData.stat.attackDamage);
        }

        /// <inheritdoc/>
        public override void Die()
        {
            StopMovement();
            gameObject.SetActive(false);
        }

        #endregion

        #region ITameable

        /// <inheritdoc/>
        public override void Stun()
        {
            IsStunned = true;
            StopMovement();
        }

        /// <inheritdoc/>
        public override void Tame()
        {
            IsTamed   = true;
            IsStunned = false;
        }

        #endregion
    }
}
