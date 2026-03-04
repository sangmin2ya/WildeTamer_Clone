namespace WildTamer
{
    /// <summary>
    /// 쿼터뷰에서 입체감을 표현하기 위해 깊이 정렬이 필요한 모든 객체가 구현하는 인터페이스입니다.
    /// Y 위치가 낮을수록(화면 하단) SpriteRenderer.sortingOrder를 높게 설정하여 앞에 렌더링합니다.
    /// 아래 static 필드를 수정하면 모든 구현체에 일괄 적용됩니다.
    /// </summary>
    public interface IDepthSortable
    {
        /// <summary>sortingOrder 최솟값 — 맵 상단(배경)에 해당</summary>
        public static int MinSortingOrder = -20;

        /// <summary>sortingOrder 최댓값 — 맵 하단(전경)에 해당</summary>
        public static int MaxSortingOrder = 20;

        /// <summary>이 거리 이상이면 sortingOrder 업데이트를 건너뜁니다 (최적화)</summary>
        public static float SortUpdateDistance = 15f;

        /// <summary>
        /// 현재 Y 위치를 기반으로 SpriteRenderer의 sortingOrder를 갱신합니다.
        /// 최적화를 위해 플레이어와 일정 거리 이상이면 업데이트를 건너뛸 수 있습니다.
        /// </summary>
        void UpdateSortingOrder();
    }
}
