using UnityEngine;
using UnityEditor;
using System;
using System.Collections.Generic;

namespace FlammAlpha.UnityTools.Common
{
    /// <summary>
    /// Utility class for creating consistent, styled lists in Unity Editor windows.
    /// Provides collapsible sections, alternating row colors, and standardized spacing.
    /// </summary>
    public static class EditorListUtility
    {
        private static readonly Color AlternateRowColor = new Color(0.85f, 0.85f, 0.85f, 1f);
        private static readonly Color HoveredRowColor = new Color(0.1f, 0.1f, 0.3f, 1f);
        private static readonly Color SelectedRowColor = new Color(0.1f, 0.3f, 0.1f, 1f);

        /// <summary>
        /// Data structure for handling mouse interactions with list items.
        /// </summary>
        public struct ListItemInteraction
        {
            public bool isHovered;
            public bool wasClicked;
            public Rect itemRect;
        }

        /// <summary>
        /// Draws expand/collapse all buttons for foldout lists.
        /// </summary>
        /// <param name="onExpandAll">Action to execute when Expand All is clicked</param>
        /// <param name="onCollapseAll">Action to execute when Collapse All is clicked</param>
        public static void DrawExpandCollapseButtons(Action onExpandAll, Action onCollapseAll)
        {
            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("Expand All", GUILayout.Width(90)))
                    onExpandAll?.Invoke();

