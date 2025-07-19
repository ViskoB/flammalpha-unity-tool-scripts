using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.Linq;
using FlammAlpha.UnityTools.Common;

namespace FlammAlpha.UnityTools.Materials
{
    /// <summary>
    /// Unity Editor window for fixing material instance issues.
    /// Can revert material instances to asset materials or create instances from assets.
    /// </summary>
    public class MaterialInstanceFix : EditorWindow
    {
        private Object targetRoot;
        private List<InstanceIssue> issues = new List<InstanceIssue>();
        private Vector2 scroll;

        [MenuItem("Tools/FlammAlpha/Material Instance Fix")]
        static void ShowWindow()
        {
            GetWindow<MaterialInstanceFix>("Material Instance Tools");
        }

        private void OnEnable()
        {
            // Auto-scan when window first opens if we have a target
            if (targetRoot != null && targetRoot is GameObject go)
            {
                ScanMaterialInstances(go);
            }
        }

        /// <summary>
        /// Represents a material instance issue found during scanning.
        /// </summary>
        private class InstanceIssue
        {
            public Renderer renderer;
            public int slot;
            public Material instance;
            public Material assetCandidate;
            public bool foldout = true;
            public bool selected = true;
        }

        private void OnGUI()
        {
            EditorGUILayout.LabelField("Material Instance Tools", EditorStyles.boldLabel);
            EditorGUILayout.Space();

            EditorGUILayout.LabelField("Select the root GameObject (Prefab instance, FBX, etc.):");
            EditorGUI.BeginChangeCheck();
            targetRoot = EditorGUILayout.ObjectField(targetRoot, typeof(GameObject), true);
            bool targetChanged = EditorGUI.EndChangeCheck();

            // Auto-refresh when target root changes
            if (targetChanged && targetRoot != null && targetRoot is GameObject go)
            {
                ScanMaterialInstances(go);
            }
            else if (targetChanged && targetRoot == null)
            {
                issues.Clear();
            }

            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Manual Refresh (Scan for Material Instances)"))
            {
                if (targetRoot != null && targetRoot is GameObject gameObject)
                {
                    ScanMaterialInstances(gameObject);
                }
                else
                {
                    EditorUtility.DisplayDialog("Error", "Please select a valid GameObject.", "OK");
                }
            }

            // Small debug button for creating instances
            if (GUILayout.Button("Debug: Create Instances", GUILayout.Width(150)))
            {
                if (targetRoot != null && targetRoot is GameObject debugGO)
                {
                    CreateInstancesForAssetMaterials(debugGO);
                }
                else
                {
                    EditorUtility.DisplayDialog("Error", "Please select a valid GameObject.", "OK");
                }
            }
            EditorGUILayout.EndHorizontal();

            if (issues.Count > 0)
            {
                EditorGUILayout.Space();
                EditorGUILayout.LabelField("Found Material Instances:", EditorStyles.boldLabel);

                DrawControlButtons();
                DrawIssuesList();
                DrawActionButton();
            }
        }

        private void DrawControlButtons()
        {
            EditorListUtility.DrawAllControlButtons(
                onExpandAll: () =>
                {
                    foreach (var issue in issues)
                        issue.foldout = true;
                },
                onCollapseAll: () =>
                {
                    foreach (var issue in issues)
                        issue.foldout = false;
                },
                onSelectAll: () =>
                {
                    foreach (var issue in issues)
                        issue.selected = true;
                },
                onDeselectAll: () =>
                {
                    foreach (var issue in issues)
                        issue.selected = false;
                }
            );
        }

        private void DrawIssuesList()
        {
            scroll = EditorGUILayout.BeginScrollView(scroll);
            for (int idx = 0; idx < issues.Count; idx++)
            {
                var issue = issues[idx];
                var hasCandidate = issue.assetCandidate != null;
                var customColor = hasCandidate ? (Color?)null : Color.red;

                var (newToggle, newFoldout) = EditorListUtility.DrawSelectableListItem(
                    index: idx,
                    isToggled: issue.selected,
                    foldout: issue.foldout,
                    title: $"Renderer: {issue.renderer.name} | Slot: {issue.slot}",
                    drawContent: () => DrawIssueContent(issue, hasCandidate),
                    customBackgroundColor: customColor,
                    onToggleChanged: (value) => issue.selected = value,
                    onFoldoutChanged: (value) => issue.foldout = value
                );

                issue.selected = newToggle;
                issue.foldout = newFoldout;
            }
            EditorGUILayout.EndScrollView();
        }

        private void DrawIssueContent(InstanceIssue issue, bool hasCandidate)
        {
            using (new EditorGUI.DisabledScope(true))
            {
                EditorGUILayout.ObjectField("Renderer", issue.renderer, typeof(Renderer), true);
                EditorGUILayout.LabelField($"Slot: {issue.slot}");
            }

            EditorGUILayout.ObjectField("Instance Material", issue.instance, typeof(Material), false);
            EditorGUILayout.ObjectField("Replacement Asset", issue.assetCandidate, typeof(Material), false);
            if (!hasCandidate)
            {
                GUIStyle redLabel = new GUIStyle(EditorStyles.boldLabel);
                redLabel.normal.textColor = Color.red;
                EditorGUILayout.LabelField("No suitable replacement asset found!", redLabel);
            }
        }

