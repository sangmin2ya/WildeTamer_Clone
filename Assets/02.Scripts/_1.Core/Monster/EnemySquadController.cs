using UnityEngine;

namespace WildTamer
{
    /// <summary>
    /// 적 스쿼드의 자율 이동을 제어하는 컨트롤러입니다.
    ///
    /// 궤도 이동 알고리즘:
    ///   스쿼드는 원의 중심점(_circleCenter)을 기준으로 반지름(zoneCenterRadius)인
    ///   원의 둘레를 따라 공전합니다. zoneCenterInterval마다 현재 접선(이동 방향)의
    ///   정확히 좌/우 수직 위치에 새 중심점을 배치합니다.
    ///   S커브를 위해 현재 중심의 반대편을 우선 선택하며,
    ///   좌 중심 → CCW(+1), 우 중심 → CW(-1)로 공전 방향을 자동 결정합니다.
    ///   새 중심은 항상 현재 위치에서 zoneCenterRadius만큼 떨어지므로
    ///   스쿼드는 끊김 없이 다음 원으로 자연스럽게 전환됩니다.
    ///   이동 범위 제한 없이 중심점을 계속 갱신하며 자유롭게 이동합니다.
    ///   GroundGrid 밖 → 현재 원에서 방향 반전 후 유효 지형으로 복귀합니다.
    ///
    /// 전투 연동:
    ///   전투 중 이동을 중단하고, 종료 시 Squad가 centroid로 텔레포트한 뒤
    ///   현재 위치에서 새 궤도를 초기화하여 이동을 재개합니다.
    /// </summary>
    [RequireComponent(typeof(Squad))]
    public class EnemySquadController : MonoBehaviour
    {
        #region SerializeField 필드

        [Header("궤도 설정")]
        [SerializeField, Tooltip("궤도 원의 반지름 (m) — 스쿼드가 공전하는 원의 크기이자 새 중심점까지의 거리")]
        private float zoneCenterRadius = 8f;

        [SerializeField, Tooltip("중심점을 교체하는 주기 (초)")]
        private float zoneCenterInterval = 5f;

        [SerializeField, Tooltip("스쿼드의 선속도 (m/s) — 원 위를 이동하는 실제 이동 속도")]
        private float squadMoveSpeed = 2f;

        #endregion

        #region Private 필드

        private Squad _squad;

        private Vector2 _circleCenter;     // 현재 궤도 원의 중심점
        private float   _orbitAngle;       // 현재 궤도 각도 (라디안, 원점 기준)
        private float   _orbitDirection;   // +1 = 반시계(CCW), -1 = 시계(CW)
        private float   _zoneCenterTimer;  // 중심점 교체 타이머
        private bool    _inCombat;         // 전투 상태 추적

        #endregion

        #region Unity 메소드

        private void Awake()
        {
            _squad = GetComponent<Squad>();
        }

        private void Start()
        {
            _squad.MoveSpeed = squadMoveSpeed;
            InitOrbit(transform.position);
            _zoneCenterTimer = zoneCenterInterval;
            _squad.ForceMove();
        }

        private void Update()
        {
            HandleCombatTransition();

            if (!_inCombat)
            {
                ExecuteOrbit();
            }
        }

        #endregion

        #region 전투 전환 감지

        /// <summary>
        /// Squad 전투 상태 변화를 감지하여 궤도 이동을 일시 중단/재개합니다.
        /// </summary>
        private void HandleCombatTransition()
        {
            bool squadInCombat = _squad.CurrentState == SquadState.전투;

            if (squadInCombat && !_inCombat)
            {
                _inCombat = true;
            }
            else if (!squadInCombat && _inCombat)
            {
                // 전투 종료 — Squad.SetState()가 멤버 centroid로 텔레포트한 직후
                // 현재(텔레포트된) 위치에서 새 궤도 초기화
                _inCombat = false;
                InitOrbit(transform.position);
                _zoneCenterTimer = zoneCenterInterval;
                _squad.ForceMove();
            }
        }

        #endregion

        #region 궤도 이동

        /// <summary>
        /// 매 프레임 스쿼드를 원 둘레 위로 이동시키고,
        /// 타이머 만료 시 새 중심점을 선정합니다.
        /// </summary>
        private void ExecuteOrbit()
        {
            // 각속도(rad/s) = 선속도 / 반지름
            float angularSpeed = squadMoveSpeed / Mathf.Max(zoneCenterRadius, 0.01f);
            _orbitAngle += _orbitDirection * angularSpeed * Time.deltaTime;

            // 스쿼드 transform을 원 위에 배치
            Vector2 onCircle = _circleCenter
                + new Vector2(Mathf.Cos(_orbitAngle), Mathf.Sin(_orbitAngle)) * zoneCenterRadius;

            transform.position = new Vector3(onCircle.x, onCircle.y, transform.position.z);

            // 주기 만료 시 새 중심점 선정
            _zoneCenterTimer -= Time.deltaTime;

            if (_zoneCenterTimer <= 0f)
            {
                PickNewCircleCenter();
                _zoneCenterTimer = zoneCenterInterval;
            }
        }

        #endregion

        #region 궤도 초기화 / 중심점 교체

        /// <summary>
        /// 지정 위치에서 임의 방향으로 궤도를 초기화합니다.
        /// Start 및 전투 종료 후 재개 시 호출됩니다.
        /// </summary>
        private void InitOrbit(Vector2 fromPosition)
        {
            Vector2 randomDir = Random.insideUnitCircle.normalized;
            _circleCenter   = fromPosition + randomDir * zoneCenterRadius;
            _orbitDirection = Random.value > 0.5f ? 1f : -1f;
            _orbitAngle     = AngleFromCenter(fromPosition);
        }