                if (GUILayout.Button("Collapse All", GUILayout.Width(90)))
                    onCollapseAll?.Invoke();
            }
        }

        /// <summary>
        /// Draws select/deselect all buttons for selectable lists.
        /// </summary>
        /// <param name="onSelectAll">Action to execute when Select All is clicked</param>
        /// <param name="onDeselectAll">Action to execute when Deselect All is clicked</param>
        public static void DrawSelectDeselectButtons(Action onSelectAll, Action onDeselectAll)
        {
            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("Select All", GUILayout.Width(90)))
                    onSelectAll?.Invoke();

                if (GUILayout.Button("Deselect All", GUILayout.Width(90)))
                    onDeselectAll?.Invoke();
            }
        }

        /// <summary>
        /// Draws both expand/collapse and select/deselect control buttons.
        /// </summary>
        /// <param name="onExpandAll">Action to execute when Expand All is clicked</param>
        /// <param name="onCollapseAll">Action to execute when Collapse All is clicked</param>
        /// <param name="onSelectAll">Action to execute when Select All is clicked</param>
        /// <param name="onDeselectAll">Action to execute when Deselect All is clicked</param>
        public static void DrawAllControlButtons(Action onExpandAll, Action onCollapseAll, Action onSelectAll, Action onDeselectAll)
        {
            using (new GUILayout.VerticalScope("helpbox"))
            {
                DrawExpandCollapseButtons(onExpandAll, onCollapseAll);
                DrawSelectDeselectButtons(onSelectAll, onDeselectAll);
            }
        }

        /// <summary>
        /// Draws a collapsible section header.
        /// </summary>
        /// <param name="foldout">Current foldout state</param>
        /// <param name="title">Section title</param>
        /// <param name="onToggle">Optional action when foldout is toggled</param>
        /// <returns>New foldout state</returns>
        public static bool DrawCollapsibleHeader(bool foldout, string title, Action<bool> onToggle = null)
        {
            bool newFoldout = EditorGUILayout.Foldout(foldout, title, true, EditorStyles.foldoutHeader);
            if (newFoldout != foldout)
                onToggle?.Invoke(newFoldout);
            return newFoldout;
        }

        /// <summary>
        /// Draws a list item with consistent styling including alternating backgrounds and mouse interaction handling.
        /// </summary>
        /// <param name="index">Item index for alternating colors</param>
        /// <param name="drawContent">Action to draw the item content</param>
        /// <param name="isSelected">Whether this item is selected</param>
        /// <param name="isHovered">Whether this item is hovered</param>
        /// <param name="customBackgroundColor">Optional custom background color</param>
        /// <param name="onItemClicked">Optional action when item is clicked</param>
        /// <returns>Interaction data for this list item</returns>
        public static ListItemInteraction DrawListItem(int index, Action drawContent, bool isSelected = false, bool isHovered = false, Color? customBackgroundColor = null, Action onItemClicked = null)
        {
            Color originalColor = GUI.backgroundColor;

            // Determine background color
            Color backgroundColor;
            if (customBackgroundColor.HasValue)
                backgroundColor = customBackgroundColor.Value;
            else if (isHovered)
                backgroundColor = HoveredRowColor;
            else if (isSelected)
                backgroundColor = SelectedRowColor;
            else if (index % 2 == 1)
                backgroundColor = AlternateRowColor;
            else
                backgroundColor = originalColor;

            GUI.backgroundColor = backgroundColor;

            Rect itemRect;
            using (new GUILayout.VerticalScope(GUI.skin.box))
            {
                drawContent?.Invoke();

                // Get the rect after content is drawn
                if (Event.current.type == EventType.Repaint)
                    itemRect = GUILayoutUtility.GetLastRect();
                else
                    itemRect = new Rect();
            }

            GUI.backgroundColor = originalColor;

            // Handle click interaction
            bool wasClicked = false;
            if (Event.current.type == EventType.MouseDown && Event.current.button == 0 && itemRect.Contains(Event.current.mousePosition))
            {
                wasClicked = true;
                onItemClicked?.Invoke();
                Event.current.Use();
            }

            // Check for hover
            bool currentlyHovered = Event.current.type == EventType.Repaint && itemRect.Contains(Event.current.mousePosition);

            return new ListItemInteraction
            {
                isHovered = currentlyHovered,
                wasClicked = wasClicked,
                itemRect = itemRect
            };
        }

        /// <summary>
        /// Draws a horizontal list item (single row layout).
        /// </summary>
        /// <param name="index">Item index for alternating colors</param>
        /// <param name="drawContent">Action to draw the item content</param>
        /// <param name="isSelected">Whether this item is selected</param>
        /// <param name="isHovered">Whether this item is hovered</param>
        /// <param name="customBackgroundColor">Optional custom background color</param>
        /// <param name="onItemClicked">Optional action when item is clicked</param>
        /// <returns>Interaction data for this list item</returns>
        public static ListItemInteraction DrawHorizontalListItem(int index, Action drawContent, bool isSelected = false, bool isHovered = false, Color? customBackgroundColor = null, Action onItemClicked = null)
        {
            return DrawListItem(index, () =>
            {
                using (new GUILayout.HorizontalScope())
                {
                    drawContent?.Invoke();
                }
            }, isSelected, isHovered, customBackgroundColor, onItemClicked);
        }

        /// <summary>
        /// Draws a list item with a toggle checkbox for selection.
        /// </summary>
        /// <param name="index">Item index for alternating colors</param>
        /// <param name="isToggled">Current toggle state</param>
        /// <param name="foldout">Current foldout state</param>
        /// <param name="title">Foldout title</param>
        /// <param name="drawContent">Action to draw the foldout content when expanded</param>
        /// <param name="customBackgroundColor">Optional custom background color</param>
        /// <param name="onToggleChanged">Action when toggle state changes</param>
        /// <param name="onFoldoutChanged">Action when foldout state changes</param>
        /// <returns>New toggle and foldout states</returns>
        public static (bool newToggle, bool newFoldout) DrawSelectableListItem(
            int index,
            bool isToggled,
            bool foldout,
            string title,
            Action drawContent = null,
            Color? customBackgroundColor = null,
            Action<bool> onToggleChanged = null,
            Action<bool> onFoldoutChanged = null)
        {
            bool newToggle = isToggled;
            bool newFoldout = foldout;

            DrawListItem(index, () =>
            {
                using (new EditorGUILayout.HorizontalScope())
                {
                    bool toggleResult = EditorGUILayout.Toggle(isToggled, GUILayout.Width(18));
                    if (toggleResult != isToggled)
                    {
                        newToggle = toggleResult;
                        onToggleChanged?.Invoke(newToggle);
                    }

                    bool foldoutResult = EditorGUILayout.Foldout(foldout, title, true);
                    if (foldoutResult != foldout)
                    {
                        newFoldout = foldoutResult;
                        onFoldoutChanged?.Invoke(newFoldout);
                    }
                }

                if (foldout && drawContent != null)
                {
                    drawContent.Invoke();
                }
            }, customBackgroundColor: customBackgroundColor);

            return (newToggle, newFoldout);
        }

        /// <summary>
        /// Draws spacing between list items.
        /// </summary>
        /// <param name="currentIndex">Current item index</param>
        /// <param name="totalCount">Total number of items</param>
        /// <param name="spacing">Spacing amount (default 2)</param>
        public static void DrawItemSpacing(int currentIndex, int totalCount, float spacing = 2f)
        {
            if (currentIndex < totalCount - 1)
                GUILayout.Space(spacing);
        }

        /// <summary>
        /// Draws a section spacing between different sections.
        /// </summary>
        /// <param name="currentIndex">Current section index</param>
        /// <param name="totalCount">Total number of sections</param>
        /// <param name="spacing">Spacing amount (default 5)</param>
        public static void DrawSectionSpacing(int currentIndex, int totalCount, float spacing = 5f)
        {
            if (currentIndex < totalCount - 1)
                GUILayout.Space(spacing);
        }

        /// <summary>
        /// Creates a scope that applies indentation for nested content.
        /// </summary>
        /// <returns>Disposable scope</returns>
        public static IDisposable CreateIndentScope()
        {
            return new EditorGUI.IndentLevelScope();
        }

        /// <summary>
        /// Calculates the scroll position to bring a target item into view.
        /// </summary>
        /// <param name="targetItemY">Y position of the target item</param>
        /// <param name="scrollViewHeight">Height of the scroll view</param>
        /// <param name="viewPortPercentage">Percentage from top of view where item should appear (0.0 to 1.0)</param>
        /// <returns>New scroll position Y value</returns>
        public static float CalculateScrollToItem(float targetItemY, float scrollViewHeight, float viewPortPercentage = 0.3f)
        {
            float targetScrollY = targetItemY - (scrollViewHeight * viewPortPercentage);
            return Mathf.Max(0, targetScrollY);
        }

        /// <summary>
        /// Draws a collapsible foldout header that can be clicked to select an object.
        /// </summary>
        /// <param name="foldout">Current foldout state</param>
        /// <param name="title">Foldout title</param>
        /// <param name="onToggle">Action when foldout is toggled</param>
        /// <param name="onHeaderClicked">Action when header area is clicked</param>
        /// <returns>New foldout state</returns>
        public static bool DrawClickableFoldout(bool foldout, string title, Action<bool> onToggle = null, Action onHeaderClicked = null)
        {
            EditorGUILayout.BeginHorizontal();
            bool newFoldout = EditorGUILayout.Foldout(foldout, title, true);
            if (newFoldout != foldout)
            {
                onToggle?.Invoke(newFoldout);
            }

            // Check if the foldout area was clicked
            Rect foldoutRect = GUILayoutUtility.GetLastRect();
            if (Event.current.type == EventType.MouseDown && Event.current.button == 0 && foldoutRect.Contains(Event.current.mousePosition))
            {
                onHeaderClicked?.Invoke();
                Event.current.Use();
            }
            EditorGUILayout.EndHorizontal();

            return newFoldout;
        }

        /// <summary>
        /// Utility class for managing foldout states in collections.
        /// </summary>
        /// <typeparam name="T">Type of the key for foldout tracking</typeparam>
        public class FoldoutManager<T>
        {
            private readonly Dictionary<T, bool> foldoutStates = new Dictionary<T, bool>();
            private readonly bool defaultState;

            public FoldoutManager(bool defaultState = true)
            {
                this.defaultState = defaultState;
            }

            /// <summary>
            /// Gets or sets the foldout state for a given key.
            /// </summary>
            public bool this[T key]
            {
                get
                {
                    if (!foldoutStates.ContainsKey(key))
                        foldoutStates[key] = defaultState;
                    return foldoutStates[key];
                }
                set => foldoutStates[key] = value;
            }

            /// <summary>
            /// Ensures a foldout state exists for the given key.
            /// </summary>
            public void EnsureExists(T key)
            {
                if (!foldoutStates.ContainsKey(key))
                    foldoutStates[key] = defaultState;
            }

            /// <summary>
            /// Sets all foldout states to the specified value.
            /// </summary>
            public void SetAll(bool value)
            {
                var keys = new List<T>(foldoutStates.Keys);
                foreach (var key in keys)
                    foldoutStates[key] = value;
            }

            /// <summary>
            /// Sets all foldout states for the given collection to the specified value.
            /// </summary>
            public void SetAll(IEnumerable<T> keys, bool value)
            {
                foreach (var key in keys)
                    foldoutStates[key] = value;
            }
        }
    }
}
