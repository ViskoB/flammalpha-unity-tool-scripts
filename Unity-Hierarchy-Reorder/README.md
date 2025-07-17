# Unity Hierarchy Reorder: Merge with Parent

A Unity Editor tool for merging one GameObject (and its components) into its parent, either within an active scene or directly in prefab assets—accessible via the GameObject context menu.

## Features

- **Merge Components:** Moves all non-Transform components from a selected GameObject to its parent, then deletes (merges) the child GameObject.
- **Scene & Prefab Asset Support:** Operates either on GameObjects in open scenes or directly on prefab files from the Project panel.
- **Undo Support:** In scenes, you can undo merged operations.
- **Context Menu Integration:** Accessible via right-click in the Hierarchy or Project window under:  
  `GameObject -> FlammAlpha -> Merge with Parent`

## How to Use

1. **In the Scene (Hierarchy Window):**
   - Right-click a GameObject that has a parent.
   - Select `FlammAlpha -> Merge with Parent`.
   - All its components (except Transform) are transferred to its parent, and the GameObject is removed.

2. **With Prefabs (Project Window):**
   - Select a prefab asset in the Project window.
   - Go to `GameObject -> FlammAlpha -> Merge with Parent` from the top menu bar.
   - The first child of the prefab root (if any) is merged up into the prefab root, and the child is deleted.
   - Changes are automatically applied to the prefab file.

## Limitations & Notes

- Only merges the **first child** of a prefab root (when used in Project window).
- Components referencing the merged GameObject may need manual adjustment.
- Cannot merge if no parent exists.
- Will skip merging the `Transform` component and can only merge onto existing parents.
- The tool does not handle merging materials, children, or references; it only merges components (other than Transform).
- For undoing prefab asset changes: use Unity's versioning or manually revert changes.
- Use with caution; always keep backups of important prefabs and scene data.
