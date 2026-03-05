using System;
using UnityEngine;

namespace WildTamer
{
    /// <summary>
    /// 플레이어를 제어하는 단일 컨트롤러입니다.
    /// 상태 머신 없이 이동과 전투를 독립적으로 처리하여 동시 동작을 지원합니다.
    ///
    /// FixedUpdate — 이동(물리) + 공격(쿨타임)
    /// Update      — 스쿼드 이동 동기화 + Facing + 애니메이션
    ///
    /// Facing 우선순위: 이동 중 → 이동 방향 / 정지 중 → 공격 타겟 방향
    /// </summary>
    [RequireComponent(typeof(Rigidbody2D))]
    [RequireComponent(typeof(Animator))]
    [RequireComponent(typeof(PlayerInputController))]
    public class PlayerController : MonoBehaviour, IMovable, IFightable
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
        [SerializeField, Tooltip("flipX에 사용할 SpriteRenderer")]
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

        // 공격 쿨타임 카운터
        private float _attackTimer;

        // 현재 공격 타겟 — FixedUpdate마다 갱신
        private IFightable _attackTarget;

        // 스쿼드 이동 상태 추적 — 변경 시점에만 ForceMove/ForceStop 호출
        private bool _wasMovingForSquad;

        private static readonly int IsMovingHash = Animator.StringToHash("IsMoving");
        private static readonly int AttackHash   = Animator.StringToHash("Attack");
        private static readonly int HitHash      = Animator.StringToHash("Hit");

        #endregion

        #region Public 프로퍼티

        /// <summary>현재 오른쪽을 바라보고 있는지 여부</summary>
        public bool IsFacingRight => _isFacingRight;

        /// <summary>입력 컴포넌트</summary>
        public PlayerInputController Input => _input;

        /// <summary>소속 스쿼드</summary>
        public Squad Squad => squad;

        // ── IFightable ──────────────────────────────────────────

        /// <inheritdoc/>
        public Transform Transform => transform;

        /// <inheritdoc/>
        public float CurrentHp { get; private set; }

        /// <inheritdoc/>
        public bool IsAlive => CurrentHp > 0f;

        /// <inheritdoc/>
        public event Action<float, float> OnHpChanged;

        #endregion

        #region Unity 메소드

        private void Awake()
        {
            _rb    = GetComponent<Rigidbody2D>();
            _animator = GetComponent<Animator>();
            _input = GetComponent<PlayerInputController>();

            InitRigidbody();
        }

        private void Start()
        {
            CurrentHp = playerData.maxHp;
            OnHpChanged?.Invoke(CurrentHp, playerData.maxHp);

            squad?.SetLeader(this);

            // 아군 스쿼드 멤버 이동 속도를 플레이어 속도로 동기화
            if (squad != null)
            {
                squad.MoveSpeed = playerData.moveSpeed;
            }
        }

        private void FixedUpdate()
        {
            HandleMovement();
            HandleAttack();
        }

        private void Update()
        {
            SyncSquadMoveState();
            HandleFacing();
            UpdateMoveAnimation();

            // 이동 방향 갱신 (입력이 있을 때만 갱신하여 마지막 방향을 유지)
            if (_input.MoveInput.sqrMagnitude > 0.01f)
            {
                _lastMoveDir = _input.MoveInput.normalized;
            }
        }

        #endregion

        #region 초기화

        /// <summary>
        /// Rigidbody2D 물리 설정을 초기화합니다.
        /// </summary>
        private void InitRigidbody()
        {
            _rb.bodyType       = RigidbodyType2D.Dynamic;
            _rb.gravityScale   = 0f;
            _rb.mass           = playerData.mass;
            _rb.angularDamping = float.MaxValue;
            _rb.interpolation  = RigidbodyInterpolation2D.Interpolate;
            _rb.constraints    = RigidbodyConstraints2D.FreezeRotation;
        }

        #endregion

        #region 이동

        /// <summary>
        /// 입력에 따라 velocity를 적용하거나 정지합니다.
        /// </summary>
        private void HandleMovement()
        {
            if (_input.IsMoving)
            {
                Move(_input.MoveInput);
            }
            else
            {
                StopMovement();
            }
        }

        #endregion

