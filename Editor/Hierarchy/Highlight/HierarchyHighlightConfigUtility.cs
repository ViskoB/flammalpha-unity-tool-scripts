using UnityEngine;
using UnityEditor;
using System.IO;
using System.Collections.Generic;
using System.Linq;

namespace FlammAlpha.UnityTools.Hierarchy.Highlight
{
    public static class HierarchyHighlightConfigUtility
    {
        public const string ConfigResourcePath = "HierarchyHighlightConfig";
        private const string ConfigAssetPath = "Assets/Resources/HierarchyHighlightConfig.asset";
        private const string ResourcesFolder = "Assets/Resources";

        /// <summary>
        /// Event fired whenever the config is loaded or saved.
        /// </summary>
        public static event System.Action<HierarchyHighlightConfig> OnConfigUpdate;

        // Cache to avoid repeated loading operations
        private static HierarchyHighlightConfig _cachedConfig;

        [InitializeOnLoadMethod]
        private static void EnsureConfigAssetExists()
        {
            Debug.Log("HierarchyHighlightConfigUtility: InitializeOnLoadMethod called");

            // Use EditorApplication.delayCall to ensure this runs after Unity is fully initialized
            EditorApplication.delayCall += () =>
            {
                if (!ConfigAssetExists())
                {
                    Debug.Log("HierarchyHighlightConfigUtility: Config asset does not exist, creating...");
                    CreateAndSaveConfigAsset();
                }
                else
                {
                    Debug.Log("HierarchyHighlightConfigUtility: Config asset already exists");
                }
            };
        }

        /// <summary>
        /// Loads config using Resources.Load method.
        /// Returns null if config doesn't exist.
        /// </summary>
        private static HierarchyHighlightConfig LoadConfigAsset()
        {
            var config = Resources.Load<HierarchyHighlightConfig>(ConfigResourcePath);

            if (config == null)
            {
                Debug.Log("LoadConfigAsset: Config not found in Resources");
            }
            else
            {
                Debug.Log("LoadConfigAsset: Config loaded successfully from Resources");
            }

            return config;
        }

        /// <summary>
        /// Ensures Resources directory exists and creates the config asset.
        /// </summary>
        private static HierarchyHighlightConfig CreateAndSaveConfigAsset()
        {
            var config = CreateDefaultConfigAsset();

            // Ensure the Resources directory exists
            EnsureResourcesDirectoryExists();

            AssetDatabase.CreateAsset(config, ConfigAssetPath);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Debug.Log($"HierarchyHighlightConfigUtility: Config asset created at {ConfigAssetPath}");

            // Update cache and fire event
            _cachedConfig = config;
            OnConfigUpdate?.Invoke(config);

            return config;
        }

        /// <summary>
        /// Ensures the Resources directory exists.
        /// </summary>
        private static void EnsureResourcesDirectoryExists()
        {
            if (!AssetDatabase.IsValidFolder(ResourcesFolder))
            {
                AssetDatabase.CreateFolder("Assets", "Resources");
                AssetDatabase.Refresh();
            }
        }

        /// <summary>
        /// Handles cleanup and recreation of problematic config assets.
        /// Attempts to preserve existing configuration data when possible.
        /// </summary>
        private static HierarchyHighlightConfig HandleProblematicAsset()
        {
            // First, try to load the asset using AssetDatabase as a fallback
            var existingConfig = AssetDatabase.LoadAssetAtPath<HierarchyHighlightConfig>(ConfigAssetPath);

            if (existingConfig != null)
            {
                Debug.Log("HandleProblematicAsset: Found existing config via AssetDatabase, using it and updating cache.");
                _cachedConfig = existingConfig;
                return existingConfig;
            }

            // If that fails, check if there's a generic asset at the path that might be corrupted
            var genericAsset = AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(ConfigAssetPath);
            if (genericAsset != null)
            {
                Debug.LogWarning($"HandleProblematicAsset: Found asset of type {genericAsset.GetType()} at config path, but it's not a HierarchyHighlightConfig. Creating new config with default values.");
            }
            else if (File.Exists(ConfigAssetPath))
            {
                Debug.LogWarning("HandleProblematicAsset: Config file exists but cannot be loaded. Creating new config with default values.");
            }
            else
            {
                Debug.Log("HandleProblematicAsset: No existing config found. Creating new config with default values.");
            }

            // Only create a new config if we couldn't recover the existing one
            return CreateAndSaveConfigAsset();
        }

        /// <summary>
        /// Gets the config asset, creating it if necessary.
        /// Uses caching to avoid repeated loading operations.
        /// </summary>
        public static HierarchyHighlightConfig GetOrCreateConfig()
        {
            // Return cached config if available
            if (_cachedConfig != null)
            {
                return _cachedConfig;
            }

            var config = LoadConfigAsset();

            if (config == null)
            {
                Debug.Log("HierarchyHighlightConfigUtility.GetOrCreateConfig: Config not found, creating new one...");
                config = HandleProblematicAsset();
            }

            // Cache the loaded/created config
            _cachedConfig = config;
            return config;
        }

        /// <summary>
        /// Gets the config asset if it exists, otherwise null. Will not create a new one.
        /// Uses caching to avoid repeated loading operations.
        /// </summary>
        public static HierarchyHighlightConfig GetConfigIfExists()
        {
            // Return cached config if available
            if (_cachedConfig != null)
            {
                return _cachedConfig;
            }

            var config = LoadConfigAsset();

            // Cache the loaded config (even if null)
            _cachedConfig = config;
            return config;
        }

