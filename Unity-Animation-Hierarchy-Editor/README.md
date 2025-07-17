# Unity Animation Hierarchy Editor

A powerful Unity Editor window for inspecting and modifying the transform paths and animation bindings inside one or multiple Animation Clips.  
Developed to make refactoring animation hierarchies and retargeting much faster and less error-prone.

---

## Features

- **List All Animation Paths:**  
  View the transform (`path`) structure used in one or more selected animation clips.

- **Batch Path Replacement:**  
  Quickly replace old object/transform paths with new ones across all selected clips. Supports regular expressions for advanced replacements.

- **Preview and Edit Paths:**  
  Change transform paths directly in the editor window and instantly apply/reset changes.

- **Multiple Clip Support:**  
  Select multiple Animation Clips in the Project view and see each one’s animation hierarchy displayed and editable separately.

- **Direct Object Reference:**  
  Easily assign root Animator and connect paths to actual GameObjects in your scene for quick validation/visual feedback.

- **Undo/Redo Integration:**  
  All path modifications support Unity’s undo system.

---

## Usage

1. **Opening the Window:**
   - Menu: `Tools > FlammAlpha > Animation Hierarchy Editor`

2. **Selecting Clips:**
   - Select one or more Animation Clips in the Project window (Assets view).

3. **Setting Root Animator (Optional):**
   - Drag and drop a root Animator from your scene for object reference validation.

4. **Path Editing:**
   - See all binding paths for each selected clip.
   - Edit, reset, or apply changes to each path.
   - Use the "Replace Path" fields to swap paths globally (optionally as Regex).

---

## UI Overview

- **Root Animator:** Scene object used for validation of transform paths.
- **Animation Clips:** Every selected animation clip is displayed in its own section.
- **Path List:** For each clip, all animation curve bindings are listed and editable.
- **Batch Replace:** Replace paths across all clips, with optional Regex support.
- **Colored Object Field:** Shows green if the path resolves to a scene object (given Animator is set), red if not found.

---

## Limitations

- Designed for use in the Editor only (`#if UNITY_EDITOR`).
- Does not support editing non-transform properties directly.
- Paths must be compatible with your Animator transform hierarchy for object validation to work.
