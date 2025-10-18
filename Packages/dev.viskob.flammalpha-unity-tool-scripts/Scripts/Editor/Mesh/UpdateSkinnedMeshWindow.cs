using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

namespace FlammAlpha.UnityTools.Mesh
{
    /// <summary>
    /// Unity Editor window for updating SkinnedMeshRenderer meshes and bone references.
    /// Useful for updating avatar meshes while preserving bone structure.
    /// </summary>
    public class UpdateSkinnedMeshWindow : EditorWindow
    {
        private GameObject target;
        private GameObject prefab;
        private Transform armature;

        [MenuItem("Tools/FlammAlpha/Update Skinned Mesh Bones")]
        static void ShowWindow() => GetWindow<UpdateSkinnedMeshWindow>("Skinned Mesh Updater");

        /// <summary>
        /// Gets SkinnedMeshRenderers from direct children only (non-recursive).
        /// </summary>
        /// <param name="obj">Parent GameObject to search</param>
        /// <returns>Array of SkinnedMeshRenderers found in direct children</returns>
        public SkinnedMeshRenderer[] GetRenderersInChildrenNonRecursive(GameObject obj)
        {
            List<SkinnedMeshRenderer> results = new List<SkinnedMeshRenderer>();

            foreach (Transform child in obj.transform)
            {
                SkinnedMeshRenderer renderer = child.GetComponent<SkinnedMeshRenderer>();
                if (renderer != null)
                {
                    results.Add(renderer);
                }
            }

            return results.ToArray();
        }

        private void OnGUI()
        {
            EditorGUILayout.LabelField("Skinned Mesh Updater", EditorStyles.boldLabel);
            EditorGUILayout.Space();

            target = (GameObject)EditorGUILayout.ObjectField("Target", target, typeof(GameObject), true);
            prefab = (GameObject)EditorGUILayout.ObjectField("Prefab", prefab, typeof(GameObject), true);
            armature = (Transform)EditorGUILayout.ObjectField("Armature", armature, typeof(Transform), true);

            EditorGUILayout.Space();

            GUI.enabled = (armature != null && prefab != null && target != null);

            if (GUILayout.Button("Update Mesh"))
            {
                SkinnedMeshRenderer[] targetSkinList = GetRenderersInChildrenNonRecursive(target);
                foreach (SkinnedMeshRenderer targetRenderer in targetSkinList)
                {
                    ChangeMesh(targetRenderer, prefab);
                }
            }

            if (GUILayout.Button("Update Bones"))
            {
                SkinnedMeshRenderer[] targetSkinList = GetRenderersInChildrenNonRecursive(target);
                foreach (SkinnedMeshRenderer targetRenderer in targetSkinList)
                {
                    FixRenderer(targetRenderer, armature);
                }
            }

            GUI.enabled = true;
        }

        /// <summary>
        /// Changes the mesh of a SkinnedMeshRenderer to match a mesh from the prefab.
        /// </summary>
        /// <param name="targetSkin">Target SkinnedMeshRenderer to update</param>
        /// <param name="prefab">Prefab containing the new mesh</param>
        private void ChangeMesh(SkinnedMeshRenderer targetSkin, GameObject prefab)
        {
            GameObject loadedPrefab = (GameObject)Instantiate((GameObject)AssetDatabase.LoadAssetAtPath(AssetDatabase.GetAssetPath(prefab), typeof(GameObject)));
            SkinnedMeshRenderer[] prefabMeshList = GetRenderersInChildrenNonRecursive(loadedPrefab);

            foreach (SkinnedMeshRenderer prefabMesh in prefabMeshList)
            {
                if (prefabMesh.sharedMesh.name == targetSkin.sharedMesh.name)
                {
                    Undo.RecordObject(targetSkin, "Update SkinnedMeshRenderer Mesh");
                    targetSkin.sharedMesh = prefabMesh.sharedMesh;
                    EditorUtility.SetDirty(targetSkin.gameObject);
                    Debug.Log($"Updated mesh for {targetSkin.name}");
                    break;
                }
            }
            DestroyImmediate(loadedPrefab);
        }

        /// <summary>
        /// Fixes bone references in a SkinnedMeshRenderer to match the provided armature.
        /// </summary>
        /// <param name="targetSkin">SkinnedMeshRenderer to fix</param>
        /// <param name="rootBone">Root bone of the armature</param>
        private void FixRenderer(SkinnedMeshRenderer targetSkin, Transform rootBone)
        {
            Debug.Log($"Fixing bone references for {targetSkin.name}");

            Dictionary<string, Transform> boneDictionary = new Dictionary<string, Transform>();
            Transform[] rootBoneChildren = rootBone.GetComponentsInChildren<Transform>(true);

            foreach (Transform child in rootBoneChildren)
            {
                boneDictionary[child.name] = child;
            }

            Transform[] newBones = new Transform[targetSkin.bones.Length];
            int foundBones = 0;

            for (int i = 0; i < targetSkin.bones.Length; i++)
            {
                if (boneDictionary.TryGetValue(targetSkin.bones[i].name, out Transform newBone))
                {
                    newBones[i] = newBone;
                    foundBones++;
                }
                else
                {
                    Debug.LogWarning($"Bone '{targetSkin.bones[i].name}' not found in armature");
                }
            }

            Undo.RecordObject(targetSkin, "Update SkinnedMeshRenderer Bones");
            targetSkin.bones = newBones;
            EditorUtility.SetDirty(targetSkin.gameObject);

            Debug.Log($"Updated {foundBones}/{newBones.Length} bone references for {targetSkin.name}");
        }
    }
}
