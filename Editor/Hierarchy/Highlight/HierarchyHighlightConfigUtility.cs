using UnityEngine;
using UnityEditor;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using FlammAlpha.UnityTools.Data;

namespace FlammAlpha.UnityTools.Hierarchy.Highlight
{
    /// <summary>
    /// Utility class for managing HierarchyHighlightConfig assets with backup and migration support.
    /// 
    /// Features:
    /// - Automatic backup creation before config structure changes
    /// - Migration of existing config data when structure changes
    /// - Backup management (create, restore, cleanup)
    /// - Version checking and migration detection
    /// 
    /// When the config structure changes:
    /// 1. A backup is automatically created in Assets/Resources/Backups/
    /// 2. Existing config data is migrated to the new structure
    /// 3. Default values are applied for new fields
    /// 4. Old backups are automatically cleaned up (keeps 5 most recent)
    /// 
    /// Use the Backup Manager window (Tools/FlammAlpha/Hierarchy/Backup Manager) to:
    /// - View all available backups
    /// - Create manual backups
    /// - Restore from any backup
    /// - Force config migration
    /// - Cleanup old backups
    /// </summary>
    public static class HierarchyHighlightConfigUtility
    {
        public const string ConfigResourcePath = "HierarchyHighlightConfig";
        private const string ConfigAssetPath = "Assets/Resources/HierarchyHighlightConfig.asset";
        private const string ResourcesFolder = "Assets/Resources";
        private const string BackupFolder = "Assets/Resources/Backups";
        private const int CurrentConfigVersion = 1; // Increment this when config structure changes

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
        /// Ensures the backup directory exists.
        /// </summary>
        private static void EnsureBackupDirectoryExists()
        {
            if (!AssetDatabase.IsValidFolder(BackupFolder))
            {
                EnsureResourcesDirectoryExists();
                AssetDatabase.CreateFolder(ResourcesFolder, "Backups");
                AssetDatabase.Refresh();
            }
        }

