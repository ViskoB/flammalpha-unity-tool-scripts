using System;
using System.Collections.Generic;
using UnityEngine;
using FlammAlpha.UnityTools.Common;
using FlammAlpha.UnityTools.Data;

namespace FlammAlpha.UnityTools.Hierarchy.Highlight
{
    /// <summary>
    /// Handles type and property evaluation logic for hierarchy highlighting.
    /// Manages type caching and component/property checking.
    /// </summary>
    public static class HierarchyEvaluationEngine
    {
        const int MaxDepth = 100;
        private static HierarchyHighlightConfig currentConfig;
        private static readonly Dictionary<string, Type> typeCache = new();
        private static readonly Dictionary<string, Type> propertyTypeCache = new();

        /// <summary>
        /// Updates the current configuration and refreshes type caches.
        /// </summary>
        public static void UpdateConfig(HierarchyHighlightConfig config)
        {
            currentConfig = config;
            RefreshTypeCaches();
        }

        /// <summary>
        /// Refreshes the type caches based on the current configuration.
        /// </summary>
        public static void RefreshTypeCaches()
        {
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
        }

        /// <summary>
        /// Gets a cached type by name.
        /// </summary>
        public static Type GetCachedType(string typeName)
            => typeCache.TryGetValue(typeName, out var cachedType) ? cachedType : null;

        /// <summary>
        /// Gets a cached property type by component type name.
        /// </summary>
        public static Type GetCachedPropertyType(string componentTypeName)
            => propertyTypeCache.TryGetValue(componentTypeName, out var cachedType) ? cachedType : null;

        /// <summary>
        /// Gets the current type configurations.
        /// </summary>
        public static List<TypeConfigEntry> GetTypeConfigs() => currentConfig?.typeConfigs;

        /// <summary>
        /// Gets the current name highlight configurations.
        /// </summary>
        public static List<NameHighlightEntry> GetNameHighlightConfigs() => currentConfig?.nameHighlightConfigs;

        /// <summary>
        /// Gets the current property highlight configurations.
        /// </summary>
        public static List<PropertyHighlightEntry> GetPropertyHighlightConfigs() => currentConfig?.propertyHighlightConfigs;

        /// <summary>
        /// Checks if a GameObject matches a specific type configuration.
        /// </summary>
        public static bool MatchesTypeConfig(GameObject obj, TypeConfigEntry typeConfig)
        {
            if (typeConfig == null || !typeConfig.enabled || string.IsNullOrEmpty(typeConfig.typeName)) 
                return false;

            Type type = GetCachedType(typeConfig.typeName);
            if (type == null) return false;

            return typeConfig.propagateUpwards 
                ? HierarchyTraversalUtility.HasComponentInHierarchy(obj, type, MaxDepth) 
                : obj.GetComponent(type) != null;
        }

        /// <summary>
        /// Checks if a GameObject matches a specific name configuration.
        /// </summary>
        public static bool MatchesNameConfig(GameObject obj, NameHighlightEntry nameConfig)
        {
            if (nameConfig == null || !nameConfig.enabled || string.IsNullOrEmpty(nameConfig.prefix)) 
                return false;

            return nameConfig.propagateUpwards 
                ? HierarchyTraversalUtility.HasNamePrefixInHierarchy(obj, nameConfig.prefix, MaxDepth) 
                : obj.name.StartsWith(nameConfig.prefix, StringComparison.Ordinal);
        }

        /// <summary>
        /// Checks if a GameObject matches a specific property configuration.
        /// </summary>
        public static bool MatchesPropertyConfig(GameObject obj, PropertyHighlightEntry propertyConfig)
        {
            if (propertyConfig == null || !propertyConfig.enabled || 
                string.IsNullOrEmpty(propertyConfig.componentTypeName) || 
                string.IsNullOrEmpty(propertyConfig.propertyName)) 
                return false;

            Type ptype = GetCachedPropertyType(propertyConfig.componentTypeName);
            if (ptype == null) return false;

            var comps = obj.GetComponents(ptype);
            foreach (var comp in comps)
            {
                object val = ComponentReflectionUtility.GetComponentValue(comp, ptype, propertyConfig.propertyName);
                if (CollectionCountUtility.IsValueActive(val))
                {
                    return true;
                }
            }
            return false;
        }

        /// <summary>
        /// Checks if a GameObject matches filter criteria based on a filter index.
        /// </summary>
        public static bool MatchesFilter(GameObject obj, int filteredTypeIndex, out int objectFilterIndex)
        {
            objectFilterIndex = -1;
            
            if (filteredTypeIndex < 0) return true;

            var typeConfigs = GetTypeConfigs();
            var propertyConfigs = GetPropertyHighlightConfigs();

            if (typeConfigs != null && filteredTypeIndex < typeConfigs.Count)
            {
                var tce = typeConfigs[filteredTypeIndex];
                if (MatchesTypeConfig(obj, tce))
                {
                    objectFilterIndex = filteredTypeIndex;
                    return true;
                }
                return false;
            }
            else if (propertyConfigs != null && filteredTypeIndex >= (typeConfigs?.Count ?? 0))
            {
                int pIdx = filteredTypeIndex - (typeConfigs?.Count ?? 0);
                if (pIdx < propertyConfigs.Count)
                {
                    var phe = propertyConfigs[pIdx];
                    if (MatchesPropertyConfig(obj, phe))
                    {
                        objectFilterIndex = filteredTypeIndex;
                        return true;
                    }
                    return false;
                }
                return false;
            }

            return false;
        }

        /// <summary>
        /// Gets the background highlight color for a GameObject based on configured rules.
        /// </summary>
        public static Color GetBackgroundHighlightColor(
            GameObject obj,
            List<NameHighlightEntry> nameHighlightConfigs,
            List<TypeConfigEntry> typeConfigs,
            List<PropertyHighlightEntry> propertyConfigs)
        {
            Color defaultBackground = UnityEditor.EditorGUIUtility.isProSkin 
                ? new Color(0.21f, 0.21f, 0.21f, 1) 
                : Color.white;

            // Check name-based highlighting first (highest priority)
            if (nameHighlightConfigs != null && nameHighlightConfigs.Count > 0)
            {
                foreach (var nh in nameHighlightConfigs)
                {
                    if (MatchesNameConfig(obj, nh))
                    {
                        return nh.color;
                    }
                }
            }

            // Check type-based highlighting second
            if (typeConfigs != null)
            {
                foreach (var tce in typeConfigs)
                {
                    if (MatchesTypeConfig(obj, tce))
                    {
                        return tce.color;
                    }
                }
            }

            // Check property-based highlighting last
            if (propertyConfigs != null && propertyConfigs.Count > 0)
            {
                foreach (var phe in propertyConfigs)
                {
                    if (MatchesPropertyConfig(obj, phe))
                    {
                        return phe.color;
                    }
                }
            }

            return defaultBackground;
        }
    }
}
