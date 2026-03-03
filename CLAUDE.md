# WildTamerClone 프로젝트 가이드

## 프로젝트 정보
- **언어**: C#
- **플랫폼**: Unity 6000.3.10f1 2D 프로젝트
- **응답 언어**: 한국어

---

## 코드 컨벤션

### 네이밍 규칙

| 대상 | 규칙 | 예시 |
|------|------|------|
| 클래스, 인터페이스, 메소드 | PascalCase | `GameManager`, `PlayerController` |
| 인터페이스 | I 접두사 + 형용사 | `IInteractable`, `IDamageable` |
| private 필드 | _camelCase | `_playerHealth`, `_moveSpeed` |
| [SerializeField] private 필드 | camelCase (언더스코어 없음) | `moveSpeed`, `targetObject` |
| public 필드 | camelCase | `currentHealth` |
| public static 필드 | PascalCase | `Instance`, `MaxHealth` |
| private static 필드 | camelCase | `instance`, `maxCount` |
| 프로퍼티 | PascalCase | `Health`, `IsAlive` |
| 열거형 (enum) 이름 | PascalCase (영어) | `ObjectType`, `PlantState` |
| 열거형 (enum) 값 | **한글로 작성** | `ObjectType.길`, `PlantState.싱싱함` |
| 클래스 멤버변수 | 영어만 사용 (한글 금지) | `_currentState`, `moveSpeed` |

### 코드 스타일

#### 중괄호
- 모든 `if`, `for`, `while`, `foreach` 등에 중괄호 필수 사용
- 중괄호는 **새 줄에서 시작** (Allman 스타일)

```csharp
if (condition)
{
    DoSomething();
}
```

#### 접근 제한자
- **생략하지 않고 명시적으로 작성**

```csharp
private int _count;      // O
int _count;              // X (생략 금지)
```

### Inspector 어트리뷰트 규칙

#### Header와 Tooltip
- `[SerializeField]` 또는 `public` 필드는 Inspector에서 구분되도록 **한글로** `[Header]`와 `[Tooltip]` 작성
- `[Header]`는 카테고리별로 묶어서 하나만 작성
- `[Tooltip]`은 필드의 용도를 설명

```csharp
[Header("이동 설정")]
[SerializeField, Tooltip("캐릭터 이동 속도")]
private float moveSpeed = 5f;

[SerializeField, Tooltip("최대 이동 속도")]
private float maxSpeed = 10f;

[Header("참조")]
[SerializeField, Tooltip("타겟 트랜스폼")]
private Transform targetTransform;
```

### 필드 정렬 순서

```csharp
public class ExampleClass : MonoBehaviour
{
    #region SerializeField 필드

    [Header("설정")]
    [SerializeField, Tooltip("이동 속도")]
    private float moveSpeed;

    [SerializeField, Tooltip("대상 오브젝트")]
    private GameObject targetObject;

    #endregion

    #region Private 필드

    private int _currentIndex;
    private bool _isActive;

    #endregion

    #region Public 필드 및 프로퍼티

    public int CurrentHealth { get; private set; }
    public bool IsAlive => CurrentHealth > 0;

    #endregion
}
```

### 주석 규칙

#### 함수 주석
- `<summary>` 태그를 사용하여 **한글로 작성**
- Unity 고유 메소드 (`Awake`, `Start`, `Update`, `OnEnable` 등)는 주석 **작성하지 않음**

```csharp
/// <summary>
/// 플레이어에게 데미지를 적용하고 체력을 감소시킵니다.
/// </summary>
/// <param name="damage">적용할 데미지 양</param>
public void TakeDamage(int damage)
{
    _currentHealth -= damage;
}

// Unity 메소드는 주석 없이
private void Start()
{
    Initialize();
}
```

### Region 사용
- 기능별로 `#region` 지시문을 사용하여 구분
- **한글로 작성**

```csharp
#region 초기화

private void Initialize()
{
    // ...
}

#endregion

#region 이동 관련

private void Move()
{
    // ...
}

#endregion
```

---

## 코드 템플릿 예시