        /// <summary>
        /// Forces a reload of the config asset and fires the OnConfigUpdate event.
        /// If the config does not exist, it will be created automatically.
        /// </summary>
        public static void ForceLoadConfig()
        {
            // Clear cache to force reload
            _cachedConfig = null;

            // Refresh the AssetDatabase to ensure we have the latest state
            AssetDatabase.Refresh();

            var config = LoadConfigAsset();

            if (config == null)
            {
                Debug.Log("ForceLoadConfig: Config asset not found, creating it automatically...");
                config = HandleProblematicAsset();

                if (config == null)
                {
                    throw new InvalidDataException($"Failed to create config asset at path: {ConfigAssetPath}. This may be due to file system permissions or Unity asset database issues.");
                }
            }

            // Update cache and fire event
            _cachedConfig = config;
            OnConfigUpdate?.Invoke(config);
        }

        /// <summary>
        /// Saves changes made to the given config asset and triggers OnConfigUpdate.
        /// </summary>
        public static void SaveConfig(HierarchyHighlightConfig config)
        {
            if (config == null) return;

            EditorUtility.SetDirty(config);
            AssetDatabase.SaveAssets();

            // Update cache
            _cachedConfig = config;
            OnConfigUpdate?.Invoke(config);
        }

        /// <summary>
        /// Checks if the config asset exists using Resources.Load method.
        /// </summary>
        public static bool ConfigAssetExists()
        {
            var config = Resources.Load<HierarchyHighlightConfig>(ConfigResourcePath);
            return config != null;
        }

        /// <summary>
        /// Creates a new default HierarchyHighlightConfig asset instance with default entries.
        /// </summary>
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
            newConfig.propertyHighlightConfigs = new List<PropertyHighlightEntry>();
            return newConfig;
        }

        /// <summary>
        /// Adds or updates a type config entry. Returns true if an entry was added/updated.
        /// </summary>
        public static bool AddOrUpdateTypeConfigEntry(
            string typeName,
            string symbol,
            Color color,
            bool propagateUpwards = false)
        {
            var config = GetOrCreateConfig();

            var entry = config.typeConfigs.FirstOrDefault(e => e.typeName == typeName);
            if (entry != null)
            {
                bool changed = false;
                if (entry.symbol != symbol) { entry.symbol = symbol; changed = true; }
                if (entry.color != color) { entry.color = color; changed = true; }
                if (entry.propagateUpwards != propagateUpwards) { entry.propagateUpwards = propagateUpwards; changed = true; }
                if (changed) SaveConfig(config);
                return changed;
            }
            else
            {
                config.typeConfigs.Add(new TypeConfigEntry
                {
                    typeName = typeName,
                    symbol = symbol,
                    color = color,
                    propagateUpwards = propagateUpwards
                });
                SaveConfig(config);
                return true;
            }
        }

        /// <summary>
        /// Adds or updates a property highlight entry. Returns true if an entry was added/updated.
        /// </summary>
        public static bool AddOrUpdatePropertyHighlightEntry(
            string componentTypeName,
            string propertyName,
            Color color,
            bool propagateUpwards = false)
        {
            var config = GetOrCreateConfig();
            var existing = config.propertyHighlightConfigs.Find(e =>
                e.componentTypeName == componentTypeName &&
                e.propertyName == propertyName);

            if (existing != null)
            {
                existing.color = color;
                existing.propagateUpwards = propagateUpwards;
                SaveConfig(config);
                return true;
            }
            config.propertyHighlightConfigs.Add(new PropertyHighlightEntry
            {
                componentTypeName = componentTypeName,
                propertyName = propertyName,
                color = color,
                propagateUpwards = propagateUpwards
            });
            SaveConfig(config);
            return true;
        }

        /// <summary>
        /// Manually creates the config asset for testing/debugging purposes.
        /// </summary>
        [UnityEditor.MenuItem("FlammAlpha/Create Hierarchy Highlight Config")]
        public static void ManuallyCreateConfig()
        {
            Debug.Log("ManuallyCreateConfig: Force creating config asset...");

            // Clear cache to force recreation
            _cachedConfig = null;

            // Check if existing config exists and warn user
            if (File.Exists(ConfigAssetPath))
            {
                if (!EditorUtility.DisplayDialog("Config Already Exists",
                    "A hierarchy highlight config already exists. Creating a new one will overwrite your existing configuration.\n\nAre you sure you want to continue?",
                    "Yes, Overwrite", "Cancel"))
                {
                    Debug.Log("ManuallyCreateConfig: User cancelled operation.");
                    return;
                }

                AssetDatabase.DeleteAsset(ConfigAssetPath);
                AssetDatabase.Refresh();
            }

            var config = CreateAndSaveConfigAsset();

            Debug.Log($"ManuallyCreateConfig: Config asset created at {ConfigAssetPath}");

            // Test loading it back
            var loadedConfig = Resources.Load<HierarchyHighlightConfig>(ConfigResourcePath);
            if (loadedConfig != null)
            {
                Debug.Log("ManuallyCreateConfig: Successfully loaded config from Resources!");
            }
            else
            {
                Debug.LogError("ManuallyCreateConfig: Failed to load config from Resources!");
            }
        }
    }
}
