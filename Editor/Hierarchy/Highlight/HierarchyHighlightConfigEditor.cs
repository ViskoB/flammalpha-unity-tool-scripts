using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditorInternal;
using UnityEngine;
using FlammAlpha.UnityTools.Common;
using FlammAlpha.UnityTools.Data;

namespace FlammAlpha.UnityTools.Hierarchy.Highlight
{
    /// <summary>
    /// Unity Editor window for configuring hierarchy highlighting rules.
    /// Provides UI for managing component highlighting, name highlighting, and property highlighting rules.
    /// Also includes backup management functionality for configurations.
    /// </summary>
    public class HierarchyHighlightConfigEditor : EditorWindow
    {
        #region Constants
        
        // Layout constants
        private const float MARGIN = 5f;
        private const float BUTTON_REMOVE_WIDTH = 25f;
        private const float COLOR_FIELD_WIDTH = 80f;
        private const float SYMBOL_FIELD_WIDTH = 40f;
        private const float CHANGE_BUTTON_WIDTH = 70f;
        private const float TYPE_LABEL_WIDTH = 120f;
        private const float PROPAGATE_TOGGLE_WIDTH = 90f;
        private const float ENABLE_TOGGLE_WIDTH = 20f;
        private const float PROPERTY_DROPDOWN_WIDTH = 100f;
        private const float VERTICAL_OFFSET = 2f;

        #endregion

        #region Fields

        private Vector2 scrollPos;
        private ReorderableList typeConfigList;
        private ReorderableList nameHighlightList;
        private ReorderableList propertyHighlightList;

        // Backup manager fields
        private bool showBackupSection = false;
        private Vector2 backupScrollPosition;
        private List<string> availableBackups;

        #endregion

        #region Unity Editor Window Methods

        [MenuItem("Tools/FlammAlpha/Hierarchy/Highlight Config")]
        protected static void OpenWindow()
        {
            GetWindow<HierarchyHighlightConfigEditor>("Hierarchy Highlight Config");
        }

        protected void OnEnable()
        {
            HierarchyHighlightConfigUtility.OnConfigUpdate += OnConfigUpdated;
            SetupReorderableLists();
            RefreshBackupList();
        }

        protected void OnDisable()
        {
            HierarchyHighlightConfigUtility.OnConfigUpdate -= OnConfigUpdated;
        }

        private void OnConfigUpdated(HierarchyHighlightConfig loadedConfig)
        {
            SetupReorderableLists();
            Repaint();
        }

        #endregion

        #region List Management Methods

        private void RemoveTypeConfigElement(int index)
        {
            RemoveListElement(index, config => config.typeConfigs, "Remove Type Config");
        }

        private void RemoveNameHighlightElement(int index)
        {
            RemoveListElement(index, config => config.nameHighlightConfigs, "Remove Name Highlight");
        }

        private void RemovePropertyHighlightElement(int index)
        {
            RemoveListElement(index, config => config.propertyHighlightConfigs, "Remove Property Highlight");
        }

        private void RemoveListElement<T>(int index, Func<HierarchyHighlightConfig, IList<T>> getList, string undoText)
        {
            var config = HierarchyHighlightConfigUtility.GetOrCreateConfig();
            if (config == null) return;

            var list = getList(config);
            if (list == null || index < 0 || index >= list.Count) return;

            Undo.RegisterCompleteObjectUndo(config, undoText);
            list.RemoveAt(index);
            HierarchyHighlightConfigUtility.SaveConfig(config);
            GUIUtility.keyboardControl = 0;
        }

        #endregion

        #region Type and Property Utilities

        private void ShowTypePicker(Action<Type> onPick)
        {
            TypeSearchPopup.Show(type =>
                    {
                        if (type != null)
                        {
                            onPick(type);
                            var config = HierarchyHighlightConfigUtility.GetOrCreateConfig();
                            HierarchyHighlightConfigUtility.SaveConfig(config);
                        }
                    });
        }

