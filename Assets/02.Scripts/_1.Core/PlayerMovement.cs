using UnityEngine;

namespace WildTamer
{
    /// <summary>
    /// 플레이어 이동을 처리하는 컨트롤러
    /// </summary>
    [RequireComponent(typeof(Rigidbody2D))]
    [RequireComponent(typeof(Animator))]
    [RequireComponent(typeof(PlayerInputController))]
    public class PlayerMovement : MonoBehaviour, IMovable, IDepthSortable
    {
        #region SerializeField 필드

        [Header("데이터")]
        [SerializeField, Tooltip("플레이어 스탯 데이터")]
        private PlayerData playerData;

        [Header("애니메이션")]
        [SerializeField, Tooltip("이동 애니메이션이 재생될 최소 속도")]
        private float moveAnimThreshold = 0.1f;

        [Header("참조")]
        [SerializeField, Tooltip("flipX 및 sortingOrder에 사용할 SpriteRenderer")]
        private SpriteRenderer spriteRenderer;

        #endregion

        #region Private 필드

        private Rigidbody2D _rb;
        private Animator _animator;
        private PlayerInputController _input;

        private bool _wasMoving;
        private bool _isFacingRight = true;

        private static readonly int IsMovingHash = Animator.StringToHash("IsMoving");
        private static readonly int AttackTriggerHash = Animator.StringToHash("AttackTrigger");

        #endregion

        #region Unity 메소드

        private void Awake()
        {
            _rb = GetComponent<Rigidbody2D>();
            _animator = GetComponent<Animator>();
            _input = GetComponent<PlayerInputController>();

            // 플레이어는 깊이 정렬 기준값 — 항상 0 고정
            spriteRenderer.sortingOrder = 0;

            InitRigidbody();
        }

        private void FixedUpdate()
        {
            Move();
        }

        private void Update()
        {
            UpdateMoveAnimation();
        }

        #endregion

        #region 초기화

        /// <summary>
        /// Rigidbody2D의 물리 설정을 초기화합니다.
        /// </summary>
        private void InitRigidbody()
        {
            _rb.bodyType = RigidbodyType2D.Dynamic;
            _rb.gravityScale = 0f;
            _rb.mass = playerData.mass;
            _rb.angularDamping = float.MaxValue;
            _rb.constraints = RigidbodyConstraints2D.FreezeRotation;
        }

        #endregion

        #region IMovable

        /// <inheritdoc/>
        public void Move()
        {
            if (!_input.IsMoving)
            {
                _rb.linearVelocity = Vector2.zero;
                return;
            }

            Vector2 dir = _input.MoveInput;
            _rb.linearVelocity = dir * playerData.moveSpeed;
        }

        /// <inheritdoc/>
        public void UpdateFacing(Vector2 direction)
        {
            if (direction.x == 0f)
            {
                return;
            }

            bool facingRight = direction.x > 0f;

            if (_isFacingRight == facingRight)
            {
                return;
            }

            _isFacingRight = facingRight;
            spriteRenderer.flipX = _isFacingRight;
        }

        /// <inheritdoc/>
        public void UpdateMoveAnimation()
        {
            bool isMoving = _rb.linearVelocity.sqrMagnitude > moveAnimThreshold * moveAnimThreshold;
            UpdateFacing(_rb.linearVelocity);

            if (_wasMoving == isMoving)
            {
                return;
            }

            _wasMoving = isMoving;
            _animator.SetBool(IsMovingHash, _wasMoving);
        }

        #endregion

        #region IDepthSortable

        /// <summary>
        /// 플레이어는 깊이 정렬 기준(0)으로 Awake에서 고정됩니다.
        /// 인터페이스 구현용 메서드이며 런타임에 호출되지 않습니다.
        /// </summary>
        public void UpdateSortingOrder() { }

        #endregion
    }
}
