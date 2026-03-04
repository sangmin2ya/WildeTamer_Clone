using System;
using UnityEngine;

namespace WildTamer
{
    /// <summary>
    /// 모든 몬스터의 기반이 되는 추상 클래스입니다.
    /// IMovable, IFightable, ITameable, IDepthSortable을 구현하며
    /// 정지·이동·전투 3개 상태로 구성된 상태 머신을 내장합니다.
    ///
    /// 상태 스크립트는 감지·판단 후 이 클래스의 프리미티브(Move, UpdateFacing 등)를 호출합니다.
    /// 공격·테이밍 등 개체별로 다른 동작은 서브클래스에서 abstract 메소드로 구현합니다.
    /// </summary>
    [RequireComponent(typeof(Rigidbody2D))]
    [RequireComponent(typeof(Animator))]
    public abstract class Monster : MonoBehaviour, IMovable, IFightable, ITameable
    {
        #region SerializeField 필드

        [Header("데이터")]
        [SerializeField, Tooltip("몬스터 스탯 및 프리팹 데이터")]
        protected MonsterData monsterData;

        [Header("군중 참조")]
        [SerializeField, Tooltip("이 유닛이 소속된 스쿼드")]
        protected Squad squad;

        [Header("애니메이션")]
        [SerializeField, Tooltip("이동 애니메이션이 재생될 최소 속도 임계값")]
        protected float moveAnimThreshold = 0.1f;

        [Header("참조")]
        [SerializeField, Tooltip("flipX 및 sortingOrder에 사용할 SpriteRenderer")]
        protected SpriteRenderer spriteRenderer;

        #endregion

        #region Protected 필드

        protected Rigidbody2D _rb;
        protected Animator _animator;

        protected bool _wasMoving;
        protected bool _isFacingRight = true;

        // Animator 파라미터 해시 캐싱 — 모든 서브클래스에서 공용으로 사용
        protected static readonly int AnimIsMoving = Animator.StringToHash("IsMoving");
        protected static readonly int AnimAttack   = Animator.StringToHash("Attack");
        protected static readonly int AnimHit      = Animator.StringToHash("Hit");

        #endregion

        #region Private 필드

        private UnitState<Monster> _currentState;

        #endregion

        #region Protected 상태 필드

        protected MonsterIdleState _idleState;
        protected MonsterMoveState _moveState;
        protected MonsterCombatState _combatState;

        #endregion

        #region Public 프로퍼티

        public float CurrentHp { get; protected set; }
        public bool IsAlive => CurrentHp > 0f;

        public bool IsStunned { get; protected set; }
        public bool IsTamed { get; protected set; }

        /// <summary>소속 스쿼드 — 상태 스크립트에서 목표 위치 참조 등에 사용</summary>
        public Squad Squad => squad;

        /// <summary>몬스터 스탯 및 타입 데이터 — 상태 스크립트에서 attackRange 등 접근에 사용</summary>
        public MonsterData Data => monsterData;

        /// <summary>
        /// 체력이 변경될 때 발행됩니다. (현재 체력, 최대 체력)
        /// MonsterHpBar 등 UI 컴포넌트가 구독하여 표시를 갱신합니다.
        /// </summary>
        public event Action<float, float> OnHpChanged;

        #endregion

        #region Unity 메소드

        protected virtual void Awake()
        {
            _rb = GetComponent<Rigidbody2D>();
            _animator = GetComponent<Animator>();

            _idleState   = new MonsterIdleState(this);
            _moveState   = new MonsterMoveState(this);
            _combatState = new MonsterCombatState(this);
        }

        protected virtual void OnEnable()
        {
            DepthSorter.Register(spriteRenderer);
        }

        protected virtual void OnDisable()
        {
            DepthSorter.Unregister(spriteRenderer);
        }

        protected virtual void Start()
        {
            Initialize();
            ChangeState(_idleState);
        }

        protected virtual void FixedUpdate()
        {
            _currentState?.FixedUpdate();
        }

        protected virtual void Update()
        {
            _currentState?.Update();
        }

        #endregion

        #region 초기화

        /// <summary>
        /// 몬스터를 초기 상태로 설정합니다.
        /// 풀에서 꺼낼 때마다 호출하여 상태를 리셋할 수 있습니다.
        /// </summary>
        protected virtual void Initialize()
        {
            CurrentHp  = monsterData.stat.maxHp;
            IsStunned  = false;
            IsTamed    = false;
            InitRigidbody();
            OnHpChanged?.Invoke(CurrentHp, monsterData.stat.maxHp);
        }