        private void DrawActionButton()
        {
            if (GUILayout.Button("Revert All Listed Material Instances"))
            {
                RevertScannedMaterials();
            }
        }

        /// <summary>
        /// Creates material instances for asset materials (debug function).
        /// </summary>
        /// <param name="root">Root GameObject to scan</param>
        private void CreateInstancesForAssetMaterials(GameObject root)
        {
            int createdCount = 0;
            foreach (var mr in root.GetComponentsInChildren<Renderer>(true))
            {
                var mats = mr.sharedMaterials;
                for (int i = 0; i < mats.Length; i++)
                {
                    var m = mats[i];
                    if (m != null && AssetDatabase.Contains(m))
                    {
                        Material inst = Object.Instantiate(m);
                        inst.name = m.name + " (Instance)";
                        mats[i] = inst;
                        Undo.RecordObject(mr, "Create Material Instance");
                        mr.sharedMaterials = mats;
                        EditorUtility.SetDirty(mr);
                        createdCount++;
                    }
                }
            }
            EditorUtility.DisplayDialog("Debug", $"Created {createdCount} material instances.", "Close");

            // Refresh the scan after creating instances
            if (targetRoot != null && targetRoot is GameObject go)
            {
                ScanMaterialInstances(go);
            }
        }

        /// <summary>
        /// Scans for material instances in the given GameObject hierarchy.
        /// </summary>
        /// <param name="root">Root GameObject to scan</param>
        private void ScanMaterialInstances(GameObject root)
        {
            issues.Clear();
            string[] matGuids = AssetDatabase.FindAssets("t:Material");
            List<Material> allMaterials = new List<Material>();

            foreach (var guid in matGuids)
            {
                var path = AssetDatabase.GUIDToAssetPath(guid);
                var mat = AssetDatabase.LoadAssetAtPath<Material>(path);
                allMaterials.Add(mat);
            }

            foreach (var mr in root.GetComponentsInChildren<Renderer>(true))
            {
                var mats = mr.sharedMaterials;
                for (int i = 0; i < mats.Length; i++)
                {
                    var m = mats[i];
                    if (m != null && !AssetDatabase.Contains(m))
                    {
                        var candidate = FindBestMaterialMatch(m, allMaterials);
                        issues.Add(new InstanceIssue { renderer = mr, slot = i, instance = m, assetCandidate = candidate });
                    }
                }
            }
        }

        /// <summary>
        /// Finds the best matching asset material for a given instance material.
        /// </summary>
        /// <param name="instance">Instance material to match</param>
        /// <param name="assets">List of asset materials to search</param>
        /// <returns>Best matching material or null if none found</returns>
        private Material FindBestMaterialMatch(Material instance, List<Material> assets)
        {
            // First priority: exact name and shader match
            var nameShader = assets.Where(a => a.name == instance.name && a.shader == instance.shader).ToList();
            if (nameShader.Count == 1)
                return nameShader[0];

            if (nameShader.Count > 1)
            {
                // Multiple matches, try to match by main texture too
                var mainTex = instance.HasProperty("_MainTex") ? instance.GetTexture("_MainTex") : null;
                foreach (var a in nameShader)
                {
                    if (a.HasProperty("_MainTex") && a.GetTexture("_MainTex") == mainTex)
                        return a;
                }
                return nameShader[0];
            }

            // Second priority: name match only
            var byName = assets.Where(a => a.name == instance.name).ToList();
            if (byName.Count == 1)
                return byName[0];

            // Third priority: shader and main texture match
            var shaderAndTex = assets.Where(a =>
                a.shader == instance.shader &&
                a.HasProperty("_MainTex") == instance.HasProperty("_MainTex") &&
                (!a.HasProperty("_MainTex") || a.GetTexture("_MainTex") == instance.GetTexture("_MainTex"))
            ).ToList();
            if (shaderAndTex.Count > 0)
                return shaderAndTex[0];

            return null;
        }

        /// <summary>
        /// Reverts selected material instances to their matching asset materials.
        /// </summary>
        private void RevertScannedMaterials()
        {
            int fixedCount = 0;
            foreach (var issue in issues)
            {
                if (issue.selected && issue.assetCandidate != null)
                {
                    var mats = issue.renderer.sharedMaterials;
                    mats[issue.slot] = issue.assetCandidate;
                    Undo.RecordObject(issue.renderer, "Revert Material Instance");
                    issue.renderer.sharedMaterials = mats;
                    EditorUtility.SetDirty(issue.renderer);
                    fixedCount++;
                }
            }
            EditorUtility.DisplayDialog("Finished", $"Reverted {fixedCount} material assignments.", "Close");
            issues.Clear();
        }
    }
}
