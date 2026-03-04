using UnityEngine;

namespace WildTamer
{
    /// <summary>
    /// 돌, 나무 등 환경 오브젝트의 깊이 정렬을 처리하는 컴포넌트입니다.
    /// 플레이어와의 상대 Y 차이를 기반으로 sortingOrder를 결정합니다.
    /// 플레이어가 감지 거리 밖에 있으면 업데이트를 건너뜁니다 (최적화).
    /// </summary>
    public class EnvironmentObject : MonoBehaviour, IDepthSortable
    {
        #region SerializeField 필드

        [Header("참조")]
        [SerializeField, Tooltip("sortingOrder에 사용할 SpriteRenderer")]
        private SpriteRenderer spriteRenderer;

        #endregion

        #region Private 필드

        private Transform _playerTransform;

        #endregion

        #region Unity 메소드

        private void Awake()
        {
            GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
            if (playerObj != null)
            {
                _playerTransform = playerObj.transform;
            }
        }

        private void Update()
        {
            UpdateSortingOrder();
        }

        #endregion

        #region IDepthSortable

        /// <summary>
        /// 플레이어 기준 상대 Y를 기반으로 sortingOrder를 갱신합니다.
        /// 절댓값에 올림을 적용해 플레이어보다 낮으면 -1 이하, 높으면 +1 이상을 보장합니다.
        /// </summary>
        public void UpdateSortingOrder()
        {
            if (_playerTransform == null)
            {
                return;
            }

            // 최적화: 플레이어와 너무 멀면 업데이트 건너뜀
            Vector2 diff = (Vector2)transform.position - (Vector2)_playerTransform.position;
            float distSqr = IDepthSortable.SortUpdateDistance * IDepthSortable.SortUpdateDistance;
            if (diff.sqrMagnitude > distSqr)
            {
                return;
            }

            // 오브젝트 Y - 플레이어 Y: 낮으면 음수(뒤), 높으면 양수(앞)
            float rawDiff = transform.position.y - _playerTransform.position.y;
            int magnitude = Mathf.CeilToInt(Mathf.Abs(rawDiff));

            // Y가 완전히 같으면 스킵
            if (magnitude == 0)
            {
                return;
            }

            int sign = rawDiff > 0f ? -1 : 1;
            spriteRenderer.sortingOrder = Mathf.Clamp(
                sign * magnitude,
                IDepthSortable.MinSortingOrder,
                IDepthSortable.MaxSortingOrder
            );
        }

        #endregion
    }
}