        /// <summary>
        /// Rigidbody2D 물리 설정을 초기화합니다.
        /// </summary>
        protected virtual void InitRigidbody()
        {
            _rb.bodyType        = RigidbodyType2D.Dynamic;
            _rb.gravityScale    = 0f;
            _rb.mass            = monsterData.mass;
            _rb.angularDamping  = float.MaxValue;
            _rb.interpolation   = RigidbodyInterpolation2D.Interpolate;
            _rb.constraints     = RigidbodyConstraints2D.FreezeRotation;
        }
        
        /// <summary>
        /// 런타임에 소속 스쿼드를 설정합니다.
        /// GameManager가 스폰 직후 호출하여 Squad 참조를 주입합니다.
        /// </summary>
        /// <param name="newSquad">소속시킬 스쿼드</param>
        public void SetSquad(Squad newSquad)
        {
            squad = newSquad;
        }

        #endregion

        #region 상태 머신

        /// <summary>
        /// 상태를 전환합니다. 이전 상태의 Exit → 새 상태의 Enter 순서로 호출됩니다.
        /// </summary>
        /// <param name="newState">전환할 상태 인스턴스</param>
        public void ChangeState(UnitState<Monster> newState)
        {
            _currentState?.Exit();
            _currentState = newState;
            _currentState?.Enter();
        }

        /// <summary>
        /// 소속 군중의 상태가 변경될 때 Squad에 의해 호출됩니다.
        /// SquadState를 대응하는 MonsterState로 매핑하여 전환합니다.
        /// </summary>
        /// <param name="state">새로운 군중 상태</param>
        public virtual void OnSquadStateChanged(SquadState state)
        {
            switch (state)
            {
                case SquadState.정지:
                    ChangeState(_idleState);
                    break;
                case SquadState.이동:
                    ChangeState(_moveState);
                    break;
                case SquadState.전투:
                    ChangeState(_combatState);
                    break;
            }
        }

        #endregion

        #region IMovable

        /// <summary>
        /// 목표 위치 방향으로 velocity를 설정합니다.
        /// 상태 스크립트가 목적지를 계산해 호출하는 이동 프리미티브입니다.
        /// </summary>
        /// <param name="destination">이동할 목표 위치 (월드 좌표)</param>
        public void Move(Vector2 destination)
        {
            if (!IsAlive)
            {
                _rb.linearVelocity = Vector2.zero;
                return;
            }

            Vector2 direction = destination - _rb.position;

            if (direction.sqrMagnitude < 0.01f)
            {
                _rb.linearVelocity = Vector2.zero;
                return;
            }

            _rb.linearVelocity = direction.normalized * monsterData.stat.moveSpeed;
        }

        /// <summary>
        /// Rigidbody2D의 velocity를 즉시 0으로 멈춥니다.
        /// 정지 상태 진입 시 또는 도착 판정 시 호출합니다.
        /// </summary>
        public void StopMovement()
        {
            _rb.linearVelocity = Vector2.zero;
        }

        /// <summary>
        /// 이동 방향에 따라 스프라이트를 좌우 반전합니다.
        /// 서브클래스에서 override하여 다른 방향 기준(예: 플레이어 방향)을 사용할 수 있습니다.
        /// </summary>
        /// <param name="direction">이동 방향 벡터</param>
        public virtual void UpdateFacing(Vector2 direction)
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
        /// 상태가 바뀔 때만 Animator에 전달합니다.
        /// </summary>
        public void UpdateMoveAnimation()
        {
            bool isMoving = _rb.linearVelocity.sqrMagnitude > moveAnimThreshold * moveAnimThreshold;

            if (isMoving == _wasMoving)
            {
                return;
            }

            _wasMoving = isMoving;
            _animator.SetBool(AnimIsMoving, isMoving);
        }

        #endregion

        #region IFightable

        /// <inheritdoc/>
        public Transform Transform => transform;

        /// <inheritdoc/>
        public abstract void Attack(IFightable target);

        /// <inheritdoc/>
        public virtual void TakeDamage(float damage)
        {
            if (!IsAlive)
            {
                return;
            }

            CurrentHp -= damage;
            OnHpChanged?.Invoke(CurrentHp, monsterData.stat.maxHp);
            PlayHitAnimation();

            if (!IsAlive)
            {
                Die();
            }
        }

        /// <inheritdoc/>
        public abstract void Die();

        /// <summary>
        /// 공격 애니메이션을 재생합니다.
        /// </summary>
        public virtual void PlayAttackAnimation()
        {
            _animator.SetTrigger(AnimAttack);
        }

        /// <summary>
        /// 피격 애니메이션을 재생합니다.
        /// </summary>
        public virtual void PlayHitAnimation()
        {
            _animator.SetTrigger(AnimHit);
        }

        #endregion

        #region ITameable

        /// <inheritdoc/>
        public abstract void Stun();

        /// <inheritdoc/>
        public abstract void Tame();

        #endregion
    }
}
