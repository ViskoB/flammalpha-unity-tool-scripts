using System;
using UnityEngine;

namespace FlammAlpha.UnityTools.Common
{
    /// <summary>
    /// Utility for safely traversing GameObject hierarchies with depth limits and circular reference protection.
    /// </summary>
    public static class HierarchyTraversalUtility
    {
        public const int DEFAULT_MAX_DEPTH = 100;

        /// <summary>
        /// Checks if a GameObject or any of its children has a component of the specified type.
        /// </summary>
        /// <param name="obj">The GameObject to start searching from</param>
        /// <param name="componentType">The component type to search for</param>
        /// <param name="maxDepth">Maximum traversal depth to prevent infinite recursion</param>
        /// <returns>True if the component is found in the hierarchy</returns>
        public static bool HasComponentInHierarchy(GameObject obj, Type componentType, int maxDepth = DEFAULT_MAX_DEPTH)
        {
            return HasComponentInHierarchyRecursive(obj, componentType, 0, maxDepth);
        }

        private static bool HasComponentInHierarchyRecursive(GameObject obj, Type componentType, int depth, int maxDepth)
        {
            if (depth > maxDepth)
            {
                Debug.LogWarning($"HierarchyTraversalUtility: Maximum recursion depth ({maxDepth}) reached in HasComponentInHierarchy. Possible circular reference detected.");
                return false;
            }
            
            if (obj == null) return false;
            
            if (obj.GetComponent(componentType) != null)
                return true;
                
            foreach (Transform child in obj.transform)
            {
                if (HasComponentInHierarchyRecursive(child.gameObject, componentType, depth + 1, maxDepth))
                    return true;
            }
            
            return false;
        }

        /// <summary>
        /// Checks if a GameObject's name starts with a prefix, or if any of its children do.
        /// </summary>
        /// <param name="obj">The GameObject to start searching from</param>
        /// <param name="prefix">The name prefix to search for</param>
        /// <param name="maxDepth">Maximum traversal depth to prevent infinite recursion</param>
        /// <returns>True if a GameObject with the prefix is found in the hierarchy</returns>
        public static bool HasNamePrefixInHierarchy(GameObject obj, string prefix, int maxDepth = DEFAULT_MAX_DEPTH)
        {
            return HasNamePrefixInHierarchyRecursive(obj, prefix, 0, maxDepth);
        }

        private static bool HasNamePrefixInHierarchyRecursive(GameObject obj, string prefix, int depth, int maxDepth)
        {
            if (depth > maxDepth)
            {
                Debug.LogWarning($"HierarchyTraversalUtility: Maximum recursion depth ({maxDepth}) reached in HasNamePrefixInHierarchy. Possible circular reference detected.");
                return false;
            }
            
            if (obj == null || string.IsNullOrEmpty(prefix)) return false;
            
            if (obj.name.StartsWith(prefix, StringComparison.Ordinal))
                return true;
                
            foreach (Transform child in obj.transform)
            {
                if (HasNamePrefixInHierarchyRecursive(child.gameObject, prefix, depth + 1, maxDepth))
                    return true;
            }
            
            return false;
        }

        /// <summary>
        /// Executes an action on each GameObject in a hierarchy, with depth limiting.
        /// </summary>
        /// <param name="obj">The GameObject to start from</param>
        /// <param name="action">The action to execute on each GameObject</param>
        /// <param name="includeRoot">Whether to execute the action on the root GameObject</param>
        /// <param name="maxDepth">Maximum traversal depth</param>
        public static void ForEachInHierarchy(GameObject obj, Action<GameObject, int> action, bool includeRoot = true, int maxDepth = DEFAULT_MAX_DEPTH)
        {
            if (obj == null || action == null) return;
            
            if (includeRoot)
                action(obj, 0);
                
            ForEachInHierarchyRecursive(obj.transform, action, includeRoot ? 1 : 0, maxDepth);
        }

        private static void ForEachInHierarchyRecursive(Transform parent, Action<GameObject, int> action, int depth, int maxDepth)
        {
            if (depth > maxDepth)
            {
                Debug.LogWarning($"HierarchyTraversalUtility: Maximum recursion depth ({maxDepth}) reached in ForEachInHierarchy. Possible circular reference detected.");
                return;
            }
            
            foreach (Transform child in parent)
            {
                action(child.gameObject, depth);
                ForEachInHierarchyRecursive(child, action, depth + 1, maxDepth);
            }
        }
    }
}
