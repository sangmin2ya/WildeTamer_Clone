using UnityEngine;

namespace WildTamer
{
    /// <summary>
    /// 플레이어·몬스터 등 모든 유닛이 공유하는 상태 머신의 기반 추상 클래스입니다.
    /// 제네릭 타입 T로 소유자를 지정하여 각 상태에서 타입 안전하게 Owner에 접근합니다.
    /// </summary>
    /// <typeparam name="T">이 상태를 소유하는 유닛 타입 (MonoBehaviour 서브클래스)</typeparam>
    public abstract class UnitState<T> where T : MonoBehaviour
    {
        #region Protected 필드

        protected T Owner;

        #endregion

        #region 생성자

        protected UnitState(T owner)
        {
            Owner = owner;
        }

        #endregion

        #region 상태 생명주기

        /// <summary>상태 진입 시 한 번 호출됩니다.</summary>
        public virtual void Enter() { }

        /// <summary>상태 이탈 시 한 번 호출됩니다.</summary>
        public virtual void Exit() { }

        /// <summary>매 프레임 호출됩니다. (감지, 판단, 애니메이션 갱신)</summary>
        public virtual void Update() { }

        /// <summary>고정 물리 프레임마다 호출됩니다. (velocity 설정)</summary>
        public virtual void FixedUpdate() { }

        #endregion
    }
}
