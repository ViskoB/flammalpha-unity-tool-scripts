using System;
using UnityEngine;

namespace FlammAlpha.UnityTools.Hierarchy.Highlight
{
    /// <summary>
    /// Configuration entry for type-based hierarchy highlighting.
    /// Highlights GameObjects that contain specific component types.
    /// </summary>
    [Serializable]
    public class TypeConfigEntry
    {
        [Tooltip("Fully qualified type name of the component to highlight")]
        public string typeName;
        
        [Tooltip("Symbol to display next to the GameObject name")]
        public string symbol;
        
        [Tooltip("Background color for highlighted GameObjects")]
        public Color color;
        
        [Tooltip("If true, parent objects are also highlighted when children contain this component")]
        public bool propagateUpwards;
        
        [Tooltip("Whether this highlighting rule is active")]
        public bool enabled = true;
    }
}
