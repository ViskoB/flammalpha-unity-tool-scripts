using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using FlammAlpha.UnityTools.Common;

namespace FlammAlpha.UnityTools.Animation
{
    /// <summary>
    /// Editor window for managing animation hierarchy paths.
    /// Allows replacing paths in animation clips with new values.
    /// </summary>
    [InitializeOnLoad]
    public class AnimationHierarchyEditor : EditorWindow
    {
        // Fields
        private readonly Dictionary<string, List<EditorCurveBinding>> paths = new();
        private readonly List<string> pathsKeys = new();
        private static GUIContent resetIcon;
        private AnimationClip[] animationClips;
        private Vector2 scrollPos;
        private string[] tempPathOverrides;
        private Animator animatorObject;
        private bool regexReplace;
        private string oldPathValue = "Root";
        private string newPathValue = "SomeNewObject/Root";
        private EditorListUtility.FoldoutManager<AnimationClip> clipFoldouts = new();
        [MenuItem("Tools/FlammAlpha/Animation Hierarchy Editor")]
        private static void ShowWindow() => GetWindow<AnimationHierarchyEditor>();

        private void OnSelectionChange()
        {
            animationClips = Selection.GetFiltered<AnimationClip>(SelectionMode.Assets);
            FillModel();
            Repaint();
        }

        private void OnGUI()
        {
            if (animationClips == null || animationClips.Length == 0)
            {
                DrawTitle("Please select an Animation Clip");
                return;
            }

            scrollPos = GUILayout.BeginScrollView(scrollPos);

            using (new GUILayout.VerticalScope("helpbox"))
            {
                animatorObject = (Animator)EditorGUILayout.ObjectField("Root Animator:", animatorObject, typeof(Animator), true);
            }

            GUILayout.Space(12);

            using (new GUILayout.VerticalScope("helpbox"))
            {
                using (new EditorGUILayout.HorizontalScope("box"))
                {
                    oldPathValue = EditorGUILayout.TextField(oldPathValue);
                    newPathValue = EditorGUILayout.TextField(newPathValue);

                    using (new EditorGUI.DisabledScope(string.IsNullOrEmpty(oldPathValue)))
                    {
                        if (GUILayout.Button(new GUIContent("Replace Path", "Replaces the old string (left) with the new string (right). Field on the left will be used as a Regex Pattern if Use Regex (@) is enabled.")))
                        {
                            UpdatePath(oldPathValue, newPathValue, true);
                        }
                    }
                    regexReplace = GUILayout.Toggle(regexReplace, new GUIContent("@", "Use Regex"), "button", GUILayout.Width(22));
                }
            }

            GUILayout.Space(12);

            // Expand/Collapse All buttons
            EditorListUtility.DrawExpandCollapseButtons(
                () => clipFoldouts.SetAll(animationClips, true),
                () => clipFoldouts.SetAll(animationClips, false)
            );

            GUILayout.Space(12);

            for (int clipIndex = 0; clipIndex < animationClips.Length; ++clipIndex)
            {
                var clip = animationClips[clipIndex];

                // Ensure foldout state exists for this clip
                clipFoldouts.EnsureExists(clip);

                // Draw clip as a list item with foldout (vertical layout)
                EditorListUtility.DrawListItem(clipIndex, () =>
                {
                    // Collapsible header
                    clipFoldouts[clip] = EditorListUtility.DrawCollapsibleHeader(clipFoldouts[clip], $"Clip: {clip.name}");

                    if (clipFoldouts[clip])
                    {
                        using (EditorListUtility.CreateIndentScope())
                        {
                            DisplayPathItemsForClip(clip);
                        }
                    }
                });

                EditorListUtility.DrawSectionSpacing(clipIndex, animationClips.Length);
            }

            GUILayout.Space(40);
            GUILayout.EndScrollView();
        }

        private void DisplayPathItemsForClip(AnimationClip clip)
        {
            var allBindings = AnimationUtility.GetCurveBindings(clip).Concat(AnimationUtility.GetObjectReferenceCurveBindings(clip)).ToList();
            var grouped = allBindings.GroupBy(b => b.path).Select(g => new { Path = g.Key, Bindings = g.ToList() }).ToList();
            GUIStyle resetButtonStyle = new() { contentOffset = new Vector2(0, 3.5f) };

            for (int i = 0; i < grouped.Count; ++i)
            {
                string path = grouped[i].Path;
                GameObject obj = FindObjectInRoot(path);
                var properties = grouped[i].Bindings;

                // Draw path items as simple horizontal layouts without backgrounds
                using (new EditorGUILayout.HorizontalScope())
                {
                    int globalPathIndex = pathsKeys.IndexOf(path);
                    bool isModifiedPath = globalPathIndex >= 0 && tempPathOverrides[globalPathIndex] != path;

                    using (new EditorGUI.DisabledScope(!isModifiedPath))
                    {
                        if (GUILayout.Button(resetIcon, resetButtonStyle, GUILayout.ExpandWidth(false)))
                            tempPathOverrides[globalPathIndex] = path;
                    }
                    tempPathOverrides[globalPathIndex] = EditorGUILayout.TextField(tempPathOverrides[globalPathIndex]);

                    using (new EditorGUI.DisabledScope(!isModifiedPath))
                    {
                        if (GUILayout.Button("Apply", GUILayout.Width(60)))
                            UpdatePath(path, tempPathOverrides[globalPathIndex], false);
                    }

                    GUILayout.Label($"({properties.Count})", GUILayout.Width(40));

                    Color prevColor = GUI.color;
                    GUI.color = obj ? Color.green : Color.red;
                    EditorGUI.BeginChangeCheck();
                    GameObject newObj = (GameObject)EditorGUILayout.ObjectField(obj, typeof(GameObject), true);
                    if (EditorGUI.EndChangeCheck())
                        UpdatePath(path, ChildPath(newObj), false);
                    GUI.color = prevColor;
                }

                // Add minimal spacing between path items
                if (i < grouped.Count - 1)
                    GUILayout.Space(1);
            }
        }

