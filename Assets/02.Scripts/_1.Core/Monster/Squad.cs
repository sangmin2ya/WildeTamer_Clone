using System.Collections.Generic;
using UnityEngine;

namespace WildTamer
{
    /// <summary>
    /// 군중의 종류를 나타냅니다.
    /// </summary>
    public enum SquadType { 플레이어, 적 }

    /// <summary>
    /// 군중의 현재 행동 상태를 나타냅니다.
    /// </summary>
    public enum SquadState { 정지, 이동, 전투 }

    /// <summary>
    /// 플레이어 또는 적 군중을 관리하는 클래스입니다.
    /// 자신의 위치를 멤버 몬스터의 이동 목표점으로 제공하며,
    /// 외부 충돌 감지 스크립트로부터 전투/이동 상태 전환을 전달받습니다.
    /// 군중 오브젝트의 이동·충돌 감지 로직은 별도 스크립트에서 처리합니다.
    /// </summary>
    public class Squad : MonoBehaviour
    {
        #region SerializeField 필드

        [Header("군중 설정")]
        [SerializeField, Tooltip("이 군중의 종류 (플레이어 or 적)")]
        private SquadType squadType;

        [Header("멤버")]
        [SerializeField, Tooltip("이 군중에 소속된 몬스터 목록")]
        private List<Monster> members = new List<Monster>();

        #endregion

        #region Private 필드

        private SquadState _currentState = SquadState.정지;

        #endregion

        #region Public 프로퍼티

        /// <summary>군중의 현재 행동 상태</summary>
        public SquadState CurrentState => _currentState;

        /// <summary>군중의 종류</summary>
        public SquadType Type => squadType;

        /// <summary>멤버 몬스터의 이동 목표점 — 이 오브젝트의 현재 위치</summary>
        public Vector3 TargetPosition => transform.position;

        #endregion

        #region 멤버 관리

        /// <summary>
        /// 몬스터를 군중 멤버로 추가합니다.
        /// </summary>
        /// <param name="monster">추가할 몬스터</param>
        public void AddMember(Monster monster)
        {
            if (monster != null && !members.Contains(monster))
            {
                members.Add(monster);
            }
        }

        /// <summary>
        /// 몬스터를 군중 멤버에서 제거합니다.
        /// </summary>
        /// <param name="monster">제거할 몬스터</param>
        public void RemoveMember(Monster monster)
        {
            members.Remove(monster);
        }

        /// <summary>
        /// 현재 군중에 등록된 모든 멤버 목록을 반환합니다.
        /// </summary>
        public IReadOnlyList<Monster> GetMembers()
        {
            return members;
        }

        #endregion

        #region 상태 전환

        /// <summary>
        /// 이 군중을 전투 상태로 전환합니다.
        /// 충돌 감지 스크립트가 적 군중을 인식했을 때 호출합니다.
        /// </summary>
        public void ForceEnterCombat()
        {
            SetState(SquadState.전투);
        }

        /// <summary>
        /// 이 군중을 이동 상태로 복귀시킵니다.
        /// 충돌 감지 스크립트가 범위 내 적 군중이 없어졌을 때 호출합니다.
        /// </summary>
        public void ForceMove()
        {
            SetState(SquadState.이동);
        }

        /// <summary>
        /// 이 군중을 강제로 정지 상태로 전환합니다.
        /// </summary>
        public void ForceStop()
        {
            SetState(SquadState.정지);
        }

        /// <summary>
        /// 군중 상태를 전환하고 모든 멤버에게 알립니다.
        /// 상태가 동일하면 아무 작업도 수행하지 않습니다.
        /// </summary>
        /// <param name="newState">전환할 새 상태</param>
        private void SetState(SquadState newState)
        {
            if (_currentState == newState)
            {
                return;
            }

            _currentState = newState;

            foreach (Monster member in members)
            {
                if (member != null)
                {
                    member.OnSquadStateChanged(newState);
                }
            }
        }

        #endregion

        #region 멤버 제어

        /// <summary>
        /// 모든 멤버가 지정된 방향을 바라보도록 강제합니다.
        /// 이동중에 플레이어와 같은 방향을 바라보게 할 때 사용합니다.
        /// </summary>
        public void ForceFaceDirection(Vector2 direction)
        {
            foreach (Monster member in members)
            {
                if (member != null)
                {
                    member.UpdateFacing(direction);
                }
            }
        }

        #endregion
    }
}
