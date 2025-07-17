/**
* FlammAlpha 2024
* Lists all components of an object in console
*/

#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;


/// <summary> Lists all components of an object in unity debug console</summary>
[InitializeOnLoad]
public class ComponentLister
{

    [MenuItem("GameObject/FlammAlpha/List Components")]
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