        private Type GetTypeFromString(string typeName)
        {
            if (string.IsNullOrEmpty(typeName)) return null;
            var t = Type.GetType(typeName);
            if (t != null) return t;
            return AppDomain.CurrentDomain.GetAssemblies()
                .SelectMany(a =>
                {
                    try { return a.GetTypes(); } catch { return new Type[0]; }
                })
                .FirstOrDefault(tt => tt.AssemblyQualifiedName == typeName || tt.FullName == typeName);
        }

        private List<string> GetListPropertyNames(Type type)
        {
            if (type == null) return new List<string>();

            // Use ComponentReflectionUtility to get filtered property names
            var props = ComponentReflectionUtility.GetPropertyNames(
                type,
                includeCollections: true,
                includeMaterials: true,
                includeBooleans: true,
                filterProblematic: true
            ).ToList();

            // Add safe alternatives for problematic properties
            var safeAlternatives = PropertySafetyUtility.GetSafeAlternatives(type);
            foreach (var alternative in safeAlternatives)
            {
                if (!props.Contains(alternative))
                    props.Add(alternative);
            }

            return props.OrderBy(p => p).ToList();
        }

        /// <summary>
        /// Checks if a property on a given type would cause issues when accessed in edit mode.
        /// Returns true for properties that create material instances or other problematic behaviors.
        /// </summary>
        private bool IsProblematicProperty(Type componentType, string propertyName)
        {
            return PropertySafetyUtility.IsProblematicProperty(componentType, propertyName);
        }

        #endregion

        #region ReorderableList Setup

        private void SetupReorderableLists()
        {
            var config = HierarchyHighlightConfigUtility.GetOrCreateConfig();
            if (config == null) return;

            EnsureConfigListsInitialized(config);
            SetupTypeConfigList(config);
            SetupNameHighlightList(config);
            SetupPropertyHighlightList(config);
        }

        private void EnsureConfigListsInitialized(HierarchyHighlightConfig config)
        {
            if (config.typeConfigs == null)
                config.typeConfigs = new List<TypeConfigEntry>();
            if (config.nameHighlightConfigs == null)
                config.nameHighlightConfigs = new List<NameHighlightEntry>();
            if (config.propertyHighlightConfigs == null)
                config.propertyHighlightConfigs = new List<PropertyHighlightEntry>();
        }

        private void SetupTypeConfigList(HierarchyHighlightConfig config)
        {
            typeConfigList = CreateReorderableList(
                config.typeConfigs,
                typeof(TypeConfigEntry),
                "Component Highlight Rules",
                DrawTypeConfigElement,
                () => {
                    Undo.RegisterCompleteObjectUndo(config, "Add Type Config");
                    config.typeConfigs.Add(new TypeConfigEntry());
                    HierarchyHighlightConfigUtility.SaveConfig(config);
                },
                list => RemoveTypeConfigElement(list.index)
            );
        }

        private void SetupNameHighlightList(HierarchyHighlightConfig config)
        {
            nameHighlightList = CreateReorderableList(
                config.nameHighlightConfigs,
                typeof(NameHighlightEntry),
                "Name Highlight Rules",
                DrawNameHighlightElement,
                () => {
                    Undo.RegisterCompleteObjectUndo(config, "Add Name Highlight");
                    config.nameHighlightConfigs.Add(new NameHighlightEntry());
                    HierarchyHighlightConfigUtility.SaveConfig(config);
                },
                list => RemoveNameHighlightElement(list.index)
            );
        }

        private void SetupPropertyHighlightList(HierarchyHighlightConfig config)
        {
            propertyHighlightList = CreateReorderableList(
                config.propertyHighlightConfigs,
                typeof(PropertyHighlightEntry),
                $"Property Highlight Rules ({config.propertyHighlightConfigs.Count})",
                DrawPropertyHighlightElement,
                () => {
                    Undo.RegisterCompleteObjectUndo(config, "Add Property Highlight");
                    config.propertyHighlightConfigs.Add(new PropertyHighlightEntry());
                    HierarchyHighlightConfigUtility.SaveConfig(config);
                },
                list => RemovePropertyHighlightElement(list.index)
            );
        }

