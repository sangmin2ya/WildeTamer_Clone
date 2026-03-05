using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.Tilemaps;

namespace WildTamer
{
    /// <summary>
    /// GroundTilemap 위에 환경 오브젝트를 밀도 기반으로 자동 배치하는 에디터 툴입니다.
    ///
    /// 사용법:
    ///   1. WildTamer → Environment Painter 메뉴로 창을 엽니다.
    ///   2. Ground Tilemap과 부모 오브젝트를 연결합니다.
    ///   3. 배치할 프리팹과 밀도(0~1, 타일당 확률)를 설정합니다.
    ///   4. '배치' 버튼을 누르면 기존 오브젝트를 지우고 새로 배치합니다.
    ///   5. Ctrl+Z로 되돌릴 수 있습니다.
    /// </summary>
    public class EnvironmentPainterWindow : EditorWindow
    {
        #region 내부 데이터

        [System.Serializable]
        private class PaintEntry
        {
            public GameObject prefab;
            [Range(0f, 1f)]
            public float density = 0.1f;
            public bool enabled = true;
        }

        #endregion

        #region 직렬화 필드 (에디터 재시작 후 유지)

        [SerializeField]
        private Tilemap groundTilemap;

        [SerializeField]
        private Transform parentTransform;

        [SerializeField]
        private List<PaintEntry> entries = new List<PaintEntry>();

        [SerializeField]
        private bool randomSeed = true;

        [SerializeField]
        private int seed = 0;

        [SerializeField]
        private bool randomOffsetInTile = true;

        #endregion

        #region Private 필드

        private Vector2 _scrollPos;
        private SerializedObject _so;

        #endregion

        #region 메뉴 등록

        [MenuItem("WildTamer/Environment Painter")]
        public static void ShowWindow()
        {
            GetWindow<EnvironmentPainterWindow>("Environment Painter");
        }

        #endregion

        #region EditorWindow 생명주기

        private void OnEnable()
        {
            _so = new SerializedObject(this);
        }

        private void OnGUI()
        {
            _so.Update();

            DrawHeader();
            EditorGUILayout.Space(4);

            DrawTilemapSettings();
            EditorGUILayout.Space(8);

            DrawEntryList();
            EditorGUILayout.Space(8);

            DrawOptions();
            EditorGUILayout.Space(8);

            DrawActionButtons();

            _so.ApplyModifiedProperties();
        }

        #endregion

        #region GUI 섹션

        private void DrawHeader()
        {
            EditorGUILayout.LabelField("Environment Painter", EditorStyles.boldLabel);
            EditorGUILayout.LabelField("GroundTilemap 위에 환경 오브젝트를 밀도 기반으로 배치합니다.", EditorStyles.miniLabel);
        }

        private void DrawTilemapSettings()
        {
            EditorGUILayout.LabelField("참조", EditorStyles.boldLabel);
            groundTilemap    = (Tilemap)EditorGUILayout.ObjectField("Ground Tilemap", groundTilemap, typeof(Tilemap), true);
            parentTransform  = (Transform)EditorGUILayout.ObjectField("부모 오브젝트", parentTransform, typeof(Transform), true);

            if (groundTilemap == null)
            {
                EditorGUILayout.HelpBox("Ground Tilemap을 연결해 주세요.", MessageType.Warning);
            }

            if (parentTransform == null)
            {
                EditorGUILayout.HelpBox("배치된 오브젝트를 담을 부모 오브젝트를 연결해 주세요. (지우기 기능에 필요)", MessageType.Info);
            }
        }

        private void DrawEntryList()
        {
            EditorGUILayout.LabelField("배치 오브젝트 목록", EditorStyles.boldLabel);

            // 헤더
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("활성",    GUILayout.Width(28));
            EditorGUILayout.LabelField("프리팹",                      GUILayout.MinWidth(100));
            EditorGUILayout.LabelField("밀도 (타일당 확률)",          GUILayout.Width(200));
            EditorGUILayout.LabelField("%",       GUILayout.Width(36));
            EditorGUILayout.LabelField("",        GUILayout.Width(24));
            EditorGUILayout.EndHorizontal();

            _scrollPos = EditorGUILayout.BeginScrollView(_scrollPos, GUILayout.MaxHeight(300));

            for (int i = 0; i < entries.Count; i++)
            {
                PaintEntry entry = entries[i];

                EditorGUILayout.BeginHorizontal();

                entry.enabled = EditorGUILayout.Toggle(entry.enabled, GUILayout.Width(28));

                GUI.enabled = entry.enabled;
                entry.prefab  = (GameObject)EditorGUILayout.ObjectField(entry.prefab, typeof(GameObject), false);
                entry.density = EditorGUILayout.Slider(entry.density, 0f, 1f, GUILayout.Width(200));
                EditorGUILayout.LabelField($"{entry.density * 100f:F1}%", GUILayout.Width(36));
                GUI.enabled = true;

                if (GUILayout.Button("✕", GUILayout.Width(24)))
                {
                    entries.RemoveAt(i);
                    i--;
                }

                EditorGUILayout.EndHorizontal();
            }

            EditorGUILayout.EndScrollView();

            if (GUILayout.Button("+ 오브젝트 추가"))
            {
                entries.Add(new PaintEntry());
            }
        }

