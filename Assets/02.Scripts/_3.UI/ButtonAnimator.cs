using DG.Tweening;
using UnityEngine;
using UnityEngine.EventSystems;

namespace WildTamer
{
    /// <summary>
    /// 버튼에 Hover·Click 스케일 애니메이션을 부여하는 범용 컴포넌트입니다.
    /// DOTween을 사용하며 TimeScale=0 환경(팝업 등)에서도 정상 동작합니다.
    /// </summary>
    public class ButtonAnimator : MonoBehaviour,
        IPointerEnterHandler,
        IPointerExitHandler,
        IPointerDownHandler,
        IPointerUpHandler
    {
        #region SerializeField 필드

        [Header("Hover 설정")]
        [SerializeField, Tooltip("마우스를 올렸을 때 목표 스케일 배율 (기준: 원래 스케일)")]
        private float hoverScale = 1.1f;

        [SerializeField, Tooltip("Hover 진입·탈출 애니메이션 재생 시간 (초)")]
        private float hoverDuration = 0.15f;

        [SerializeField, Tooltip("Hover 애니메이션 이징 모드")]
        private Ease hoverEase = Ease.OutBack;

        [Header("클릭 설정")]
        [SerializeField, Tooltip("클릭(PointerDown) 시 목표 스케일 배율 (기준: 원래 스케일)")]
        private float clickScale = 0.9f;

        [SerializeField, Tooltip("클릭 눌림·복귀 애니메이션 재생 시간 (초)")]
        private float clickDuration = 0.1f;

        [SerializeField, Tooltip("클릭 애니메이션 이징 모드")]
        private Ease clickEase = Ease.OutQuad;

        #endregion

        #region Private 필드

        private RectTransform _rectTransform;
        private Vector3 _originalScale;
        private bool _isHovered;

        #endregion

        #region Unity 메소드

        private void Awake()
        {
            _rectTransform = GetComponent<RectTransform>();
            _originalScale = _rectTransform.localScale;
        }

        private void OnDisable()
        {
            _rectTransform.DOKill();
            _rectTransform.localScale = _originalScale;
            _isHovered = false;
        }

        #endregion

        #region 포인터 이벤트

        public void OnPointerEnter(PointerEventData eventData)
        {
            _isHovered = true;
            _rectTransform.DOKill();
            _rectTransform
                .DOScale(_originalScale * hoverScale, hoverDuration)
                .SetEase(hoverEase)
                .SetUpdate(true);
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            _isHovered = false;
            _rectTransform.DOKill();
            _rectTransform
                .DOScale(_originalScale, hoverDuration)
                .SetEase(hoverEase)
                .SetUpdate(true);
        }

        public void OnPointerDown(PointerEventData eventData)
        {
            _rectTransform.DOKill();
            _rectTransform
                .DOScale(_originalScale * clickScale, clickDuration)
                .SetEase(clickEase)
                .SetUpdate(true);
        }

        public void OnPointerUp(PointerEventData eventData)
        {
            _rectTransform.DOKill();
            // 마우스가 여전히 버튼 위에 있으면 Hover 스케일로 복귀, 아니면 원래 스케일로 복귀
            Vector3 targetScale = _isHovered ? _originalScale * hoverScale : _originalScale;
            _rectTransform
                .DOScale(targetScale, clickDuration)
                .SetEase(clickEase)
                .SetUpdate(true);
        }

        #endregion
    }
}
