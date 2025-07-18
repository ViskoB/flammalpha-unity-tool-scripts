using UnityEngine;
using System;
using System.Collections.Generic;

[Serializable]
public class HierarchyHighlightConfig : ScriptableObject
{
    public List<TypeConfigEntry> typeConfigs = new List<TypeConfigEntry>();
    public List<NameHighlightEntry> nameHighlightConfigs = new List<NameHighlightEntry>();
    public List<PropertyHighlightEntry> propertyHighlightConfigs = new List<PropertyHighlightEntry>();
}
