using UnityEngine;

namespace WildTamer
{
    /// <summary>
    /// 기절한 몬스터에 선택적으로 부착하는 테이밍 전용 컴포넌트입니다.
    /// 기절 시 Screen Space Overlay 캔버스에 tameButtonPrefab을 인스턴스화하여
    /// 몬스터 위치를 추적하는 테이밍 버튼을 표시합니다.
    ///
    /// ■ 설정 방법
    ///   1. 몬스터 프리팹에 이 컴포넌트를 추가합니다.
    ///   2. tameButtonPrefab: TameButtonUI 컴포넌트가 붙은 버튼 프리팹을 연결합니다.
    ///   3. overlayCanvas: Screen Space Overlay 캔버스를 연결합니다. (미연결 시 자동 탐색)
    /// </summary>
    [RequireComponent(typeof(Monster))]
    public class TameController : MonoBehaviour
    {
        #region SerializeField 필드

        [Header("UI 설정")]
        [SerializeField, Tooltip("기절 시 생성할 테이밍 버튼 프리팹 (TameButtonUI 포함)")]
        private GameObject tameButtonPrefab;

        [SerializeField, Tooltip("버튼을 생성할 Screen Space Overlay 캔버스 (미연결 시 씬에서 자동 탐색)")]
        private Canvas overlayCanvas;

        [SerializeField, Tooltip("버튼이 표시될 위치의 월드 오프셋 (몬스터 위치 기준)")]
        private Vector3 buttonWorldOffset = new Vector3(0f, 1f, 0f);

        #endregion

        #region Private 필드

        private Monster _monster;
        private Camera _mainCamera;
        private TameButtonUI _tameButtonInstance;

        #endregion

        #region Unity 메소드

        private void Awake()
        {
            _monster    = GetComponent<Monster>();
            _mainCamera = Camera.main;

            // overlayCanvas가 Inspector에서 미연결된 경우 씬에서 Screen Space Overlay 캔버스를 탐색
            if (overlayCanvas == null)
            {
                foreach (Canvas c in FindObjectsByType<Canvas>(FindObjectsSortMode.None))
                {
                    if (c.renderMode == RenderMode.ScreenSpaceOverlay)
                    {
                        overlayCanvas = c;
                        break;
                    }
                }
            }
        }

        private void OnDisable()
        {
            HideTameUI();
        }

        #endregion

        #region UI 제어

        /// <summary>
        /// 테이밍 버튼을 오버레이 캔버스에 생성하고 이 몬스터를 추적하도록 초기화합니다.
        /// MonsterStunState.Enter()에서 호출됩니다.
        /// </summary>
        public void ShowTameUI()
        {
            if (tameButtonPrefab == null || overlayCanvas == null)
            {
                return;
            }

            if (_tameButtonInstance != null)
            {
                return;
            }

            GameObject buttonObj = Instantiate(tameButtonPrefab, overlayCanvas.transform);
            _tameButtonInstance  = buttonObj.GetComponent<TameButtonUI>();

            _tameButtonInstance?.Initialize(
                transform,
                _mainCamera,
                overlayCanvas.GetComponent<RectTransform>(),
                buttonWorldOffset,
                OnTameButtonClicked,
                OnRootButtonClicked,
                Squad.GetPlayerSquad()
            );
        }

        /// <summary>
        /// 테이밍 버튼 인스턴스를 파괴합니다.
        /// MonsterStunState.Exit()에서 호출됩니다.
        /// </summary>
        public void HideTameUI()
        {
            if (_tameButtonInstance != null)
            {
                Destroy(_tameButtonInstance.gameObject);
                _tameButtonInstance = null;
            }
        }

        #endregion

        #region 버튼 콜백

        /// <summary>
        /// "길들이기" 버튼 클릭 시 호출됩니다.
        /// Monster.Tame()에 테이밍 처리를 위임합니다.
        /// </summary>
        private void OnTameButtonClicked()
        {
            _monster.Tame();
        }

        /// <summary>
        /// "수확하기" 버튼 클릭 시 호출됩니다.
        /// Monster.Root()에 재화 획득 처리를 위임합니다.
        /// </summary>
        private void OnRootButtonClicked()
        {
            _monster.Root();
        }

        #endregion
    }
}
