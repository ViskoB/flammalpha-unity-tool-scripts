using UnityEditor;
using UnityEngine;
using System.Collections.Generic;

namespace FlammAlpha.UnityTools.BoundingBox
{
    /// <summary>
    /// Unity Editor window for visualizing and editing bounding boxes of SkinnedMeshRenderers.
    /// Provides an intuitive interface to adjust local bounds with real-time scene view visualization.
    /// </summary>
    public class BoundingBoxFinderWindow : EditorWindow
    {
        private GameObject targetRoot;
        private Vector2 scrollPosition;

        private readonly List<RendererBoundsInfo> skinnedMeshRendererBoundsList = new();
        private static readonly List<RendererBoundsInfo> _currentBoundingBoxes = new();
        private static bool _drawGizmos;
        private static int _hoveredBoxIdx = -1;
        private static int _selectedBoxIdx = -1;
        private static bool _gizmosEnabled = true;
        private static bool _blockScenePicking = true;

        [MenuItem("Tools/FlammAlpha/Bounding Box Finder")]
        public static void ShowWindow()
        {
            GetWindow<BoundingBoxFinderWindow>("Bounding Box Finder");
        }

        private static string GetBreadcrumbPathRelative(GameObject targetRoot, GameObject go)
        {
            var pathSegments = new List<string>();
            var current = go.transform;
            while (current != null && current.gameObject != targetRoot)
            {
                pathSegments.Insert(0, current.name);
                current = current.parent;
            }
            return string.Join("/", pathSegments);
        }

        private void UpdateSkinnedMeshRendererBoundsList()
        {
            skinnedMeshRendererBoundsList.Clear();
            _currentBoundingBoxes.Clear();
            if (targetRoot == null)
            {
                _drawGizmos = false;
                SceneView.RepaintAll();
                return;
            }
            var foundInfos = new List<RendererBoundsInfo>();
            foreach (var skinnedRenderer in targetRoot.GetComponentsInChildren<SkinnedMeshRenderer>(true))
            {
                Transform boneTransform = skinnedRenderer.rootBone != null ? skinnedRenderer.rootBone : skinnedRenderer.transform;
                foundInfos.Add(new RendererBoundsInfo
                {
                    GameObject = skinnedRenderer.gameObject,
                    Bounds = skinnedRenderer.localBounds,
                    BoneTransform = boneTransform
                });
            }
            foundInfos.Sort((a, b) => GetBreadcrumbPathRelative(targetRoot, a.GameObject).CompareTo(GetBreadcrumbPathRelative(targetRoot, b.GameObject)));
            foreach (var info in foundInfos)
            {
                skinnedMeshRendererBoundsList.Add(info);
                _currentBoundingBoxes.Add(info);
            }
            _drawGizmos = skinnedMeshRendererBoundsList.Count > 0;
            SceneView.RepaintAll();
            OnSelectionChanged();
        }

        private void OnEnable()
        {
            SceneView.duringSceneGui += OnSceneGUI;
            UpdateSkinnedMeshRendererBoundsList();
            Selection.selectionChanged += OnSelectionChanged;
            OnSelectionChanged();
        }

        private void OnDisable()
        {
            SceneView.duringSceneGui -= OnSceneGUI;
            _drawGizmos = false;
            _currentBoundingBoxes.Clear();
            SceneView.RepaintAll();
            Selection.selectionChanged -= OnSelectionChanged;
        }

        private void OnSelectionChanged()
        {
            _selectedBoxIdx = -1;
            if (_currentBoundingBoxes != null && Selection.activeGameObject != null)
            {
                for (int i = 0; i < _currentBoundingBoxes.Count; i++)
                {
                    if (_currentBoundingBoxes[i].GameObject == Selection.activeGameObject)
                    {
                        _selectedBoxIdx = i;
                        break;
                    }
                }
            }
            SceneView.RepaintAll();
        }

