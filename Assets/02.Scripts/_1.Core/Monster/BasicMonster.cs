using UnityEngine;

namespace WildTamer
{
    /// <summary>
    /// Monster 추상 클래스의 기본 구현체입니다.
    /// 이동·애니메이션·상태 전환은 Monster 기반 클래스의 상태 머신이 처리하며,
    /// 이 클래스는 개체별 전투 동작(공격)만 담당합니다.
    /// 기절·테이밍 로직은 Monster 기반 클래스에서 제공하는 virtual 구현을 사용합니다.
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

        #endregion
    }
}