        /// <summary>
        /// Creates a backup of the existing config asset.
        /// </summary>
        private static string CreateConfigBackup()
        {
            if (!File.Exists(ConfigAssetPath))
            {
                Debug.Log("CreateConfigBackup: No existing config to backup.");
                return null;
            }

            EnsureBackupDirectoryExists();

            var timestamp = System.DateTime.Now.ToString("yyyy-MM-dd_HH-mm-ss");
            var backupPath = $"{BackupFolder}/HierarchyHighlightConfig_backup_{timestamp}.asset";

            try
            {
                AssetDatabase.CopyAsset(ConfigAssetPath, backupPath);
                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();

                Debug.Log($"CreateConfigBackup: Backup created at {backupPath}");

                // Automatically cleanup old backups to prevent accumulation
                CleanupOldBackups(5); // Keep only the 5 most recent backups

                return backupPath;
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"CreateConfigBackup: Failed to create backup - {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// Creates a backup of the existing config asset (public version for manual backups).
        /// </summary>
        public static string CreateManualBackup()
        {
            return CreateConfigBackup();
        }

        /// <summary>
        /// Attempts to migrate data from an old config to a new config structure.
        /// </summary>
        private static HierarchyHighlightConfig TryMigrateConfig(HierarchyHighlightConfig oldConfig)
        {
            if (oldConfig == null)
            {
                Debug.Log("TryMigrateConfig: No old config to migrate from.");
                return CreateDefaultConfigAsset();
            }

            Debug.Log("TryMigrateConfig: Attempting to migrate config data...");

            try
            {
                var newConfig = CreateDefaultConfigAsset();

                // Migrate type configs
                if (oldConfig.typeConfigs != null)
                {
                    foreach (var oldEntry in oldConfig.typeConfigs)
                    {
                        if (oldEntry != null && !string.IsNullOrEmpty(oldEntry.typeName))
                        {
                            var newEntry = new TypeConfigEntry
                            {
                                typeName = oldEntry.typeName,
                                symbol = oldEntry.symbol ?? "",
                                color = oldEntry.color,
                                propagateUpwards = oldEntry.propagateUpwards,
                                enabled = oldEntry.enabled
                            };

                            // Check if this type already exists in the default config
                            var existingEntry = newConfig.typeConfigs.FirstOrDefault(e => e.typeName == oldEntry.typeName);
                            if (existingEntry != null)
                            {
                                // Update existing entry with old values
                                existingEntry.symbol = newEntry.symbol;
                                existingEntry.color = newEntry.color;
                                existingEntry.propagateUpwards = newEntry.propagateUpwards;
                                existingEntry.enabled = newEntry.enabled;
                            }
                            else
                            {
                                // Add as new entry
                                newConfig.typeConfigs.Add(newEntry);
                            }
                        }
                    }
                }

                // Migrate name highlight configs
                if (oldConfig.nameHighlightConfigs != null)
                {
                    foreach (var oldEntry in oldConfig.nameHighlightConfigs)
                    {
                        if (oldEntry != null && !string.IsNullOrEmpty(oldEntry.prefix))
                        {
                            var newEntry = new NameHighlightEntry
                            {
                                prefix = oldEntry.prefix,
                                color = oldEntry.color,
                                propagateUpwards = oldEntry.propagateUpwards,
                                enabled = oldEntry.enabled
                            };

                            // Check if this prefix already exists in the default config
                            var existingEntry = newConfig.nameHighlightConfigs.FirstOrDefault(e => e.prefix == oldEntry.prefix);
                            if (existingEntry != null)
                            {
                                // Update existing entry with old values
                                existingEntry.color = newEntry.color;
                                existingEntry.propagateUpwards = newEntry.propagateUpwards;
                                existingEntry.enabled = newEntry.enabled;
                            }
                            else
                            {
                                // Add as new entry
                                newConfig.nameHighlightConfigs.Add(newEntry);
                            }
                        }
                    }
                }

                // Migrate property highlight configs
                if (oldConfig.propertyHighlightConfigs != null)
                {
                    foreach (var oldEntry in oldConfig.propertyHighlightConfigs)
                    {
                        if (oldEntry != null && !string.IsNullOrEmpty(oldEntry.componentTypeName) && !string.IsNullOrEmpty(oldEntry.propertyName))
                        {
                            var newEntry = new PropertyHighlightEntry
                            {
                                componentTypeName = oldEntry.componentTypeName,
                                propertyName = oldEntry.propertyName,
                                symbol = oldEntry.symbol ?? "",
                                color = oldEntry.color,
                                propagateUpwards = oldEntry.propagateUpwards,
                                enabled = oldEntry.enabled
                            };

                            // Check if this property config already exists
                            var existingEntry = newConfig.propertyHighlightConfigs.FirstOrDefault(e =>
                                e.componentTypeName == oldEntry.componentTypeName &&
                                e.propertyName == oldEntry.propertyName);

                            if (existingEntry != null)
                            {
                                // Update existing entry with old values
                                existingEntry.symbol = newEntry.symbol;
                                existingEntry.color = newEntry.color;
                                existingEntry.propagateUpwards = newEntry.propagateUpwards;
                                existingEntry.enabled = newEntry.enabled;
                            }
                            else
                            {
                                // Add as new entry
                                newConfig.propertyHighlightConfigs.Add(newEntry);
                            }
                        }
                    }
                }

                var typeCount = oldConfig.typeConfigs?.Count ?? 0;
                var nameCount = oldConfig.nameHighlightConfigs?.Count ?? 0;
                var propertyCount = oldConfig.propertyHighlightConfigs?.Count ?? 0;

                Debug.Log($"TryMigrateConfig: Successfully migrated {typeCount} type configs, " +
                         $"{nameCount} name configs, and {propertyCount} property configs.");

                return newConfig;
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"TryMigrateConfig: Migration failed - {ex.Message}");
                Debug.LogError($"TryMigrateConfig: Stack trace: {ex.StackTrace}");
                return CreateDefaultConfigAsset();
            }
        }

        /// <summary>
        /// Handles cleanup and recreation of problematic config assets.
        /// Attempts to preserve existing configuration data when possible.
        /// </summary>
        private static HierarchyHighlightConfig HandleProblematicAsset()
        {
            Debug.Log("HandleProblematicAsset: Detected problematic or outdated config, attempting recovery...");

            // Create backup before attempting any changes
            var backupPath = CreateConfigBackup();

            HierarchyHighlightConfig oldConfig = null;

            // First, try to load the asset using AssetDatabase as a fallback
            var existingConfig = AssetDatabase.LoadAssetAtPath<HierarchyHighlightConfig>(ConfigAssetPath);

            if (existingConfig != null)
            {
                Debug.Log("HandleProblematicAsset: Found existing config via AssetDatabase, attempting migration.");
                oldConfig = existingConfig;
            }
            else
            {
                // If that fails, check if there's a generic asset at the path that might be corrupted
                var genericAsset = AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(ConfigAssetPath);
                if (genericAsset != null)
                {
                    Debug.LogWarning($"HandleProblematicAsset: Found asset of type {genericAsset.GetType()} at config path, but it's not a HierarchyHighlightConfig. Will create new config with default values.");
                }
                else if (File.Exists(ConfigAssetPath))
                {
                    Debug.LogWarning("HandleProblematicAsset: Config file exists but cannot be loaded. Will create new config with default values.");
                }
                else
                {
                    Debug.Log("HandleProblematicAsset: No existing config found. Creating new config with default values.");
                }
            }

            // Try to migrate existing data or create new config
            var migratedConfig = TryMigrateConfig(oldConfig);

            // Delete the old problematic asset if it exists
            if (File.Exists(ConfigAssetPath))
            {
                AssetDatabase.DeleteAsset(ConfigAssetPath);
                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();
            }

            // Create the new migrated config
            EnsureResourcesDirectoryExists();
            AssetDatabase.CreateAsset(migratedConfig, ConfigAssetPath);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Debug.Log($"HandleProblematicAsset: New config created at {ConfigAssetPath}" +
                     (backupPath != null ? $" (backup saved at {backupPath})" : ""));

            // Update cache and fire event
            _cachedConfig = migratedConfig;
            OnConfigUpdate?.Invoke(migratedConfig);

            return migratedConfig;
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
            else if (NeedsMigration(config))
            {
                Debug.Log("HierarchyHighlightConfigUtility.GetOrCreateConfig: Config needs migration, performing backup and migration...");
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
            else
            {
                // Check if migration is needed for existing config
                if (NeedsMigration(config))
                {
                    Debug.Log("ForceLoadConfig: Config needs migration, performing backup and migration...");
                    config = HandleProblematicAsset();
                }
            }

            // Update cache and fire event
            _cachedConfig = config;
            OnConfigUpdate?.Invoke(config);
        }

        /// <summary>
        /// Checks if the given config needs migration to the current version.
        /// </summary>
        private static bool NeedsMigration(HierarchyHighlightConfig config)
        {
            if (config == null) return true;

            try
            {
                // Check version first - this is the primary migration trigger
                if (config.configVersion < CurrentConfigVersion)
                {
                    Debug.Log($"NeedsMigration: Config version {config.configVersion} is older than current version {CurrentConfigVersion}, migration needed.");
                    return true;
                }

                // Check if all expected lists are present and initialized
                if (config.typeConfigs == null || 
                    config.nameHighlightConfigs == null || 
                    config.propertyHighlightConfigs == null)
                {
                    Debug.Log("NeedsMigration: Config missing required lists, migration needed.");
                    return true;
                }

                // Check if any entries are missing (corrupted data)
                foreach (var entry in config.typeConfigs)
                {
                    if (entry == null)
                    {
                        Debug.Log("NeedsMigration: Found null type config entry, migration needed.");
                        return true;
                    }
                }

                foreach (var entry in config.nameHighlightConfigs)
                {
                    if (entry == null)
                    {
                        Debug.Log("NeedsMigration: Found null name config entry, migration needed.");
                        return true;
                    }
                }

                foreach (var entry in config.propertyHighlightConfigs)
                {
                    if (entry == null)
                    {
                        Debug.Log("NeedsMigration: Found null property config entry, migration needed.");
                        return true;
                    }
                }

                // If we get here, the config is up to date
                return false;
            }
            catch (System.Exception ex)
            {
                Debug.LogWarning($"NeedsMigration: Exception while checking config - {ex.Message}. Migration needed.");
                return true;
            }
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
            
            // Set the current version
            newConfig.configVersion = CurrentConfigVersion;
            
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
        /// Lists all available backup files.
        /// </summary>
        public static List<string> GetAvailableBackups()
        {
            var backups = new List<string>();

            if (!AssetDatabase.IsValidFolder(BackupFolder))
            {
                return backups;
            }

            var guids = AssetDatabase.FindAssets("HierarchyHighlightConfig_backup", new[] { BackupFolder });
            foreach (var guid in guids)
            {
                var path = AssetDatabase.GUIDToAssetPath(guid);
                if (path.EndsWith(".asset"))
                {
                    backups.Add(path);
                }
            }

            return backups.OrderByDescending(x => x).ToList(); // Most recent first
        }

        /// <summary>
        /// Restores configuration from a backup file.
        /// </summary>
        public static bool RestoreFromBackup(string backupPath)
        {
            if (string.IsNullOrEmpty(backupPath) || !File.Exists(backupPath))
            {
                Debug.LogError($"RestoreFromBackup: Backup file not found at {backupPath}");
                return false;
            }

            try
            {
                // Create a backup of the current config before restoring
                CreateConfigBackup();

                // Load the backup config
                var backupConfig = AssetDatabase.LoadAssetAtPath<HierarchyHighlightConfig>(backupPath);
                if (backupConfig == null)
                {
                    Debug.LogError($"RestoreFromBackup: Could not load backup config from {backupPath}");
                    return false;
                }

                // Delete current config
                if (File.Exists(ConfigAssetPath))
                {
                    AssetDatabase.DeleteAsset(ConfigAssetPath);
                }

                // Copy the backup to the main config location
                AssetDatabase.CopyAsset(backupPath, ConfigAssetPath);
                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();

                // Clear cache and reload
                _cachedConfig = null;
                ForceLoadConfig();

                Debug.Log($"RestoreFromBackup: Successfully restored config from {backupPath}");
                return true;
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"RestoreFromBackup: Failed to restore from backup - {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Cleans up old backup files, keeping only the most recent ones.
        /// </summary>
        public static void CleanupOldBackups(int maxBackupsToKeep = 10)
        {
            var backups = GetAvailableBackups();

            if (backups.Count <= maxBackupsToKeep)
            {
                return; // Nothing to clean up
            }

            var backupsToDelete = backups.Skip(maxBackupsToKeep).ToList();

            foreach (var backup in backupsToDelete)
            {
                try
                {
                    AssetDatabase.DeleteAsset(backup);
                    Debug.Log($"CleanupOldBackups: Deleted old backup {backup}");
                }
                catch (System.Exception ex)
                {
                    Debug.LogWarning($"CleanupOldBackups: Failed to delete backup {backup} - {ex.Message}");
                }
            }

            if (backupsToDelete.Count > 0)
            {
                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();
                Debug.Log($"CleanupOldBackups: Cleaned up {backupsToDelete.Count} old backup files.");
            }
        }
    }
}
