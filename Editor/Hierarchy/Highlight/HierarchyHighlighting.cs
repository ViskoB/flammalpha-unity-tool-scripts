using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using System.Collections.Concurrent;
using FlammAlpha.UnityTools.Common;

namespace FlammAlpha.UnityTools.Hierarchy.Highlight
{
    /// <summary>
    /// Sets a background color for game objects in the Hierarchy tab with asynchronous computation
    /// and property-based coloring support.
    /// </summary>
    [InitializeOnLoad]
    public class HierarchyHighlighting
    {
        public static event Action<int> OnCountsCacheUpdated;

        private static int filteredTypeIndex = -1;

        /// <summary>
        /// Context for managing count caching operations.
        /// </summary>
        private class CountContext
        {
            public readonly ConcurrentDictionary<int, int[]> RecursiveCache;
            public readonly ConcurrentDictionary<int, int[]> SelfCache;
            public readonly ConcurrentQueue<GameObject> RecursiveQueue;
            public readonly ConcurrentQueue<GameObject> SelfQueue;
            public readonly HashSet<int> PendingRecursive;
            public readonly HashSet<int> PendingSelf;
            public readonly Func<List<object>> ConfigGetter;
            public readonly Action<Transform, int[]> RecursiveAccum;
            public readonly Action<Transform, int[]> SelfAccum;
            public int NumConfigs => ConfigGetter()?.Count ?? 0;

            public CountContext(
                ConcurrentDictionary<int, int[]> recursiveCache,
                ConcurrentDictionary<int, int[]> selfCache,
                ConcurrentQueue<GameObject> recursiveQueue,
                ConcurrentQueue<GameObject> selfQueue,
                HashSet<int> pendingRecursive,
                HashSet<int> pendingSelf,
                Func<List<object>> configGetter,
                Action<Transform, int[]> recursiveAccum,
                Action<Transform, int[]> selfAccum
            )
            {
                RecursiveCache = recursiveCache;
                SelfCache = selfCache;
                RecursiveQueue = recursiveQueue;
                SelfQueue = selfQueue;
                PendingRecursive = pendingRecursive;
                PendingSelf = pendingSelf;
                ConfigGetter = configGetter;
                RecursiveAccum = recursiveAccum;
                SelfAccum = selfAccum;
            }
        }

        static HierarchyHighlighting()
        {
            HierarchyHighlightConfigUtility.OnConfigUpdate += OnConfigUpdate;
            EditorApplication.hierarchyWindowItemOnGUI += HandleHierarchyWindowItemOnGUI;
            EditorApplication.update += EditorUpdate;
            HierarchyCounterManager.OnCountsCacheUpdated += OnCountsCacheUpdated;

            // Delay the config loading to ensure Unity is fully initialized
            EditorApplication.delayCall += () =>
            {
                try
                {
                    HierarchyHighlightConfigUtility.ForceLoadConfig();
                }
                catch (Exception ex)
                {
                    Debug.LogError($"HierarchyHighlighting: Failed to load config: {ex.Message}");
                }
            };
        }

        private static void OnConfigUpdate(HierarchyHighlightConfig config)
        {
            HierarchyEvaluationEngine.UpdateConfig(config);
            ForceRecache();
        }

        [MenuItem("Tools/FlammAlpha/Hierarchy/Force Recache Highlight")]
        private static void ForceRecacheMenuItem() => ForceRecache();

        /// <summary>
        /// Forces a complete recache of all hierarchy highlight data.
        /// </summary>
        public static void ForceRecache()
        {
            HierarchyCounterManager.ForceRecache();
            EditorApplication.RepaintHierarchyWindow();
        }