```csharp
using UnityEngine;

namespace WildTamer
{
    /// <summary>
    /// 클래스에 대한 설명을 작성합니다.
    /// </summary>
    public class ExampleClass : MonoBehaviour
    {
        #region SerializeField 필드

        [Header("이동 설정")]
        [SerializeField, Tooltip("캐릭터 이동 속도")]
        private float moveSpeed = 5f;

        [Header("참조")]
        [SerializeField, Tooltip("이동 목표 트랜스폼")]
        private Transform targetTransform;

        #endregion

        #region Private 필드

        private bool _isInitialized;
        private int _currentScore;

        #endregion

        #region Public 필드 및 프로퍼티

        public int Score => _currentScore;
        public bool IsReady { get; private set; }

        #endregion

        #region Unity 메소드

        private void Awake()
        {
            _isInitialized = false;
        }

        private void Start()
        {
            Initialize();
        }

        private void Update()
        {
            if (_isInitialized)
            {
                UpdateMovement();
            }
        }

        #endregion

        #region 초기화

        /// <summary>
        /// 컴포넌트를 초기화합니다.
        /// </summary>
        private void Initialize()
        {
            _isInitialized = true;
            IsReady = true;
        }

        #endregion

        #region 이동 관련

        /// <summary>
        /// 매 프레임 이동을 처리합니다.
        /// </summary>
        private void UpdateMovement()
        {
            if (targetTransform != null)
            {
                Vector3 direction = (targetTransform.position - transform.position).normalized;
                transform.position += direction * moveSpeed * Time.deltaTime;
            }
        }

        #endregion
    }
}
```

---

## 성능 최적화 가이드

### 컴포넌트 참조 캐싱

- `GetComponent`, `Find`, `FindObjectOfType` 등은 **비용이 큰 연산**이므로 `Update`에서 절대 호출 금지
- 반드시 `Awake` 또는 `Start`에서 한 번만 호출하여 **private 필드에 캐싱**
- `Camera.main`도 내부적으로 `FindObjectOfType` 방식이므로 마찬가지로 캐싱 필수

```csharp
// ❌ 금지
private void Update()
{
    GetComponent<Rigidbody2D>().velocity = Vector2.zero;
    GameObject.Find("Player").transform.position;
    Camera.main.ScreenToWorldPoint(Input.mousePosition);
}

// ✅ 올바른 방법
private Rigidbody2D _rigidbody;
private Transform _playerTransform;
private Camera _mainCamera;

private void Awake()
{
    _rigidbody = GetComponent<Rigidbody2D>();
    _playerTransform = GameObject.Find("Player").transform;
    _mainCamera = Camera.main;
}
```

---

### Update 호출 최소화

- **매 프레임 실행이 불필요한 로직**은 `Update` 밖으로 분리
- 주기적 처리는 코루틴(`WaitForSeconds`) 또는 이벤트 기반으로 대체
- 조건부 로직은 상태 변경 시점에만 처리하고, 상태가 없을 땐 비활성화 고려

```csharp
// ❌ 금지: 매 프레임 불필요한 연산
private void Update()
{
    _hpBarFill = _currentHp / _maxHp; // 매 프레임 계산
    UpdateUI();
}

// ✅ 올바른 방법: 값이 바뀔 때만 호출
public void TakeDamage(int damage)
{
    _currentHp -= damage;
    UpdateUI(); // 변경 시점에만 실행
}

// ✅ 주기적 처리는 코루틴 사용
private IEnumerator RegenerateHp()
{
    WaitForSeconds wait = new WaitForSeconds(1f); // WaitForSeconds 캐싱
    while (_currentHp < _maxHp)
    {
        _currentHp += _regenAmount;
        yield return wait;
    }
}
```

---

### 오브젝트 풀링

- **총알, 이펙트, 적 등 반복적으로 생성/파괴되는 오브젝트**는 반드시 풀링 사용
- `Instantiate` / `Destroy`의 잦은 호출은 GC Allocation과 스파이크를 유발
- Unity 2021+ 내장 `ObjectPool<T>` 활용 권장

```csharp
// ✅ ObjectPool 사용 예시
private ObjectPool<GameObject> _bulletPool;

private void Awake()
{
    _bulletPool = new ObjectPool<GameObject>(
        createFunc: () => Instantiate(bulletPrefab),
        actionOnGet: obj => obj.SetActive(true),
        actionOnRelease: obj => obj.SetActive(false),
        actionOnDestroy: obj => Destroy(obj),
        defaultCapacity: 20,
        maxSize: 100
    );
}

private void Fire()
{
    GameObject bullet = _bulletPool.Get();
    // 사용 후 반납: _bulletPool.Release(bullet);
}
```

---

### 메모리 관리 (struct vs class)

- **소규모 데이터 묶음** (위치, 색상, 스탯 등)은 `struct` 사용으로 힙 할당 방지
- 단, `struct`는 복사 비용이 있으므로 **8~16바이트 이하**의 단순 값 타입에만 사용
- 상속이나 다형성이 필요하거나 자주 참조로 전달되는 경우는 `class` 사용

```csharp
// ✅ 적합한 struct 사용 예시
public struct DamageInfo
{
    public int Amount;
    public bool IsCritical;
    public Vector2 HitPoint;
}

// ❌ 부적합한 struct 사용 (너무 크거나 참조 전달이 빈번한 경우)
// → class 사용 권장
```

---

### 문자열 및 LINQ 최적화

