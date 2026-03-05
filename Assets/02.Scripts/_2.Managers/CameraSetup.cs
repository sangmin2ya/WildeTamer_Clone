using UnityEngine;

namespace WildTamer
{
    /// <summary>
    /// 메인 카메라에 쿼터뷰 깊이 정렬(Custom Sort Axis)을 적용합니다.
    /// URP에서는 Project Settings의 Transparency Sort Mode가 무시되므로
    /// 카메라 컴포넌트에 직접 설정해야 합니다.
    /// </summary>
    [RequireComponent(typeof(Camera))]
    public class CameraSetup : MonoBehaviour
    {
        private void Awake()
        {
            Camera cam = GetComponent<Camera>();
            cam.transparencySortMode = TransparencySortMode.CustomAxis;
            cam.transparencySortAxis = new Vector3(0f, 1f, 0f);
        }
    }
}
