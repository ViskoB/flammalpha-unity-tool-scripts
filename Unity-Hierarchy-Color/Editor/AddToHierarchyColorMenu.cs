using UnityEngine;
using UnityEditor;
using System;
using System.Collections.Generic;
using System.Reflection;
using UnityHierarchyColor;
public static class AddToHierarchyColorMenu
{
    [MenuItem("CONTEXT/Component/FlammAlpha/Add to Hierarchy Color Config")]
    private static void AddComponentToHierarchyColorConfig(MenuCommand command)
    {
        var component = command.context as Component;
        if (component == null) return;

        string typeName = component.GetType().AssemblyQualifiedName;
        string symbol = component.GetType().Name[0].ToString();
        Color color = Color.yellow;
        bool propagateUpwards = false;

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

    [MenuItem("CONTEXT/Component/FlammAlpha/Add Property List to Hierarchy Color Config")]
    private static void AddPropertyListToHierarchyColorConfig(MenuCommand command)
    {
        var component = command.context as Component;
        if (component == null) return;

        var type = component.GetType();
        var fields = type.GetFields(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);

        var listFields = new List<FieldInfo>();
        foreach (var f in fields)
        {
            if (typeof(System.Collections.IEnumerable).IsAssignableFrom(f.FieldType) &&
                f.FieldType != typeof(string) &&
                (f.FieldType.IsArray ||
                 (f.FieldType.IsGenericType && f.FieldType.GetGenericTypeDefinition() == typeof(List<>))))
            {
                listFields.Add(f);
            }
        }

        if (listFields.Count == 0)
        {
            EditorUtility.DisplayDialog("Property Highlight", "No list/array fields found on " + type.Name, "OK");
            return;
        }

        var field = listFields[0];
        string propertyName = field.Name;

        string componentTypeName = type.AssemblyQualifiedName;
        Color color = Color.cyan;
        bool propagateUpwards = false;

        bool addedOrUpdated = HierarchyHighlightConfigUtility.AddOrUpdatePropertyHighlightEntry(
            componentTypeName,
            propertyName,
            color,
            propagateUpwards
        );

        if (addedOrUpdated)
        {
            EditorUtility.DisplayDialog("Property List Highlight",
                $"Added or updated property '{propertyName}' in component '{type.Name}' to property highlight config.",
                "OK");
        }
        else
        {
            EditorUtility.DisplayDialog("Property List Highlight",
                $"Property '{propertyName}' in component '{type.Name}' is already in the config.",
                "OK");
        }
    }
}
