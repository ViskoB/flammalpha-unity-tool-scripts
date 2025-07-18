using System;
using UnityEngine;

[Serializable]
public class TypeConfigEntry
{
    public string typeName;
    public string symbol;
    public Color color;
    public bool propagateUpwards;
    public bool enabled = true; // <--- Added for enable/disable per entry
}