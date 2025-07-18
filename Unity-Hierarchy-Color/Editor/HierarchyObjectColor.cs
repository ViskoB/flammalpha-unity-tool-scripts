/*************************************************************************************
* FlammAlpha 2024
* Colors the Hierarchy-View in Unity (Async Rewrite, with property highlight support)
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
    /// <summary> Sets a background color for game objects in the Hierarchy tab (asynchronously batches heavy computation, supports property-based coloring)</summary>
    [InitializeOnLoad]
    public class HierarchyObjectColor
    {
        const int MaxDepth = 100;
        private static HierarchyHighlightConfig currentConfig;
        private static readonly Dictionary<string, Type> typeCache = new();
        private static readonly Dictionary<string, Type> propertyTypeCache = new();

        private static readonly CountContext typeCountContext = new(
            new ConcurrentDictionary<int, int[]>(),
            new ConcurrentDictionary<int, int[]>(),
            new ConcurrentQueue<GameObject>(),
            new ConcurrentQueue<GameObject>(),
            new HashSet<int>(),
            new HashSet<int>(),
                () => GetTypeConfigs()?.Cast<object>().ToList(),
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
                () => GetPropertyHighlightConfigs()?.Cast<object>().ToList(),
                AccumulatePropertyCountsRecursiveSafe,
                AccumulatePropertyCountSelfSafe
        );

        private static readonly string[] spinner = { "|", "/", "-", "\\" };

        public static event Action<int> OnCountsCacheUpdated;

        private static int filteredTypeIndex = -1;

        private enum CountKind { Type, Property }

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
            propertyTypeCache.Clear();
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
            if (currentConfig?.propertyHighlightConfigs != null)
            {
                foreach (var phe in currentConfig.propertyHighlightConfigs)
                {
                    if (phe == null || string.IsNullOrEmpty(phe.componentTypeName)) continue;
                    if (!propertyTypeCache.ContainsKey(phe.componentTypeName))
                    {
                        propertyTypeCache[phe.componentTypeName] = Type.GetType(phe.componentTypeName);
                    }
                }
            }
            ForceRecache();
        }

#if UNITY_EDITOR
        [MenuItem("Tools/FlammAlpha/Hierarchy Color/Force Recache")]
        private static void ForceRecacheMenuItem() => ForceRecache();
#endif

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
            EditorApplication.RepaintHierarchyWindow();
        }

        private static Type GetCachedType(string typeName)
            => typeCache.TryGetValue(typeName, out var cachedType) ? cachedType : null;

        private static List<TypeConfigEntry> GetTypeConfigs() => currentConfig?.typeConfigs;
        private static List<NameHighlightEntry> GetNameHighlightConfigs() => currentConfig?.nameHighlightConfigs;
        private static List<PropertyHighlightEntry> GetPropertyHighlightConfigs() => currentConfig?.propertyHighlightConfigs;

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

        private static Color GetBackgroundHighlightColor(
            GameObject obj,
            List<NameHighlightEntry> nameHighlightConfigs,
            List<TypeConfigEntry> typeConfigs,
            List<PropertyHighlightEntry> propertyConfigs
        )
        {
            Color nameBackground = EditorGUIUtility.isProSkin ? new Color(0.21f, 0.21f, 0.21f, 1) : Color.white;

            if (nameHighlightConfigs != null && nameHighlightConfigs.Count > 0)
            {
                foreach (var nh in nameHighlightConfigs)
                {
                    if (!nh.enabled) continue;
                    if (string.IsNullOrEmpty(nh.prefix)) continue;
                    bool match = nh.propagateUpwards ? NameOrChildHasPrefixRecursive(obj, nh.prefix) : obj.name.StartsWith(nh.prefix, StringComparison.Ordinal);
                    if (match)
                    {
                        return nh.color;
                    }
                }
            }

            if (typeConfigs != null)
            {
                for (int i = 0; i < typeConfigs.Count; i++)
                {
                    var tce = typeConfigs[i];
                    if (tce == null || !tce.enabled || string.IsNullOrEmpty(tce.typeName)) continue;
                    Type type = GetCachedType(tce.typeName);
                    if (type == null) continue;
                    bool match = tce.propagateUpwards ? HasComponentInHierarchy(obj, type) : obj.GetComponent(type) != null;
                    if (match)
                    {
                        return tce.color;
                    }
                }
            }

            if (propertyConfigs != null && propertyConfigs.Count > 0)
            {
                for (int i = 0; i < propertyConfigs.Count; i++)
                {
                    var phe = propertyConfigs[i];
                    if (phe == null || !phe.enabled || string.IsNullOrEmpty(phe.componentTypeName) || string.IsNullOrEmpty(phe.propertyName)) continue;
                    Type ptype = propertyTypeCache.TryGetValue(phe.componentTypeName, out var foundType) ? foundType : null;
                    if (ptype == null) continue;
                    var comps = obj.GetComponents(ptype);
                    foreach (var comp in comps)
                    {
                        object val = GetComponentValue(comp, ptype, phe.propertyName);
                        if ((val is bool b && b) || (val != null && !(val is bool)))
                        {
                            return phe.color;
                        }
                    }
                }
            }

            return nameBackground;
        }

        private static void HandleHierarchyWindowItemOnGUI(int instanceID, Rect selectionRect)
        {
            var typeConfigs = GetTypeConfigs();
            var nameHighlightConfigs = GetNameHighlightConfigs();
            var propertyConfigs = GetPropertyHighlightConfigs();
            if (typeConfigs == null && propertyConfigs == null) return;
            GameObject obj = EditorUtility.InstanceIDToObject(instanceID) as GameObject;
            if (obj == null) return;

            int[] counts;
            int[] countsOnSelf;
            int[] propertyCounts;
            int[] propertyCountsOnSelf;

            bool countsReady = typeCountContext.RecursiveCache.TryGetValue(instanceID, out counts);
            bool countsSelfReady = typeCountContext.SelfCache.TryGetValue(instanceID, out countsOnSelf);
            bool propertyCountsReady = propertyCountContext.RecursiveCache.TryGetValue(instanceID, out propertyCounts);
            bool propertyCountsSelfReady = propertyCountContext.SelfCache.TryGetValue(instanceID, out propertyCountsOnSelf);

            EnsureCountsQueued(typeCountContext, instanceID, obj);
            if (propertyConfigs != null)
                EnsureCountsQueued(propertyCountContext, instanceID, obj);

            float nextX = selectionRect.xMax;

            List<(int typeIndex, Rect counterRect, bool isProperty)> counterRects = new();

            Color nameBackground;
            Color textColor = Color.white;
            Color disabledTextColor = new Color(0.4f, 0.4f, 0.4f);

            bool isMatchedByFilter = true;
            int objectFilterIndex = -1;
            if (filteredTypeIndex >= 0)
            {
                if (typeConfigs != null && filteredTypeIndex < typeConfigs.Count)
                {
                    var tce = typeConfigs[filteredTypeIndex];
                    Type type = GetCachedType(tce.typeName);
                    if (type != null && obj.GetComponent(type) != null)
                        objectFilterIndex = filteredTypeIndex;
                    else
                        isMatchedByFilter = false;
                }
                else if (propertyConfigs != null && filteredTypeIndex >= (typeConfigs?.Count ?? 0))
                {
                    int pIdx = filteredTypeIndex - (typeConfigs?.Count ?? 0);
                    if (pIdx < propertyConfigs.Count)
                    {
                        var phe = propertyConfigs[pIdx];
                        Type ptype = propertyTypeCache.TryGetValue(phe.componentTypeName, out var foundType) ? foundType : null;
                        if (ptype != null && obj.GetComponent(ptype) != null)
                        {
                            bool match = false;
                            var comps = obj.GetComponents(ptype);
                            foreach (var comp in comps)
                            {
                                object val = GetComponentValue(comp, ptype, phe.propertyName);
                                if ((val is bool b && b) || (val != null && !(val is bool)))
                                {
                                    match = true; break;
                                }
                            }
                            if (match)
                                objectFilterIndex = filteredTypeIndex;
                            else
                                isMatchedByFilter = false;
                        }
                        else
                        {
                            isMatchedByFilter = false;
                        }
                    }
                    else isMatchedByFilter = false;
                }
            }

            if (isMatchedByFilter)
                nameBackground = GetBackgroundHighlightColor(obj, nameHighlightConfigs, typeConfigs, propertyConfigs);
            else
                nameBackground = EditorGUIUtility.isProSkin ? new Color(0.21f, 0.21f, 0.21f, 1) : Color.white;

            if (Selection.instanceIDs.Contains(instanceID))
            {
                Color baseColor = nameBackground;
                Color blue = EditorGUIUtility.isProSkin ? new Color(0.17f, 0.32f, 0.78f, 1f) : new Color(0.18f, 0.44f, 1f, 1f);
                nameBackground = Color.Lerp(baseColor, blue, 0.6f);
            }
            else if (selectionRect.Contains(Event.current.mousePosition) && Event.current.type == EventType.Repaint)
            {
                Color baseColor = nameBackground;
                Color lightColor = Color.Lerp(baseColor, Color.white, EditorGUIUtility.isProSkin ? 0.14f : 0.09f);
                nameBackground = lightColor;
            }

            GUIStyle countStyle = new(EditorStyles.label)
            {
                alignment = TextAnchor.MiddleCenter,
                fontSize = 11,
                fontStyle = FontStyle.Bold,
                normal = { textColor = Color.white }
            };

            if ((countsReady && countsSelfReady) &&
                (propertyConfigs == null || (propertyCountsReady && propertyCountsSelfReady)))
            {
                if (propertyConfigs != null)
                {
                    for (int i = propertyConfigs.Count - 1; i >= 0; i--)
                    {
                        if ((propertyCounts[i] == 0 && propertyCountsOnSelf[i] == 0) || string.IsNullOrWhiteSpace(propertyConfigs[i].symbol)) continue;
                        string totalCount = propertyCounts[i] == propertyCountsOnSelf[i] ? "" : $"{propertyCounts[i]}";
                        string selfCount = propertyCountsOnSelf[i] > 0 ? $" ({propertyCountsOnSelf[i]})" : "";
                        string label = $"{propertyConfigs[i].symbol} {totalCount}{selfCount}".Trim();
                        float labelWidth = Mathf.Max(EditorStyles.label.CalcSize(new GUIContent(label)).x + 6, 32);
                        nextX -= labelWidth + 2f;
                        Rect countRect = new Rect(nextX, selectionRect.y, labelWidth, selectionRect.height);

                        int filterIdx = i + (typeConfigs?.Count ?? 0);
                        counterRects.Add((filterIdx, countRect, true));
                        EditorGUI.DrawRect(countRect, propertyConfigs[i].color);

                        if (filteredTypeIndex == filterIdx)
                        {
                            Handles.color = Color.yellow;
                            Handles.DrawAAPolyLine(3,
                                new Vector3(countRect.x, countRect.y),
                                new Vector3(countRect.xMax, countRect.y),
                                new Vector3(countRect.xMax, countRect.yMax),
                                new Vector3(countRect.x, countRect.yMax),
                                new Vector3(countRect.x, countRect.y)
                            );
                        }

                        EditorGUI.LabelField(countRect, new GUIContent(label,
                            $"{propertyConfigs[i].componentTypeName}.{propertyConfigs[i].propertyName}"), countStyle);
                    }
                }
                if (typeConfigs != null)
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

                        counterRects.Add((i, countRect, false));
                        EditorGUI.DrawRect(countRect, typeConfigs[i].color);
                        if (filteredTypeIndex == i)
                        {
                            Handles.color = Color.yellow;
                            Handles.DrawAAPolyLine(3,
                                new Vector3(countRect.x, countRect.y),
                                new Vector3(countRect.xMax, countRect.y),
                                new Vector3(countRect.xMax, countRect.yMax),
                                new Vector3(countRect.x, countRect.yMax),
                                new Vector3(countRect.x, countRect.y)
                            );
                        }

                        EditorGUI.LabelField(countRect, new GUIContent(label, typeConfigs[i].typeName), countStyle);
                    }
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

            if (Event.current.type == EventType.MouseDown && Event.current.button == 0)
            {
                foreach (var (typeIdx, rect, isProperty) in counterRects)
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
            GUIStyle nameStyle = new GUIStyle(EditorStyles.label)
            {
                normal = { textColor = obj.activeInHierarchy ? textColor : disabledTextColor },
                fontStyle = obj.activeInHierarchy ? FontStyle.Normal : FontStyle.Bold,
                alignment = TextAnchor.MiddleLeft
            };

            EditorGUI.DrawRect(nameRect, nameBackground);
            EditorGUI.LabelField(nameRect, obj.name, nameStyle);

            if (filteredTypeIndex >= 0 && objectFilterIndex != filteredTypeIndex)
            {
                Color overlay = new Color(0f, 0f, 0f, 0.18f);
                EditorGUI.DrawRect(selectionRect, overlay);
            }

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

                for (int i = 0; i < batchCount && ctx.RecursiveQueue.TryDequeue(out var obj); i++)
                {
                    int id = obj?.GetInstanceID() ?? 0;
                    int[] counts = new int[ctx.NumConfigs];
                    ctx.RecursiveAccum?.Invoke(obj?.transform, counts);
                    ctx.RecursiveCache[id] = counts;
                    lock (ctx.PendingRecursive) ctx.PendingRecursive.Remove(id);
                    OnCountsCacheUpdated?.Invoke(id);
                    EditorApplication.RepaintHierarchyWindow();
                }
                for (int i = 0; i < batchCount && ctx.SelfQueue.TryDequeue(out var obj); i++)
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
        private static void AccumulatePropertyCountsRecursiveSafe(Transform obj, int[] counts)
        {
            try { if (obj != null) AccumulatePropertyCountsRecursive(obj, counts); }
            catch (Exception e) { Debug.LogError("HierarchyObjectColor: Exception in AccumulatePropertyCountsRecursive: " + e); }
        }
        private static void AccumulatePropertyCountSelfSafe(Transform obj, int[] counts)
        {
            try { if (obj != null) AccumulatePropertyCountSelf(obj, counts); }
            catch (Exception e) { Debug.LogError("HierarchyObjectColor: Exception in AccumulatePropertyCountSelf: " + e); }
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

        private delegate object ValueExtractor(object component, Type type, string propertyName);

        private static object GetComponentValue(object comp, Type t, string propertyName)
        {
            var prop = t.GetProperty(propertyName);
            if (prop != null) return prop.GetValue(comp);
            var field = t.GetField(propertyName);
            if (field != null) return field.GetValue(comp);
            return null;
        }
        private static void IncrementCountForValue(object val, int[] counts, int idx)
        {
            if (val is System.Collections.ICollection collection)
            {
                counts[idx] += collection.Count;
            }
            // fallback: if it's null, do nothing
        }

        private static void AccumulatePropertyCountsGeneric(
            Transform obj,
            int[] counts,
            bool recursive,
            int depth = 0)
        {
            if (depth > MaxDepth)
            {
                Debug.LogWarning("HierarchyObjectColor: Maximum recursion depth reached in AccumulatePropertyCountsGeneric. Possible circular reference detected.");
                return;
            }
            var propertyConfigs = GetPropertyHighlightConfigs();
            if (propertyConfigs == null) return;
            for (int i = 0; i < propertyConfigs.Count; i++)
            {
                var phe = propertyConfigs[i];
                if (phe == null || !phe.enabled || string.IsNullOrEmpty(phe.componentTypeName) || string.IsNullOrEmpty(phe.propertyName)) continue;
                Type t = propertyTypeCache.TryGetValue(phe.componentTypeName, out var ptype) ? ptype : null;
                if (t != null)
                {
                    foreach (var comp in obj.GetComponents(t))
                    {
                        object val = GetComponentValue(comp, t, phe.propertyName);
                        IncrementCountForValue(val, counts, i);
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
                Debug.LogWarning("HierarchyObjectColor: Maximum recursion depth reached in AccumulateTypeCountsGeneric. Possible circular reference detected.");
                return;
            }
            var typeConfigs = GetTypeConfigs();
            if (typeConfigs == null) return;
            for (int i = 0; i < typeConfigs.Count; i++)
            {
                if (typeConfigs[i] == null || !typeConfigs[i].enabled || string.IsNullOrEmpty(typeConfigs[i].typeName)) continue;
                Type t = GetCachedType(typeConfigs[i].typeName);
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