        private void DrawBoundingBoxWithControls(RendererBoundsInfo info, int idx, Color color, float btnScale, float pickScale, Event e)
        {
            Handles.color = color;
            Matrix4x4 oldMatrix = Handles.matrix;
            Handles.matrix = info.BoneTransform.localToWorldMatrix;
            DrawBoundingBox(info.Bounds);
            Handles.matrix = oldMatrix;
        }

        private void DrawAllBoundingBoxes(Event e)
        {
            for (int i = 0; i < _currentBoundingBoxes.Count; i++)
            {
                if (i == _selectedBoxIdx || i == _hoveredBoxIdx)
                    continue;
                var info = _currentBoundingBoxes[i];
                DrawBoundingBoxWithControls(info, i, Color.yellow, 0.05f, 0.08f, e);
            }
            if (_hoveredBoxIdx != -1 && _hoveredBoxIdx != _selectedBoxIdx)
            {
                var info = _currentBoundingBoxes[_hoveredBoxIdx];
                DrawBoundingBoxWithControls(info, _hoveredBoxIdx, Color.cyan, 0.10f, 0.16f, e);
            }
            if (_selectedBoxIdx != -1)
            {
                var info = _currentBoundingBoxes[_selectedBoxIdx];
                DrawBoundingBoxWithControls(info, _selectedBoxIdx, Color.green, 0.10f, 0.16f, e);
            }
        }

        private int FindHoveredBoundingBox(Ray mouseRay, float maxDistanceToEdge)
        {
            float minDist = float.MaxValue;
            int hoveredIdx = -1;
            for (int i = 0; i < _currentBoundingBoxes.Count; i++)
            {
                var info = _currentBoundingBoxes[i];
                Vector3[] corners = GetBoxCorners(info.Bounds);
                Matrix4x4 localToWorld = info.BoneTransform.localToWorldMatrix;
                for (int edge = 0; edge < _boundingBoxEdges.GetLength(0); edge++)
                {
                    Vector3 a = localToWorld.MultiplyPoint3x4(corners[_boundingBoxEdges[edge, 0]]);
                    Vector3 b = localToWorld.MultiplyPoint3x4(corners[_boundingBoxEdges[edge, 1]]);
                    float dist = DistanceRayToSegment(mouseRay, a, b, out float alongRay);
                    if (dist < minDist && dist < maxDistanceToEdge && alongRay > 0)
                    {
                        minDist = dist;
                        hoveredIdx = i;
                    }
                }
            }
            return hoveredIdx;
        }

        private void OnSceneGUI(SceneView sceneView)
        {
            if (!_drawGizmos || !_gizmosEnabled || _currentBoundingBoxes.Count == 0)
                return;

            Event e = Event.current;
            Ray mouseRay = HandleUtility.GUIPointToWorldRay(e.mousePosition);
            const float maxDistanceToEdge = 0.11f;
            int newHoveredIdx = FindHoveredBoundingBox(mouseRay, maxDistanceToEdge);
            
            // Update hover state and repaint if it changed
            if (_hoveredBoxIdx != newHoveredIdx)
            {
                _hoveredBoxIdx = newHoveredIdx;
                Repaint(); // Repaint the window to update list highlighting
            }

            if (e.type == EventType.MouseDown && e.button == 0 && _hoveredBoxIdx != -1)
            {
                var hitInfo = _currentBoundingBoxes[_hoveredBoxIdx];
                Selection.activeGameObject = hitInfo.GameObject;
                EditorGUIUtility.PingObject(hitInfo.GameObject);
                e.Use();
            }

            DrawAllBoundingBoxes(e);

            sceneView.Repaint();
            if (_blockScenePicking && _gizmosEnabled && _hoveredBoxIdx != -1)
            {
                HandleUtility.AddDefaultControl(GUIUtility.GetControlID(FocusType.Passive));
            }
        }

