using System;
using UnityEngine;

[Serializable]
public class PropertyHighlightEntry
{
    public string componentTypeName;
    public string propertyName;
    public string symbol;
    public Color color;
    public bool propagateUpwards;
    public bool enabled = true; // <--- Added for enable/disable per entry
}