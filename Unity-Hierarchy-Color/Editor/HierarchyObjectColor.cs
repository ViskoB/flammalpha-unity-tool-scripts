/*************************************************************************************
* FlammAlpha 2024
* Colors the Hierarchy-View in Unity
*************************************************************************************/
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace UnityHierarchyColor
{
    /// <summary> Sets a background color for game objects in the Hierarchy tab</summary>
    [InitializeOnLoad]
    public class HierarchyObjectColor
    {
        const int MaxDepth = 100;

        // Store the current config reference
        private static HierarchyHighlightConfig currentConfig;

        private static Dictionary<string, Type> typeCache = new Dictionary<string, Type>();

        private static Dictionary<int, int[]> cachedCounts = new();
        private static Dictionary<int, int[]> cachedCountsOnSelf = new();
        private static int lastRepaintFrame = -1;
        private static int customFrameCounter = 1;

        static HierarchyObjectColor()
        {
            HierarchyHighlightConfigUtility.OnConfigUpdate += OnConfigUpdate;
            EditorApplication.hierarchyWindowItemOnGUI += HandleHierarchyWindowItemOnGUI;
            HierarchyHighlightConfigUtility.ForceLoadConfig();
        }

        private static void OnConfigUpdate(HierarchyHighlightConfig config)
        {
            currentConfig = config;

            // Pre-cache all types for symbols so we don't call Type.GetType at runtime
            typeCache.Clear();
            if (currentConfig?.typeConfigs != null)
            {
                foreach (var tce in currentConfig.typeConfigs)
                {
                    if (tce == null || string.IsNullOrEmpty(tce.typeName)) continue;
                    if (!typeCache.ContainsKey(tce.typeName))
                    {
                        typeCache[tce.typeName] = Type.GetType(tce.typeName);
                    }
                }
            }
        }

        private static Type GetCachedType(string typeName)
        {
            if (typeCache.TryGetValue(typeName, out var cachedType))
                return cachedType;
            return null;
        }

        private static List<TypeConfigEntry> GetTypeConfigs()
        {
            if (currentConfig == null) return null;
            return currentConfig.typeConfigs;
        }

        private static List<NameHighlightEntry> GetNameHighlightConfigs()
        {
            if (currentConfig == null) return null;
            return currentConfig.nameHighlightConfigs;
        }

        private static void HandleHierarchyWindowItemOnGUI(int instanceID, Rect selectionRect)
        {
            var typeConfigs = GetTypeConfigs();
            var nameHighlightConfigs = GetNameHighlightConfigs();
            if (typeConfigs == null)
                return;

            GameObject obj = EditorUtility.InstanceIDToObject(instanceID) as GameObject;
            if (obj == null) return;

            if (Event.current.type == EventType.Repaint)
            {
                // Time.frameCount may be 0 in some Editor contexts, use a custom counter if needed
                int frame = Application.isPlaying ? Time.frameCount : ++customFrameCounter;
                if (frame != lastRepaintFrame)
                {
                    cachedCounts.Clear();
                    cachedCountsOnSelf.Clear();
                    lastRepaintFrame = frame;
                }
            }

            Color textColor = Color.white;
            Color disabledTextColor = new Color(0.4f, 0.4f, 0.4f);
            Texture texture = !obj.activeSelf ? AssetPreview.GetMiniThumbnail(obj) : null;

            int[] counts;
            if (!cachedCounts.TryGetValue(instanceID, out counts))
            {
                counts = new int[typeConfigs.Count];
                AccumulateCountsRecursive(obj.transform, counts);
                cachedCounts[instanceID] = counts;
            }

            int[] countsOnSelf;
            if (!cachedCountsOnSelf.TryGetValue(instanceID, out countsOnSelf))
            {
                countsOnSelf = new int[typeConfigs.Count];
                AccumulateCountSelf(obj.transform, countsOnSelf);
                cachedCountsOnSelf[instanceID] = countsOnSelf;
            }

            float nextX = selectionRect.xMax;

            Color nameBackground = EditorGUIUtility.isProSkin ? new Color(0.21f, 0.21f, 0.21f, 1) : Color.white;

            // === Prefix Match First ===
            bool prefixHighlightFound = false;
            if (nameHighlightConfigs != null && nameHighlightConfigs.Count > 0)
            {
                foreach (var nh in nameHighlightConfigs)
                {
                    if (string.IsNullOrEmpty(nh.prefix)) continue;

                    bool match = false;
                    if (nh.propagateUpwards)
                    {
                        match = NameOrChildHasPrefixRecursive(obj, nh.prefix);
                    }
                    else
                    {
                        match = obj.name.StartsWith(nh.prefix, StringComparison.Ordinal);
                    }
                    if (match)
                    {
                        nameBackground = nh.color;
                        prefixHighlightFound = true;
                        break;
                    }
                }
            }

            // === Component matching only if no prefix match ===
            if (!prefixHighlightFound)
            {
                for (int i = 0; i < typeConfigs.Count; i++)
                {
                    var tce = typeConfigs[i];
                    if (tce == null || string.IsNullOrEmpty(tce.typeName))
                        continue;
                    Type type = GetCachedType(tce.typeName);
                    if (type == null)
                        continue;

                    bool match = false;
                    if (tce.propagateUpwards)
                    {
                        match = HasComponentInHierarchy(obj, type);
                    }
                    else
                    {
                        match = obj.GetComponent(type) != null;
                    }

                    if (match)
                    {
                        nameBackground = tce.color;
                        break; // Use the first matching rule for color
                    }
                }
            }

            if (Selection.instanceIDs.Contains(instanceID))
                nameBackground = new Color(0.24f, 0.48f, 0.90f, 1f);
            else if (selectionRect.Contains(Event.current.mousePosition) && Event.current.type == EventType.Repaint)
                nameBackground = new Color(1f, 1f, 1f, 0.07f);

            GUIStyle countStyle = new GUIStyle(EditorStyles.label)
            {
                alignment = TextAnchor.MiddleCenter,
                fontSize = 11,
                fontStyle = FontStyle.Bold,
                normal = { textColor = Color.white }
            };

            for (int i = typeConfigs.Count - 1; i >= 0; i--)
            {
                if (counts[i] == 0 && countsOnSelf[i] == 0)
                    continue;

                if (string.IsNullOrWhiteSpace(typeConfigs[i].symbol))
                    continue;

                string totalCount = counts[i] == countsOnSelf[i] ? "" : $"{counts[i]}";
                string selfCount = countsOnSelf[i] > 0 ? $" ({countsOnSelf[i]})" : "";
                string label = $"{typeConfigs[i].symbol} {totalCount}{selfCount}".Trim();
                float labelWidth = Mathf.Max(EditorStyles.label.CalcSize(new GUIContent(label)).x + 6, 32);

                nextX -= labelWidth + 2f;
                Rect countRect = new Rect(nextX, selectionRect.y, labelWidth, selectionRect.height);

                EditorGUI.DrawRect(countRect, typeConfigs[i].color);
                EditorGUI.LabelField(countRect, new GUIContent(label, typeConfigs[i].typeName), countStyle);
            }

            Rect nameRect = new Rect(selectionRect.x + selectionRect.height, selectionRect.y, nextX - selectionRect.x - selectionRect.height, selectionRect.height);

            GUIStyle nameStyle = new GUIStyle(EditorStyles.label)
            {
                normal = { textColor = obj.activeInHierarchy ? textColor : disabledTextColor },
                fontStyle = obj.activeInHierarchy ? FontStyle.Normal : FontStyle.Bold,
                alignment = TextAnchor.MiddleLeft
            };

            EditorGUI.DrawRect(nameRect, nameBackground);
            EditorGUI.LabelField(nameRect, obj.name, nameStyle);

            if (texture != null)
            {
                Rect iconRect = new Rect(selectionRect.x, selectionRect.y, selectionRect.height, selectionRect.height);
                GUI.DrawTexture(iconRect, texture, ScaleMode.ScaleToFit, true);
            }
        }

        static bool HasComponentInHierarchy(GameObject obj, Type componentType, int depth = 0)
        {
            if (depth > MaxDepth)
            {
                Debug.LogWarning("HierarchyObjectColor: Maximum recursion depth reached in HasComponentInHierarchy. Possible circular reference detected.");
                return false;
            }
            if (obj.GetComponent(componentType) != null)
                return true;
            foreach (Transform child in obj.transform)
            {
                if (HasComponentInHierarchy(child.gameObject, componentType, depth + 1))
                    return true;
            }
            return false;
        }

        static bool NameOrChildHasPrefixRecursive(GameObject obj, string prefix, int depth = 0)
        {
            if (depth > MaxDepth)
            {
                Debug.LogWarning("HierarchyObjectColor: Maximum recursion depth reached in NameOrChildHasPrefixRecursive. Possible circular reference detected.");
                return false;
            }
            if (obj.name.StartsWith(prefix, StringComparison.Ordinal))
                return true;
            foreach (Transform child in obj.transform)
            {
                if (NameOrChildHasPrefixRecursive(child.gameObject, prefix, depth + 1))
                    return true;
            }
            return false;
        }

        static void AccumulateCountSelf(Transform obj, int[] counts)
        {
            var typeConfigs = GetTypeConfigs();
            if (typeConfigs == null)
                return;

            for (int i = 0; i < typeConfigs.Count; i++)
            {
                if (typeConfigs[i] == null || string.IsNullOrEmpty(typeConfigs[i].typeName))
                    continue;
                Type t = GetCachedType(typeConfigs[i].typeName);
                if (t != null)
                {
                    counts[i] += obj.GetComponents(t).Length;
                }
            }
        }

        static void AccumulateCountsRecursive(Transform obj, int[] counts, int depth = 0)
        {
            if (depth > MaxDepth)
            {
                Debug.LogWarning("HierarchyObjectColor: Maximum recursion depth reached in AccumulateCountsRecursive. Possible circular reference detected.");
                return;
            }
            AccumulateCountSelf(obj, counts);

            for (int c = 0; c < obj.childCount; ++c)
            {
                AccumulateCountsRecursive(obj.GetChild(c), counts, depth + 1);
            }
        }
    }
}
