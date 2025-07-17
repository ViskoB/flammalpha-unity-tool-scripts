/*************************************************************************************
* FlammAlpha 2024
* Colors the Hierarchy-View in Unity (Async Rewrite)
*************************************************************************************/
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using UnityEditor;
using UnityEngine;
using System.Collections.Concurrent;

namespace UnityHierarchyColor
{
    /// <summary> Sets a background color for game objects in the Hierarchy tab (asynchronously batches heavy computation)</summary>
    [InitializeOnLoad]
    public class HierarchyObjectColor
    {
        const int MaxDepth = 100;
        private static HierarchyHighlightConfig currentConfig;
        private static readonly Dictionary<string, Type> typeCache = new();

        // Caches
        private static readonly ConcurrentDictionary<int, int[]> cachedCounts = new();
        private static readonly ConcurrentDictionary<int, int[]> cachedCountsOnSelf = new();

        // Queue for unprocessed instanceIDs
        private static readonly ConcurrentQueue<GameObject> countsQueue = new();
        private static readonly ConcurrentQueue<GameObject> countsSelfQueue = new();

        // Keeps track of what's already requested (so we don't re-queue unnecessarily)
        private static readonly HashSet<int> pendingCount = new();
        private static readonly HashSet<int> pendingCountSelf = new();

        private static int lastRepaintFrame = -1;
        private static int customFrameCounter = 1;

        // Loading spinner symbols, for fun
        private static readonly string[] spinner = { "|", "/", "-", "\\" };

        // Event raised when a cache entry finishes updating
        public static event Action<int> OnCountsCacheUpdated;

        static HierarchyObjectColor()
        {
            HierarchyHighlightConfigUtility.OnConfigUpdate += OnConfigUpdate;
            EditorApplication.hierarchyWindowItemOnGUI += HandleHierarchyWindowItemOnGUI;
            EditorApplication.update += EditorUpdate;
            HierarchyHighlightConfigUtility.ForceLoadConfig();
        }

        private static void OnConfigUpdate(HierarchyHighlightConfig config)
        {
            currentConfig = config;
            typeCache.Clear();
            if (currentConfig?.typeConfigs != null)
            {
                foreach (var tce in currentConfig.typeConfigs)
                {
                    if (tce == null || string.IsNullOrEmpty(tce.typeName)) continue;
                    if (!typeCache.ContainsKey(tce.typeName))
                        typeCache[tce.typeName] = Type.GetType(tce.typeName);
                }
            }
            ForceRecache();
        }

#if UNITY_EDITOR
        [MenuItem("Tools/FlammAlpha/Hierarchy Color/Force Recache")]
        private static void ForceRecacheMenuItem() => ForceRecache();
#endif

        /// <summary>
        /// Call to clear all type/name caches and their queues, then repaints entire Hierarchy.
        /// </summary>
        public static void ForceRecache()
        {
            cachedCounts.Clear();
            cachedCountsOnSelf.Clear();
            pendingCount.Clear();
            pendingCountSelf.Clear();
            ClearQueuesAndPending();
            EditorApplication.RepaintHierarchyWindow();
        }

        private static Type GetCachedType(string typeName)
            => typeCache.TryGetValue(typeName, out var cachedType) ? cachedType : null;

        private static List<TypeConfigEntry> GetTypeConfigs() => currentConfig?.typeConfigs;

