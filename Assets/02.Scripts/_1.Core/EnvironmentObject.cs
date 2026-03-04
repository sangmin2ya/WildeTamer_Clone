using UnityEngine;

namespace WildTamer
{
    /// <summary>
    /// 돌, 나무 등 환경 오브젝트의 깊이 정렬을 처리하는 컴포넌트입니다.
    /// OnEnable/OnDisable에서 DepthSorter에 등록하여 중앙 정렬을 위임합니다.
    /// </summary>
    public class EnvironmentObject : MonoBehaviour
    {
        #region SerializeField 필드

        [Header("참조")]
        [SerializeField, Tooltip("sortingOrder에 사용할 SpriteRenderer")]
        private SpriteRenderer spriteRenderer;

        #endregion

        #region Unity 메소드

        private void OnEnable()
        {
            DepthSorter.Register(spriteRenderer);
        }

        private void OnDisable()
        {
            DepthSorter.Unregister(spriteRenderer);
        }

        #endregion
    }
}
