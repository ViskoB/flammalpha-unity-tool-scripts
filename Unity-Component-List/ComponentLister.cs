/**
* FlammAlpha 2024
* Lists all components of an object in console
*/

#if UNITY_EDITOR
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.VFX;


/// <summary> Lists all components of an object in unity debug console</summary>
[UnityEditor.InitializeOnLoad]
public class ComponentLister
{

    [MenuItem("GameObject/List Components")]
    private static void RevertAllAnimationClips()
    {
        if (Selection.activeObject && Selection.activeObject is GameObject) {
            
            Component[] components = ((GameObject)Selection.activeObject).GetComponents<Component>();

            foreach(Component component in components) {
                Debug.Log(component.GetType().AssemblyQualifiedName + " /// " + component.GetType().FullName);
            }

            Debug.Log("All components have been listed!");
        }
    }
}
#endif