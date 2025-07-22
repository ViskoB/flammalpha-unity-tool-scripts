using UnityEditor;
using UnityEngine;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace FlammAlpha.UnityTools.Hierarchy.Highlight
{
    public class HierarchyHighlightBackupManager : EditorWindow
    {
        private Vector2 scrollPosition;
        private List<string> availableBackups;

        [MenuItem("Tools/FlammAlpha/Hierarchy/Backup Manager")]
        public static void ShowWindow()
        {
            GetWindow<HierarchyHighlightBackupManager>("Hierarchy Config Backup Manager");
        }

        private void OnEnable()
        {
            RefreshBackupList();
        }

        private void RefreshBackupList()
        {
            availableBackups = HierarchyHighlightConfigUtility.GetAvailableBackups();
        }

        private void OnGUI()
        {
            EditorGUILayout.LabelField("Hierarchy Highlight Config Backup Manager", EditorStyles.boldLabel);
            EditorGUILayout.Space();

            // Current config status
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

            EditorGUILayout.Space();

            // Backup operations
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
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Force Config Migration"))
            {
                ForceConfigMigration();
            }
            if (GUILayout.Button("Cleanup Old Backups"))
            {
                HierarchyHighlightConfigUtility.CleanupOldBackups();
                RefreshBackupList();
            }
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.Space();

            // Available backups
            EditorGUILayout.LabelField($"Available Backups ({availableBackups?.Count ?? 0})", EditorStyles.boldLabel);

            if (availableBackups == null || availableBackups.Count == 0)
            {
                EditorGUILayout.HelpBox("No backups found.", MessageType.Info);
                return;
            }

            scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition);

            foreach (var backup in availableBackups)
            {
                EditorGUILayout.BeginVertical("box");
                
                var fileName = Path.GetFileNameWithoutExtension(backup);
                var fileInfo = new FileInfo(backup);
                var createdTime = fileInfo.Exists ? fileInfo.CreationTime.ToString("yyyy-MM-dd HH:mm:ss") : "Unknown";
                
                EditorGUILayout.LabelField($"Backup: {fileName}", EditorStyles.boldLabel);
                EditorGUILayout.LabelField($"Created: {createdTime}");
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
                    EditorGUIUtility.PingObject(AssetDatabase.LoadAssetAtPath<Object>(backup));
                }

                EditorGUILayout.EndHorizontal();
                EditorGUILayout.EndVertical();
                EditorGUILayout.Space();
            }

            EditorGUILayout.EndScrollView();
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

        private void ForceConfigMigration()
        {
            if (EditorUtility.DisplayDialog("Force Migration", 
                "This will create a backup of the current config and force a migration. Continue?", 
                "Yes", "Cancel"))
            {
                try
                {
                    HierarchyHighlightConfigUtility.ForceLoadConfig();
                    RefreshBackupList();
                    EditorUtility.DisplayDialog("Migration Complete", "Config migration completed successfully.", "OK");
                }
                catch (System.Exception ex)
                {
                    EditorUtility.DisplayDialog("Migration Failed", $"Config migration failed: {ex.Message}", "OK");
                }
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
                    RefreshBackupList();
                    EditorUtility.DisplayDialog("Restore Complete", "Config restored successfully from backup.", "OK");
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
    }
}
