using UnityEditor;
using UnityEditorInternal;
using UnityEngine;
using System;
using System.Collections.Generic;
using System.Linq;
using FlammAlpha.UnityTools.Common;

namespace FlammAlpha.UnityTools.Hierarchy.Highlight
{
    public class HierarchyHighlightConfigEditor : EditorWindow
    {
        private Vector2 scrollPos;
        [NonSerialized] private HierarchyHighlightConfig config;
        [NonSerialized] private ReorderableList typeConfigList;
        [NonSerialized] private ReorderableList nameHighlightList;
        [NonSerialized] private ReorderableList propertyHighlightList;

        [MenuItem("Tools/FlammAlpha/Hierarchy/Highlight Config")]
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

        private void RemovePropertyHighlightElement(int index)
        {
            if (config == null || config.propertyHighlightConfigs == null) return;
            if (index >= 0 && index < config.propertyHighlightConfigs.Count)
            {
                Undo.RegisterCompleteObjectUndo(config, "Remove List Element");
                config.propertyHighlightConfigs.RemoveAt(index);
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

        private void SetupReorderableLists()
        {
            EnsureConfigListsInitialized();

            SetupTypeConfigList();
            SetupNameHighlightList();
            SetupPropertyHighlightList();
        }

        private void EnsureConfigListsInitialized()
        {
            if (config.typeConfigs == null)
                config.typeConfigs = new List<TypeConfigEntry>();
            if (config.nameHighlightConfigs == null)
                config.nameHighlightConfigs = new List<NameHighlightEntry>();
            if (config.propertyHighlightConfigs == null)
                config.propertyHighlightConfigs = new List<PropertyHighlightEntry>();
        }

        private void SetupTypeConfigList()
        {
            typeConfigList = new ReorderableList(config.typeConfigs, typeof(TypeConfigEntry), true, true, true, true);
            typeConfigList.drawHeaderCallback = rect =>
            {
                EditorGUI.LabelField(rect, "Component Highlight Rules");
            };
            typeConfigList.drawElementCallback = DrawTypeConfigElement;
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
        }

        private void DrawTypeConfigElement(Rect rect, int index, bool active, bool focused)
        {
            float margin = 5;
            float wRemove = 25, wColor = 80, wSymbol = 40, wChange = 70, wType = 120, wPropagate = 90, wEnable = 20;
            float xMax = rect.xMax, y = rect.y + 2;
            float xRemove = xMax - wRemove;
            float xPropagate = xRemove - margin - wPropagate;
            float xColor = xPropagate - margin - wColor;
            float xSymbol = xColor - margin - wSymbol;
            float xType = rect.x + wEnable + margin;
            float xChange = xType + wType + margin;
            float xEnable = rect.x;

            if (config.typeConfigs == null || index < 0 || index >= config.typeConfigs.Count) return;
            var entry = config.typeConfigs[index];
            Type selectedType = GetTypeFromString(entry.typeName);

            entry.enabled = EditorGUI.Toggle(new Rect(xEnable, y, wEnable, EditorGUIUtility.singleLineHeight), entry.enabled);

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
        }

        private void SetupNameHighlightList()
        {
            nameHighlightList = new ReorderableList(config.nameHighlightConfigs, typeof(NameHighlightEntry), true, true, true, true);
            nameHighlightList.drawHeaderCallback = rect =>
            {
                EditorGUI.LabelField(rect, "Name Highlight Rules");
            };
            nameHighlightList.drawElementCallback = DrawNameHighlightElement;
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

        private void DrawNameHighlightElement(Rect rect, int index, bool active, bool focused)
        {
            float margin = 5;
            float wColor = 80, wRemove = 25, wPropagate = 90, wEnable = 20;
            float xMax = rect.xMax, y = rect.y + 2;
            float xRemove = xMax - wRemove;
            float xPropagate = xRemove - margin - wPropagate;
            float xColor = xPropagate - margin - wColor;
            float xPrefix = rect.x + wEnable + margin;
            float xEnable = rect.x;

            if (config.nameHighlightConfigs == null || index < 0 || index >= config.nameHighlightConfigs.Count) return;
            var entry = config.nameHighlightConfigs[index];
            entry.enabled = EditorGUI.Toggle(new Rect(xEnable, y, wEnable, EditorGUIUtility.singleLineHeight), entry.enabled);
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
        }

        private void SetupPropertyHighlightList()
        {
            propertyHighlightList = new ReorderableList(
                config.propertyHighlightConfigs,
                typeof(PropertyHighlightEntry),
                true, true, true, true
            );
            propertyHighlightList.drawHeaderCallback = rect =>
            {
                var label = $"Property Highlight Rules ({config.propertyHighlightConfigs.Count})";
                EditorGUI.LabelField(rect, label);
            };
            propertyHighlightList.drawElementCallback = DrawPropertyHighlightElement;
            propertyHighlightList.onAddCallback = list =>
            {
                Undo.RegisterCompleteObjectUndo(config, "Add Property Highlight");
                config.propertyHighlightConfigs.Add(new PropertyHighlightEntry());
                HierarchyHighlightConfigUtility.SaveConfig(config);
            };
            propertyHighlightList.onRemoveCallback = list =>
            {
                RemovePropertyHighlightElement(list.index);
            };
        }

        private void DrawPropertyHighlightElement(Rect rect, int index, bool isActive, bool isFocused)
        {
            float margin = 5;
            float wRemove = 25, wColor = 80, wSymbol = 40, wPropagate = 90, wEnable = 20;
            float wComponent = 120, wProperty = 100, wChange = 70;
            float y = rect.y + 2;
            float x = rect.x;
            float xMax = rect.xMax;

            float xRemove = xMax - wRemove;
            float xPropagate = xRemove - margin - wPropagate;
            float xColor = xPropagate - margin - wColor;
            float xSymbol = xColor - margin - wSymbol;
            float xProperty = xSymbol - margin - wProperty;
            float xComponent = x + wEnable + margin;
            float xEnable = x;

            if (config.propertyHighlightConfigs == null || index < 0 || index >= config.propertyHighlightConfigs.Count) return;
            var entry = config.propertyHighlightConfigs[index];
            entry.enabled = EditorGUI.Toggle(new Rect(xEnable, y, wEnable, EditorGUIUtility.singleLineHeight), entry.enabled);
            Type componentType = GetTypeFromString(entry.componentTypeName);
            if (componentType != null)
            {
                EditorGUI.LabelField(new Rect(xComponent, y, wComponent, EditorGUIUtility.singleLineHeight), componentType.Name);
                if (GUI.Button(new Rect(xComponent + wComponent + margin, y, wChange, EditorGUIUtility.singleLineHeight), "Change"))
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
                if (GUI.Button(new Rect(xComponent, y, wComponent + wChange + margin, EditorGUIUtility.singleLineHeight), "Pick Type"))
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
                    new Rect(xProperty, y, wProperty, EditorGUIUtility.singleLineHeight),
                    new GUIContent("", tooltipText),
                    selectedIndex,
                    propertyOptions.Select(p => new GUIContent(p)).ToArray()
                );
                entry.propertyName = propertyOptions[newSelectedIndex];
            }
            else
            {
                EditorGUI.LabelField(
                    new Rect(xProperty, y, wProperty, EditorGUIUtility.singleLineHeight),
                    new GUIContent("No properties", "No suitable properties found for highlighting on this component type")
                );
                entry.propertyName = null;
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
                RemovePropertyHighlightElement(index);
            }
        }

        protected void OnGUI()
        {
            if (
                config == null ||
                typeConfigList == null || nameHighlightList == null || propertyHighlightList == null ||
                config.typeConfigs == null || config.nameHighlightConfigs == null || config.propertyHighlightConfigs == null
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

            propertyHighlightList.DoLayoutList();

            // Check for and display warnings about problematic property configurations
            bool hasProblematicConfigs = false;
            foreach (var entry in config.propertyHighlightConfigs)
            {
                if (entry != null && entry.enabled && !string.IsNullOrEmpty(entry.componentTypeName) && !string.IsNullOrEmpty(entry.propertyName))
                {
                    Type componentType = GetTypeFromString(entry.componentTypeName);
                    if (componentType != null && PropertySafetyUtility.IsProblematicProperty(componentType, entry.propertyName))
                    {
                        hasProblematicConfigs = true;
                        break;
                    }
                }
            }

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
