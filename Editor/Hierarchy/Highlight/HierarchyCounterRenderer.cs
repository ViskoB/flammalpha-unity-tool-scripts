using System;
using System.Collections.Generic;
using System.Linq;
using FlammAlpha.UnityTools.Data;
using UnityEditor;
using UnityEngine;

namespace FlammAlpha.UnityTools.Hierarchy.Highlight
{
    /// <summary>
    /// Handles rendering of counters in the hierarchy window.
    /// </summary>
    public static class HierarchyCounterRenderer
    {
        private static readonly string[] spinner = { "|", "/", "-", "\\" };

        /// <summary>
        /// Represents a single counter to be displayed.
        /// </summary>
        public struct CounterInfo
        {
            public readonly string Symbol;
            public readonly int TotalCount;
            public readonly int SelfCount;
            public readonly Color BackgroundColor;
            public readonly string TooltipText;
            public readonly int FilterIndex;
            public readonly string Label;
            public readonly float Width;

            public CounterInfo(string symbol, int totalCount, int selfCount, Color backgroundColor,
                string tooltipText, int filterIndex, bool isProperty)
            {
                Symbol = symbol;
                TotalCount = totalCount;
                SelfCount = selfCount;
                BackgroundColor = backgroundColor;
                TooltipText = tooltipText;
                FilterIndex = filterIndex;

                string totalCountStr = totalCount == selfCount ? "" : $"{totalCount}";
                string selfCountStr = selfCount > 0 ? $" ({selfCount})" : "";
                Label = $"{symbol} {totalCountStr}{selfCountStr}".Trim();
                Width = Mathf.Max(EditorStyles.label.CalcSize(new GUIContent(Label)).x + 6, 32) + 2f;
            }

            public bool IsVisible => (TotalCount > 0 || SelfCount > 0) && !string.IsNullOrWhiteSpace(Symbol);
        }

        /// <summary>
        /// Context for drawing counters, containing all the state needed for counter rendering.
        /// </summary>
        public struct CounterDrawContext
        {
            public float NextX;
            public float UsedWidth;
            public readonly float AvailableWidth;
            public readonly Rect SelectionRect;
            public readonly List<(int typeIndex, Rect counterRect)> CounterRects;
            public readonly GUIStyle CountStyle;

            public CounterDrawContext(
                float nextX,
                float usedWidth,
                float availableWidth,
                Rect selectionRect,
                List<(int typeIndex, Rect counterRect)> counterRects,
                GUIStyle countStyle)
            {
                NextX = nextX;
                UsedWidth = usedWidth;
                AvailableWidth = availableWidth;
                SelectionRect = selectionRect;
                CounterRects = counterRects;
                CountStyle = countStyle;
            }
        }

        /// <summary>
        /// Collects all visible counters for a GameObject.
        /// </summary>
        public static List<CounterInfo> CollectCounters(
            List<TypeConfigEntry> typeConfigs,
            List<PropertyHighlightEntry> propertyConfigs,
            int[] counts,
            int[] countsOnSelf,
            int[] propertyCounts,
            int[] propertyCountsOnSelf)
        {
            var counters = new List<CounterInfo>();

            // Add property counters (in reverse order for display)
            if (propertyConfigs != null && propertyConfigs.Count > 0)
            {
                for (int i = propertyConfigs.Count - 1; i >= 0; i--)
                {
                    int filterIdx = i + (typeConfigs?.Count ?? 0);
                    string tooltipText = $"{propertyConfigs[i].componentTypeName}.{propertyConfigs[i].propertyName}";

                    var counter = new CounterInfo(
                        propertyConfigs[i].symbol,
                        propertyCounts[i],
                        propertyCountsOnSelf[i],
                        propertyConfigs[i].color,
                        tooltipText,
                        filterIdx,
                        true);

                    if (counter.IsVisible)
                        counters.Add(counter);
                }
            }

            // Add type counters (in reverse order for display)
            if (typeConfigs != null && typeConfigs.Count > 0)
            {
                for (int i = typeConfigs.Count - 1; i >= 0; i--)
                {
                    var counter = new CounterInfo(
                        typeConfigs[i].symbol,
                        counts[i],
                        countsOnSelf[i],
                        typeConfigs[i].color,
                        typeConfigs[i].typeName,
                        i,
                        false);

                    if (counter.IsVisible)
                        counters.Add(counter);
                }
            }

            return counters;
        }

