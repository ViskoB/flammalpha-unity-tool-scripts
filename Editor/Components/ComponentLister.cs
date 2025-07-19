using UnityEditor;
using UnityEngine;

namespace FlammAlpha.UnityTools.Components
{
    /// <summary>
    /// Lists all components of an object in Unity debug console.
    /// </summary>
    [InitializeOnLoad]
    public static class ComponentLister
    {
        [MenuItem("GameObject/FlammAlpha/List Components")]
        private static void ListComponents()
        {
            if (Selection.activeObject && Selection.activeObject is GameObject)
            {
                Component[] components = ((GameObject)Selection.activeObject).GetComponents<Component>();

                foreach (Component component in components)
                {
                    Debug.Log($"{component.GetType().AssemblyQualifiedName} /// {component.GetType().FullName}");
                }

                Debug.Log("All components have been listed!");
            }
        }
    }
}
