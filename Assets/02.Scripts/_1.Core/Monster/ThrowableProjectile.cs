using UnityEngine;

namespace WildTamer
{
    /// <summary>
    /// 원거리 몬스터가 던지는 투척 오브젝트입니다.
    /// PoolManager를 통해 생성·반환되며,
    /// 발사 시점에 고정된 타겟 위치로 직선 비행합니다.
    ///
    /// 동작 흐름:
    ///   Initialize() 호출 시 목표 위치 고정 → FixedUpdate에서 직선 비행
    ///   → 목표 위치 도달 시 타겟 생존 여부 확인 후 데미지 적용(또는 미적용) 및 풀 반환
    ///   → 수명(lifetime) 초과 시 데미지 없이 풀 반환
    /// </summary>
    public class ThrowableProjectile : MonoBehaviour
    {
        #region Private 필드

        private GameObject _sourcePrefab;   // 풀 반환에 사용할 원본 프리팹 참조
        private IFightable _target;          // 도달 시 데미지 대상 여부 판정용
        private float      _damage;
        private Vector2    _targetPosition;  // 발사 시점에 고정된 목표 위치
        private Vector2    _direction;       // 발사 시점에 고정된 비행 방향
        private float      _speed;
        private float      _hitRadiusSqr;   // 성능을 위해 제곱값으로 보관
        private float      _remainingLifetime;

        #endregion

        #region 초기화

        /// <summary>
        /// 투척 오브젝트를 풀에서 꺼낸 직후 호출하여 비행 파라미터를 설정합니다.
        /// 목표 위치는 발사 시점의 타겟 위치로 고정되며, 이후 타겟이 이동해도 추적하지 않습니다.
        /// </summary>
        /// <param name="sourcePrefab">풀 반환에 사용할 원본 프리팹</param>
        /// <param name="target">도달 시 데미지 대상 (생존 여부 확인용)</param>
        /// <param name="damage">적용할 데미지</param>
        /// <param name="speed">비행 속도 (m/s)</param>
        /// <param name="hitRadius">목표 위치 도달 판정 반경 (m)</param>
        /// <param name="lifetime">최대 비행 시간 (초)</param>
        public void Initialize(
            GameObject sourcePrefab,
            IFightable target,
            float      damage,
            float      speed,
            float      hitRadius,
            float      lifetime)
        {
            _sourcePrefab      = sourcePrefab;
            _target            = target;
            _damage            = damage;
            _speed             = speed;
            _hitRadiusSqr      = hitRadius * hitRadius;
            _remainingLifetime = lifetime;

            if (target != null)
            {
                // 발사 시점의 타겟 위치를 고정 목표로 저장
                _targetPosition = (Vector2)target.Transform.position;
                _direction = (_targetPosition - (Vector2)transform.position).normalized;
            }
            else
            {
                _targetPosition = (Vector2)transform.position + Vector2.right;
                _direction = Vector2.right;
            }
        }

        #endregion

        #region Unity 메소드

        private void FixedUpdate()
        {
            _remainingLifetime -= Time.fixedDeltaTime;

            if (_remainingLifetime <= 0f)
            {
                ReturnToPool();
                return;
            }

            // 고정 방향으로 직선 비행
            transform.position += (Vector3)(_direction * _speed * Time.fixedDeltaTime);

            Vector2 toTarget = _targetPosition - (Vector2)transform.position;

            // 목표 위치에 도달했거나 지나쳤는지 확인
            // (거리 도달 판정 또는 진행 방향이 역전된 경우 — 오버슛 감지)
            bool arrived = toTarget.sqrMagnitude <= _hitRadiusSqr;
            bool overshot = Vector2.Dot(toTarget, _direction) < 0f;

            if (arrived || overshot)
            {
                // 타겟이 살아있을 때만 데미지 적용
                if (_target != null && _target.IsAlive)
                {
                    _target.TakeDamage(_damage);
                }

                ReturnToPool();
            }
        }

        private void OnDisable()
        {
            // 풀 반환 시 상태 초기화 — 다음 재사용을 위해
            _target            = null;
            _sourcePrefab      = null;
            _remainingLifetime = 0f;
            _targetPosition    = Vector2.zero;
        }

        #endregion

        #region 풀 반환

        /// <summary>
        /// 오브젝트를 PoolManager에 반환합니다.
        /// PoolManager가 없는 경우 단순 비활성화로 폴백합니다.
        /// </summary>
        private void ReturnToPool()
        {
            if (PoolManager.Instance != null && _sourcePrefab != null)
            {
                PoolManager.Instance.Release(_sourcePrefab, gameObject);
            }
            else
            {
                gameObject.SetActive(false);
            }
        }

        #endregion
    }
}
