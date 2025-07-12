using UnityEngine;
using System;
using System.Collections.Generic;

// namespace UnityHierarchyColor
// {
    [Serializable]
    public class HierarchyHighlightConfig : ScriptableObject
    {
        public List<TypeConfigEntry> typeConfigs = new List<TypeConfigEntry>();
        public List<NameHighlightEntry> nameHighlightConfigs = new List<NameHighlightEntry>();
    }
// }
