using System;
using System.Collections;
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
    /// 코루틴 기반 감지 루프를 통해 상대 군중을 인식하고 전투 상태를 자율 전환합니다.
    /// 씬 내 모든 Squad는 정적 레지스트리에 자동 등록되어 Physics 쿼리 없이 거리를 비교합니다.
    ///
    /// 감지 루프 흐름:
    ///   1. 소멸하거나 범위를 벗어난 적 스쿼드를 _detectedEnemySquads에서 제거
    ///   2. 새로 범위에 들어온 적 스쿼드를 추가
    ///   3. 목록 유무에 따라 전투 진입 / 이탈 전환
    /// </summary>
    public class Squad : MonoBehaviour
    {
        #region SerializeField 필드

        [Header("군중 설정")]
        [SerializeField, Tooltip("이 군중의 종류 (플레이어 or 적)")]
        private SquadType squadType;

        [SerializeField, Tooltip("적 군중 감지 주기 (초) — 값이 클수록 CPU 절약, 작을수록 빠른 반응")]
        private float detectInterval = 0.3f;

        [Header("멤버")]
        [SerializeField, Tooltip("이 군중에 소속된 몬스터 목록")]
        private List<Monster> members = new List<Monster>();

        [SerializeField, Tooltip("플레이어 스쿼드 최대 멤버 수")]
        private int maxMembers = 10;

        #endregion

        #region Private 필드

        private SquadState _currentState = SquadState.정지;

        // 현재 감지된 적 Squad 목록 (매 감지 주기마다 재구성)
        private readonly List<Squad> _detectedEnemySquads = new List<Squad>();

        // WaitForSeconds 캐싱 — 코루틴 내 반복 할당 방지
        private WaitForSeconds _detectWait;

        // 플레이어 이동 중 강제 이동 플래그 — true이면 감지 코루틴이 전투 전환을 차단
        private bool _movementForced;

        // 이 군중의 리더 (플레이어) — 적 군중의 타겟 탐색 시 멤버와 함께 고려됨
        private IFightable _leader;

        #endregion

        #region 정적 레지스트리

        // 씬 내 모든 Squad가 등록되는 전역 목록
        // Physics 쿼리 없이 O(n) 순회만으로 적 군중 감지 가능
        private static readonly List<Squad> AllSquads = new List<Squad>();

        /// <summary>
        /// 씬 내 플레이어 타입 스쿼드를 반환합니다.
        /// Monster.Tame()에서 아군 편입 시 사용합니다.
        /// </summary>
        public static Squad GetPlayerSquad()
        {
            foreach (Squad s in AllSquads)
            {
                if (s.Type == SquadType.플레이어)
                {
                    return s;
                }
            }

            return null;
        }

        #endregion

        #region Public 프로퍼티

        /// <summary>군중의 현재 행동 상태</summary>
        public SquadState CurrentState => _currentState;

        /// <summary>군중의 종류</summary>
        public SquadType Type => squadType;

        /// <summary>멤버 몬스터의 이동 목표점 — 이 오브젝트의 현재 위치</summary>
        public Vector3 TargetPosition => transform.position;

        /// <summary>현재 멤버 수</summary>
        public int MemberCount => members.Count;

        /// <summary>최대 멤버 수</summary>
        public int MaxMembers => maxMembers;

        /// <summary>최대 멤버 수에 도달했는지 여부</summary>
        public bool IsFull => members.Count >= maxMembers;

        /// <summary>
        /// 멤버가 이동할 속도 (m/s).
        /// EnemySquadController가 squadMoveSpeed를 설정하고,
        /// PlayerController가 playerData.moveSpeed를 설정합니다.
        /// </summary>
        public float MoveSpeed { get; set; } = 3f;

        /// <summary>
        /// 멤버 수가 변경될 때 발행됩니다. (현재 수, 최대 수)
        /// TamerCountUI 등 UI 컴포넌트가 구독하여 표시를 갱신합니다.
        /// </summary>
        public event Action<int, int> OnMemberCountChanged;

        /// <summary>
        /// 멤버가 0명이 될 때 발행됩니다.
        /// GameManager가 구독하여 적 스쿼드를 풀에 반환합니다.
        /// </summary>
        public event Action<Squad> OnEmpty;

        #endregion

        #region Unity 메소드

        private void Awake()
        {
            _detectWait = new WaitForSeconds(detectInterval);
        }

        private void OnEnable()
        {
            AllSquads.Add(this);
            StartCoroutine(DetectEnemySquadsRoutine());
        }

        private void OnDisable()
        {
            AllSquads.Remove(this);
            _detectedEnemySquads.Clear();
            _currentState = SquadState.정지;
            _movementForced = false;
            _leader = null;

            // 비활성화 시 멤버 목록 초기화 (적 스쿼드 전용)
            if (squadType == SquadType.적)
            {
                members.Clear();
            }
        }

        #endregion

        #region 적 군중 감지

        /// <summary>
        /// 주기적으로 상대 군중과의 거리를 확인하여 전투 상태를 자율 전환하는 코루틴입니다.
        /// </summary>
        private IEnumerator DetectEnemySquadsRoutine()
        {
            while (true)
            {
                yield return _detectWait;
                CheckEnemySquads();
            }
        }

        /// <summary>
        /// 멤버별 감지 범위로 상대 군중을 탐색하고,
        /// 감지 결과에 따라 전투 진입 / 이탈을 자동 전환합니다.
        /// </summary>
        private void CheckEnemySquads()
        {
            // 1. 소멸했거나 감지 범위를 벗어난 스쿼드 제거
            for (int i = _detectedEnemySquads.Count - 1; i >= 0; i--)
            {
                Squad s = _detectedEnemySquads[i];

                if (s == null || s.MemberCount == 0 || !IsEnemySquadDetected(s))
                {
                    _detectedEnemySquads.RemoveAt(i);
                }
            }

            // 2. 새로 범위에 들어온 스쿼드 추가
            foreach (Squad other in AllSquads)
            {
                if (other == this || other.Type == squadType)
                {
                    continue;
                }

                if (IsEnemySquadDetected(other) && !_detectedEnemySquads.Contains(other))
                {
                    _detectedEnemySquads.Add(other);
                }
            }

            // 3. 전투 상태 전환
            bool blocked = squadType == SquadType.플레이어 && _movementForced;

            if (_detectedEnemySquads.Count > 0 && !blocked && _currentState != SquadState.전투)
            {
                // 전투 진입 — 감지된 상대 스쿼드에 상호 전투 알림
                SetState(SquadState.전투);

                foreach (Squad enemy in _detectedEnemySquads)
                {
                    if (enemy != null)
                    {
                        enemy.EnterCombatFromDetection(this);
                    }
                }
            }
            else if (_detectedEnemySquads.Count == 0 && _currentState == SquadState.전투)
            {
                // 전투 이탈
                SetState(SquadState.정지);
            }
        }

        /// <summary>
        /// 이 스쿼드의 멤버 중 한 명이라도 상대 스쿼드의 유닛을 감지하면 true를 반환합니다.
        /// 각 멤버의 MonsterData.stat.detectionRange를 감지 반경으로 사용합니다.
        /// </summary>
        /// <param name="enemySquad">검사할 상대 스쿼드</param>
        private bool IsEnemySquadDetected(Squad enemySquad)
        {
            // ─ 멤버별 감지 ─────────────────────────────────────────────
            foreach (Monster member in members)
            {
                if (member == null)
                {
                    continue;
                }

                float rangeSqr = member.Data.stat.detectionRange * member.Data.stat.detectionRange;
                Vector2 memberPos = member.transform.position;

                // 상대 리더 감지
                if (enemySquad._leader != null && enemySquad._leader.IsAlive)
                {
                    float dSqr = ((Vector2)enemySquad._leader.Transform.position - memberPos).sqrMagnitude;
                    if (dSqr <= rangeSqr)
                    {
                        return true;
                    }
                }

                // 상대 멤버 감지
                foreach (Monster enemy in enemySquad.GetMembers())
                {
                    if (enemy == null || !enemy.IsAlive)
                    {
                        continue;
                    }

                    float dSqr = ((Vector2)enemy.transform.position - memberPos).sqrMagnitude;
                    if (dSqr <= rangeSqr)
                    {
                        return true;
                    }
                }
            }

            return false;
        }

        /// <summary>
        /// 이 군중의 리더(플레이어)를 설정합니다.
        /// 적 군중이 타겟을 탐색할 때 멤버와 함께 리더도 후보에 포함됩니다.
        /// </summary>
        /// <param name="leader">리더로 설정할 IFightable (보통 PlayerController)</param>
        public void SetLeader(IFightable leader)
        {
            _leader = leader;
        }

        /// <summary>
        /// 현재 감지된 적 군중의 리더 및 멤버 중
        /// 지정 위치에서 가장 가까운 살아있는 IFightable을 반환합니다.
        /// MonsterCombatState에서 개체별 타겟 탐색에 사용합니다.
        /// </summary>
        /// <returns>가장 가까운 살아있는 적 IFightable, 없으면 null</returns>
        public IFightable GetNearestEnemyTarget(Vector2 from)
        {
            IFightable nearest = null;
            float nearestSqr = float.MaxValue;

            foreach (Squad enemySquad in _detectedEnemySquads)
            {
                if (enemySquad == null)
                {
                    continue;
                }

                // 리더(플레이어) 후보 검사
                IFightable leader = enemySquad._leader;

                if (leader != null && leader.IsAlive)
                {
                    float distSqr = ((Vector2)leader.Transform.position - from).sqrMagnitude;

                    if (distSqr < nearestSqr)
                    {
                        nearestSqr = distSqr;
                        nearest = leader;
                    }
                }

                // 멤버 후보 검사
                foreach (Monster monster in enemySquad.GetMembers())
                {
                    if (monster == null || !monster.IsAlive)
                    {
                        continue;
                    }

                    float distSqr = ((Vector2)monster.Transform.position - from).sqrMagnitude;

                    if (distSqr < nearestSqr)
                    {
                        nearestSqr = distSqr;
                        nearest = monster;
                    }
                }
            }

            return nearest;
        }

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
                OnMemberCountChanged?.Invoke(members.Count, maxMembers);
            }
        }

        /// <summary>
        /// 몬스터를 군중 멤버에서 제거합니다.
        /// 마지막 멤버가 제거되어 목록이 비면 OnEmpty를 발행합니다.
        /// </summary>
        /// <param name="monster">제거할 몬스터</param>
        public void RemoveMember(Monster monster)
        {
            if (members.Remove(monster))
            {
                OnMemberCountChanged?.Invoke(members.Count, maxMembers);

                if (members.Count == 0)
                {
                    OnEmpty?.Invoke(this);
                }
            }
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
        /// 상대 스쿼드에 의해 감지되어 즉시 전투로 진입합니다.
        /// 감지한 스쿼드를 _detectedEnemySquads에 추가하여 타겟 탐색이 가능하도록 합니다.
        /// 플레이어 스쿼드가 이동 강제 중이면 무시합니다.
        /// </summary>
        /// <param name="detectedBy">이 스쿼드를 감지한 상대 스쿼드</param>
        public void EnterCombatFromDetection(Squad detectedBy)
        {
            // 플레이어 스쿼드: 이동 강제 중에는 전투 전환 차단
            if (squadType == SquadType.플레이어 && _movementForced)
            {
                return;
            }

            // 리스트 등록은 전투 상태와 무관하게 먼저 수행
            // (이미 전투 중이어도 새 적 스쿼드를 타겟 후보에 포함)
            if (!_detectedEnemySquads.Contains(detectedBy))
            {
                _detectedEnemySquads.Add(detectedBy);
            }

            if (_currentState == SquadState.전투)
            {
                return;
            }

            SetState(SquadState.전투);
        }

        /// <summary>
        /// 이 군중을 전투 상태로 전환합니다.
        /// 외부 스크립트에서 명시적으로 전투를 강제할 때 사용합니다.
        /// </summary>
        public void ForceEnterCombat()
        {
            _movementForced = false;
            SetState(SquadState.전투);
        }

        /// <summary>
        /// 이 군중을 이동 상태로 전환합니다.
        /// 플레이어 스쿼드: 전투 중에도 즉시 이동으로 전환 (이동 최우선).
        /// 적 스쿼드: 전투 중이 아닐 때만 이동으로 전환 (전투는 이동보다 우선).
        /// </summary>
        public void ForceMove()
        {
            _movementForced = true;

            // 적 스쿼드는 전투 중 이동 강제 전환 금지 — 전투 종료 후 다음 호출 시 자연스럽게 이동
            if (squadType == SquadType.적 && _currentState == SquadState.전투)
            {
                return;
            }

            SetState(SquadState.이동);
        }

        /// <summary>
        /// 이 군중을 강제로 정지 상태로 전환합니다.
        /// 플레이어가 멈추면 이동 강제 플래그가 해제되어 다음 감지 틱에서 전투 전환이 재개됩니다.
        /// </summary>
        public void ForceStop()
        {
            _movementForced = false;
            SetState(SquadState.정지);
        }

        /// <summary>
        /// 군중 상태를 전환하고 모든 멤버에게 알립니다.
        /// 전투에서 벗어날 때는 스쿼드 위치를 멤버 중심으로 순간이동합니다.
        /// </summary>
        private void SetState(SquadState newState)
        {
            if (_currentState == newState)
            {
                return;
            }

            // 전투 종료 시 스쿼드 위치를 멤버 중심으로 순간이동
            // (멤버들이 흩어진 상태에서 원래 위치로 돌아가는 긴 이동을 방지)
            if (_currentState == SquadState.전투 && newState != SquadState.전투)
            {
                TeleportToMembersCentroid();
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

        /// <summary>
        /// 스쿼드 위치를 살아있는 멤버들의 중심점으로 순간이동합니다.
        /// </summary>
        private void TeleportToMembersCentroid()
        {
            if (members.Count == 0)
            {
                return;
            }

            Vector2 centroid = Vector2.zero;
            int count = 0;

            foreach (Monster member in members)
            {
                if (member == null)
                {
                    continue;
                }

                centroid += (Vector2)member.transform.position;
                count++;
            }

            if (count > 0)
            {
                transform.position = (Vector3)(centroid / count);
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
                    member.ForceUpdateFacing(direction);
                }
            }
        }

        #endregion

        #region 디버그 기즈모

#if UNITY_EDITOR
        private void OnDrawGizmos()
        {
        }
#endif

        #endregion
    }
}