        private void DrawOptions()
        {
            EditorGUILayout.LabelField("옵션", EditorStyles.boldLabel);

            EditorGUILayout.BeginHorizontal();
            randomSeed = EditorGUILayout.Toggle("랜덤 시드", randomSeed, GUILayout.Width(180));

            GUI.enabled = !randomSeed;
            seed = EditorGUILayout.IntField(seed);
            GUI.enabled = true;
            EditorGUILayout.EndHorizontal();

            randomOffsetInTile = EditorGUILayout.Toggle("타일 내 랜덤 오프셋", randomOffsetInTile);
        }

        private void DrawActionButtons()
        {
            bool canPaint = groundTilemap != null && HasEnabledEntry();

            GUI.enabled = canPaint;
            if (GUILayout.Button("배치", GUILayout.Height(32)))
            {
                Paint();
            }
            GUI.enabled = true;

            GUI.enabled = parentTransform != null;
            if (GUILayout.Button("지우기"))
            {
                Clear();
            }
            GUI.enabled = true;

            // 마지막 배치 결과 표시
            if (Application.isPlaying)
            {
                EditorGUILayout.HelpBox("플레이 모드에서는 배치를 권장하지 않습니다.", MessageType.Warning);
            }
        }

        #endregion

        #region 배치 / 제거 로직

        /// <summary>
        /// Tilemap의 모든 타일을 순회하며 각 엔트리의 밀도 확률로 오브젝트를 스폰합니다.
        /// 기존 배치 오브젝트는 먼저 제거합니다.
        /// </summary>
        private void Paint()
        {
            Clear();

            int usedSeed = randomSeed ? Random.Range(0, int.MaxValue) : seed;
            seed = usedSeed;
            Random.InitState(usedSeed);

            BoundsInt bounds = groundTilemap.cellBounds;
            int spawnCount   = 0;

            Undo.SetCurrentGroupName("Environment Paint");
            int undoGroup = Undo.GetCurrentGroup();

            for (int x = bounds.xMin; x < bounds.xMax; x++)
            {
                for (int y = bounds.yMin; y < bounds.yMax; y++)
                {
                    Vector3Int cellPos = new Vector3Int(x, y, 0);

                    if (!groundTilemap.HasTile(cellPos))
                    {
                        continue;
                    }

                    Vector3 worldPos = groundTilemap.GetCellCenterWorld(cellPos);
                    worldPos.z = 0f;

                    foreach (PaintEntry entry in entries)
                    {
                        if (!entry.enabled || entry.prefab == null)
                        {
                            continue;
                        }

                        if (Random.value > entry.density)
                        {
                            continue;
                        }

                        Vector3 spawnPos = worldPos;

                        if (randomOffsetInTile)
                        {
                            Vector3 cellSize = groundTilemap.cellSize;
                            spawnPos += new Vector3(
                                Random.Range(-cellSize.x * 0.4f, cellSize.x * 0.4f),
                                Random.Range(-cellSize.y * 0.4f, cellSize.y * 0.4f),
                                0f
                            );
                        }

                        GameObject obj = (GameObject)PrefabUtility.InstantiatePrefab(entry.prefab);
                        obj.transform.position = spawnPos;

                        if (parentTransform != null)
                        {
                            obj.transform.SetParent(parentTransform, true);
                        }

                        Undo.RegisterCreatedObjectUndo(obj, "Place Environment Object");
                        spawnCount++;
                    }
                }
            }

            Undo.CollapseUndoOperations(undoGroup);
            Debug.Log($"[EnvironmentPainter] 배치 완료 — {spawnCount}개 (시드: {usedSeed})");
        }

        /// <summary>
        /// 부모 오브젝트의 모든 자식을 제거합니다.
        /// </summary>
        private void Clear()
        {
            if (parentTransform == null)
            {
                return;
            }

            List<GameObject> toDestroy = new List<GameObject>();

            foreach (Transform child in parentTransform)
            {
                toDestroy.Add(child.gameObject);
            }

            foreach (GameObject obj in toDestroy)
            {
                Undo.DestroyObjectImmediate(obj);
            }
        }

        #endregion

        #region 유틸리티

        private bool HasEnabledEntry()
        {
            foreach (PaintEntry entry in entries)
            {
                if (entry.enabled && entry.prefab != null)
                {
                    return true;
                }
            }

            return false;
        }

        #endregion
    }
}
