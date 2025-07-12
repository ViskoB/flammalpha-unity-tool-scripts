/*************************************************************************************
* FlammAlpha 2025
* Configuration for the Hierarchy Color View
*************************************************************************************/
using UnityEditor;
using UnityEditorInternal;
using UnityEngine;
using System;
using System.Collections.Generic;
using System.Linq;

namespace UnityHierarchyColor
{
    public class HierarchyHighlightConfigEditor : EditorWindow
    {
        private Vector2 scrollPos;
        [NonSerialized] private HierarchyHighlightConfig config;
        [NonSerialized] private ReorderableList typeConfigList;
        [NonSerialized] private ReorderableList nameHighlightList;

        [MenuItem("Tools/FlammAlpha/Hierarchy Highlight Config")]
        protected static void OpenWindow()
        {
            GetWindow<HierarchyHighlightConfigEditor>("Hierarchy Highlight Config");
        }

        protected void OnEnable()
        {
            HierarchyHighlightConfigUtility.OnConfigUpdate += OnConfigUpdated;
        }

        protected void OnDisable()
        {
            HierarchyHighlightConfigUtility.OnConfigUpdate -= OnConfigUpdated;
        }

        private void OnConfigUpdated(HierarchyHighlightConfig loadedConfig)
        {
            config = loadedConfig;
            SetupReorderableLists();
            Repaint();
        }

        private void RemoveTypeConfigElement(int index)
        {
            if (config == null || config.typeConfigs == null) return;
            if (index >= 0 && index < config.typeConfigs.Count)
            {
                Undo.RegisterCompleteObjectUndo(config, "Remove List Element");
                config.typeConfigs.RemoveAt(index);
                HierarchyHighlightConfigUtility.SaveConfig(config);
                GUIUtility.keyboardControl = 0;
            }
        }

        private void RemoveNameHighlightElement(int index)
        {
            if (config == null || config.nameHighlightConfigs == null) return;
            if (index >= 0 && index < config.nameHighlightConfigs.Count)
            {
                Undo.RegisterCompleteObjectUndo(config, "Remove List Element");
                config.nameHighlightConfigs.RemoveAt(index);
                HierarchyHighlightConfigUtility.SaveConfig(config);
                GUIUtility.keyboardControl = 0;
            }
        }

