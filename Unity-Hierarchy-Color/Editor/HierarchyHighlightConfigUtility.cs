/*************************************************************************************
* FlammAlpha 2025
* Utility for the Hierarchy Color Config Asset
*************************************************************************************/
using UnityEngine;
using UnityEditor;
using System.IO;
using System.Collections.Generic;

namespace UnityHierarchyColor
{
    public static class HierarchyHighlightConfigUtility
    {
        public const string ConfigAssetPath = "Assets/HierarchyHighlightConfig.asset";

        /// <summary>
        /// Event fired whenever the config is loaded or saved.
        /// </summary>
        public static event System.Action<HierarchyHighlightConfig> OnConfigUpdate;

        [InitializeOnLoadMethod]
        private static void EnsureConfigAssetExists()
        {
            if (!ConfigAssetExists())
            {
                var config = CreateDefaultConfigAsset();
                AssetDatabase.CreateAsset(config, ConfigAssetPath);
                AssetDatabase.SaveAssets();
                // Optionally fire event (if required)
                OnConfigUpdate?.Invoke(config);
            }
        }

        public static void ForceLoadConfig()
        {
            ScriptableObject.CreateInstance<HierarchyHighlightConfig>();
            var config = AssetDatabase.LoadAssetAtPath<HierarchyHighlightConfig>(ConfigAssetPath);
            if (config == null)
            {
                throw new InvalidDataException();
            }
            OnConfigUpdate?.Invoke(config);
        }

        public static void SaveConfig(HierarchyHighlightConfig config)
        {
            EditorUtility.SetDirty(config);
            AssetDatabase.SaveAssets();
            if (config != null)
            {
                OnConfigUpdate?.Invoke(config);
            }
        }

        public static bool ConfigAssetExists()
        {
            bool exists = File.Exists(ConfigAssetPath);
            return exists;
        }

        private static HierarchyHighlightConfig CreateDefaultConfigAsset()
        {
            Debug.Log("CreateDefaultConfigAsset: Creating a new HierarchyHighlightConfig asset.");
            var newConfig = ScriptableObject.CreateInstance<HierarchyHighlightConfig>();
            newConfig.typeConfigs = new List<TypeConfigEntry>
            {
                new TypeConfigEntry
                {
                    typeName = "UnityEngine.Camera, UnityEngine.CoreModule, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null",
                    symbol = "",
                    color = new Color(0.1f, 0.1f, 0.3f)
                },
                new TypeConfigEntry
                {
                    typeName = "UnityEngine.Light, UnityEngine.CoreModule, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null",
                    symbol = "",
                    color = new Color(0.3f, 0.3f, 0.1f)
                },
                new TypeConfigEntry
                {
                    typeName = "VF.Model.VRCFury, VRCFury, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null",
                    symbol = "VF",
                    color = new Color(0.3f, 0.1f, 0.3f)
                },
                new TypeConfigEntry
                {
                    typeName = "VRC.Dynamics.ContactSender, VRC.Dynamics, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null",
                    symbol = "#S",
                    color = new Color(0.15f, 0.25f, 0.35f)
                },
                new TypeConfigEntry
                {
                    typeName = "VRC.Dynamics.ContactReceiver, VRC.Dynamics, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null",
                    symbol = "#R",
                    color = new Color(0.1f, 0.2f, 0.3f)
                },
                new TypeConfigEntry
                {
                    typeName = "VRC.SDK3.Dynamics.PhysBone.Components.VRCPhysBone, VRC.SDK3.Dynamics.PhysBone, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null",
                    symbol = "◊B",
                    color = new Color(0.15f, 0.2f, 0.15f)
                },
                new TypeConfigEntry
                {
                    typeName = "VRC.SDK3.Dynamics.PhysBone.Components.VRCPhysBoneCollider, VRC.SDK3.Dynamics.PhysBone, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null",
                    symbol = "◊C",
                    color = new Color(0.2f, 0.235f, 0.2f)
                },
                new TypeConfigEntry
                {
                    typeName = "VRC.Dynamics.ManagedTypes.VRCParentConstraintBase, VRC.Dynamics, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null",
                    symbol = "C",
                    color = new Color(0.3f, 0.25f, 0.2f)
                }
            };
            newConfig.nameHighlightConfigs = new List<NameHighlightEntry>
            {
                new NameHighlightEntry
                {
                    prefix = "Toys",
                    color = new Color(0.1f, 0.3f, 0.1f)
                },
                new NameHighlightEntry
                {
                    prefix = "SPS",
                    color = new Color(0.3f, 0.1f, 0.1f)
                }
            };
            return newConfig;
        }
    }
}