        private void FillModel()
        {
            paths.Clear();
            pathsKeys.Clear();

            if (animationClips != null)
            {
                // Initialize foldout states for new clips
                foreach (var clip in animationClips)
                {
                    clipFoldouts.EnsureExists(clip);
                }

                foreach (var animationClip in animationClips)
                {
                    FillModelWithCurves(AnimationUtility.GetCurveBindings(animationClip));
                    FillModelWithCurves(AnimationUtility.GetObjectReferenceCurveBindings(animationClip));
                }
                tempPathOverrides = pathsKeys.ToArray();
            }
            else
            {
                tempPathOverrides = System.Array.Empty<string>();
            }
        }

        private void FillModelWithCurves(EditorCurveBinding[] curves)
        {
            foreach (var curveData in curves)
            {
                string key = curveData.path;
                if (paths.ContainsKey(key))
                {
                    paths[key].Add(curveData);
                }
                else
                {
                    paths[key] = new List<EditorCurveBinding> { curveData };
                    pathsKeys.Add(key);
                }
            }
        }

        private void UpdatePath(string oldPath, string newPath, bool matchWholeWord)
        {
            if (oldPath == newPath) return;
            bool identicalMayExist = !matchWholeWord && paths.TryGetValue(newPath, out _);
            try
            {
                AssetDatabase.StartAssetEditing();
                for (int clipIndex = 0; clipIndex < animationClips.Length; ++clipIndex)
                {
                    AnimationClip animationClip = animationClips[clipIndex];
                    Undo.RecordObject(animationClip, "Animation Hierarchy Change");
                    var curves = AnimationUtility.GetCurveBindings(animationClip).Concat(AnimationUtility.GetObjectReferenceCurveBindings(animationClip)).ToList();
                    foreach (var c in curves)
                    {
                        var binding = c;
                        var curve = AnimationUtility.GetEditorCurve(animationClip, binding);
                        var objectReferenceCurve = AnimationUtility.GetObjectReferenceCurve(animationClip, binding);
                        bool isFloatCurve = curve != null;

                        if (isFloatCurve) AnimationUtility.SetEditorCurve(animationClip, binding, null);
                        else AnimationUtility.SetObjectReferenceCurve(animationClip, binding, null);

                        if (!matchWholeWord && binding.path == oldPath)
                        {
                            if (identicalMayExist && curves.Any(c2 => c2.path == newPath && c2.type == c.type && c2.propertyName == c.propertyName))
                            {
                                Debug.LogWarning($"Identical settings curve already exists. Skipping curve.\nPath: {c.path}\nType: {c.type.Name}\nProperty: {c.propertyName}");
                            }
                            else binding.path = newPath;
                        }
                        else if (matchWholeWord)
                        {
                            binding.path = regexReplace
                                ? Regex.Replace(binding.path, oldPath, newPath)
                                : binding.path.Replace(oldPath, newPath);
                        }

                        if (isFloatCurve) AnimationUtility.SetEditorCurve(animationClip, binding, curve);
                        else AnimationUtility.SetObjectReferenceCurve(animationClip, binding, objectReferenceCurve);
                    }
                    DisplayProgress(clipIndex);
                }
            }
            finally
            {
                AssetDatabase.StopAssetEditing();
                EditorUtility.ClearProgressBar();
            }
            FillModel();
            Repaint();
        }

        // Unity Event Methods
        private void OnFocus() => OnSelectionChange();

        private void OnEnable()
        {
            Undo.undoRedoPerformed -= FillModel;
            Undo.undoRedoPerformed += FillModel;
            resetIcon = new(EditorGUIUtility.IconContent("d_Refresh")) { tooltip = "Reset Dimensions" };
        }
        private void OnDisable() => Undo.undoRedoPerformed -= FillModel;
        // Helper Methods
        private GameObject FindObjectInRoot(string path) => animatorObject ? animatorObject.transform.Find(path)?.gameObject : null;

        private string ChildPath(GameObject obj)
        {
            if (!animatorObject) throw new UnityException("Please assign the Root Animator first!");
            if (!obj.transform.IsChildOf(animatorObject.transform))
                throw new UnityException($"Object must belong to {animatorObject.name} !");
            return AnimationUtility.CalculateTransformPath(obj.transform, animatorObject.transform);
        }

        private static void DrawTitle(string title)
        {
            using (new GUILayout.HorizontalScope("in bigtitle"))
                GUILayout.Label(title, EditorStyles.boldLabel);
        }

        private void DisplayProgress(int clipIndex)
        {
            float progress = (float)clipIndex / animationClips.Length;
            EditorUtility.DisplayProgressBar("Animation Hierarchy Progress", "Editing animations.", progress);
        }
    }
}
