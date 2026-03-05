using UnityEngine;
using UnityEngine.EventSystems;

namespace WildTamer
{
    /// <summary>
    /// 전체맵 RawImage에 부착하여 마우스 드래그로 카메라를 팬 이동시킵니다.
    /// IPointerDownHandler로 클릭 시작을 감지하고
    /// IDragHandler로 매 프레임 델타를 MinimapCamera에 전달합니다.
    /// </summary>
    public class MapDragHandler : MonoBehaviour, IPointerDownHandler, IDragHandler
    {
        #region IDragHandler

        public void OnPointerDown(PointerEventData eventData)
        {
            // 드래그 시작점 처리 (필요 시 확장)
        }

        /// <summary>
        /// 드래그 중 매 프레임 호출됩니다. 델타를 MinimapCamera에 전달합니다.
        /// </summary>
        public void OnDrag(PointerEventData eventData)
        {
            MinimapCamera.Instance?.Pan(eventData.delta);
        }

        #endregion
    }
}
