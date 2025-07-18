# Unity Tool Scripts

Useful scripts I use to enhance my Unity experience.

## Tools

### Animation Rename (`Unity-Animation-Rename`)
- Replace `_` with `/` for submenu ordering in the Animation-View.
- Replace individual animation.
- Replace all animations in project.

### Animation Hierarchy Editor (`Unity-Animation-Hierarchy-Editor`)
- View and edit the animation path (hierarchy) of one or multiple Animation Clips.
- Batch replace old transform paths.
- Supports Regex replacement and multi-clip editing.
- Visual connection to scene Animator roots and undo/redo support.
- Menu: `Tools > FlammAlpha > Animation Hierarchy Editor`

### Hierarchy Color (`Unity-Hierarchy-Color`)
- Color different GameObjects in the Hierarchy for easier navigation.
- Highlight by name, attached scripts, or components.
- Configurable highlight settings and context menu integration.

### Component List (`Unity-Component-List`)
- List all components attached to GameObjects in your scenes or prefabs.
- Useful for quick auditing of component usage.

### Hierarchy Reorder (`Unity-Hierarchy-Reorder`)
- Merge GameObject components into their parent from context menu.
- Works in scenes and with prefab assets.
- Undos supported for scene merges.

### Material Instance Fix (`Unity-Material-Instance-Fix`)
- Scan for and revert unwanted material instances on selected GameObjects (e.g., imported FBX meshes) by matching them to project asset materials.
- Create unique (instanced) material copies from asset materials for selected objects.
- Batch operation UI with expand/collapse/select, undo/redo, and color-coded issue display.
- Menu: `Tools > FlammAlpha > Revert Material Instances`

---

Each folder above may contain a more detailed `README.md` that describes its specific features, limitations, and usage in depth. For details and advanced usage, check those files!