        private static void HandleHierarchyWindowItemOnGUI(int instanceID, Rect selectionRect)
        {
            var typeConfigs = HierarchyEvaluationEngine.GetTypeConfigs();
            var nameHighlightConfigs = HierarchyEvaluationEngine.GetNameHighlightConfigs();
            var propertyConfigs = HierarchyEvaluationEngine.GetPropertyHighlightConfigs();
            GameObject obj = EditorUtility.InstanceIDToObject(instanceID) as GameObject;
            if (obj == null) return;

            int[] counts;
            int[] countsOnSelf;
            int[] propertyCounts;
            int[] propertyCountsOnSelf;

            bool countsReady = HierarchyCounterManager.TryGetTypeCounts(instanceID, out counts, out countsOnSelf);
            bool propertyCountsReady = HierarchyCounterManager.TryGetPropertyCounts(instanceID, out propertyCounts, out propertyCountsOnSelf);

            // Initialize empty arrays for empty configs to prevent null reference exceptions
            if (typeConfigs == null || typeConfigs.Count == 0)
            {
                counts = new int[0];
                countsOnSelf = new int[0];
            }
            if (propertyConfigs == null || propertyConfigs.Count == 0)
            {
                propertyCounts = new int[0];
                propertyCountsOnSelf = new int[0];
            }

            // Only queue for processing if we actually have configs to process
            bool includeTypeConfigs = typeConfigs != null && typeConfigs.Count > 0;
            bool includePropertyConfigs = propertyConfigs != null && propertyConfigs.Count > 0;
            HierarchyCounterManager.EnsureCountsQueued(instanceID, obj, includeTypeConfigs, includePropertyConfigs);

            float nextX = selectionRect.xMax;
            float minNameWidth = 60f; // Minimum width to preserve for the object name
            float availableWidth = selectionRect.width - selectionRect.height - minNameWidth; // Account for Unity's icon space and minimum name space

            List<(int typeIndex, Rect counterRect)> counterRects = new();

            bool isMatchedByFilter = HierarchyEvaluationEngine.MatchesFilter(obj, filteredTypeIndex, out int objectFilterIndex);

            Color nameBackground;
            if (isMatchedByFilter)
                nameBackground = HierarchyEvaluationEngine.GetBackgroundHighlightColor(obj, nameHighlightConfigs, typeConfigs, propertyConfigs);
            else
                nameBackground = EditorGUIUtility.isProSkin ? new Color(0.21f, 0.21f, 0.21f, 1) : Color.white;

            // Apply selection and hover effects
            if (Selection.instanceIDs.Contains(instanceID))
            {
                Color blue = EditorGUIUtility.isProSkin ? new Color(0.17f, 0.32f, 0.78f, 1f) : new Color(0.18f, 0.44f, 1f, 1f);
                nameBackground = Color.Lerp(nameBackground, blue, 0.6f);
            }
            else if (selectionRect.Contains(Event.current.mousePosition) && Event.current.type == EventType.Repaint)
            {
                Color lightColor = Color.Lerp(nameBackground, Color.white, EditorGUIUtility.isProSkin ? 0.14f : 0.09f);
                nameBackground = lightColor;
            }

            GUIStyle countStyle = HierarchyCounterRenderer.CreateCountStyle();

            // Determine if counts are ready - if there are no configs, consider them ready
            bool typeCountsActuallyReady = (typeConfigs == null || typeConfigs.Count == 0) || countsReady;
            bool propertyCountsActuallyReady = (propertyConfigs == null || propertyConfigs.Count == 0) || propertyCountsReady;

            if (typeCountsActuallyReady && propertyCountsActuallyReady)
            {
                // Collect all visible counters
                var allCounters = HierarchyCounterRenderer.CollectCounters(
                    typeConfigs,
                    propertyConfigs,
                    counts,
                    countsOnSelf,
                    propertyCounts,
                    propertyCountsOnSelf);

                var drawContext = new HierarchyCounterRenderer.CounterDrawContext(
                    nextX,
                    0f,
                    availableWidth,
                    selectionRect,
                    counterRects,
                    countStyle);

                // Draw counters with smart space management
                HierarchyCounterRenderer.DrawCountersWithMoreButton(allCounters, ref drawContext, filteredTypeIndex);

                // Update nextX from the context after drawing all counters
                nextX = drawContext.NextX;
            }
            else
            {
                HierarchyCounterRenderer.DrawLoadingSpinner(availableWidth, selectionRect, ref nextX);
            }

            if (Event.current.type == EventType.MouseDown && Event.current.button == 0)
            {
                foreach (var (typeIdx, rect) in counterRects)
                {
                    if (rect.Contains(Event.current.mousePosition))
                    {
                        if (filteredTypeIndex == typeIdx) filteredTypeIndex = -1;
                        else filteredTypeIndex = typeIdx;
                        EditorApplication.RepaintHierarchyWindow();
                        Event.current.Use();
                        break;
                    }
                }
            }

            Rect nameRect = new Rect(selectionRect.x + selectionRect.height, selectionRect.y, nextX - selectionRect.x - selectionRect.height, selectionRect.height);
            
            // Create name style
            Color textColor = obj.activeInHierarchy ? Color.white : new Color(0.4f, 0.4f, 0.4f);
            FontStyle fontStyle = obj.activeInHierarchy ? FontStyle.Normal : FontStyle.Bold;
            GUIStyle nameStyle = new GUIStyle(EditorStyles.label)
            {
                normal = { textColor = textColor },
                fontStyle = fontStyle,
                alignment = TextAnchor.MiddleLeft
            };

            EditorGUI.DrawRect(nameRect, nameBackground);
            EditorGUI.LabelField(nameRect, obj.name, nameStyle);

            // Draw filter overlay if object doesn't match filter
            if (filteredTypeIndex >= 0 && objectFilterIndex != filteredTypeIndex)
            {
                Color overlay = new Color(0f, 0f, 0f, 0.18f);
                EditorGUI.DrawRect(selectionRect, overlay);
            }
        }

        private static void EditorUpdate()
        {
            HierarchyCounterManager.ProcessCountQueues();
        }
    }
}
