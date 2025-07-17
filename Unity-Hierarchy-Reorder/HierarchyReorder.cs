#if UNITY_EDITOR
using UnityEngine;
using UnityEditorInternal;
using UnityEditor;

public class HierarchyReorder
{
    [MenuItem("GameObject/FlammAlpha/Merge with Parent", false)]
    private static void MergeWithParent(MenuCommand menuCommand)
    {
        GameObject selected = menuCommand.context as GameObject;

        // Support for Project view prefab assets
        if (selected == null && Selection.assetGUIDs != null && Selection.assetGUIDs.Length == 1)
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

            foreach (var comp in mergeTarget.GetComponents<Component>())
            {
                if (comp is Transform)
                    continue;
                Component copy = parent.AddComponent(comp.GetType());
                ComponentUtility.CopyComponent(comp);
                ComponentUtility.PasteComponentValues(copy);
            }
            Object.DestroyImmediate(mergeTarget);

            PrefabUtility.SaveAsPrefabAsset(prefabRoot, assetPath);
            PrefabUtility.UnloadPrefabContents(prefabRoot);

            Debug.Log("Prefab asset merged successfully.");
            return;
        }

        // Original scene workflow
        if (selected == null || selected.transform.parent == null)
        {
            Debug.LogWarning("No valid parent to merge into.");
            return;
        }

        GameObject parentScene = selected.transform.parent.gameObject;

        foreach (var comp in selected.GetComponents<Component>())
        {
            if (comp is Transform)
                continue;

            // Copy component
            Component copy = parentScene.AddComponent(comp.GetType());
            ComponentUtility.CopyComponent(comp);
            ComponentUtility.PasteComponentValues(copy);
        }

        Undo.RegisterFullObjectHierarchyUndo(parentScene, "Merge With Parent");
        Undo.DestroyObjectImmediate(selected);
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
#endif