        private void ShowTypePicker(Action<Type> onPick)
        {
            TypeSearchPopup.Show(type =>
                    {
                        if (type != null)
                        {
                            onPick(type);
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

        private void SetupReorderableLists()
        {
            if (config.typeConfigs == null)
                config.typeConfigs = new List<TypeConfigEntry>();
            if (config.nameHighlightConfigs == null)
                config.nameHighlightConfigs = new List<NameHighlightEntry>();

            typeConfigList = new ReorderableList(config.typeConfigs, typeof(TypeConfigEntry), true, true, true, true);
            typeConfigList.drawHeaderCallback = rect =>
            {
                EditorGUI.LabelField(rect, "Component Highlight Rules");
            };
            typeConfigList.drawElementCallback = (rect, index, active, focused) =>
            {
                float margin = 5;
                float wRemove = 25, wColor = 80, wSymbol = 40, wChange = 70, wType = 120, wPropagate = 90;
                float xMax = rect.xMax, y = rect.y + 2;
                float xRemove = xMax - wRemove;
                float xPropagate = xRemove - margin - wPropagate;
                float xColor = xPropagate - margin - wColor;
                float xSymbol = xColor - margin - wSymbol;
                float xType = rect.x;
                float xChange = xType + wType + margin;

                if (config.typeConfigs == null || index < 0 || index >= config.typeConfigs.Count) return;
                var entry = config.typeConfigs[index];

                Type selectedType = GetTypeFromString(entry.typeName);

                if (selectedType != null)
                {
                    EditorGUI.LabelField(new Rect(xType, y, wType, EditorGUIUtility.singleLineHeight), selectedType.Name);
                    if (GUI.Button(new Rect(xChange, y, wChange, EditorGUIUtility.singleLineHeight), "Change"))
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
                    if (GUI.Button(new Rect(xType, y, wType + wChange + margin, EditorGUIUtility.singleLineHeight), "Pick Type"))
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
                    new Rect(xSymbol, y, wSymbol, EditorGUIUtility.singleLineHeight),
                    entry.symbol);
                entry.color = EditorGUI.ColorField(
                    new Rect(xColor, y, wColor, EditorGUIUtility.singleLineHeight),
                    entry.color);
                entry.propagateUpwards = EditorGUI.ToggleLeft(
                    new Rect(xPropagate, y, wPropagate, EditorGUIUtility.singleLineHeight),
                    "Recursive",
                    entry.propagateUpwards);
                if (GUI.Button(new Rect(xRemove, y, wRemove, EditorGUIUtility.singleLineHeight), "✗"))
                {
                    RemoveTypeConfigElement(index);
                }
            };
            typeConfigList.onAddCallback = list =>
            {
                Undo.RegisterCompleteObjectUndo(config, "Add Type Config");
                config.typeConfigs.Add(new TypeConfigEntry());
                HierarchyHighlightConfigUtility.SaveConfig(config);
            };
            typeConfigList.onRemoveCallback = list =>
            {
                RemoveTypeConfigElement(list.index);
            };

            nameHighlightList = new ReorderableList(config.nameHighlightConfigs, typeof(NameHighlightEntry), true, true, true, true);
            nameHighlightList.drawHeaderCallback = rect =>
            {
                EditorGUI.LabelField(rect, "Name Highlight Rules");
            };
            nameHighlightList.drawElementCallback = (rect, index, active, focused) =>
            {
                float margin = 5;
                float wColor = 80, wRemove = 25, wPropagate = 90;
                float xMax = rect.xMax, y = rect.y + 2;
                float xRemove = xMax - wRemove;
                float xPropagate = xRemove - margin - wPropagate;
                float xColor = xPropagate - margin - wColor;
                float xPrefix = rect.x;

                if (config.nameHighlightConfigs == null || index < 0 || index >= config.nameHighlightConfigs.Count) return;
                var entry = config.nameHighlightConfigs[index];

                entry.prefix = EditorGUI.TextField(
                    new Rect(xPrefix, y, xColor - xPrefix - margin, EditorGUIUtility.singleLineHeight),
                    entry.prefix);
                entry.color = EditorGUI.ColorField(
                    new Rect(xColor, y, wColor, EditorGUIUtility.singleLineHeight),
                    entry.color);
                entry.propagateUpwards = EditorGUI.ToggleLeft(
                    new Rect(xPropagate, y, wPropagate, EditorGUIUtility.singleLineHeight),
                    "Recursive",
                    entry.propagateUpwards);
                if (GUI.Button(new Rect(xRemove, y, wRemove, EditorGUIUtility.singleLineHeight), "✗"))
                {
                    RemoveNameHighlightElement(index);
                }
            };
            nameHighlightList.onAddCallback = list =>
            {
                Undo.RegisterCompleteObjectUndo(config, "Add Name Highlight");
                config.nameHighlightConfigs.Add(new NameHighlightEntry());
                HierarchyHighlightConfigUtility.SaveConfig(config);
            };
            nameHighlightList.onRemoveCallback = list =>
            {
                RemoveNameHighlightElement(list.index);
            };
        }

        protected void OnGUI()
        {
            if (
                config == null ||
                typeConfigList == null || nameHighlightList == null ||
                config.typeConfigs == null || config.nameHighlightConfigs == null
            )
            {
                EditorGUILayout.HelpBox(
                    "HierarchyHighlightConfig asset is not ready.\n" +
                    "If this is the first run or after recompiles, it may take a few seconds to initialize.",
                    MessageType.Error
                );

                if (GUILayout.Button("Force Reload Config"))
                {
                    HierarchyHighlightConfigUtility.ForceLoadConfig();
                }
                return;
            }

            EditorGUILayout.BeginVertical();
            scrollPos = EditorGUILayout.BeginScrollView(scrollPos);

            EditorGUI.BeginChangeCheck();

            typeConfigList.DoLayoutList();

            EditorGUILayout.Space();

            nameHighlightList.DoLayoutList();

            EditorGUILayout.Space();

            if (EditorGUI.EndChangeCheck())
            {
                HierarchyHighlightConfigUtility.SaveConfig(config);
            }

            EditorGUILayout.EndScrollView();
            EditorGUILayout.EndVertical();
        }
    }
}