        private ReorderableList CreateReorderableList<T>(
            IList<T> list,
            Type elementType,
            string headerText,
            ReorderableList.ElementCallbackDelegate drawElementCallback,
            System.Action onAddCallback,
            ReorderableList.RemoveCallbackDelegate onRemoveCallback)
        {
            var reorderableList = new ReorderableList((System.Collections.IList)list, elementType, true, true, true, true);
            reorderableList.drawHeaderCallback = rect => EditorGUI.LabelField(rect, headerText);
            reorderableList.drawElementCallback = drawElementCallback;
            reorderableList.onAddCallback = _ => onAddCallback();
            reorderableList.onRemoveCallback = onRemoveCallback;
            return reorderableList;
        }

        #endregion

        #region Element Drawing Methods

        private void DrawTypeConfigElement(Rect rect, int index, bool active, bool focused)
        {
            var config = HierarchyHighlightConfigUtility.GetOrCreateConfig();
            float y = rect.y + VERTICAL_OFFSET;
            float xMax = rect.xMax;
            
            float xRemove = xMax - BUTTON_REMOVE_WIDTH;
            float xPropagate = xRemove - MARGIN - PROPAGATE_TOGGLE_WIDTH;
            float xColor = xPropagate - MARGIN - COLOR_FIELD_WIDTH;
            float xSymbol = xColor - MARGIN - SYMBOL_FIELD_WIDTH;
            float xType = rect.x + ENABLE_TOGGLE_WIDTH + MARGIN;
            float xChange = xType + TYPE_LABEL_WIDTH + MARGIN;
            float xEnable = rect.x;

            if (config.typeConfigs == null || index < 0 || index >= config.typeConfigs.Count) return;
            var entry = config.typeConfigs[index];
            Type selectedType = GetTypeFromString(entry.typeName);

            entry.enabled = EditorGUI.Toggle(new Rect(xEnable, y, ENABLE_TOGGLE_WIDTH, EditorGUIUtility.singleLineHeight), entry.enabled);

            if (selectedType != null)
            {
                EditorGUI.LabelField(new Rect(xType, y, TYPE_LABEL_WIDTH, EditorGUIUtility.singleLineHeight), selectedType.Name);
                if (GUI.Button(new Rect(xChange, y, CHANGE_BUTTON_WIDTH, EditorGUIUtility.singleLineHeight), "Change"))
                {
                    Undo.RegisterCompleteObjectUndo(config, "Change Type");
                    ShowTypePicker(type =>
                    {
                        entry.typeName = type.AssemblyQualifiedName;
                        HierarchyHighlightConfigUtility.SaveConfig(config);
                    });
                }
            }
            else
            {
                if (GUI.Button(new Rect(xType, y, TYPE_LABEL_WIDTH + CHANGE_BUTTON_WIDTH + MARGIN, EditorGUIUtility.singleLineHeight), "Pick Type"))
                {
                    Undo.RegisterCompleteObjectUndo(config, "Pick Type");
                    ShowTypePicker(type =>
                    {
                        entry.typeName = type.AssemblyQualifiedName;
                        HierarchyHighlightConfigUtility.SaveConfig(config);
                    });
                }
            }
            
            entry.symbol = EditorGUI.TextField(
                new Rect(xSymbol, y, SYMBOL_FIELD_WIDTH, EditorGUIUtility.singleLineHeight),
                entry.symbol);
            entry.color = EditorGUI.ColorField(
                new Rect(xColor, y, COLOR_FIELD_WIDTH, EditorGUIUtility.singleLineHeight),
                entry.color);
            entry.propagateUpwards = EditorGUI.ToggleLeft(
                new Rect(xPropagate, y, PROPAGATE_TOGGLE_WIDTH, EditorGUIUtility.singleLineHeight),
                "Recursive",
                entry.propagateUpwards);
            if (GUI.Button(new Rect(xRemove, y, BUTTON_REMOVE_WIDTH, EditorGUIUtility.singleLineHeight), "✗"))
            {
                RemoveTypeConfigElement(index);
            }
        }

