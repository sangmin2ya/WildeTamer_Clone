using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace WildTamer
{
    /// <summary>
    /// 씬의 모든 IDepthSortable 오브젝트를 Y 위치 기준으로 중앙 정렬하는 매니저입니다.
    ///
    /// 동작 원리:
    ///   1. IDepthSortable 구현체가 OnEnable/OnDisable에서 Register/Unregister를 호출합니다.
    ///   2. sortInterval마다 등록된 SpriteRenderer 목록을 Y 내림차순으로 정렬합니다.
    ///   3. 정렬된 순위(index)를 sortingOrder로 직접 부여합니다.
    ///      → N개 오브젝트 = N개 고유 sortingOrder 보장, 동일 값 충돌 원천 불가
    ///
    /// Y가 동일한 오브젝트는 X 오름차순을 타이브레이커로 사용합니다.
    /// </summary>
    public class DepthSorter : MonoBehaviour
    {
        #region Public 프로퍼티

        public static DepthSorter Instance { get; private set; }

        #endregion

        #region SerializeField 필드

        [Header("정렬 설정")]
        [SerializeField, Tooltip("정렬 실행 주기 (초). 낮을수록 부드럽지만 연산 빈도 증가")]
        private float sortInterval = 0.05f;

        #endregion

        #region Private 필드

        // 등록된 SpriteRenderer 목록 — Register/Unregister로만 변경
        private readonly List<SpriteRenderer> _renderers = new List<SpriteRenderer>();

        // 정렬 작업용 버퍼 — 정렬마다 재사용하여 GC 방지
        private readonly List<SpriteRenderer> _sortBuffer = new List<SpriteRenderer>();

        // 람다 없는 정적 비교자 — GC 방지
        private static readonly DepthSortComparer Comparer = new DepthSortComparer();

        private WaitForSeconds _sortWait;

        #endregion

        #region Unity 메소드

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            _sortWait = new WaitForSeconds(sortInterval);
        }

        private void Start()
        {
            StartCoroutine(SortRoutine());
        }

        private void OnDestroy()
        {
            if (Instance == this)
            {
                Instance = null;
            }
        }

        #endregion

        #region 등록 / 해제

        /// <summary>
        /// SpriteRenderer를 정렬 대상에 등록합니다.
        /// IDepthSortable 구현체의 OnEnable에서 호출합니다.
        /// </summary>
        public static void Register(SpriteRenderer sr)
        {
            if (sr == null || Instance == null)
            {
                return;
            }

            Instance._renderers.Add(sr);
        }

        /// <summary>
        /// SpriteRenderer를 정렬 대상에서 해제합니다.
        /// IDepthSortable 구현체의 OnDisable에서 호출합니다.
        /// </summary>
        public static void Unregister(SpriteRenderer sr)
        {
            if (sr == null || Instance == null)
            {
                return;
            }

            Instance._renderers.Remove(sr);
        }

        #endregion

        #region 정렬 루틴

        /// <summary>
        /// sortInterval마다 깊이 정렬을 수행하는 코루틴입니다.
        /// </summary>
        private IEnumerator SortRoutine()
        {
            while (true)
            {
                yield return _sortWait;
                SortAll();
            }
        }

        /// <summary>
        /// 등록된 모든 SpriteRenderer를 Y 내림차순으로 정렬하고
        /// 순위(index)를 sortingOrder로 직접 부여합니다.
        ///
        /// index 0   = Y 최대 (가장 뒤쪽) → sortingOrder 0  (가장 뒤)
        /// index N-1 = Y 최소 (가장 앞쪽) → sortingOrder N-1 (가장 앞)
        ///
        /// 복잡도: O(N log N) / sortInterval — 프레임마다 N회 계산하던 것을 정렬 1회로 대체
        /// </summary>
        private void SortAll()
        {
            // 버퍼에 복사 후 정렬 — _renderers 원본은 등록/해제 전용으로 유지
            _sortBuffer.Clear();
            _sortBuffer.AddRange(_renderers);
            _sortBuffer.Sort(Comparer);

            int count = _sortBuffer.Count;

            for (int i = 0; i < count; i++)
            {
                SpriteRenderer sr = _sortBuffer[i];

                if (sr == null)
                {
                    continue;
                }

                // index가 작을수록 뒤쪽(Y 높음) → sortingOrder 작게
                // index가 클수록 앞쪽(Y 낮음) → sortingOrder 크게
                int order = i - count / 2;

                if (sr.sortingOrder != order)
                {
                    sr.sortingOrder = order;
                }
            }
        }

        #endregion

        #region 비교자

        /// <summary>
        /// Y 내림차순 → X 오름차순(타이브레이커) 비교자입니다.
        /// 람다 대신 클래스로 구현하여 정렬 시 GC 할당을 방지합니다.
        /// </summary>
        private class DepthSortComparer : IComparer<SpriteRenderer>
        {
            public int Compare(SpriteRenderer a, SpriteRenderer b)
            {
                if (a == null || b == null)
                {
                    return 0;
                }

                // Y 내림차순: Y가 높을수록(화면 위쪽·뒤쪽) 앞에 정렬 → 낮은 sortingOrder 부여
                float ay = a.transform.position.y;
                float by = b.transform.position.y;

                int yResult = by.CompareTo(ay);

                if (yResult != 0)
                {
                    return yResult;
                }

                // 동일 Y: X 오름차순을 타이브레이커로 사용 (일관성 보장)
                return a.transform.position.x.CompareTo(b.transform.position.x);
            }
        }

        #endregion
    }
}
