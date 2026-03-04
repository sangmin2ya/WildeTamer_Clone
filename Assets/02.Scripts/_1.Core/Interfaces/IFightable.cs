using System;
using UnityEngine;

namespace WildTamer
{
    /// <summary>
    /// 전투 가능한 모든 객체가 구현해야 하는 인터페이스입니다.
    /// Monster와 PlayerController 모두 이 인터페이스를 구현하여
    /// 서로를 공격 대상으로 인식할 수 있습니다.
    /// </summary>
    public interface IFightable
    {
        /// <summary>월드 Transform — 거리 계산 및 이동 목표로 사용</summary>
        Transform Transform { get; }

        /// <summary>현재 체력</summary>
        float CurrentHp { get; }

        /// <summary>체력 변동 이벤트 — UI 갱신 등에 사용</summary>
        event Action<float, float> OnHpChanged;

        /// <summary>생존 여부 (CurrentHp > 0)</summary>
        bool IsAlive { get; }

        /// <summary>
        /// 대상을 공격합니다.
        /// </summary>
        /// <param name="target">공격 대상</param>
        void Attack(IFightable target);

        /// <summary>
        /// 데미지를 받아 체력을 감소시킵니다.
        /// </summary>
        /// <param name="damage">받는 데미지 양</param>
        void TakeDamage(float damage);

        /// <summary>
        /// 사망 처리를 수행합니다. (풀 반환, 아군화 등)
        /// </summary>
        void Die();

    }
}