        private void DrawNameHighlightElement(Rect rect, int index, bool active, bool focused)
        {
            var config = HierarchyHighlightConfigUtility.GetOrCreateConfig();
            float y = rect.y + VERTICAL_OFFSET;
            float xMax = rect.xMax;
            
            float xRemove = xMax - BUTTON_REMOVE_WIDTH;
            float xPropagate = xRemove - MARGIN - PROPAGATE_TOGGLE_WIDTH;
            float xColor = xPropagate - MARGIN - COLOR_FIELD_WIDTH;
            float xPrefix = rect.x + ENABLE_TOGGLE_WIDTH + MARGIN;
            float xEnable = rect.x;

            if (config.nameHighlightConfigs == null || index < 0 || index >= config.nameHighlightConfigs.Count) return;
            var entry = config.nameHighlightConfigs[index];
            
            entry.enabled = EditorGUI.Toggle(new Rect(xEnable, y, ENABLE_TOGGLE_WIDTH, EditorGUIUtility.singleLineHeight), entry.enabled);
            entry.prefix = EditorGUI.TextField(
                new Rect(xPrefix, y, xColor - xPrefix - MARGIN, EditorGUIUtility.singleLineHeight),
                entry.prefix);
            entry.color = EditorGUI.ColorField(
                new Rect(xColor, y, COLOR_FIELD_WIDTH, EditorGUIUtility.singleLineHeight),
                entry.color);
            entry.propagateUpwards = EditorGUI.ToggleLeft(
                new Rect(xPropagate, y, PROPAGATE_TOGGLE_WIDTH, EditorGUIUtility.singleLineHeight),
                "Recursive",
                entry.propagateUpwards);
            if (GUI.Button(new Rect(xRemove, y, BUTTON_REMOVE_WIDTH, EditorGUIUtility.singleLineHeight), "✗"))
            {
                RemoveNameHighlightElement(index);
            }
        }

        private void DrawPropertyHighlightElement(Rect rect, int index, bool isActive, bool isFocused)
        {
            var config = HierarchyHighlightConfigUtility.GetOrCreateConfig();
            float y = rect.y + VERTICAL_OFFSET;
            float x = rect.x;
            float xMax = rect.xMax;

            float xRemove = xMax - BUTTON_REMOVE_WIDTH;
            float xPropagate = xRemove - MARGIN - PROPAGATE_TOGGLE_WIDTH;
            float xColor = xPropagate - MARGIN - COLOR_FIELD_WIDTH;
            float xSymbol = xColor - MARGIN - SYMBOL_FIELD_WIDTH;
            float xProperty = xSymbol - MARGIN - PROPERTY_DROPDOWN_WIDTH;
            float xComponent = x + ENABLE_TOGGLE_WIDTH + MARGIN;
            float xEnable = x;

            if (config.propertyHighlightConfigs == null || index < 0 || index >= config.propertyHighlightConfigs.Count) return;
            var entry = config.propertyHighlightConfigs[index];
            entry.enabled = EditorGUI.Toggle(new Rect(xEnable, y, ENABLE_TOGGLE_WIDTH, EditorGUIUtility.singleLineHeight), entry.enabled);
            Type componentType = GetTypeFromString(entry.componentTypeName);
            
            if (componentType != null)
            {
                EditorGUI.LabelField(new Rect(xComponent, y, TYPE_LABEL_WIDTH, EditorGUIUtility.singleLineHeight), componentType.Name);
                if (GUI.Button(new Rect(xComponent + TYPE_LABEL_WIDTH + MARGIN, y, CHANGE_BUTTON_WIDTH, EditorGUIUtility.singleLineHeight), "Change"))
                {
                    Undo.RegisterCompleteObjectUndo(config, "Change Component Type");
                    ShowTypePicker(type =>
                    {
                        entry.componentTypeName = type.AssemblyQualifiedName;
                        entry.propertyName = null;
                        HierarchyHighlightConfigUtility.SaveConfig(config);
                    });
                }
            }
            else
            {
                if (GUI.Button(new Rect(xComponent, y, TYPE_LABEL_WIDTH + CHANGE_BUTTON_WIDTH + MARGIN, EditorGUIUtility.singleLineHeight), "Pick Type"))
                {
                    Undo.RegisterCompleteObjectUndo(config, "Pick Component Type");
                    ShowTypePicker(type =>
                    {
                        entry.componentTypeName = type.AssemblyQualifiedName;
                        entry.propertyName = null;
                        HierarchyHighlightConfigUtility.SaveConfig(config);
                    });
                }
            }

            List<string> propertyOptions = GetListPropertyNames(componentType);
            int selectedIndex = propertyOptions.IndexOf(entry.propertyName ?? "");
            if (selectedIndex < 0) selectedIndex = 0;
            if (propertyOptions.Count > 0)
            {
                string tooltipText = "Properties filtered to exclude those that create instances in edit mode (like material, materials, mesh).\n" +
                                   "Safe alternatives (like sharedMaterial, sharedMaterials, sharedMesh) are included where available.";

                int newSelectedIndex = EditorGUI.Popup(
                    new Rect(xProperty, y, PROPERTY_DROPDOWN_WIDTH, EditorGUIUtility.singleLineHeight),
                    new GUIContent("", tooltipText),
                    selectedIndex,
                    propertyOptions.Select(p => new GUIContent(p)).ToArray()
                );
                entry.propertyName = propertyOptions[newSelectedIndex];
            }
            else
            {
                EditorGUI.LabelField(
                    new Rect(xProperty, y, PROPERTY_DROPDOWN_WIDTH, EditorGUIUtility.singleLineHeight),
                    new GUIContent("No properties", "No suitable properties found for highlighting on this component type")
                );
                entry.propertyName = null;
            }

            entry.symbol = EditorGUI.TextField(
                new Rect(xSymbol, y, SYMBOL_FIELD_WIDTH, EditorGUIUtility.singleLineHeight),
                entry.symbol);
            entry.color = EditorGUI.ColorField(
                new Rect(xColor, y, COLOR_FIELD_WIDTH, EditorGUIUtility.singleLineHeight),
                entry.color);
            entry.propagateUpwards = EditorGUI.ToggleLeft(
                new Rect(xPropagate, y, PROPAGATE_TOGGLE_WIDTH, EditorGUIUtility.singleLineHeight),
                "Recursive",
                entry.propagateUpwards);
            if (GUI.Button(new Rect(xRemove, y, BUTTON_REMOVE_WIDTH, EditorGUIUtility.singleLineHeight), "✗"))
            {
                RemovePropertyHighlightElement(index);
            }
        }

