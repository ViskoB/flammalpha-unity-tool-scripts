using System;
using UnityEngine;

namespace FlammAlpha.UnityTools.Hierarchy.Highlight
{
    /// <summary>
    /// Configuration entry for name-based hierarchy highlighting.
    /// Highlights GameObjects whose names start with a specific prefix.
    /// </summary>
    [Serializable]
    public class NameHighlightEntry
    {
        [Tooltip("Name prefix to match against GameObject names")]
        public string prefix;

        [Tooltip("Background color for highlighted GameObjects")]
        public Color color;

        [Tooltip("If true, parent objects are also highlighted when children have matching names")]
        public bool propagateUpwards;

        [Tooltip("Whether this highlighting rule is active")]
        public bool enabled = true;
    }
}
