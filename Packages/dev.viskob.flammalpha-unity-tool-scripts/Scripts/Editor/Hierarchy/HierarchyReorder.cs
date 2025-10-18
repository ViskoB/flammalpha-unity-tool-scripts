using UnityEngine;
using UnityEditorInternal;
using UnityEditor;

namespace FlammAlpha.UnityTools.Hierarchy
{
    /// <summary>
    /// Utility for reordering and merging GameObjects in the hierarchy.
    /// Provides tools for merging child objects with their parents.
    /// </summary>
    public static class HierarchyReorder
    {
        [MenuItem("GameObject/FlammAlpha/Merge with Parent", false)]
        private static void MergeWithParent(MenuCommand menuCommand)
        {
            GameObject selected = menuCommand.context as GameObject;

            // Support for Project view prefab assets
            if (selected == null && Selection.assetGUIDs != null && Selection.assetGUIDs.Length == 1)
            {
                HandlePrefabMerge();
                return;
            }

            // Original scene workflow
            if (selected == null || selected.transform.parent == null)
            {
                Debug.LogWarning("No valid parent to merge into.");
                return;
            }

            MergeGameObjectWithParent(selected);
        }

        /// <summary>
        /// Handles merging operations for prefab assets.
        /// </summary>
        private static void HandlePrefabMerge()
        {
            string assetPath = AssetDatabase.GUIDToAssetPath(Selection.assetGUIDs[0]);
            if (!assetPath.EndsWith(".prefab"))
            {
                Debug.LogWarning("Selected asset is not a prefab.");
                return;
            }

            GameObject prefabRoot = PrefabUtility.LoadPrefabContents(assetPath);
            GameObject mergeTarget = prefabRoot; // Default: try root, but you may expand for subobjects.

            // Try to find a child to merge up if possible
            if (prefabRoot.transform.childCount > 0)
                mergeTarget = prefabRoot.transform.GetChild(0).gameObject;
            else
            {
                Debug.LogWarning("No child object found in prefab to merge with parent.");
                PrefabUtility.UnloadPrefabContents(prefabRoot);
                return;
            }

            GameObject parent = mergeTarget.transform.parent?.gameObject;
            if (parent == null)
            {
                Debug.LogWarning("No valid parent to merge into in prefab.");
                PrefabUtility.UnloadPrefabContents(prefabRoot);
                return;
            }

            MergeComponents(mergeTarget, parent);
            Object.DestroyImmediate(mergeTarget);

            PrefabUtility.SaveAsPrefabAsset(prefabRoot, assetPath);
            PrefabUtility.UnloadPrefabContents(prefabRoot);

            Debug.Log("Prefab asset merged successfully.");
        }

        /// <summary>
        /// Merges a GameObject with its parent in the scene.
        /// </summary>
        /// <param name="selected">GameObject to merge</param>
        private static void MergeGameObjectWithParent(GameObject selected)
        {
            GameObject parentScene = selected.transform.parent.gameObject;

            Undo.RegisterFullObjectHierarchyUndo(parentScene, "Merge With Parent");
            MergeComponents(selected, parentScene);
            Undo.DestroyObjectImmediate(selected);
        }

        /// <summary>
        /// Copies all non-Transform components from source to target GameObject.
        /// </summary>
        /// <param name="source">Source GameObject</param>
        /// <param name="target">Target GameObject</param>
        private static void MergeComponents(GameObject source, GameObject target)
        {
            foreach (var comp in source.GetComponents<Component>())
            {
                if (comp is Transform)
                    continue;

                // Copy component
                Component copy = target.AddComponent(comp.GetType());
                ComponentUtility.CopyComponent(comp);
                ComponentUtility.PasteComponentValues(copy);
            }
        }

        [MenuItem("GameObject/FlammAlpha/Merge with Parent", true)]
        private static bool ValidateMergeWithParent(MenuCommand menuCommand)
        {
            // Check context object first (works for right-click and some menu invokes)
            GameObject selected = menuCommand.context as GameObject;
            if (selected != null && selected.transform.parent != null)
                return true;

            // Fallback: check the main Hierarchy selection (works for top menu invoke)
            if (Selection.activeGameObject != null && Selection.activeGameObject.transform.parent != null)
                return true;

            // Check if a single prefab asset is selected
            if (Selection.assetGUIDs != null && Selection.assetGUIDs.Length == 1)
            {
                string assetPath = AssetDatabase.GUIDToAssetPath(Selection.assetGUIDs[0]);
                if (assetPath.EndsWith(".prefab"))
                    return true;
            }

            return false;
        }
    }
}
