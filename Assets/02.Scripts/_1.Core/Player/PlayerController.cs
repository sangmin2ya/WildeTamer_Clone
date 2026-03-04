using UnityEngine;

namespace WildTamer
{
    /// <summary>
    /// 플레이어를 제어하는 컨트롤러입니다.
    /// 정지·이동·전투 3개 상태를 가진 상태 머신을 내장하며,
    /// 이동 입력(IsMoving)이 감지되면 무조건 이동 상태로 전환합니다.
    /// </summary>
    [RequireComponent(typeof(Rigidbody2D))]
    [RequireComponent(typeof(Animator))]
    [RequireComponent(typeof(PlayerInputController))]
    public class PlayerController : MonoBehaviour, IMovable, IDepthSortable
    {
        #region SerializeField 필드

        [Header("데이터")]
        [SerializeField, Tooltip("플레이어 스탯 데이터")]
        private PlayerData playerData;

        [Header("군중 참조")]
        [SerializeField, Tooltip("플레이어가 이끄는 아군 스쿼드")]
        private Squad squad;

        [SerializeField, Tooltip("스쿼드가 플레이어 뒤에서 따라올 거리 (미터)")]
        private float squadFollowOffset = 1.5f;

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

        // 마지막 이동 방향 — 정지 시에도 스쿼드 위치 유지에 사용
        private Vector2 _lastMoveDir = Vector2.down;

        private UnitState<PlayerController> _currentState;

        private static readonly int IsMovingHash = Animator.StringToHash("IsMoving");

        #endregion

        #region Private 상태 필드

        private PlayerIdleState _idleState;
        private PlayerMoveState _moveState;
        private PlayerCombatState _combatState;

        #endregion

        #region Public 프로퍼티

        /// <summary>현재 오른쪽을 바라보고 있는지 여부</summary>
        public bool IsFacingRight => _isFacingRight;

        /// <summary>입력 컴포넌트 — 상태 스크립트에서 MoveInput 참조용</summary>
        public PlayerInputController Input => _input;

        /// <summary>소속 스쿼드 — 상태 진입 시 ForceStop/ForceRetreat 호출용</summary>
        public Squad Squad => squad;

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

            _idleState = new PlayerIdleState(this);
            _moveState = new PlayerMoveState(this);
            _combatState = new PlayerCombatState(this);
        }

        private void Start()
        {
            ChangeState(_idleState);
        }

        private void FixedUpdate()
        {
            _currentState?.FixedUpdate();
        }

        private void Update()
        {
            // 이동 입력 최고 우선순위 — IsMoving이면 무조건 이동 상태로 전환
            if (_input.IsMoving)
            {
                if (_currentState != _moveState)
                {
                    ChangeState(_moveState);
                }
            }
            else if (_currentState == _moveState)
            {
                ChangeState(_idleState);
            }

            // 이동 방향 갱신 (입력이 있을 때만 갱신하여 마지막 방향을 유지)
            if (_input.MoveInput.sqrMagnitude > 0.01f)
            {
                _lastMoveDir = _input.MoveInput.normalized;
            }

            _currentState?.Update();
            UpdateSortingOrder();
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
            _rb.interpolation = RigidbodyInterpolation2D.Interpolate;
            _rb.constraints = RigidbodyConstraints2D.FreezeRotation;
        }

        #endregion

        #region 스쿼드 위치

        /// <summary>
        /// 스쿼드의 위치를 플레이어 이동 방향 기준 뒤쪽으로 갱신합니다.
        /// 플레이어가 정지 중일 때는 마지막 이동 방향 기준을 유지합니다.
        /// </summary>
        private void UpdateSquadPosition()
        {
            if (squad == null)
            {
                return;
            }

            squad.transform.position = (Vector2)transform.position - _lastMoveDir * squadFollowOffset;
        }

        #endregion

        #region 상태 머신

        /// <summary>
        /// 상태를 전환합니다. 이전 상태의 Exit → 새 상태의 Enter 순서로 호출됩니다.
        /// </summary>
        /// <param name="newState">전환할 상태 인스턴스</param>
        public void ChangeState(UnitState<PlayerController> newState)
        {
            _currentState?.Exit();
            _currentState = newState;
            _currentState?.Enter();

            switch (newState)
            {
                case PlayerIdleState:
                    squad.ForceStop();
                    break;
                case PlayerMoveState:
                    squad.ForceMove();
                    break;
                case PlayerCombatState:
                    squad.ForceEnterCombat();
                    break;
            }
        }

        #endregion

        #region IMovable

        /// <inheritdoc/>
        public void Move(Vector2 moveInput)
        {
            _rb.linearVelocity = moveInput * playerData.moveSpeed;
            UpdateSquadPosition();
        }

        /// <summary>
        /// Rigidbody2D의 velocity를 즉시 0으로 멈춥니다.
        /// </summary>
        public void StopMovement()
        {
            _rb.linearVelocity = Vector2.zero;
        }

        /// <inheritdoc/>
        public void UpdateFacing(Vector2 direction)
        {
            if (spriteRenderer == null)
            {
                return;
            }

            if (direction.x > 0.01f && !_isFacingRight)
            {
                spriteRenderer.flipX = false;
                _isFacingRight = true;
            }
            else if (direction.x < -0.01f && _isFacingRight)
            {
                spriteRenderer.flipX = true;
                _isFacingRight = false;
            }
        }

        /// <inheritdoc/>
        public void UpdateMoveAnimation()
        {
            bool isMoving = _input.IsMoving;

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
        /// 인터페이스 구현용 메소드이며 런타임에 호출되지 않습니다.
        /// </summary>
        public void UpdateSortingOrder() { }

        #endregion
    }
}