- **`Update` 및 루프 내에서 문자열 연결(`+`) 금지** → GC Allocation 유발
- 동적 문자열 생성이 필요하면 `StringBuilder` 사용
- **LINQ는 `Update` 내에서 사용 금지** → 매 호출마다 GC 할당 발생
- 초기화 단계나 일회성 연산에서만 LINQ 허용

```csharp
// ❌ 금지
private void Update()
{
    string log = "HP: " + _currentHp + "/" + _maxHp; // 매 프레임 문자열 생성
    var aliveEnemies = _enemies.Where(e => e.IsAlive).ToList(); // LINQ + 리스트 할당
}

// ✅ 올바른 방법
private StringBuilder _sb = new StringBuilder();

private void UpdateHpText()
{
    _sb.Clear();
    _sb.Append("HP: ").Append(_currentHp).Append("/").Append(_maxHp);
    _hpText.text = _sb.ToString();
}
```

---

### 태그 비교

- `gameObject.tag == "Player"` 는 **문자열 할당**이 발생하므로 금지
- 반드시 `CompareTag()` 사용

```csharp
// ❌ 금지
if (other.gameObject.tag == "Player") { }

// ✅ 올바른 방법
if (other.gameObject.CompareTag("Player")) { }
```

---

### Physics 최적화

- 레이캐스트/오버랩 계열 함수에 **LayerMask를 항상 명시**하여 불필요한 충돌 검사 제거
- `RaycastAll`, `OverlapCircleAll` 등 배열 반환 함수는 GC Allocation 유발 → `NonAlloc` 버전 사용

```csharp
// ❌ 금지
RaycastHit2D[] hits = Physics2D.RaycastAll(origin, direction);

// ✅ 올바른 방법
private RaycastHit2D[] _hitBuffer = new RaycastHit2D[10]; // 재사용 버퍼
private LayerMask _groundLayer;

private void Awake()
{
    _groundLayer = LayerMask.GetMask("Ground");
}

private void CheckGround()
{
    int count = Physics2D.RaycastNonAlloc(transform.position, Vector2.down, _hitBuffer, 1f, _groundLayer);
    if (count > 0) { /* 지면 감지 */ }
}
```

---

### WaitForSeconds 캐싱

- 코루틴 내에서 `new WaitForSeconds()`를 매번 생성하면 GC 발생
- 자주 사용하는 대기 객체는 **필드에 캐싱**하여 재사용

```csharp
// ❌ 금지
private IEnumerator SpawnLoop()
{
    while (true)
    {
        yield return new WaitForSeconds(2f); // 매번 할당
        Spawn();
    }
}

// ✅ 올바른 방법
private readonly WaitForSeconds _spawnInterval = new WaitForSeconds(2f);

private IEnumerator SpawnLoop()
{
    while (true)
    {
        yield return _spawnInterval;
        Spawn();
    }
}
```

---

## Git 컨벤션

### 브랜치 컨벤션

**형식**: `[branch 종류]/[목적 명시]`

| branch 종류 | 용도 |
|-------------|------|
| main | 메인 브랜치 |
| develop | 개발 브랜치 |
| feature | 기능 개발 |
| hotfix | 긴급 버그 수정 |

**예시**:
- `feature/BasicMovement`
- `hotfix/NetworkErrorFix`

### 커밋 컨벤션

**형식**: `[<타입>] <제목>`

**본문** (필요시): 구체적인 내용을 "-"로 구분하여 작성

| 타입 | 용도 |
|------|------|
| feat | 새로운 기능 추가 |
| fix | 버그 수정 |
| docs | 문서 수정 |
| style | 코드 포맷팅 |
| refactor | 리팩토링 |
| test | 테스트 코드 |
| chore | 빌드, 설정 변경 |
| asset | 에셋 추가/수정 |
| merge | 머지 커밋 |

**예시**:
```
[feat] 플레이어 움직임 구현

- 벽면 감지용 레이케스트 추가
- 리지드바디 제거 후 Transform 이동으로 구현
```

### PR 규칙

- 머지 전 반드시 rebase 후 오류 확인
- 머지한 브랜치는 삭제하고 새 브랜치 생성
- 핵심 기능 PR은 코드리뷰 진행

---

## 폴더 구조

```
Assets/
├── 01.Scenes/
├── 02.Scripts/
│   ├── _1.Core/
│   ├── _2.Managers/
│   ├── _3.UI/
│   ├── _4.Systems/
│   ├── _5.Utils/
│   └── _6.ScriptableObjects/
├── 03.Prefabs/
├── 04.Sprites/
├── 05.Animations/
├── 06.Audios/
├── 07.ThirdParty/
├── 08.UI/
├── 09.Data/
├── 10.Materials/
├── Resources/
└── Settings/
```
