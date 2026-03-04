using System;
using UnityEngine;
using UnityEngine.UI;

namespace WildTamer
{
    /// <summary>
    /// Screen Space Overlay 캔버스 위에서 월드 오브젝트 위치를 추적하는 테이밍 패널 UI입니다.
    /// "길들이기"와 "수확하기" 두 버튼을 보유하며, 각각 별도 콜백을 받습니다.
    /// TameController가 기절 시 인스턴스를 생성하고 Initialize()로 추적 대상을 주입합니다.
    ///
    /// ■ 프리팹 구조
    ///   TamePanel (RectTransform + TameButtonUI)
    ///     ├─ TameButton    (Button — "길들이기")
    ///     └─ RootButton (Button — "수확하기")
    /// </summary>
    public class TameButtonUI : MonoBehaviour
    {
        #region SerializeField 필드

        [Header("버튼 참조")]
        [SerializeField, Tooltip("길들이기 버튼 — 적을 아군으로 편입")]
        private Button tameButton;

        [SerializeField, Tooltip("수확하기 버튼 — 적을 재화로 소비")]
        private Button rootButton;

        #endregion

        #region Private 필드

        private RectTransform _rectTransform;

        private Transform _target;
        private Camera _mainCamera;
        private RectTransform _canvasRect;
        private Vector3 _worldOffset;
        private Squad _playerSquad;

        #endregion

        #region Unity 메소드

        private void Awake()
        {
            _rectTransform = GetComponent<RectTransform>();
        }

        private void Update()
        {
            UpdatePosition();
        }

        private void OnDestroy()
        {
            tameButton.onClick.RemoveAllListeners();
            rootButton.onClick.RemoveAllListeners();

            if (_playerSquad != null)
            {
                _playerSquad.OnMemberCountChanged -= OnSquadMemberCountChanged;
            }
        }

        #endregion

        #region 초기화

        /// <summary>
        /// 추적 대상과 두 버튼의 클릭 콜백을 초기화합니다.
        /// TameController.ShowTameUI()에서 인스턴스 생성 직후 호출됩니다.
        /// </summary>
        /// <param name="target">추적할 월드 트랜스폼 (기절 몬스터)</param>
        /// <param name="mainCamera">월드→스크린 변환에 사용할 카메라</param>
        /// <param name="canvasRect">버튼이 속한 오버레이 캔버스의 RectTransform</param>
        /// <param name="worldOffset">몬스터 위치로부터의 월드 공간 오프셋</param>
        /// <param name="onTame">길들이기 버튼 클릭 시 실행할 콜백</param>
        /// <param name="onRoot">수확하기 버튼 클릭 시 실행할 콜백</param>
        /// <param name="playerSquad">플레이어 스쿼드 — 멤버 수 변경 이벤트 구독에 사용</param>
        public void Initialize(Transform target, Camera mainCamera, RectTransform canvasRect,
                               Vector3 worldOffset, Action onTame, Action onRoot, Squad playerSquad)
        {
            _target      = target;
            _mainCamera  = mainCamera;
            _canvasRect  = canvasRect;
            _worldOffset = worldOffset;

            tameButton.onClick.AddListener(() => onTame?.Invoke());
            rootButton.onClick.AddListener(() => onRoot?.Invoke());

            // 플레이어 스쿼드 멤버 수 변경 이벤트 구독
            _playerSquad = playerSquad;

            if (_playerSquad != null)
            {
                _playerSquad.OnMemberCountChanged += OnSquadMemberCountChanged;
                // 현재 상태로 초기 활성화 여부 결정
                tameButton.interactable = !_playerSquad.IsFull;
            }

            UpdatePosition();
        }

        #endregion

        #region 버튼 상태 갱신

        /// <summary>
        /// 플레이어 스쿼드 멤버 수 변경 시 호출됩니다.
        /// 스쿼드가 가득 차면 길들이기 버튼을 비활성화하고, 자리가 생기면 다시 활성화합니다.
        /// </summary>
        private void OnSquadMemberCountChanged(int current, int max)
        {
            tameButton.interactable = current < max;
        }

        #endregion

        #region 위치 갱신

        /// <summary>
        /// 월드 좌표를 스크린 좌표로 변환하여 RectTransform 위치를 갱신합니다.
        /// </summary>
        private void UpdatePosition()
        {
            if (_target == null || _mainCamera == null || _canvasRect == null)
            {
                return;
            }

            Vector3 worldPos  = _target.position + _worldOffset;
            Vector2 screenPos = _mainCamera.WorldToScreenPoint(worldPos);

            // Screen Space Overlay는 eventCamera가 null
            RectTransformUtility.ScreenPointToLocalPointInRectangle(
                _canvasRect,
                screenPos,
                null,
                out Vector2 localPoint
            );

            _rectTransform.anchoredPosition = localPoint;
        }

        #endregion
    }
}
