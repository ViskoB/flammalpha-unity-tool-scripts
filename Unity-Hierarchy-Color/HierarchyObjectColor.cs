/**
* FlammAlpha 2024
* Colors the Hierarchy-View in Unity
*/

#if UNITY_EDITOR
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.VFX;


/// <summary> Sets a background color for game objects in the Hierarchy tab</summary>
[UnityEditor.InitializeOnLoad]
public class HierarchyObjectColor
{
    struct TypeConfig
    {
        public Type type;
        public string symbol;
        public Color color;

        public TypeConfig(Type t, string s, Color c) { type = t; symbol = s; color = c; }
    }

    // List the component types, symbols and colors you want to display
    static readonly TypeConfig[] typeConfigs =
    {
        // new TypeConfig(Type.GetType("VRCFuryComponent"), "F", new Color(0.3f, 0.1f, 0.3f) /* Purple */),
        new TypeConfig(Type.GetType("VF.Model.VRCFury, VRCFury"), "F", new Color(0.3f, 0.1f, 0.3f) /* Purple */),
        new TypeConfig(typeof(VRC.Dynamics.ContactSender), "S", new Color(0.1f, 0.3f, 0.2f) /* Green */),
        new TypeConfig(typeof(VRC.Dynamics.ContactReceiver), "R", new Color(0.1f, 0.2f, 0.3f) /* Blue */),
        new TypeConfig(typeof(VRC.Dynamics.VRCPhysBoneBase), "B", new Color(0.1f, 0.2f, 0.2f) /* Cyan */),
        new TypeConfig(typeof(VRC.Dynamics.VRCConstraintBase), "C", new Color(0.2f, 0.3f, 0.4f) /* Pale Blue */),
    };

    private static Vector2 offset = new Vector2(20, 1);

    static HierarchyObjectColor()
    {
        EditorApplication.hierarchyWindowItemOnGUI += HandleHierarchyWindowItemOnGUI;
    }

    private static void HandleHierarchyWindowItemOnGUI(int instanceID, Rect selectionRect)
    {
        GameObject obj = (GameObject)EditorUtility.InstanceIDToObject(instanceID);
        if (obj == null)
            return; // If the object is null, do nothing

        Color backgroundColor = Color.white; // White
        Color textColor = Color.white; // White
        Color disabledTextColor = new Color(0.4f, 0.4f, 0.4f); // Gray
        Texture2D texture = !obj.activeSelf ? AssetPreview.GetMiniThumbnail(obj) : null;

        int[] counts = new int[typeConfigs.Length];
        AccumulateCountsRecursive(obj.transform, counts);
        int[] countsOnSelf = new int[typeConfigs.Length];
        AccumulateCountSelf(obj.transform, countsOnSelf);

        // Check if Object has a specific component
        if (obj.GetComponent<Camera>())
        {
            backgroundColor = new Color(0.1f, 0.1f, 0.3f); // Blue
            texture = AssetPreview.GetMiniTypeThumbnail(typeof(Camera));
        }
        else if (obj.GetComponent<Light>())
        {
            backgroundColor = new Color(0.3f, 0.3f, 0.1f); // Yellow
            texture = AssetPreview.GetMiniTypeThumbnail(typeof(Light));
        }

        // Check if Object has a specific name
        if (obj.name.StartsWith("SPS"))
        {
            backgroundColor = new Color(0.3f, 0.1f, 0.1f); // Red
        }
        else if (obj.name.StartsWith("Toys"))
        {
            backgroundColor = new Color(0.1f, 0.3f, 0.1f); // Green
        }

        for (int i = 0; i < typeConfigs.Length; i++)
        {
            // Check if Object has a specific component
            if (obj.GetComponent(typeConfigs[i].type) != null)
            {
                backgroundColor = typeConfigs[i].color;
                break; // Stop checking after the first match
            }
        }

        // Check if Object has a specific component in children
        if (obj.GetComponentsInChildren<Transform>(true).Any(x => x.GetComponent("VRCFuryComponent")))
        {
            backgroundColor = new Color(0.6f, 0.1f, 0.1f); // Dark Red
        }

        // Draw the object name in the hierarchy.
        Rect offsetRect = new Rect(selectionRect.position + offset, selectionRect.size);
        float iconHeight = selectionRect.height - 2;
        GUIStyle countStyle = new GUIStyle(EditorStyles.label) {
            alignment = TextAnchor.MiddleCenter,
            fontSize = 11,
            fontStyle = FontStyle.Bold
        };
        Vector2 labelSize = EditorStyles.label.CalcSize(new GUIContent(obj.name));
        float countWidth = 0f;

        // Draw the count for each type
        if (counts.Any(c => c > 0))
        {
            for (int i = 0; i < typeConfigs.Length; i++)
            {
                if (counts[i] == 0)
                    continue;

                string totalCount = counts[i] == countsOnSelf[i] ? "" : $"{counts[i]}";
                string selfCount = countsOnSelf[i] > 0 ? $" ({countsOnSelf[i]})" : "";
                string label = $"{typeConfigs[i].symbol} {totalCount}{selfCount}";
                float labelWidth = MathF.Max(EditorStyles.label.CalcSize(new GUIContent(label)).x + 2, 30);
                Rect countRect = new Rect(selectionRect.x + selectionRect.width - countWidth - labelWidth, selectionRect.y, labelWidth, iconHeight);
                EditorGUI.DrawRect(countRect, typeConfigs[i].color);
                GUIContent content = new GUIContent(label, typeConfigs[i].type.Name);
                EditorGUI.LabelField(countRect, content, countStyle);

                countWidth += countRect.width;
            }
        }

        if (backgroundColor != Color.white)
        {
            // Draw background color for the object name
            Rect bgRect = new Rect(selectionRect.x, selectionRect.y, selectionRect.width - countWidth - 10, selectionRect.height);
            EditorGUI.DrawRect(bgRect, backgroundColor);

            // Draw the object name in the hierarchy.
            EditorGUI.LabelField(offsetRect, obj.name, new GUIStyle()
            {
                normal = new GUIStyleState() { textColor = !obj.activeInHierarchy ? disabledTextColor : textColor },
                fontStyle = !obj.activeInHierarchy ? FontStyle.Bold : FontStyle.Normal
            });
        }
        if (texture != null)
        {
            EditorGUI.DrawPreviewTexture(new Rect(selectionRect.position, new Vector2(selectionRect.height, selectionRect.height)), texture);
        }
    }

    const int MaxDepth = 100; // Limit to prevent stack overflow in case of circular references

    static void AccumulateCountSelf(Transform obj, int[] counts)
    {
        for (int i = 0; i < typeConfigs.Length; i++)
        {
            var components = obj.GetComponents(typeConfigs[i].type);
            counts[i] += components != null ? components.Length : 0;
        }
    }

    static void AccumulateCountsRecursive(Transform obj, int[] counts, int depth = 0)
    {
        if (depth > MaxDepth)
        {
            Debug.LogWarning("HierarchyObjectColor: Maximum recursion depth reached. Possible circular reference detected.");
            return; // Prevent stack overflow
        }
        // Count on this GameObject
        AccumulateCountSelf(obj, counts);

        // Recursively count children
        for (int c = 0; c < obj.childCount; ++c)
        {
            AccumulateCountsRecursive(obj.GetChild(c), counts, depth + 1);
        }
    }
}
#endif