        #region GUI Helper Methods

        private void DisplayProblematicPropertyWarning(HierarchyHighlightConfig config)
        {
            bool hasProblematicConfigs = config.propertyHighlightConfigs.Any(entry =>
                entry != null && entry.enabled && 
                !string.IsNullOrEmpty(entry.componentTypeName) && 
                !string.IsNullOrEmpty(entry.propertyName) &&
                PropertySafetyUtility.IsProblematicProperty(GetTypeFromString(entry.componentTypeName), entry.propertyName));

            if (hasProblematicConfigs)
            {
                EditorGUILayout.HelpBox(
                    "⚠️ Warning: Some property configurations use problematic properties that may create material/mesh instances in edit mode.\n" +
                    "The hierarchy highlighting system automatically handles these safely, but consider using the safe alternatives:\n" +
                    "• Use 'sharedMaterial' instead of 'material'\n" +
                    "• Use 'sharedMaterials' instead of 'materials'\n" +
                    "• Use 'sharedMesh' instead of 'mesh'",
                    MessageType.Warning
                );
            }
        }

        private void DrawBackupManagementSection()
        {
            showBackupSection = EditorGUILayout.Foldout(showBackupSection, "Backup Management", true);
            if (!showBackupSection) return;

            EditorGUILayout.BeginVertical("box");
            DrawCurrentConfigStatus();
            EditorGUILayout.Space();
            DrawBackupOperations();
            EditorGUILayout.Space();
            DrawAvailableBackups();
            EditorGUILayout.EndVertical();
        }

        private void DrawCurrentConfigStatus()
        {
            EditorGUILayout.LabelField("Current Configuration", EditorStyles.boldLabel);
            var currentConfig = HierarchyHighlightConfigUtility.GetConfigIfExists();
            if (currentConfig != null)
            {
                EditorGUILayout.LabelField($"Config found with {currentConfig.typeConfigs?.Count ?? 0} type rules, " +
                                         $"{currentConfig.nameHighlightConfigs?.Count ?? 0} name rules, and " +
                                         $"{currentConfig.propertyHighlightConfigs?.Count ?? 0} property rules.");
            }
            else
            {
                EditorGUILayout.HelpBox("No configuration found.", MessageType.Warning);
            }
        }

