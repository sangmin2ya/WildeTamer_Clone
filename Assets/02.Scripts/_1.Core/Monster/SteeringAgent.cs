using UnityEngine;

namespace WildTamer
{
    /// <summary>
    /// 레이캐스트 기반 장애물 우회 알고리즘 컴포넌트입니다.
    /// 목표 방향으로 레이캐스트를 쏘고, 막혀 있으면 각도를 단계적으로 틀어
    /// 첫 번째 통과 가능한 방향을 이동 목표로 반환합니다.
    /// 자기 자신의 콜라이더는 충돌에서 제외합니다.
    /// </summary>
    public class SteeringAgent : MonoBehaviour
    {
        #region SerializeField 필드

        [Header("장애물 회피 설정")]
        [SerializeField, Tooltip("전방 장애물 감지 거리 (미터). 이 거리 내의 장애물만 회피합니다.")]
        private float detectionRange = 0.5f;

        [SerializeField, Tooltip("장애물로 인식할 레이어. 반드시 설정해야 회피가 작동합니다!")]
        private LayerMask obstacleLayer;

        #endregion

        #region 상수

        // 각도 탐색 단계: 15° × 6 = 최대 ±90°
        private const float AngleStep = 15f;
        private const int MaxSteps = 6;

        #endregion

        #region Private 필드

        // 자기 자신 콜라이더 — 레이캐스트 결과에서 제외
        private Collider2D _selfCollider;

        // NonAlloc 레이캐스트용 재사용 버퍼 (GC 방지)
        private readonly RaycastHit2D[] _hitBuffer = new RaycastHit2D[8];

        #endregion

        #region Unity 메소드

        private void Awake()
        {
            _selfCollider = GetComponent<Collider2D>();
        }

        private void Start()
        {
#if UNITY_EDITOR
            if (obstacleLayer.value == 0)
            {
                Debug.LogWarning(
                    $"[SteeringAgent] '{name}': obstacleLayer가 설정되지 않았습니다. " +
                    "Inspector에서 장애물 레이어를 선택하지 않으면 회피가 작동하지 않습니다.",
                    this
                );
            }
#endif
        }

        #endregion

        #region Public API

        /// <summary>
        /// 현재 위치에서 목표 위치로 향하는 스티어링 목표점을 반환합니다.
        /// 직선 경로가 막혀 있으면 좌우 교대로 각도를 틀어 통과 가능한 방향을 탐색합니다.
        /// 통과 가능한 방향을 찾으면 원래 목표와 같은 거리의 중간 목표점을 반환합니다.
        /// 모든 방향이 막힌 경우 원래 목표를 폴백으로 반환합니다.
        /// </summary>
        /// <param name="target">최종 이동 목표 위치</param>
        /// <returns>이 프레임에 이동할 목표 위치</returns>
        public Vector2 ComputeSteeringTarget(Vector2 target)
        {
            Vector2 from = transform.position;
            Vector2 toTarget = target - from;
            float dist = toTarget.magnitude;

            if (dist < 0.01f)
            {
                return target;
            }

            Vector2 dir = toTarget / dist;

            // 직선 경로가 뚫려 있으면 목표로 직행
            if (!IsBlocked(from, dir))
            {
                return target;
            }

            // 좌우 교대로 각도를 단계적으로 틀어 통과 방향 탐색
            for (int step = 1; step <= MaxSteps; step++)
            {
                float angle = AngleStep * step;

                // 왼쪽 방향 먼저
                Vector2 leftDir = RotateVector(dir, angle);

                if (!IsBlocked(from, leftDir))
                {
                    // 원래 목표까지의 거리(dist)만큼 우회 방향으로 이동 → 몬스터가 도중에 멈추지 않음
                    return from + leftDir * dist;
                }

                // 오른쪽 방향
                Vector2 rightDir = RotateVector(dir, -angle);

                if (!IsBlocked(from, rightDir))
                {
                    return from + rightDir * dist;
                }
            }

            // 모든 방향이 막혀도 원래 목표로 이동 (폴백)
            return target;
        }

        #endregion

        #region 내부 유틸리티

        /// <summary>
        /// 지정 방향으로 NonAlloc 레이캐스트를 쏩니다.
        /// 자기 자신의 콜라이더는 무시하고, 나머지 장애물 히트 여부를 반환합니다.
        /// 에디터에서는 결과에 따라 빨강/초록 선으로 씬뷰에 시각화합니다.
        /// </summary>
        private bool IsBlocked(Vector2 from, Vector2 dir)
        {
            int count = Physics2D.RaycastNonAlloc(from, dir, _hitBuffer, detectionRange, obstacleLayer);

            bool blocked = false;

            for (int i = 0; i < count; i++)
            {
                if (_hitBuffer[i].collider != _selfCollider)
                {
                    blocked = true;
                    break;
                }
            }

#if UNITY_EDITOR
            Debug.DrawRay(from, dir * detectionRange, blocked ? Color.red : Color.green);
#endif

            return blocked;
        }

        /// <summary>
        /// 2D 벡터를 지정한 각도만큼 회전시킵니다. 양수는 반시계 방향입니다.
        /// </summary>
        /// <param name="v">회전할 벡터</param>
        /// <param name="degrees">회전 각도 (도)</param>
        private static Vector2 RotateVector(Vector2 v, float degrees)
        {
            float rad = degrees * Mathf.Deg2Rad;
            float cos = Mathf.Cos(rad);
            float sin = Mathf.Sin(rad);

            return new Vector2(
                v.x * cos - v.y * sin,
                v.x * sin + v.y * cos
            );
        }

        #endregion

        #region 디버그 시각화

#if UNITY_EDITOR
        private void OnDrawGizmosSelected()
        {
            Gizmos.color = new Color(1f, 1f, 0f, 0.4f);
            Gizmos.DrawWireSphere(transform.position, detectionRange);
        }
#endif

        #endregion
    }
}