        private static List<NameHighlightEntry> GetNameHighlightConfigs() => currentConfig?.nameHighlightConfigs;
        private static void HandleHierarchyWindowItemOnGUI(int instanceID, Rect selectionRect)
        {
            var typeConfigs = GetTypeConfigs();
            var nameHighlightConfigs = GetNameHighlightConfigs();
            if (typeConfigs == null) return;
            GameObject obj = EditorUtility.InstanceIDToObject(instanceID) as GameObject;
            if (obj == null) return;

            int[] counts;
            int[] countsOnSelf;
            bool countsReady = cachedCounts.TryGetValue(instanceID, out counts);
            bool countsSelfReady = cachedCountsOnSelf.TryGetValue(instanceID, out countsOnSelf);

            if (!countsReady && lockset(pendingCount, instanceID)) countsQueue.Enqueue(obj);
            if (!countsSelfReady && lockset(pendingCountSelf, instanceID)) countsSelfQueue.Enqueue(obj);
            float nextX = selectionRect.xMax;

            Color nameBackground = EditorGUIUtility.isProSkin ? new Color(0.21f, 0.21f, 0.21f, 1) : Color.white;
            Color textColor = Color.white;
            Color disabledTextColor = new Color(0.4f, 0.4f, 0.4f);

            bool prefixHighlightFound = false;
            if (nameHighlightConfigs != null && nameHighlightConfigs.Count > 0)
            {
                foreach (var nh in nameHighlightConfigs)
                {
                    if (string.IsNullOrEmpty(nh.prefix)) continue;
                    bool match = nh.propagateUpwards ? NameOrChildHasPrefixRecursive(obj, nh.prefix) : obj.name.StartsWith(nh.prefix, StringComparison.Ordinal);
                    if (match)
                    {
                        nameBackground = nh.color;
                        prefixHighlightFound = true;
                        break;
                    }
                }
            }

            if (!prefixHighlightFound && typeConfigs != null)
            {
                for (int i = 0; i < typeConfigs.Count; i++)
                {
                    var tce = typeConfigs[i];
                    if (tce == null || string.IsNullOrEmpty(tce.typeName)) continue;
                    Type type = GetCachedType(tce.typeName);
                    if (type == null) continue;
                    bool match = tce.propagateUpwards ? HasComponentInHierarchy(obj, type) : obj.GetComponent(type) != null;
                    if (match)
                    {
                        nameBackground = tce.color;
                        break;
                    }
                }
            }

            if (Selection.instanceIDs.Contains(instanceID))
                nameBackground = new Color(0.24f, 0.48f, 0.90f, 1f);
            else if (selectionRect.Contains(Event.current.mousePosition) && Event.current.type == EventType.Repaint)
                nameBackground = new Color(1f, 1f, 1f, 0.07f);

            GUIStyle countStyle = new(EditorStyles.label)
            {
                alignment = TextAnchor.MiddleCenter,
                fontSize = 11,
                fontStyle = FontStyle.Bold,
                normal = { textColor = Color.white }
            };

            if (countsReady && countsSelfReady)
            {
                for (int i = typeConfigs.Count - 1; i >= 0; i--)
                {
                    if ((counts[i] == 0 && countsOnSelf[i] == 0) || string.IsNullOrWhiteSpace(typeConfigs[i].symbol)) continue;
                    string totalCount = counts[i] == countsOnSelf[i] ? "" : $"{counts[i]}";
                    string selfCount = countsOnSelf[i] > 0 ? $" ({countsOnSelf[i]})" : "";
                    string label = $"{typeConfigs[i].symbol} {totalCount}{selfCount}".Trim();
                    float labelWidth = Mathf.Max(EditorStyles.label.CalcSize(new GUIContent(label)).x + 6, 32);
                    nextX -= labelWidth + 2f;
                    Rect countRect = new Rect(nextX, selectionRect.y, labelWidth, selectionRect.height);
                    EditorGUI.DrawRect(countRect, typeConfigs[i].color);
                    EditorGUI.LabelField(countRect, new GUIContent(label, typeConfigs[i].typeName), countStyle);
                }
            }
            else
            {
                float spinnerW = 20;
                float padX = 2;
                nextX -= spinnerW + padX;
                Rect loadingRect = new(nextX, selectionRect.y, spinnerW, selectionRect.height);
                int tick = (int)(EditorApplication.timeSinceStartup * 8) % spinner.Length;
                string anim = spinner[tick];
                EditorGUI.DrawRect(loadingRect, new Color(0.5f, 0.5f, 0.5f, 0.12f));
                GUIStyle spinnerStyle = new(EditorStyles.label)
                {
                    alignment = TextAnchor.MiddleCenter,
                    fontSize = 16,
                    normal = { textColor = Color.gray }
                };
                EditorGUI.LabelField(loadingRect, anim, spinnerStyle);
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

            Texture texture = !obj.activeSelf ? AssetPreview.GetMiniThumbnail(obj) : null;
            if (texture != null)
            {
                Rect iconRect = new Rect(selectionRect.x, selectionRect.y, selectionRect.height, selectionRect.height);
                GUI.DrawTexture(iconRect, texture, ScaleMode.ScaleToFit, true);
            }
        }

        private static void EditorUpdate()
        {
            int batchCount = 6;
            var typeConfigs = GetTypeConfigs();
            int typeCount = typeConfigs?.Count ?? 0;
            if (typeCount == 0)
            {
                ClearQueuesAndPending();
                return;
            }

            for (int i = 0; i < batchCount && countsQueue.TryDequeue(out var obj); i++)
            {
                int id = obj?.GetInstanceID() ?? 0;
                int[] counts = new int[typeCount];
                if (obj != null) AccumulateCountsRecursiveSafe(obj.transform, counts);
                    cachedCounts[id] = counts;
                lock (pendingCount) pendingCount.Remove(id);
                OnCountsCacheUpdated?.Invoke(id);
                EditorApplication.RepaintHierarchyWindow();
            }

            for (int i = 0; i < batchCount && countsSelfQueue.TryDequeue(out var obj); i++)
            {
                int id = obj?.GetInstanceID() ?? 0;
                int[] counts = new int[typeCount];
                if (obj != null) AccumulateCountSelfSafe(obj.transform, counts);
                    cachedCountsOnSelf[id] = counts;
                lock (pendingCountSelf) pendingCountSelf.Remove(id);
                OnCountsCacheUpdated?.Invoke(id);
                EditorApplication.RepaintHierarchyWindow();
            }
        }

        private static void AccumulateCountsRecursiveSafe(Transform obj, int[] counts)
        {
            try { if (obj != null) AccumulateCountsRecursive(obj, counts); }
            catch (Exception e) { Debug.LogError("HierarchyObjectColor: Exception in AccumulateCountsRecursive: " + e); }
            }
        private static void AccumulateCountSelfSafe(Transform obj, int[] counts)
        {
            try { if (obj != null) AccumulateCountSelf(obj, counts); }
            catch (Exception e) { Debug.LogError("HierarchyObjectColor: Exception in AccumulateCountSelf: " + e); }
            }
        private static void ClearQueuesAndPending()
        {
            while (countsQueue.TryDequeue(out _)) { }
            while (countsSelfQueue.TryDequeue(out _)) { }
            lock (pendingCount) pendingCount.Clear();
            lock (pendingCountSelf) pendingCountSelf.Clear();
        }

        #region Utility

        private static bool lockset(HashSet<int> hs, int id)
        {
            lock (hs) { return hs.Add(id); }
            }
        private static bool HasComponentInHierarchy(GameObject obj, Type componentType, int depth = 0)
        {
            if (depth > MaxDepth)
            {
                Debug.LogWarning("HierarchyObjectColor: Maximum recursion depth reached in HasComponentInHierarchy. Possible circular reference detected.");
                return false;
            }
            if (obj.GetComponent(componentType) != null)
                return true;
            foreach (Transform child in obj.transform)
                if (HasComponentInHierarchy(child.gameObject, componentType, depth + 1))
                    return true;
            return false;
        }

        private static bool NameOrChildHasPrefixRecursive(GameObject obj, string prefix, int depth = 0)
        {
            if (depth > MaxDepth)
            {
                Debug.LogWarning("HierarchyObjectColor: Maximum recursion depth reached in NameOrChildHasPrefixRecursive. Possible circular reference detected.");
                return false;
            }
            if (obj.name.StartsWith(prefix, StringComparison.Ordinal))
                return true;
            foreach (Transform child in obj.transform)
                if (NameOrChildHasPrefixRecursive(child.gameObject, prefix, depth + 1))
                    return true;
            return false;
        }

        private static void AccumulateCountSelf(Transform obj, int[] counts)
        {
            var typeConfigs = GetTypeConfigs();
            if (typeConfigs == null) return;
            for (int i = 0; i < typeConfigs.Count; i++)
            {
                if (typeConfigs[i] == null || string.IsNullOrEmpty(typeConfigs[i].typeName)) continue;
                Type t = GetCachedType(typeConfigs[i].typeName);
                if (t != null)
                    counts[i] += obj.GetComponents(t).Length;
                }
            }
        private static void AccumulateCountsRecursive(Transform obj, int[] counts, int depth = 0)
        {
            if (depth > MaxDepth)
            {
                Debug.LogWarning("HierarchyObjectColor: Maximum recursion depth reached in AccumulateCountsRecursive. Possible circular reference detected.");
                return;
            }
            AccumulateCountSelf(obj, counts);
            for (int c = 0; c < obj.childCount; ++c)
                AccumulateCountsRecursive(obj.GetChild(c), counts, depth + 1);
            }
        #endregion
    }
}