        private void DrawBackupOperations()
        {
            EditorGUILayout.LabelField("Backup Operations", EditorStyles.boldLabel);

            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Create Manual Backup"))
            {
                CreateManualBackup();
            }
            if (GUILayout.Button("Refresh Backup List"))
            {
                RefreshBackupList();
            }
            if (GUILayout.Button("Force Reimport Backups"))
            {
                ForceReimportBackups();
            }
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Clear Config Cache"))
            {
                HierarchyHighlightConfigUtility.ClearCache();
                SetupReorderableLists();
                Repaint();
                EditorUtility.DisplayDialog("Cache Cleared", "Config cache has been cleared. The editor will reload the config from disk on next access.", "OK");
            }
            if (GUILayout.Button("Force Reload Config"))
            {
                HierarchyHighlightConfigUtility.ForceLoadConfig();
                SetupReorderableLists();
                Repaint();
                EditorUtility.DisplayDialog("Config Reloaded", "Config has been forcefully reloaded from disk.", "OK");
            }
            EditorGUILayout.EndHorizontal();
        }

        private void DrawAvailableBackups()
        {
            EditorGUILayout.LabelField($"Available Backups ({availableBackups?.Count ?? 0})", EditorStyles.boldLabel);

            if (availableBackups == null || availableBackups.Count == 0)
            {
                EditorGUILayout.HelpBox("No backups found.", MessageType.Info);
                return;
            }

            backupScrollPosition = EditorGUILayout.BeginScrollView(backupScrollPosition, GUILayout.MinHeight(200), GUILayout.MaxHeight(0));

            foreach (var backup in availableBackups)
            {
                DrawBackupEntry(backup);
            }

            EditorGUILayout.EndScrollView();
        }

        private void DrawBackupEntry(string backup)
        {
            EditorGUILayout.BeginVertical("box");

            var fileName = Path.GetFileNameWithoutExtension(backup);
            var fileInfo = new FileInfo(backup);
            var createdTime = fileInfo.Exists ? fileInfo.CreationTime.ToString("yyyy-MM-dd HH:mm:ss") : "Unknown";
            var (typeCount, nameCount, propertyCount) = GetBackupConfigCounts(backup);

            EditorGUILayout.LabelField($"Backup: {fileName}", EditorStyles.boldLabel);
            EditorGUILayout.LabelField($"Created: {createdTime}");
            EditorGUILayout.LabelField($"Config: {typeCount} type rules, {nameCount} name rules, {propertyCount} property rules");
            EditorGUILayout.LabelField($"Path: {backup}");

            EditorGUILayout.BeginHorizontal();

            if (GUILayout.Button("Restore This Backup", GUILayout.Width(150)))
            {
                RestoreBackup(backup);
            }

            if (GUILayout.Button("Delete", GUILayout.Width(60)))
            {
                DeleteBackup(backup);
            }

            if (GUILayout.Button("Show in Project", GUILayout.Width(120)))
            {
                EditorGUIUtility.PingObject(AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(backup));
            }

            if (GUILayout.Button("Test Load", GUILayout.Width(80)))
            {
                TestLoadBackup(backup);
            }

            EditorGUILayout.EndHorizontal();
            EditorGUILayout.EndVertical();
            EditorGUILayout.Space();
        }

        #endregion

        #endregion

        #region Backup Management Methods
        private void RefreshBackupList()
        {
            availableBackups = HierarchyHighlightConfigUtility.GetAvailableBackups();
            Debug.Log($"RefreshBackupList: Found {availableBackups?.Count ?? 0} backup files");

            if (availableBackups != null)
            {
                foreach (var backup in availableBackups)
                {
                    Debug.Log($"Backup found: {backup}");
                }
            }
        }