        #region 공격

        /// <summary>
        /// 사정거리 내 가장 가까운 적을 탐색하고 쿨타임마다 자동 공격합니다.
        /// 이동 중에도 동작합니다.
        /// </summary>
        private void HandleAttack()
        {
            _attackTimer -= Time.fixedDeltaTime;

            _attackTarget = FindNearestEnemyInRange();

            if (_attackTarget != null && _attackTimer <= 0f)
            {
                Attack(_attackTarget);
                _attackTimer = playerData.attackCooldown;
            }
        }

        /// <summary>
        /// Squad 감지 목록에서 공격 사정거리 내 가장 가까운 적을 반환합니다.
        /// 사정거리 밖이거나 대상이 없으면 null을 반환합니다.
        /// </summary>
        private IFightable FindNearestEnemyInRange()
        {
            if (squad == null)
            {
                return null;
            }

            IFightable nearest = squad.GetNearestEnemyTarget(transform.position);

            if (nearest == null)
            {
                return null;
            }

            float attackRangeSqr = playerData.attackRange * playerData.attackRange;
            float distSqr = ((Vector2)nearest.Transform.position - (Vector2)transform.position).sqrMagnitude;

            return distSqr <= attackRangeSqr ? nearest : null;
        }

        #endregion

        #region 스쿼드 동기화

        /// <summary>
        /// 이동 상태가 바뀔 때만 ForceMove / ForceStop을 호출합니다.
        /// 매 프레임 중복 호출을 방지합니다.
        /// </summary>
        private void SyncSquadMoveState()
        {
            bool isMovingNow = _input.IsMoving;

            if (isMovingNow == _wasMovingForSquad)
            {
                return;
            }

            _wasMovingForSquad = isMovingNow;

            if (isMovingNow)
            {
                squad?.ForceMove();
            }
            else
            {
                squad?.ForceStop();
            }
        }

        /// <summary>
        /// 스쿼드 위치를 플레이어 이동 방향 기준 뒤쪽으로 갱신합니다.
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

        #region Facing

        /// <summary>
        /// 이동 중에는 이동 방향으로, 정지 중 공격 타겟이 있으면 타겟 방향으로 facing을 갱신합니다.
        /// </summary>
        private void HandleFacing()
        {
            if (_input.IsMoving)
            {
                UpdateFacing(_input.MoveInput);
                squad?.ForceFaceDirection(_input.MoveInput);
            }
            else if (_attackTarget != null)
            {
                Vector2 toTarget = (Vector2)_attackTarget.Transform.position - (Vector2)transform.position;
                UpdateFacing(toTarget);
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
        /// Rigidbody2D velocity를 즉시 0으로 멈춥니다.
        /// </summary>
        public void StopMovement()
        {
            _rb.linearVelocity = Vector2.zero;
        }

        /// <summary>
        /// 이동 방향에 따라 스프라이트를 좌우 반전합니다.
        /// </summary>
        /// <param name="direction">이동 방향 벡터</param>
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

        /// <summary>
        /// 실제 이동 속도를 기반으로 이동 애니메이션 파라미터를 갱신합니다.
        /// </summary>
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

        #region IFightable

        /// <inheritdoc/>
        public void Attack(IFightable target)
        {
            PlayAttackAnimation();
            target.TakeDamage(playerData.attackDamage);
        }

        /// <inheritdoc/>
        public void TakeDamage(float damage)
        {
            if (!IsAlive)
            {
                return;
            }

            CurrentHp -= damage;
            OnHpChanged?.Invoke(CurrentHp, playerData.maxHp);
            PlayHitAnimation();

            if (!IsAlive)
            {
                Die();
            }
        }

        /// <inheritdoc/>
        public void Die()
        {
            StopMovement();
            // TODO: 게임오버 처리 (GameManager 연동)
        }

        /// <summary>
        /// 공격 애니메이션을 재생합니다.
        /// </summary>
        public void PlayAttackAnimation()
        {
            _animator.SetTrigger(AttackHash);
        }

        /// <summary>
        /// 피격 애니메이션을 재생합니다.
        /// </summary>
        public void PlayHitAnimation()
        {
            _animator.SetTrigger(HitHash);
        }

        #endregion
    }
}
