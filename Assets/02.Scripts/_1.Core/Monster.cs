using UnityEngine;

namespace WildTamer
{
    /// <summary>
    /// 모든 몬스터의 기반이 되는 추상 클래스입니다.
    /// IMovable, IFightable, ITameable, IDepthSortable을 구현하며
    /// 공통 필드와 초기화·피격·깊이정렬 로직을 제공합니다.
    /// 구체적인 이동, 공격, 테이밍 동작은 서브클래스에서 구현합니다.
    /// </summary>
    [RequireComponent(typeof(Rigidbody2D))]
    [RequireComponent(typeof(Animator))]
    public abstract class Monster : MonoBehaviour, IMovable, IFightable, ITameable, IDepthSortable
    {
        #region SerializeField 필드

        [Header("데이터")]
        [SerializeField, Tooltip("몬스터 스탯 및 프리팹 데이터")]
        protected MonsterData monsterData;

        [Header("애니메이션")]
        [SerializeField, Tooltip("이동 애니메이션이 재생될 최소 속도")]
        protected float moveAnimThreshold = 0.1f;

        [Header("참조")]
        [SerializeField, Tooltip("flipX 및 sortingOrder에 사용할 SpriteRenderer")]
        protected SpriteRenderer spriteRenderer;

        #endregion

        #region Protected 필드

        protected Rigidbody2D _rb;
        protected Animator _animator;
        protected Transform _playerTransform;

        protected bool _wasMoving;
        protected bool _isFacingRight = true;

        #endregion

        #region Public 필드 및 프로퍼티

        public float CurrentHp { get; protected set; }
        public bool IsAlive => CurrentHp > 0f;

        public bool IsStunned { get; protected set; }
        public bool IsTamed { get; protected set; }

        #endregion

        #region Unity 메소드

        protected virtual void Awake()
        {
            _rb = GetComponent<Rigidbody2D>();
            _animator = GetComponent<Animator>();

            // "Player" 태그의 오브젝트를 캐싱합니다.
            // 플레이어가 씬에 사전 배치된 경우 Awake 시점에 올바르게 동작합니다.
            GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
            if (playerObj != null)
            {
                _playerTransform = playerObj.transform;
            }
        }

        protected virtual void Start()
        {
            Initialize();
        }

        protected virtual void FixedUpdate()
        {
            Move();
        }

        protected virtual void Update()
        {
            UpdateMoveAnimation();
            UpdateSortingOrder();
        }

        #endregion

        #region 초기화

        /// <summary>
        /// 몬스터를 초기 상태로 설정합니다.
        /// 풀에서 꺼낼 때마다 호출하여 상태를 리셋할 수 있습니다.
        /// </summary>
        protected virtual void Initialize()
        {
            CurrentHp = monsterData.stat.maxHp;
            IsStunned = false;
            IsTamed = false;
            InitRigidbody();
        }

        /// <summary>
        /// Rigidbody2D 물리 설정을 초기화합니다.
        /// </summary>
        protected virtual void InitRigidbody()
        {
            _rb.bodyType = RigidbodyType2D.Dynamic;
            _rb.gravityScale = 0f;
            _rb.mass = monsterData.mass;
            _rb.angularDamping = float.MaxValue;
            _rb.constraints = RigidbodyConstraints2D.FreezeRotation;
        }

        #endregion

        #region IMovable

        /// <inheritdoc/>
        public abstract void Move();

        /// <inheritdoc/>
        public abstract void UpdateFacing(Vector2 direction);

        /// <inheritdoc/>
        public abstract void UpdateMoveAnimation();

        #endregion

        #region IFightable

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
            PlayHitAnimation();

            if (!IsAlive)
            {
                Die();
            }
        }

        /// <inheritdoc/>
        public abstract void Die();

        /// <inheritdoc/>
        public abstract void PlayAttackAnimation();

        /// <inheritdoc/>
        public abstract void PlayHitAnimation();

        #endregion

        #region ITameable

        /// <inheritdoc/>
        public abstract void Stun();

        /// <inheritdoc/>
        public abstract void Tame();

        /// <inheritdoc/>
        public abstract void Dismantle();

        #endregion

        #region IDepthSortable

        /// <summary>
        /// 현재 Y 위치를 기반으로 sortingOrder를 갱신합니다.
        /// 플레이어와의 거리가 sortUpdateDistance를 초과하면 업데이트를 건너뜁니다.
        /// </summary>
        public virtual void UpdateSortingOrder()
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

            // 플레이어 기준 상대 Y: 오브젝트가 플레이어보다 아래(낮은 Y)면 양수(앞에 렌더링)
            int order = Mathf.Clamp(
                Mathf.RoundToInt(_playerTransform.position.y - transform.position.y),
                IDepthSortable.MinSortingOrder,
                IDepthSortable.MaxSortingOrder
            );

            // 플레이어와 Y가 같으면 스킵
            if (order == 0)
            {
                return;
            }

            spriteRenderer.sortingOrder = order;
        }

        #endregion

        #region 상태 관리

        /// <summary>
        /// 소속 군중의 상태가 변경될 때 Squad에 의해 호출됩니다.
        /// 이동/전투 전환에 따른 개체별 동작을 서브클래스에서 구현합니다.
        /// </summary>
        /// <param name="state">새로운 군중 상태</param>
        public abstract void OnSquadStateChanged(SquadState state);

        #endregion
    }
}