        private (int typeCount, int nameCount, int propertyCount) GetBackupConfigCounts(string backupPath)
        {
            try
            {
                var backupConfig = AssetDatabase.LoadAssetAtPath<HierarchyHighlightConfig>(backupPath);
                if (backupConfig != null)
                {
                    return (
                        backupConfig.typeConfigs?.Count ?? 0,
                        backupConfig.nameHighlightConfigs?.Count ?? 0,
                        backupConfig.propertyHighlightConfigs?.Count ?? 0
                    );
                }
                else
                {
                    // If AssetDatabase loading fails, try loading as a generic object and check its type
                    var genericAsset = AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(backupPath);
                    if (genericAsset != null)
                    {
                        Debug.LogWarning($"Backup file exists at {backupPath} but is not recognized as HierarchyHighlightConfig. Type: {genericAsset.GetType()}");
                    }
                    else
                    {
                        Debug.LogWarning($"Could not load backup file at {backupPath} - file may be corrupted or not properly imported");
                    }
                }
            }
            catch (System.Exception ex)
            {
                Debug.LogWarning($"Failed to load backup config counts from {backupPath}: {ex.Message}");
            }
            return (0, 0, 0);
        }

        private void ForceReimportBackups()
        {
            // Force refresh and reimport all backup files
            AssetDatabase.Refresh();

            var backupFolder = "Assets/Resources/Backups";
            if (AssetDatabase.IsValidFolder(backupFolder))
            {
                var guids = AssetDatabase.FindAssets("HierarchyHighlightConfig_backup", new[] { backupFolder });
                foreach (var guid in guids)
                {
                    var path = AssetDatabase.GUIDToAssetPath(guid);
                    if (path.EndsWith(".asset"))
                    {
                        AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate);
                        Debug.Log($"Force reimported backup: {path}");
                    }
                }
                AssetDatabase.SaveAssets();
            }

            RefreshBackupList();
            EditorUtility.DisplayDialog("Reimport Complete", "All backup files have been reimported.", "OK");
        }

        private void CreateManualBackup()
        {
            var backupPath = HierarchyHighlightConfigUtility.CreateManualBackup();
            if (!string.IsNullOrEmpty(backupPath))
            {
                RefreshBackupList();
                EditorUtility.DisplayDialog("Backup Created", $"Manual backup created successfully at:\n{backupPath}", "OK");
            }
            else
            {
                EditorUtility.DisplayDialog("Backup Failed", "Failed to create backup. Check the console for details.", "OK");
            }
        }

        private void RestoreBackup(string backupPath)
        {
            if (EditorUtility.DisplayDialog("Restore Backup",
                $"This will replace the current config with the selected backup.\n\nBackup: {Path.GetFileName(backupPath)}\n\nA backup of the current config will be created first. Continue?",
                "Restore", "Cancel"))
            {
                if (HierarchyHighlightConfigUtility.RestoreFromBackup(backupPath))
                {
                    // Force refresh the reorderable lists and editor state
                    SetupReorderableLists();
                    RefreshBackupList();
                    Repaint();

                    EditorUtility.DisplayDialog("Restore Complete", "Config restored successfully from backup.\n\nThe editor has been refreshed to show the restored configuration.", "OK");
                }
                else
                {
                    EditorUtility.DisplayDialog("Restore Failed", "Failed to restore from backup. Check the console for details.", "OK");
                }
            }
        }

        private void DeleteBackup(string backupPath)
        {
            if (EditorUtility.DisplayDialog("Delete Backup",
                $"Are you sure you want to delete this backup?\n\n{Path.GetFileName(backupPath)}\n\nThis action cannot be undone.",
                "Delete", "Cancel"))
            {
                try
                {
                    AssetDatabase.DeleteAsset(backupPath);
                    AssetDatabase.SaveAssets();
                    AssetDatabase.Refresh();
                    RefreshBackupList();
                }
                catch (System.Exception ex)
                {
                    EditorUtility.DisplayDialog("Delete Failed", $"Failed to delete backup: {ex.Message}", "OK");
                }
            }
        }

