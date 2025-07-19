using System;
using UnityEngine;

namespace FlammAlpha.UnityTools.Hierarchy.Highlight
{
    /// <summary>
    /// Configuration entry for property-based hierarchy highlighting.
    /// Highlights GameObjects based on component property values.
    /// </summary>
    [Serializable]
    public class PropertyHighlightEntry
    {
        [Tooltip("Fully qualified type name of the component containing the property")]
        public string componentTypeName;
        
        [Tooltip("Name of the property to evaluate")]
        public string propertyName;
        
        [Tooltip("Symbol to display next to the GameObject name")]
        public string symbol;
        
        [Tooltip("Background color for highlighted GameObjects")]
        public Color color;
        
        [Tooltip("If true, parent objects are also highlighted when children match this property condition")]
        public bool propagateUpwards;
        
        [Tooltip("Whether this highlighting rule is active")]
        public bool enabled = true;
    }
}
