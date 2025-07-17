using UnityEngine;
using UnityEditor;
using System;
using UnityHierarchyColor; // Import your utility's namespace

public static class AddToHierarchyColorMenu
{
    [MenuItem("CONTEXT/Component/FlammAlpha/Add to Hierarchy Color Config")]
    private static void AddComponentToHierarchyColorConfig(MenuCommand command)
    {
        var component = command.context as Component;
        if (component == null) return;

        // Prepare values for the type config entry
        string typeName = component.GetType().AssemblyQualifiedName;
        string symbol = component.GetType().Name[0].ToString(); // Just first letter by default
        Color color = Color.yellow;                            // Default color
        bool propagateUpwards = false;

        // Use the utility's AddOrUpdateTypeConfigEntry method
        bool addedOrUpdated = HierarchyHighlightConfigUtility.AddOrUpdateTypeConfigEntry(
            typeName,
            symbol,
            color,
            propagateUpwards
        );

        if (addedOrUpdated)
        {
            EditorUtility.DisplayDialog("Hierarchy Color",
                $"Added or updated component type '{component.GetType().Name}' in Hierarchy Color Config.",
                "OK");
        }
        else
        {
            EditorUtility.DisplayDialog("Hierarchy Color",
                $"Component type '{component.GetType().Name}' is already in the config.",
                "OK");
        }
    }
}