using System;
using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;

namespace WildTamer
{
    /// <summary>
    /// 몬스터 월드 캔버스의 자식으로 배치되는 테이밍 패널 UI입니다.
    /// "길들이기"와 "수확하기" 두 버튼을 보유하며, 각각 별도 콜백을 받습니다.
    /// TameController가 플레이어 근접 감지 시 Show/Hide를 호출합니다.
    ///
    /// ■ 프리팹 구조
    ///   TamePanel (RectTransform + TameButtonUI) ← 기본 비활성화 상태로 설정
    ///     ├─ TameButton (Button — "길들이기")
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

        private Squad _playerSquad;

        #endregion

        #region Unity 메소드

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
        /// 버튼 클릭 콜백과 스쿼드 이벤트를 설정합니다.
        /// TameController.ShowTameUI()에서 기절 시마다 호출됩니다.
        /// </summary>
        /// <param name="onTame">길들이기 버튼 클릭 시 실행할 콜백</param>
        /// <param name="onRoot">수확하기 버튼 클릭 시 실행할 콜백</param>
        /// <param name="playerSquad">플레이어 스쿼드 — 멤버 수 변경 이벤트 구독에 사용</param>
        public void Initialize(Action onTame, Action onRoot, Squad playerSquad)
        {
            // 이전 등록 리스너 제거 (재진입 안전)
            tameButton.onClick.RemoveAllListeners();
            rootButton.onClick.RemoveAllListeners();

            tameButton.onClick.AddListener(() => onTame?.Invoke());
            rootButton.onClick.AddListener(() => onRoot?.Invoke());

            if (_playerSquad != null)
            {
                _playerSquad.OnMemberCountChanged -= OnSquadMemberCountChanged;
            }

            _playerSquad = playerSquad;

            if (_playerSquad != null)
            {
                _playerSquad.OnMemberCountChanged += OnSquadMemberCountChanged;
                tameButton.interactable = !_playerSquad.IsFull;
            }
        }

        #endregion

        #region UI 애니메이션

        /// <summary>
        /// 스케일 0 → 1 애니메이션으로 패널을 표시합니다.
        /// </summary>
        public void Show(float duration, Ease ease)
        {
            transform.DOKill();
            transform.localScale = Vector3.zero;
            gameObject.SetActive(true);
            transform.DOScale(Vector3.one, duration).SetEase(ease).SetUpdate(true);
        }

        /// <summary>
        /// 스케일 1 → 0 애니메이션으로 패널을 숨깁니다.
        /// 애니메이션 완료 후 오브젝트를 비활성화합니다.
        /// </summary>
        public void Hide(float duration, Ease ease)
        {
            transform.DOKill();
            transform.DOScale(Vector3.zero, duration).SetEase(ease).SetUpdate(true)
                .OnComplete(() => gameObject.SetActive(false));
        }

        /// <summary>
        /// 애니메이션 없이 즉시 비활성화합니다.
        /// 기절 상태 해제 또는 풀 반환 시 사용합니다.
        /// </summary>
        public void HideImmediate()
        {
            transform.DOKill();
            transform.localScale = Vector3.zero;
            gameObject.SetActive(false);
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
    }
}