        private static float DistanceRayToSegment(Ray ray, Vector3 a, Vector3 b, out float alongRay)
        {
            Vector3 u = ray.direction;
            Vector3 v = b - a;
            Vector3 w0 = ray.origin - a;
            float a_dot = Vector3.Dot(u, u);
            float b_dot = Vector3.Dot(u, v);
            float c_dot = Vector3.Dot(v, v);
            float d_dot = Vector3.Dot(u, w0);
            float e_dot = Vector3.Dot(v, w0);
            float denom = a_dot * c_dot - b_dot * b_dot;
            float s, t;

            if (denom < 1e-6f)
            {
                s = 0f;
                t = (b_dot > c_dot ? 0f : (b_dot < 0 ? 1f : b_dot / c_dot));
            }
            else
            {
                s = (b_dot * e_dot - c_dot * d_dot) / denom;
                t = (a_dot * e_dot - b_dot * d_dot) / denom;
                t = Mathf.Clamp01(t);
            }
            Vector3 pointOnRay = ray.origin + u * s;
            Vector3 pointOnSeg = a + v * t;
            alongRay = s;
            return Vector3.Distance(pointOnRay, pointOnSeg);
        }

        private static Vector3[] GetBoxCorners(Bounds bounds)
        {
            Vector3 center = bounds.center;
            Vector3 half = bounds.size * 0.5f;
            return new Vector3[8]
            {
                center + new Vector3(-half.x,  half.y,  half.z),
                center + new Vector3( half.x,  half.y,  half.z),
                center + new Vector3(-half.x, -half.y,  half.z),
                center + new Vector3( half.x, -half.y,  half.z),
                center + new Vector3(-half.x,  half.y, -half.z),
                center + new Vector3( half.x,  half.y, -half.z),
                center + new Vector3(-half.x, -half.y, -half.z),
                center + new Vector3( half.x, -half.y, -half.z)
            };
        }

        private static readonly int[,] _boundingBoxEdges =
        {
            {0,1}, {1,3}, {3,2}, {2,0},
            {4,5}, {5,7}, {7,6}, {6,4},
            {0,4}, {1,5}, {2,6}, {3,7}
        };

        private static void DrawBoundingBox(Bounds bounds)
        {
            Vector3[] pts = GetBoxCorners(bounds);
            for (int i = 0; i < _boundingBoxEdges.GetLength(0); i++)
                Handles.DrawLine(pts[_boundingBoxEdges[i, 0]], pts[_boundingBoxEdges[i, 1]]);
        }

        private string EllipsizeBreadcrumbLeft(string breadcrumb, GUIStyle style, float width)
        {
            if (string.IsNullOrEmpty(breadcrumb)) return breadcrumb;
            Vector2 size = style.CalcSize(new GUIContent(breadcrumb));
            if (size.x <= width) return breadcrumb;

            const string ellipsis = "\u2026";
            int len = breadcrumb.Length;
            int left = 0;
            while (left < len)
            {
                string sub = ellipsis + breadcrumb.Substring(left);
                size = style.CalcSize(new GUIContent(sub));
                if (size.x <= width) break;
                left++;
            }
            return left > 0 ? ellipsis + breadcrumb.Substring(left) : breadcrumb;
        }

        private void OnGUI()
        {
            EditorGUILayout.BeginHorizontal();
            _gizmosEnabled = EditorGUILayout.Toggle("Enable Bounding Box Gizmos", _gizmosEnabled);
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.LabelField("Bounding Boxes in Selection", EditorStyles.boldLabel);
            EditorGUILayout.LabelField("Select the root GameObject (Prefab instance, FBX, etc.):");
            var selectedRoot = (GameObject)EditorGUILayout.ObjectField(targetRoot, typeof(GameObject), true);
            if (selectedRoot != targetRoot)
            {
                targetRoot = selectedRoot;
                UpdateSkinnedMeshRendererBoundsList();
            }
            EditorGUILayout.Space();
            if (skinnedMeshRendererBoundsList.Count == 0)
            {
                EditorGUILayout.HelpBox("No SkinnedMeshRenderers found in selected GameObject.", MessageType.Info);
                return;
            }
            
            // Handle mouse movement to detect when we're outside the list area
            if (Event.current.type == EventType.MouseMove)
            {
                Repaint();
            }
            
            scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition);

