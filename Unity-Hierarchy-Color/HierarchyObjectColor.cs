/**
* FlammAlpha 2024
* Colors the Hierarchy-View in Unity
*/

#if UNITY_EDITOR
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
    private static Vector2 offset = new Vector2(20, 1);

    static HierarchyObjectColor()
    {
        EditorApplication.hierarchyWindowItemOnGUI += HandleHierarchyWindowItemOnGUI;
    }

    private static void HandleHierarchyWindowItemOnGUI(int instanceID, Rect selectionRect)
    {
        GameObject obj = (GameObject)EditorUtility.InstanceIDToObject(instanceID);
        if (obj != null)
        {
            Color backgroundColor = Color.white; // White
            Color textColor = Color.white; // White
            Color disabledTextColor = new Color(0.4f, 0.4f, 0.4f); // Gray
            Texture2D texture = !obj.activeSelf ? AssetPreview.GetMiniThumbnail(obj) : null;

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

            // Check if GameObject has VRC Contact Sender or Receiver
            if (obj.GetComponent<VRC.Dynamics.ContactSender>())
            {
                backgroundColor = new Color(0.1f, 0.3f, 0.2f); // Green
            }
            else if (obj.GetComponent<VRC.Dynamics.ContactReceiver>())
            {
                backgroundColor = new Color(0.1f, 0.2f, 0.3f); // Blue
            }
            else if (obj.GetComponent<VRC.Dynamics.VRCPhysBoneBase>())
            {
                backgroundColor = new Color(0.1f, 0.2f, 0.2f); // Cyan
            }

            // Check if Object has specific script attached
            if (obj.GetComponent("VRCFuryComponent"))
            {
                backgroundColor = new Color(0.3f, 0.1f, 0.3f); // Purple
            }
            else if (obj.GetComponentsInChildren<Transform>(true).Any(x => x.GetComponent("VRCFuryComponent")))
            {
                backgroundColor = new Color(0.6f, 0.1f, 0.1f); // Dark Red
            }

            // Draw the object name in the hierarchy.
            Rect offsetRect = new Rect(selectionRect.position + offset, selectionRect.size);
            Rect bgRect = new Rect(selectionRect.x, selectionRect.y, selectionRect.width + 50, selectionRect.height);

            if (backgroundColor != Color.white)
            {
                EditorGUI.DrawRect(bgRect, backgroundColor);
                if (!obj.activeInHierarchy)
                {
                    EditorGUI.LabelField(offsetRect, obj.name, new GUIStyle()
                    {
                        normal = new GUIStyleState() { textColor = disabledTextColor },
                        fontStyle = FontStyle.Bold
                    });
                }
                else
                {
                    EditorGUI.LabelField(offsetRect, obj.name, new GUIStyle()
                    {
                        normal = new GUIStyleState() { textColor = textColor }
                    });
                }
            }
            if (texture != null)
            {
                EditorGUI.DrawPreviewTexture(new Rect(selectionRect.position, new Vector2(selectionRect.height, selectionRect.height)), texture);
            }
        }
    }
}
#endif