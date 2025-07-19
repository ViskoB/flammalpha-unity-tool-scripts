using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.Linq;

namespace FlammAlpha.UnityTools.Materials
{
    /// <summary>
    /// Unity Editor window for fixing material instance issues.
    /// Can revert material instances to asset materials or create instances from assets.
    /// </summary>
    public class MaterialInstanceFix : EditorWindow
    {
        public enum Mode 
        { 
            RevertInstances, 
            CreateInstances 
        }
        
        private Mode mode = Mode.RevertInstances;
        private Object targetRoot;
        private List<InstanceIssue> issues = new List<InstanceIssue>();
        private Vector2 scroll;

        [MenuItem("Tools/FlammAlpha/Material Instance Fix")]
        static void ShowWindow()
        {
            GetWindow<MaterialInstanceFix>("Material Instance Tools");
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

            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Mode:", GUILayout.Width(40));
            mode = (Mode)EditorGUILayout.EnumPopup(mode, GUILayout.Width(160));
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.LabelField("Select the root GameObject (Prefab instance, FBX, etc.):");
            targetRoot = EditorGUILayout.ObjectField(targetRoot, typeof(GameObject), true);

            string scanBtn = (mode == Mode.RevertInstances) ? "Scan for Material Instances" : "Scan for Asset Materials";
            if (GUILayout.Button(scanBtn))
            {
                if (targetRoot != null && targetRoot is GameObject go)
                {
                    if (mode == Mode.RevertInstances)
                        ScanMaterialInstances(go);
                    else
                        ScanAssetMaterials(go);
                }
                else
                {
                    EditorUtility.DisplayDialog("Error", "Please select a valid GameObject.", "OK");
                }
            }

            if (issues.Count > 0)
            {
                EditorGUILayout.Space();
                EditorGUILayout.LabelField(
                    mode == Mode.RevertInstances ? "Found Material Instances:" : "Found Asset Materials:",
                    EditorStyles.boldLabel
                );

                DrawControlButtons();
                DrawIssuesList();
                DrawActionButton();
            }
        }

        private void DrawControlButtons()
        {
            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Expand All", GUILayout.Width(90)))
            {
                foreach (var issue in issues)
                    issue.foldout = true;
            }
            if (GUILayout.Button("Collapse All", GUILayout.Width(90)))
            {
                foreach (var issue in issues)
                    issue.foldout = false;
            }
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Select All", GUILayout.Width(90)))
            {
                foreach (var issue in issues)
                    issue.selected = true;
            }
            if (GUILayout.Button("Deselect All", GUILayout.Width(90)))
            {
                foreach (var issue in issues)
                    issue.selected = false;
            }
            EditorGUILayout.EndHorizontal();
        }

        private void DrawIssuesList()
        {
            scroll = EditorGUILayout.BeginScrollView(scroll);
            for (int idx = 0; idx < issues.Count; idx++)
            {
                var issue = issues[idx];
                var hasCandidate = issue.assetCandidate != null;
                var boxColor = hasCandidate ? GUI.backgroundColor : Color.red;
                var prevColor = GUI.backgroundColor;
                GUI.backgroundColor = boxColor;

                EditorGUILayout.BeginVertical(GUI.skin.box);
                EditorGUILayout.BeginHorizontal();
                issue.selected = EditorGUILayout.Toggle(issue.selected, GUILayout.Width(18));
                issue.foldout = EditorGUILayout.Foldout(issue.foldout, $"Renderer: {issue.renderer.name} | Slot: {issue.slot}", true);
                EditorGUILayout.EndHorizontal();
                
                if (issue.foldout)
                {
                    using (new EditorGUI.DisabledScope(true))
                    {
                        EditorGUILayout.ObjectField("Renderer", issue.renderer, typeof(Renderer), true);
                        EditorGUILayout.LabelField($"Slot: {issue.slot}");
                    }
                    
                    if (mode == Mode.RevertInstances)
                    {
                        EditorGUILayout.ObjectField("Instance Material", issue.instance, typeof(Material), false);
                        EditorGUILayout.ObjectField("Replacement Asset", issue.assetCandidate, typeof(Material), false);
                        if (!hasCandidate)
                        {
                            GUIStyle redLabel = new GUIStyle(EditorStyles.boldLabel);
                            redLabel.normal.textColor = Color.red;
                            EditorGUILayout.LabelField("No suitable replacement asset found!", redLabel);
                        }
                    }
                    else
                    {
                        EditorGUILayout.ObjectField("Asset Material", issue.assetCandidate, typeof(Material), false);
                    }
                }
                EditorGUILayout.EndVertical();
                GUI.backgroundColor = prevColor;
            }
            EditorGUILayout.EndScrollView();
        }

        private void DrawActionButton()
        {
            string actionBtn = (mode == Mode.RevertInstances)
                ? "Revert All Listed Material Instances"
                : "Instance All Listed Asset Materials";
            if (GUILayout.Button(actionBtn))
            {
                if (mode == Mode.RevertInstances)
                    RevertScannedMaterials();
                else
                    InstanceAssetMaterials();
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
        /// Scans for asset materials in the given GameObject hierarchy.
        /// </summary>
        /// <param name="root">Root GameObject to scan</param>
        private void ScanAssetMaterials(GameObject root)
        {
            issues.Clear();
            var matGuids = AssetDatabase.FindAssets("t:Material");
            HashSet<Material> assetMaterials = new HashSet<Material>();
            
            foreach (var guid in matGuids)
            {
                var path = AssetDatabase.GUIDToAssetPath(guid);
                var mat = AssetDatabase.LoadAssetAtPath<Material>(path);
                assetMaterials.Add(mat);
            }

            foreach (var mr in root.GetComponentsInChildren<Renderer>(true))
            {
                var mats = mr.sharedMaterials;
                for (int i = 0; i < mats.Length; i++)
                {
                    var m = mats[i];
                    if (m != null && AssetDatabase.Contains(m))
                    {
                        issues.Add(new InstanceIssue
                        {
                            renderer = mr,
                            slot = i,
                            instance = null,
                            assetCandidate = m
                        });
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

        /// <summary>
        /// Creates material instances from selected asset materials.
        /// </summary>
        private void InstanceAssetMaterials()
        {
            int createdCount = 0;
            foreach (var issue in issues)
            {
                if (issue.selected && issue.assetCandidate != null)
                {
                    var mats = issue.renderer.sharedMaterials;
                    if (mats[issue.slot] == issue.assetCandidate)
                    {
                        Material inst = Object.Instantiate(issue.assetCandidate);
                        inst.name = issue.assetCandidate.name + " (Instance)";
                        mats[issue.slot] = inst;
                        Undo.RecordObject(issue.renderer, "Create Material Instance");
                        issue.renderer.sharedMaterials = mats;
                        EditorUtility.SetDirty(issue.renderer);
                        createdCount++;
                    }
                }
            }
            EditorUtility.DisplayDialog("Finished", $"Created {createdCount} material instances.", "Close");
            issues.Clear();
        }
    }
}
