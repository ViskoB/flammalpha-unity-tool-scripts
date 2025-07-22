using System;
using System.Collections.Generic;
using UnityEngine;

namespace FlammAlpha.UnityTools.Data
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

    /// <summary>
    /// Configuration asset for hierarchy highlighting settings.
    /// Contains type-based, name-based, and property-based highlighting rules.
    /// </summary>
    [Serializable]
    public class HierarchyHighlightConfig : ScriptableObject
    {
        [SerializeField]
        [Tooltip("Version of the config structure - used for migration purposes")]
        public int configVersion = 1;
        [SerializeField]
        public List<TypeConfigEntry> typeConfigs = new List<TypeConfigEntry>();

        [SerializeField]
        public List<NameHighlightEntry> nameHighlightConfigs = new List<NameHighlightEntry>();

        [SerializeField]
        public List<PropertyHighlightEntry> propertyHighlightConfigs = new List<PropertyHighlightEntry>();
    }
}
