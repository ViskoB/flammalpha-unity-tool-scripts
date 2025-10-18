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
    /// Manages asynchronous counting and caching of component and property data for hierarchy highlighting.
    /// </summary>
    public static class HierarchyCounterManager
    {
        const int MaxDepth = 100;
        const int BatchCount = 6;

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

        private static readonly CountContext typeCountContext = new(
            new ConcurrentDictionary<int, int[]>(),
            new ConcurrentDictionary<int, int[]>(),
            new ConcurrentQueue<GameObject>(),
            new ConcurrentQueue<GameObject>(),
            new HashSet<int>(),
            new HashSet<int>(),
            () => HierarchyEvaluationEngine.GetTypeConfigs()?.Cast<object>().ToList(),
            AccumulateCountsRecursiveSafe,
            AccumulateCountSelfSafe
        );

        private static readonly CountContext propertyCountContext = new(
            new ConcurrentDictionary<int, int[]>(),
            new ConcurrentDictionary<int, int[]>(),
            new ConcurrentQueue<GameObject>(),
            new ConcurrentQueue<GameObject>(),
            new HashSet<int>(),
            new HashSet<int>(),
            () => HierarchyEvaluationEngine.GetPropertyHighlightConfigs()?.Cast<object>().ToList(),
            AccumulatePropertyCountsRecursiveSafe,
            AccumulatePropertyCountSelfSafe
        );

        public static event Action<int> OnCountsCacheUpdated;

        /// <summary>
        /// Gets counts for type configurations from cache.
        /// </summary>
        public static bool TryGetTypeCounts(int instanceID, out int[] recursiveCounts, out int[] selfCounts)
        {
            bool recursiveReady = typeCountContext.RecursiveCache.TryGetValue(instanceID, out recursiveCounts);
            bool selfReady = typeCountContext.SelfCache.TryGetValue(instanceID, out selfCounts);
            return recursiveReady && selfReady;
        }

        /// <summary>
        /// Gets counts for property configurations from cache.
        /// </summary>
        public static bool TryGetPropertyCounts(int instanceID, out int[] recursiveCounts, out int[] selfCounts)
        {
            bool recursiveReady = propertyCountContext.RecursiveCache.TryGetValue(instanceID, out recursiveCounts);
            bool selfReady = propertyCountContext.SelfCache.TryGetValue(instanceID, out selfCounts);
            return recursiveReady && selfReady;
        }

        /// <summary>
        /// Ensures that counts are queued for computation if not already cached.
        /// </summary>
        public static void EnsureCountsQueued(int instanceID, GameObject obj, bool includeTypeConfigs, bool includePropertyConfigs)
        {
            if (includeTypeConfigs)
                EnsureCountsQueued(typeCountContext, instanceID, obj);
            if (includePropertyConfigs)
                EnsureCountsQueued(propertyCountContext, instanceID, obj);
        }

        private static void EnsureCountsQueued(CountContext ctx, int instanceID, GameObject obj)
        {
            if (!ctx.RecursiveCache.ContainsKey(instanceID) && lockset(ctx.PendingRecursive, instanceID))
            {
                ctx.RecursiveQueue.Enqueue(obj);
            }
            if (!ctx.SelfCache.ContainsKey(instanceID) && lockset(ctx.PendingSelf, instanceID))
            {
                ctx.SelfQueue.Enqueue(obj);
            }
        }

        /// <summary>
        /// Processes queued count operations in batches. Should be called from EditorApplication.update.
        /// </summary>
        public static void ProcessCountQueues()
        {
            var countContexts = new[] { typeCountContext, propertyCountContext };
            bool hasAnyConfig = countContexts.Any(ctx => ctx.NumConfigs > 0);

            if (!hasAnyConfig)
            {
                ClearQueuesAndPending();
                return;
            }

            foreach (var ctx in countContexts)
            {
                if (ctx.NumConfigs == 0) continue;

                // Process recursive counts
                for (int i = 0; i < BatchCount && ctx.RecursiveQueue.TryDequeue(out var obj); i++)
                {
                    int id = obj?.GetInstanceID() ?? 0;
                    int[] counts = new int[ctx.NumConfigs];
                    ctx.RecursiveAccum?.Invoke(obj?.transform, counts);
                    ctx.RecursiveCache[id] = counts;
                    lock (ctx.PendingRecursive) ctx.PendingRecursive.Remove(id);
                    OnCountsCacheUpdated?.Invoke(id);
                    EditorApplication.RepaintHierarchyWindow();
                }

                // Process self counts
                for (int i = 0; i < BatchCount && ctx.SelfQueue.TryDequeue(out var obj); i++)
                {
                    int id = obj?.GetInstanceID() ?? 0;
                    int[] counts = new int[ctx.NumConfigs];
                    ctx.SelfAccum?.Invoke(obj?.transform, counts);
                    ctx.SelfCache[id] = counts;
                    lock (ctx.PendingSelf) ctx.PendingSelf.Remove(id);
                    OnCountsCacheUpdated?.Invoke(id);
                    EditorApplication.RepaintHierarchyWindow();
                }
            }
        }

        /// <summary>
        /// Forces a complete recache of all counter data.
        /// </summary>
        public static void ForceRecache()
        {
            foreach (var ctx in new[] { typeCountContext, propertyCountContext })
            {
                ctx.RecursiveCache.Clear();
                ctx.SelfCache.Clear();
                lock (ctx.PendingRecursive) ctx.PendingRecursive.Clear();
                lock (ctx.PendingSelf) ctx.PendingSelf.Clear();
                while (ctx.RecursiveQueue.TryDequeue(out _)) { }
                while (ctx.SelfQueue.TryDequeue(out _)) { }
            }
        }

        private static void ClearQueuesAndPending()
        {
            foreach (var ctx in new[] { typeCountContext, propertyCountContext })
            {
                while (ctx.RecursiveQueue.TryDequeue(out _)) { }
                while (ctx.SelfQueue.TryDequeue(out _)) { }
                lock (ctx.PendingRecursive) ctx.PendingRecursive.Clear();
                lock (ctx.PendingSelf) ctx.PendingSelf.Clear();
            }
        }

        private static bool lockset(HashSet<int> hs, int id)
        {
            lock (hs) { return hs.Add(id); }
        }

        #region Count Accumulation Methods

        private static void AccumulateCountsRecursiveSafe(Transform obj, int[] counts)
        {
            try { if (obj != null) AccumulateCountsRecursive(obj, counts); }
            catch (Exception e) { Debug.LogError("HierarchyCounterManager: Exception in AccumulateCountsRecursive: " + e); }
        }

        private static void AccumulateCountSelfSafe(Transform obj, int[] counts)
        {
            try { if (obj != null) AccumulateCountSelf(obj, counts); }
            catch (Exception e) { Debug.LogError("HierarchyCounterManager: Exception in AccumulateCountSelf: " + e); }
        }

        private static void AccumulatePropertyCountsRecursiveSafe(Transform obj, int[] counts)
        {
            try { if (obj != null) AccumulatePropertyCountsRecursive(obj, counts); }
            catch (Exception e) { Debug.LogError("HierarchyCounterManager: Exception in AccumulatePropertyCountsRecursive: " + e); }
        }

        private static void AccumulatePropertyCountSelfSafe(Transform obj, int[] counts)
        {
            try { if (obj != null) AccumulatePropertyCountSelf(obj, counts); }
            catch (Exception e) { Debug.LogError("HierarchyCounterManager: Exception in AccumulatePropertyCountSelf: " + e); }
        }

        private static void AccumulatePropertyCountsGeneric(
            Transform obj,
            int[] counts,
            bool recursive,
            int depth = 0)
        {
            if (depth > MaxDepth)
            {
                Debug.LogWarning("HierarchyCounterManager: Maximum recursion depth reached in AccumulatePropertyCountsGeneric. Possible circular reference detected.");
                return;
            }
            var propertyConfigs = HierarchyEvaluationEngine.GetPropertyHighlightConfigs();
            if (propertyConfigs == null) return;
            for (int i = 0; i < propertyConfigs.Count; i++)
            {
                var phe = propertyConfigs[i];
                if (phe == null || !phe.enabled || string.IsNullOrEmpty(phe.componentTypeName) || string.IsNullOrEmpty(phe.propertyName)) continue;
                Type t = HierarchyEvaluationEngine.GetCachedPropertyType(phe.componentTypeName);
                if (t != null)
                {
                    foreach (var comp in obj.GetComponents(t))
                    {
                        object val = ComponentReflectionUtility.GetComponentValue(comp, t, phe.propertyName);
                        CollectionCountUtility.IncrementCountForValue(val, counts, i);
                    }
                }
            }
            if (recursive)
            {
                for (int c = 0; c < obj.childCount; ++c)
                    AccumulatePropertyCountsGeneric(obj.GetChild(c), counts, recursive, depth + 1);
            }
        }

        private static void AccumulateTypeCountsGeneric(
            Transform obj,
            int[] counts,
            bool recursive,
            int depth = 0)
        {
            if (depth > MaxDepth)
            {
                Debug.LogWarning("HierarchyCounterManager: Maximum recursion depth reached in AccumulateTypeCountsGeneric. Possible circular reference detected.");
                return;
            }
            var typeConfigs = HierarchyEvaluationEngine.GetTypeConfigs();
            if (typeConfigs == null) return;
            for (int i = 0; i < typeConfigs.Count; i++)
            {
                if (typeConfigs[i] == null || !typeConfigs[i].enabled || string.IsNullOrEmpty(typeConfigs[i].typeName)) continue;
                Type t = HierarchyEvaluationEngine.GetCachedType(typeConfigs[i].typeName);
                if (t != null)
                {
                    var num = obj.GetComponents(t).Length;
                    counts[i] += num;
                }
            }
            if (recursive)
            {
                for (int c = 0; c < obj.childCount; ++c)
                    AccumulateTypeCountsGeneric(obj.GetChild(c), counts, recursive, depth + 1);
            }
        }

        private static void AccumulateCountSelf(Transform obj, int[] counts)
            => AccumulateTypeCountsGeneric(obj, counts, false);

        private static void AccumulateCountsRecursive(Transform obj, int[] counts, int depth = 0)
            => AccumulateTypeCountsGeneric(obj, counts, true, depth);

        private static void AccumulatePropertyCountSelf(Transform obj, int[] counts)
            => AccumulatePropertyCountsGeneric(obj, counts, false);

        private static void AccumulatePropertyCountsRecursive(Transform obj, int[] counts, int depth = 0)
            => AccumulatePropertyCountsGeneric(obj, counts, true, depth);

        #endregion
    }
}