        private void TestLoadBackup(string backupPath)
        {
            Debug.Log($"TestLoadBackup: Testing load of {backupPath}");

            // Test if file exists
            if (!System.IO.File.Exists(backupPath))
            {
                Debug.LogError($"TestLoadBackup: File does not exist: {backupPath}");
                EditorUtility.DisplayDialog("Test Load Failed", $"File does not exist:\n{backupPath}", "OK");
                return;
            }

            // Only refresh when explicitly testing (not during GUI rendering)
            AssetDatabase.Refresh();
            AssetDatabase.ImportAsset(backupPath, ImportAssetOptions.ForceUpdate);

            // Try loading as HierarchyHighlightConfig
            var backupConfig = AssetDatabase.LoadAssetAtPath<HierarchyHighlightConfig>(backupPath);
            if (backupConfig != null)
            {
                var typeCount = backupConfig.typeConfigs?.Count ?? 0;
                var nameCount = backupConfig.nameHighlightConfigs?.Count ?? 0;
                var propertyCount = backupConfig.propertyHighlightConfigs?.Count ?? 0;

                Debug.Log($"TestLoadBackup: Successfully loaded backup with {typeCount} type rules, {nameCount} name rules, {propertyCount} property rules");
                Debug.Log($"TestLoadBackup: Config version: {backupConfig.configVersion}");

                // Test some of the actual data
                if (backupConfig.typeConfigs != null && backupConfig.typeConfigs.Count > 0)
                {
                    var firstType = backupConfig.typeConfigs[0];
                    Debug.Log($"TestLoadBackup: First type config - Type: {firstType?.typeName}, Symbol: {firstType?.symbol}, Enabled: {firstType?.enabled}");
                }

                EditorUtility.DisplayDialog("Test Load Success",
                    $"Successfully loaded backup:\n{System.IO.Path.GetFileName(backupPath)}\n\nType rules: {typeCount}\nName rules: {nameCount}\nProperty rules: {propertyCount}\nConfig Version: {backupConfig.configVersion}",
                    "OK");
            }
            else
            {
                // Try loading as generic object to see what we get
                var genericAsset = AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(backupPath);
                if (genericAsset != null)
                {
                    Debug.LogWarning($"TestLoadBackup: File loaded as {genericAsset.GetType()} instead of HierarchyHighlightConfig");
                    Debug.LogWarning($"TestLoadBackup: Asset name: {genericAsset.name}");
                    EditorUtility.DisplayDialog("Test Load Issue",
                        $"File exists but loaded as wrong type:\n{genericAsset.GetType()}\n\nExpected: HierarchyHighlightConfig\nAsset name: {genericAsset.name}",
                        "OK");
                }
                else
                {
                    Debug.LogError($"TestLoadBackup: Could not load file at all: {backupPath}");
                    EditorUtility.DisplayDialog("Test Load Failed",
                        $"Could not load file:\n{System.IO.Path.GetFileName(backupPath)}\n\nFile may be corrupted or not properly imported.",
                        "OK");
                }
            }
        }

        protected void OnGUI()
        {
            var config = HierarchyHighlightConfigUtility.GetOrCreateConfig();

            // Simple fallback if config is not ready
            if (config == null || typeConfigList == null || nameHighlightList == null || propertyHighlightList == null)
            {
                EditorGUILayout.HelpBox("HierarchyHighlightConfig is initializing...", MessageType.Info);
                if (GUILayout.Button("Force Reload Config"))
                {
                    HierarchyHighlightConfigUtility.ForceLoadConfig();
                }
                return;
            }

            EditorGUILayout.BeginVertical();
            scrollPos = EditorGUILayout.BeginScrollView(scrollPos);

            EditorGUI.BeginChangeCheck();

            // Draw the main lists
            typeConfigList.DoLayoutList();
            EditorGUILayout.Space();

            nameHighlightList.DoLayoutList();
            EditorGUILayout.Space();

            propertyHighlightList.DoLayoutList();

            // Display warnings about problematic properties
            DisplayProblematicPropertyWarning(config);
            EditorGUILayout.Space();

            // Backup management section
            DrawBackupManagementSection();
            EditorGUILayout.Space();

            if (EditorGUI.EndChangeCheck())
            {
                HierarchyHighlightConfigUtility.SaveConfig(config);
            }

            EditorGUILayout.EndScrollView();
            EditorGUILayout.EndVertical();
        }

        #endregion
    }
}
