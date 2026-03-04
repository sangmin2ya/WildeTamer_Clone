using System;
using UnityEngine;

namespace WildTamer
{
    /// <summary>
    /// 모든 몬스터의 기반이 되는 추상 클래스입니다.
    /// IMovable, IFightable, ITameable을 구현하며
    /// 정지·이동·전투·기절 4개 상태로 구성된 상태 머신을 내장합니다.
    ///
    /// 상태 스크립트는 감지·판단 후 이 클래스의 프리미티브(Move, UpdateFacing 등)를 호출합니다.
    /// 기절 시 stunChance 판정으로 MonsterStunState로 전환되고,
    /// TameController(선택)가 테이밍 UI를 표시합니다.
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
        [SerializeField, Tooltip("flipX에 사용할 SpriteRenderer")]
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
        protected MonsterStunState _stunState;

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
            _stunState   = new MonsterStunState(this);
        }

        protected virtual void OnEnable()
        {
            // 풀 재사용 시에도 상태를 초기화합니다.
            // Start()는 최초 1회만 실행되므로 OnEnable에서 리셋을 보장합니다.
            if (monsterData != null)
            {
                Initialize();
                ChangeState(_idleState);
            }
        }

        protected virtual void Start()
        {
            // 초기화는 OnEnable에서 처리됩니다.
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
            _rb.simulated       = true;                             // 기절 해제 후 물리 복원
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
            // 기절 중에는 Squad 상태 전이를 무시합니다.
            if (IsStunned)
            {
                return;
            }

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
            // 사망·기절 상태에서는 추가 데미지 무시
            if (!IsAlive || IsStunned)
            {
                return;
            }

            CurrentHp -= damage;
            OnHpChanged?.Invoke(CurrentHp, monsterData.stat.maxHp);
            PlayHitAnimation();

            if (CurrentHp <= 0f)
            {
                CurrentHp = 0f;

                // stunChance 판정: 통과하면 기절(테이밍 대상), 실패하면 즉시 사망 / 적 스쿼드에만 적용
                if (UnityEngine.Random.value <= monsterData.stunChance &&  squad.Type == SquadType.적)
                {
                    Stun();
                }
                else
                {
                    Die();
                }
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

        /// <summary>
        /// 유닛을 기절 상태로 전환합니다.
        /// TakeDamage에서 HP 0 도달 후 stunChance 판정 시 호출됩니다.
        /// </summary>
        public virtual void Stun()
        {
            IsStunned = true;
            // 물리 시뮬레이션 비활성화 — 제자리 고정, 다른 유닛과의 충돌 제거
            _rb.linearVelocity = Vector2.zero;
            _rb.simulated      = false;
            ChangeState(_stunState);
        }

        /// <summary>
        /// 기절한 적 유닛을 아군으로 테이밍합니다.
        /// 아군 프리팹을 풀에서 꺼내 플레이어 스쿼드에 편입하고, 자신은 풀에 반환합니다.
        /// TameController의 "길들이기" 버튼 콜백에서 호출됩니다.
        /// </summary>
        public virtual void Tame()
        {
            if (!IsStunned)
            {
                return;
            }

            IsTamed = true;
            IsStunned = false;

            // 플레이어 스쿼드를 정적 레지스트리에서 탐색
            Squad playerSquad = Squad.GetPlayerSquad();

            if (playerSquad != null)
            {
                if (playerSquad.IsFull)
                {
                    return;
                }

                // 아군 프리팹을 풀에서 꺼내 동일 위치에 배치
                GameObject allyObj = PoolManager.Instance.GetAlly(monsterData);
                allyObj.transform.position = transform.position;

                Monster allyMonster = allyObj.GetComponent<Monster>();

                if (allyMonster != null)
                {
                    allyMonster.SetSquad(playerSquad);
                    playerSquad.AddMember(allyMonster);
                }
            }

            // 자신을 적 스쿼드에서 제거하고 풀에 반환
            squad?.RemoveMember(this);
            PoolManager.Instance.ReleaseEnemy(monsterData, gameObject);
        }

        /// <summary>
        /// 기절한 적 유닛을 수확하여 재화를 획득합니다.
        /// TameController의 "수확하기" 버튼 콜백에서 호출됩니다.
        /// </summary>
        public virtual void Root()
        {
            if (!IsStunned)
            {
                return;
            }

            // TODO: ResourceManager 연동 — 몬스터 종류·등급에 따른 재화 지급
            // ResourceManager.Instance.Add(monsterData.rootReward);

            // 자신을 적 스쿼드에서 제거하고 풀에 반환
            squad?.RemoveMember(this);
            PoolManager.Instance.ReleaseEnemy(monsterData, gameObject);
        }

        #endregion
    }
}