        /// <summary>
        /// Draws counters with smart space management and a "more" button for hidden counters.
        /// </summary>
        public static void DrawCountersWithMoreButton(
            List<CounterInfo> allCounters,
            ref CounterDrawContext context,
            int filteredTypeIndex)
        {
            const float moreButtonWidth = 28f;
            const float moreButtonPadding = 2f;
            const float totalMoreButtonWidth = moreButtonWidth + moreButtonPadding;

            // First pass: see if all counters fit without reserving space for "more" button
            var visibleCounters = new List<CounterInfo>();
            var hiddenCounters = new List<CounterInfo>();
            float currentWidth = 0f;

            foreach (var counter in allCounters)
            {
                if (currentWidth + counter.Width <= context.AvailableWidth)
                {
                    visibleCounters.Add(counter);
                    currentWidth += counter.Width;
                }
                else
                {
                    hiddenCounters.Add(counter);
                }
            }

            // If some counters don't fit, we need to reserve space for the "more" button
            // and recalculate which counters can be shown
            if (hiddenCounters.Count > 0)
            {
                float availableForCounters = context.AvailableWidth - totalMoreButtonWidth;
                visibleCounters.Clear();
                hiddenCounters.Clear();
                currentWidth = 0f;

                foreach (var counter in allCounters)
                {
                    if (currentWidth + counter.Width <= availableForCounters)
                    {
                        visibleCounters.Add(counter);
                        currentWidth += counter.Width;
                    }
                    else
                    {
                        hiddenCounters.Add(counter);
                    }
                }
            }

            // Draw visible counters
            foreach (var counter in visibleCounters)
            {
                DrawSingleCounter(counter, ref context, filteredTypeIndex);
            }

            // Draw "more" button only if there are hidden counters
            if (hiddenCounters.Count > 0)
            {
                DrawMoreButton(hiddenCounters, totalMoreButtonWidth, ref context);
            }
        }

        /// <summary>
        /// Draws a single counter.
        /// </summary>
        public static void DrawSingleCounter(CounterInfo counter, ref CounterDrawContext context, int filteredTypeIndex)
        {
            context.UsedWidth += counter.Width;
            context.NextX -= counter.Width;

            float labelWidth = counter.Width - 2f; // Remove padding
            Rect countRect = new Rect(context.NextX, context.SelectionRect.y, labelWidth, context.SelectionRect.height);

            context.CounterRects.Add((counter.FilterIndex, countRect));
            EditorGUI.DrawRect(countRect, counter.BackgroundColor);

            if (filteredTypeIndex == counter.FilterIndex)
            {
                Handles.color = Color.yellow;
                Handles.DrawAAPolyLine(3,
                    new Vector3(countRect.x, countRect.y),
                    new Vector3(countRect.xMax, countRect.y),
                    new Vector3(countRect.xMax, countRect.yMax),
                    new Vector3(countRect.x, countRect.yMax),
                    new Vector3(countRect.x, countRect.y)
                );
            }

            EditorGUI.LabelField(countRect, new GUIContent(counter.Label, counter.TooltipText), context.CountStyle);
        }

        /// <summary>
        /// Draws the "more" button with tooltip listing hidden counters.
        /// </summary>
        public static void DrawMoreButton(List<CounterInfo> hiddenCounters, float totalWidth, ref CounterDrawContext context)
        {
            context.NextX -= totalWidth;
            Rect moreRect = new Rect(context.NextX, context.SelectionRect.y, totalWidth - 2f, context.SelectionRect.height);

            Color moreColor = EditorGUIUtility.isProSkin
                ? new Color(0.6f, 0.6f, 0.6f, 0.8f)
                : new Color(0.4f, 0.4f, 0.4f, 0.8f);
            EditorGUI.DrawRect(moreRect, moreColor);

            GUIStyle moreStyle = new(EditorStyles.label)
            {
                alignment = TextAnchor.MiddleCenter,
                fontSize = 10,
                fontStyle = FontStyle.Bold,
                normal = { textColor = Color.white }
            };

            // Create tooltip text
            string tooltipText;
            if (hiddenCounters.Count > 0)
            {
                var hiddenLabels = hiddenCounters.Select(c => $"• {c.Label} ({c.TooltipText})");
                tooltipText = $"Hidden counters:\n{string.Join("\n", hiddenLabels)}\n\nExpand window to see more counters";
            }
            else
            {
                tooltipText = "All counters are visible";
            }

            string displayText = hiddenCounters.Count > 0 ? $"+{hiddenCounters.Count}" : "•••";
            EditorGUI.LabelField(moreRect, new GUIContent(displayText, tooltipText), moreStyle);
        }

        /// <summary>
        /// Draws a loading spinner when counts are being computed.
        /// </summary>
        public static void DrawLoadingSpinner(float availableWidth, Rect selectionRect, ref float nextX)
        {
            float spinnerW = 20;
            float padX = 2;
            float totalSpinnerWidth = spinnerW + padX;

            // Only show spinner if we have enough space
            if (totalSpinnerWidth <= availableWidth)
            {
                nextX -= totalSpinnerWidth;
                Rect loadingRect = new(nextX, selectionRect.y, spinnerW, selectionRect.height);
                int tick = (int)(EditorApplication.timeSinceStartup * 8) % spinner.Length;
                string anim = spinner[tick];
                EditorGUI.DrawRect(loadingRect, new Color(0.5f, 0.5f, 0.5f, 0.12f));
                GUIStyle spinnerStyle = new(EditorStyles.label)
                {
                    alignment = TextAnchor.MiddleCenter,
                    fontSize = 16,
                    normal = { textColor = Color.gray }
                };
                EditorGUI.LabelField(loadingRect, anim, spinnerStyle);
            }
        }

        /// <summary>
        /// Creates the default count style for counter labels.
        /// </summary>
        public static GUIStyle CreateCountStyle()
        {
            return new GUIStyle(EditorStyles.label)
            {
                alignment = TextAnchor.MiddleCenter,
                fontSize = 11,
                fontStyle = FontStyle.Bold,
                normal = { textColor = Color.white }
            };
        }
    }
}
