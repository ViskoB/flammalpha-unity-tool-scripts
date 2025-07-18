using System;
using UnityEngine;

[Serializable]
public class NameHighlightEntry
{
    public string prefix;
    public Color color;
    public bool propagateUpwards;
    public bool enabled = true; // <--- Added for enable/disable per entry
}