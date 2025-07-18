# Material Instance Fix Tool

A Unity Editor utility for managing material assignments and instance issues in GameObjects and prefabs, especially when working with imported models (e.g., FBX files) that generate unwanted material instances.

## Features

- **Revert Material Instances:**
  - Scans a selected hierarchy for Renderer components using instanced (scene-only) materials instead of asset materials.
  - Locates the best-matching material asset in the project.
  - Allows you to bulk-replace material instances with matching project assets (when possible).

- **Create Material Instances:**
  - Scans the hierarchy for Renderer components using project asset materials.
  - Lets you create unique, instanced copies for selected materials (prefixed with "(Instance)").
  - Use this if you want scene-unique materials derived from a shared asset.

- **Batch Processing & UI:**
  - Expand, collapse, select/deselect all results for convenience.
  - Color-coded issue boxes make missing replacements obvious (red background).
  - Displays detailed info per renderer, material slot, and candidate asset.
  - Undo/redo support integrated with Unity's Undo system.

## Usage

1. Open the tool via menu: `Tools > FlammAlpha > Revert > Create Material Instances`
2. Choose the mode:
   - **RevertInstances:** Replaces instanced materials with project assets if a good match is found.
   - **CreateInstances:** Makes instances from selected asset materials in the chosen GameObject/root.
3. Assign the root GameObject (e.g., imported FBX, prefab root, or top-level scene object).
4. Press the scan button. Inspect the list of issues found.
5. (Optional) Expand/collapse/select/deselect issues as needed.
6. Press the action button to apply fixes.

## Implementation Notes

- Attempts to automatically match materials by name, shader, and main texture where possible.
- Materials without a suitable match will be highlighted in red and skipped.
- All changes are undoable and flagged as dirty for safe editing.

## When to Use

- After importing FBX meshes that reference lost or duplicated materials, to remap them back to asset materials.
- To create unique per-object variations of a common project material.

---

See the code in `MaterialInstanceFix.cs` for further customization details.