            bool foundHover = false;
            for (int i = 0; i < skinnedMeshRendererBoundsList.Count; i++)
            {
                var info = skinnedMeshRendererBoundsList[i];

                Color originalColor = GUI.backgroundColor;
                Color backgroundColor;

                if (i == _hoveredBoxIdx)
                    backgroundColor = new Color(0.1f, 0.1f, 0.3f); // Blue tint for hovered
                else if (i == _selectedBoxIdx)
                    backgroundColor = new Color(0.1f, 0.3f, 0.1f);
                else if (i % 2 == 1)
                    backgroundColor = new Color(0.15f, 0.15f, 0.15f);
                else
                    backgroundColor = new Color(0.25f, 0.25f, 0.25f);

                GUI.backgroundColor = backgroundColor;

                // Create a custom style that preserves the background color
                GUIStyle boxStyle = new GUIStyle(GUI.skin.box);
                boxStyle.normal.background = EditorGUIUtility.whiteTexture;
                
                Rect verticalRect = EditorGUILayout.BeginVertical(boxStyle);
                
                // Check for mouse hover over this list entry
                if (Event.current.type == EventType.Repaint)
                {
                    if (verticalRect.Contains(Event.current.mousePosition))
                    {
                        foundHover = true;
                        if (_hoveredBoxIdx != i)
                        {
                            _hoveredBoxIdx = i;
                            SceneView.RepaintAll();
                            Repaint();
                        }
                    }
                }

                GUIStyle labelStyle;
                if (i == _hoveredBoxIdx)
                    labelStyle = new GUIStyle(EditorStyles.boldLabel) { normal = { textColor = Color.cyan } };
                else if (i == _selectedBoxIdx)
                    labelStyle = new GUIStyle(EditorStyles.boldLabel) { normal = { textColor = Color.green } };
                else
                    labelStyle = EditorStyles.boldLabel;

                string breadcrumb = GetBreadcrumbPathRelative(targetRoot, info.GameObject);
                if (string.IsNullOrEmpty(breadcrumb))
                    breadcrumb = "(Root)";

                float maxWidth = position.width - 56;
                string displayBreadcrumb = EllipsizeBreadcrumbLeft(breadcrumb, labelStyle, maxWidth);
                if (GUILayout.Button(new GUIContent(displayBreadcrumb, breadcrumb), labelStyle))
                    Selection.activeGameObject = info.GameObject;

                var skinnedRenderer = info.GameObject.GetComponent<SkinnedMeshRenderer>();
                if (skinnedRenderer != null)
                {
                    EditorGUI.BeginChangeCheck();
                    Vector3 newLocalCenter = EditorGUILayout.Vector3Field("Center", skinnedRenderer.localBounds.center);
                    Vector3 newLocalExtents = EditorGUILayout.Vector3Field("Extents", skinnedRenderer.localBounds.extents);
                    if (EditorGUI.EndChangeCheck())
                    {
                        Undo.RecordObject(skinnedRenderer, "Change SkinnedMeshRenderer Bounds");
                        skinnedRenderer.localBounds = new Bounds(newLocalCenter, newLocalExtents * 2f);
                        UpdateSkinnedMeshRendererBoundsList();
                        EditorGUILayout.EndVertical();
                        GUI.backgroundColor = originalColor;
                        if (i < skinnedMeshRendererBoundsList.Count - 1)
                            GUILayout.Space(2);
                        continue;
                    }
                }
                EditorGUILayout.EndVertical();
                GUI.backgroundColor = originalColor;
                if (i < skinnedMeshRendererBoundsList.Count - 1)
                    GUILayout.Space(2);
            }
            EditorGUILayout.EndScrollView();
            
            // Clear hover state if mouse is not over any list entry
            if (Event.current.type == EventType.Repaint && !foundHover && _hoveredBoxIdx != -1)
            {
                _hoveredBoxIdx = -1;
                SceneView.RepaintAll();
                Repaint();
            }
        }
    }

    /// <summary>
    /// Data structure to hold renderer bounds information.
    /// </summary>
    internal class RendererBoundsInfo
    {
        public GameObject GameObject;
        public Bounds Bounds;
        public Transform BoneTransform;
    }
}
