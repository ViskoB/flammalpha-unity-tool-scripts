using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEngine;

namespace FlammAlpha.UnityTools.Hierarchy.Highlight
{
    public class TypeSearchPopup : EditorWindow
    {
        private static Action<Type> _onTypeSelected;
        private static string _search = "";
        private static List<Type> _componentTypes;
        private static MonoScript _droppedScript;
        private Vector2 _scroll;
        private bool _focusSearch;
        private bool _clearSearch;

        public static void Show(Action<Type> onSelect)
        {
            _onTypeSelected = onSelect;
            _search = "";
            _droppedScript = null;
            var window = CreateInstance<TypeSearchPopup>();
            window.titleContent = new GUIContent("Pick a Type");
            Vector2 windowSize = new Vector2(420, 440);
            Vector2 mouse = GUIUtility.GUIToScreenPoint(Event.current?.mousePosition ?? new Vector2(200, 200));
            window.position = new Rect(mouse, windowSize);
            BuildTypeList();
            window._focusSearch = true;
            window.ShowUtility();
        }

        private static void BuildTypeList()
        {
            HashSet<Type> allTypes = new HashSet<Type>();

            foreach (var asm in AppDomain.CurrentDomain.GetAssemblies())
            {
                Type[] types = null;
                try { types = asm.GetTypes(); }
                catch (ReflectionTypeLoadException typeLoadEx) { types = typeLoadEx.Types; }
                catch { continue; }
                if (types == null) continue;

                foreach (var t in types)
                {
                    if (IsValidUserType(t))
                        allTypes.Add(t);
                }
            }

            string[] scriptGuids = AssetDatabase.FindAssets("t:MonoScript");
            foreach (string guid in scriptGuids)
            {
                string assetPath = AssetDatabase.GUIDToAssetPath(guid);
                MonoScript script = AssetDatabase.LoadAssetAtPath<MonoScript>(assetPath);
                if (script == null) continue;
                Type scriptType = script.GetClass();
                if (IsValidUserType(scriptType))
                    allTypes.Add(scriptType);
            }

            _componentTypes = allTypes.OrderBy(t => t.FullName).ToList();
        }

        private static bool IsValidUserType(Type t)
        {
            if (t == null) return false;
            if (!typeof(Component).IsAssignableFrom(t)) return false;
            if (t.IsGenericType) return false;
            if (t.IsAbstract && !t.IsInterface) return false;
            // if (!t.IsVisible) return false;
            if (t.FullName.StartsWith("UnityEditor.") || t.FullName.StartsWith("UnityEngine.Editor")) return false;

            return true;
        }

        private void OnGUI()
        {
            if (_clearSearch)
            {
                _search = "";
                _clearSearch = false;
                EditorGUI.FocusTextInControl("TypeSearchBox");
            }
            EditorGUILayout.Space();

            GUILayout.BeginHorizontal();
            GUI.SetNextControlName("TypeSearchBox");
            _search = EditorGUILayout.TextField(_search, GUILayout.ExpandWidth(true));
            if (!string.IsNullOrEmpty(_search))
            {
                if (GUILayout.Button("✕", GUILayout.Width(24), GUILayout.Height(EditorGUIUtility.singleLineHeight)))
                {
                    _clearSearch = true;
                }
            }
            if (GUILayout.Button("Refresh", GUILayout.Width(70), GUILayout.Height(EditorGUIUtility.singleLineHeight)))
            {
                BuildTypeList();
                GUI.FocusControl("TypeSearchBox");
            }
            GUILayout.EndHorizontal();

            if (_focusSearch)
            {
                _focusSearch = false;
                EditorGUI.FocusTextInControl("TypeSearchBox");
            }
            EditorGUILayout.Space();

            var filtered = string.IsNullOrEmpty(_search)
                        ? _componentTypes
                        : _componentTypes.Where(t => t.FullName.IndexOf(_search, StringComparison.OrdinalIgnoreCase) >= 0).ToList();

            _scroll = EditorGUILayout.BeginScrollView(_scroll);
            foreach (var type in filtered)
            {
                if (GUILayout.Button(type.FullName, EditorStyles.label))
                {
                    _onTypeSelected?.Invoke(type);
                    Close();
                    break;
                }
            }
            EditorGUILayout.EndScrollView();
        }
    }
}
