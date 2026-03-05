using System.IO;
using UnityEngine;

namespace WildTamer
{
    /// <summary>
    /// Texture2D 기반 전장의 안개 시스템입니다.
    /// 맵 전체를 불투명 검정 텍스처로 덮고, 플레이어 이동 시 주변 영역을 원형으로 투명하게 페인팅합니다.
    /// 탐험 데이터는 PNG로 저장하여 게임 재시작 시 복원됩니다.
    /// </summary>
    public class FogOfWarManager : MonoBehaviour
    {
        #region SerializeField 필드

        [Header("맵 설정")]
        [SerializeField, Tooltip("맵 좌하단 월드 좌표 — 안개 텍스처의 기준점")]
        private Vector2 mapOrigin = Vector2.zero;

        [SerializeField, Tooltip("맵 가로·세로 크기 (월드 단위)")]
        private Vector2 mapSize = new Vector2(50f, 50f);

        [SerializeField, Tooltip("안개 텍스처 해상도 — 유닛당 픽셀 수 (클수록 정밀, 비용 증가)")]
        private int pixelsPerUnit = 4;

        [Header("안개 설정")]
        [SerializeField, Tooltip("한 번에 밝혀지는 반경 (월드 단위)")]
        private float revealRadius = 5f;

        [SerializeField, Tooltip("밝혀지는 원 가장자리의 페이드 너비 (월드 단위) — 0이면 경계 선명")]
        private float softEdgeWidth = 1f;

        [SerializeField, Tooltip("플레이어가 이 거리 이상 이동해야 안개를 갱신 — 너무 낮으면 매 프레임 Apply 호출")]
        private float moveThreshold = 0.3f;

        [Header("참조")]
        [SerializeField, Tooltip("안개를 표시할 SpriteRenderer — 이 오브젝트의 자식으로 배치")]
        private SpriteRenderer fogRenderer;

        #endregion

        #region Private 필드

        private Texture2D  _fogTexture;
        private Color32[]  _pixels;
        private int        _texWidth;
        private int        _texHeight;
        private Vector2    _lastRevealPos;
        private Transform  _playerTransform;

        private const string SaveFileName = "fogofwar.png";

        #endregion

        #region Public 프로퍼티

        public static FogOfWarManager Instance { get; private set; }

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
        }

        private void Start()
        {
            InitTexture();

            PlayerController player = FindAnyObjectByType<PlayerController>();

            if (player != null)
            {
                _playerTransform = player.transform;
            }
            else
            {
                Debug.LogWarning("[FogOfWarManager] PlayerController를 찾을 수 없습니다.");
            }

            // 저장된 탐험 데이터 복원, 없으면 시작 위치 즉시 밝힘
            if (!LoadFog() && _playerTransform != null)
            {
                RevealAt(_playerTransform.position);
                _lastRevealPos = _playerTransform.position;
            }
        }

        private void Update()
        {
            if (_playerTransform == null)
            {
                return;
            }

            Vector2 playerPos = _playerTransform.position;

            if (((Vector2)playerPos - _lastRevealPos).sqrMagnitude >= moveThreshold * moveThreshold)
            {
                RevealAt(playerPos);
                _lastRevealPos = playerPos;
            }
        }

        #endregion

        #region 안개 초기화

        /// <summary>
        /// 안개 텍스처를 생성하고 SpriteRenderer에 적용합니다.
        /// 전체 픽셀을 불투명 검정(안개)으로 초기화합니다.
        /// </summary>
        private void InitTexture()
        {
            _texWidth  = Mathf.Max(1, Mathf.RoundToInt(mapSize.x * pixelsPerUnit));
            _texHeight = Mathf.Max(1, Mathf.RoundToInt(mapSize.y * pixelsPerUnit));

            _fogTexture            = new Texture2D(_texWidth, _texHeight, TextureFormat.RGBA32, false);
            _fogTexture.filterMode = FilterMode.Bilinear;
            _fogTexture.wrapMode   = TextureWrapMode.Clamp;

            // 전체를 불투명 검정(안개)으로 초기화
            _pixels = new Color32[_texWidth * _texHeight];

            for (int i = 0; i < _pixels.Length; i++)
            {
                _pixels[i] = new Color32(0, 0, 0, 255);
            }

            _fogTexture.SetPixels32(_pixels);
            _fogTexture.Apply();

            if (fogRenderer == null)
            {
                Debug.LogWarning("[FogOfWarManager] fogRenderer가 연결되지 않았습니다.");
                return;
            }

            // 텍스처로 Sprite를 생성하고 맵 중심에 배치
            fogRenderer.sprite = Sprite.Create(
                _fogTexture,
                new Rect(0, 0, _texWidth, _texHeight),
                new Vector2(0.5f, 0.5f),
                pixelsPerUnit
            );

            fogRenderer.transform.position = new Vector3(
                mapOrigin.x + mapSize.x * 0.5f,
                mapOrigin.y + mapSize.y * 0.5f,
                0f
            );
        }