        /// <summary>
        /// 현재 접선(이동 방향) 기준 좌/우 수직에 새 궤도 중심점을 선정합니다.
        ///
        /// 새 중심 = 현재 위치 ± (접선 수직 방향) × zoneCenterRadius
        ///   → 현재 위치가 새 원의 둘레 위에 있음이 수학적으로 보장됩니다.
        ///   → 좌 중심 → CCW(+1), 우 중심 → CW(-1)로 공전 방향을 자동 결정합니다.
        ///
        /// 선택 로직:
        ///   좌/우를 50% 확률로 랜덤 선택합니다.
        ///   선택한 쪽이 유효하지 않으면 반대편으로 대체합니다.
        ///   양쪽 모두 초과이면 현재 원에서 방향만 반전하여 복귀합니다.
        ///   같은 쪽이 선택되면 이전 궤도 방향이 그대로 유지됩니다.
        /// </summary>
        private void PickNewCircleCenter()
        {
            Vector2 currentPos = transform.position;

            // 현재 이동 접선 방향 계산
            float   tangentAngle = _orbitAngle + _orbitDirection * Mathf.PI * 0.5f;
            Vector2 tangent      = new Vector2(Mathf.Cos(tangentAngle), Mathf.Sin(tangentAngle));

            // 접선 기준 좌(CCW 90°) / 우(CW 90°) 수직 방향
            Vector2 leftPerp  = new Vector2(-tangent.y,  tangent.x);
            Vector2 rightPerp = new Vector2( tangent.y, -tangent.x);

            Vector2 leftCandidate  = currentPos + leftPerp  * zoneCenterRadius;
            Vector2 rightCandidate = currentPos + rightPerp * zoneCenterRadius;

            bool leftValid  = IsPositionValid(leftCandidate);
            bool rightValid = IsPositionValid(rightCandidate);

            // 50% 확률로 좌/우 중 하나를 1순위로 선택
            bool preferLeft = Random.value > 0.5f;

            Vector2 chosen;
            bool    chosenIsLeft;

            if (preferLeft)
            {
                if (leftValid)
                {
                    chosen = leftCandidate; chosenIsLeft = true;
                }
                else if (rightValid)
                {
                    chosen = rightCandidate; chosenIsLeft = false;
                }
                else
                {
                    // 양쪽 모두 범위/지형 초과 → 현재 원에서 방향만 반전하여 복귀
                    _orbitDirection = -_orbitDirection;
                    _orbitAngle     = AngleFromCenter(currentPos);
                    return;
                }
            }
            else
            {
                if (rightValid)
                {
                    chosen = rightCandidate; chosenIsLeft = false;
                }
                else if (leftValid)
                {
                    chosen = leftCandidate; chosenIsLeft = true;
                }
                else
                {
                    // 양쪽 모두 범위/지형 초과 → 현재 원에서 방향만 반전하여 복귀
                    _orbitDirection = -_orbitDirection;
                    _orbitAngle     = AngleFromCenter(currentPos);
                    return;
                }
            }

            _circleCenter   = chosen;
            _orbitDirection = chosenIsLeft ? 1f : -1f;   // 좌→CCW(+1), 우→CW(-1)
            _orbitAngle     = AngleFromCenter(currentPos);
        }

        /// <summary>
        /// 주어진 위치가 유효 지형 위인지 반환합니다.
        /// </summary>
        private bool IsPositionValid(Vector2 pos)
        {
            return GameManager.Instance == null || GameManager.Instance.IsValidGroundPosition(pos);
        }

        /// <summary>
        /// 현재 위치와 _circleCenter 사이의 각도(라디안)를 반환합니다.
        /// </summary>
        private float AngleFromCenter(Vector2 position)
        {
            return Mathf.Atan2(
                position.y - _circleCenter.y,
                position.x - _circleCenter.x
            );
        }

        #endregion

        #region 디버그 기즈모

        private void OnDrawGizmos()
        {
            Vector3 center = Application.isPlaying
                ? new Vector3(_circleCenter.x, _circleCenter.y, 0f)
                : transform.position;

            bool inCombat = Application.isPlaying
                && _squad != null
                && _squad.CurrentState == SquadState.전투;

            if (!Application.isPlaying)
            {
                return;
            }

            // ── 현재 궤도 원 (청록색 와이어) ─────────────────────────────
            Gizmos.color = inCombat
                ? new Color(1f, 0.2f, 0.2f, 0.4f)
                : new Color(0f, 1f, 1f, 0.4f);
            Gizmos.DrawWireSphere(center, zoneCenterRadius);

            // ── 궤도 중심점 마커 (채운 구) ────────────────────────────────
            Gizmos.color = inCombat ? new Color(1f, 0.2f, 0.2f) : Color.cyan;
            Gizmos.DrawSphere(center, 0.2f);

            // ── 중심 → 스쿼드 반지름 선 ─────────────────────────────────
            Gizmos.color = inCombat
                ? new Color(1f, 0.2f, 0.2f, 0.5f)
                : new Color(0f, 1f, 1f, 0.5f);
            Gizmos.DrawLine(center, transform.position);

            // ── 공전 방향 화살표 (접선 방향 표시) ────────────────────────
            float tangentAngle = _orbitAngle + _orbitDirection * Mathf.PI * 0.5f;
            Vector3 tangentDir = new Vector3(Mathf.Cos(tangentAngle), Mathf.Sin(tangentAngle), 0f);
            Vector3 squadPos   = transform.position;

            Gizmos.color = inCombat ? new Color(1f, 0.4f, 0.4f) : Color.white;
            Gizmos.DrawLine(squadPos, squadPos + tangentDir * 1.5f);
            Gizmos.DrawSphere(squadPos + tangentDir * 1.5f, 0.12f);
        }

        #endregion
    }
}
