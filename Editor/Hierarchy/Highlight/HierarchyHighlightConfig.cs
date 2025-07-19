using UnityEngine;
using System;
using System.Collections.Generic;

namespace FlammAlpha.UnityTools.Hierarchy.Highlight
{
    /// <summary>
    /// Configuration asset for hierarchy highlighting settings.
    /// Contains type-based, name-based, and property-based highlighting rules.
    /// </summary>
    [Serializable]
    public class HierarchyHighlightConfig : ScriptableObject
    {
        [SerializeField]
        public List<TypeConfigEntry> typeConfigs = new List<TypeConfigEntry>();

        [SerializeField]
        public List<NameHighlightEntry> nameHighlightConfigs = new List<NameHighlightEntry>();

        [SerializeField]
        public List<PropertyHighlightEntry> propertyHighlightConfigs = new List<PropertyHighlightEntry>();
    }
}