        #endregion

        #region 안개 제거

        /// <summary>
        /// 지정 월드 좌표 주변을 원형으로 밝힙니다.
        /// 이미 밝혀진 픽셀은 더 어둡게 되지 않습니다 (단방향).
        /// </summary>
        /// <param name="worldPos">밝힐 월드 좌표</param>
        public void RevealAt(Vector2 worldPos)
        {
            int cx = WorldToTexX(worldPos.x);
            int cy = WorldToTexY(worldPos.y);
            int r  = Mathf.RoundToInt(revealRadius   * pixelsPerUnit);
            int se = Mathf.RoundToInt(softEdgeWidth   * pixelsPerUnit);

            bool changed = false;

            for (int x = cx - r; x <= cx + r; x++)
            {
                for (int y = cy - r; y <= cy + r; y++)
                {
                    if (x < 0 || x >= _texWidth || y < 0 || y >= _texHeight)
                    {
                        continue;
                    }

                    float dist = Mathf.Sqrt((x - cx) * (x - cx) + (y - cy) * (y - cy));

                    if (dist > r)
                    {
                        continue;
                    }

                    // 가장자리 페이드: 안쪽은 완전 투명, 외곽 softEdge 구간은 서서히 불투명
                    byte targetAlpha;

                    if (se > 0 && dist > r - se)
                    {
                        float t = (dist - (r - se)) / se;
                        targetAlpha = (byte)Mathf.RoundToInt(t * 255f);
                    }
                    else
                    {
                        targetAlpha = 0;
                    }

                    int index = y * _texWidth + x;

                    if (_pixels[index].a > targetAlpha)
                    {
                        _pixels[index].a = targetAlpha;
                        changed = true;
                    }
                }
            }

            if (changed)
            {
                _fogTexture.SetPixels32(_pixels);
                _fogTexture.Apply();
            }
        }

        #endregion

        #region 저장 / 불러오기

        /// <summary>
        /// 현재 탐험 상태를 PNG 파일로 저장합니다.
        /// GameManager.SaveGame() 호출 시 함께 실행하세요.
        /// </summary>
        public void SaveFog()
        {
            byte[] png  = _fogTexture.EncodeToPNG();
            string path = Path.Combine(Application.persistentDataPath, SaveFileName);
            File.WriteAllBytes(path, png);
            Debug.Log($"[FogOfWarManager] 탐험 데이터 저장 완료: {path}");
        }

        /// <summary>
        /// 저장된 탐험 PNG를 불러와 텍스처에 복원합니다.
        /// 파일이 없으면 false를 반환합니다.
        /// </summary>
        public bool LoadFog()
        {
            string path = Path.Combine(Application.persistentDataPath, SaveFileName);

            if (!File.Exists(path))
            {
                return false;
            }

            byte[] png = File.ReadAllBytes(path);

            if (!_fogTexture.LoadImage(png))
            {
                return false;
            }

            _pixels = _fogTexture.GetPixels32();
            Debug.Log("[FogOfWarManager] 탐험 데이터 불러오기 완료");
            return true;
        }

        #endregion

        #region 좌표 변환

        /// <summary>
        /// 월드 X 좌표를 텍스처 X 픽셀 인덱스로 변환합니다.
        /// </summary>
        private int WorldToTexX(float worldX)
        {
            return Mathf.RoundToInt((worldX - mapOrigin.x) / mapSize.x * _texWidth);
        }

        /// <summary>
        /// 월드 Y 좌표를 텍스처 Y 픽셀 인덱스로 변환합니다.
        /// </summary>
        private int WorldToTexY(float worldY)
        {
            return Mathf.RoundToInt((worldY - mapOrigin.y) / mapSize.y * _texHeight);
        }

        #endregion

        #region 디버그 기즈모

#if UNITY_EDITOR
        private void OnDrawGizmos()
        {
            Gizmos.color = new Color(0f, 0f, 0f, 0.3f);
            Vector3 center = new Vector3(mapOrigin.x + mapSize.x * 0.5f, mapOrigin.y + mapSize.y * 0.5f, 0f);
            Gizmos.DrawWireCube(center, new Vector3(mapSize.x, mapSize.y, 0f));
        }
#endif

        #endregion
    }
}
