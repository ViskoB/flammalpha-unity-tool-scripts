# Unity Hierarchy Color

**Unity Hierarchy Color** is an extension for the Unity Editor that allows you to colorize GameObjects in the Hierarchy window based on the components they contain or their names. This makes it easier to visually organize and identify objects at a glance, especially in larger projects.

## Features

- **Color Highlighting by Component Type**: Mark all objects with a specific component type by color and symbol in the Hierarchy.
- **Name-based Highlighting**: Specify prefixes and colors to highlight objects by their name.
- **Context Menu Integration**: Right-click any component to quickly add it to the highlight config.
- **Configurable Highlight Settings**: Open a dedicated editor window to manage all highlight rules.
- **Force Recache**: Recache highlight settings manually from the menu.

## Usage

### 1. Opening the Config Window
Go to `Tools > FlammAlpha > Hierarchy Color > Config` in the Unity Editor menu. This will open the Hierarchy Highlight Config editor window, where you can:
- Add or remove type-based highlight rules (components)
- Add or remove name-based highlight rules (prefixes)

### 2. Adding Component Highlight via Context Menu
Right-click any component in the Inspector and select `FlammAlpha/Add to Hierarchy Color Config` to quickly add or update a highlight rule for its type.

### 3. Force Recache
If you want to refresh the hierarchy highlights manually, go to `Tools > FlammAlpha > Hierarchy Color > Force Recache`.

## Configuration Data Structures
Highlight rules are saved as serializable ScriptableObjects:
- `TypeConfigEntry`: Associates a component type with a symbol, color, and propagation rule.
- `NameHighlightEntry`: Associates a name prefix with a color and propagation rule.
- `HierarchyHighlightConfig`: Holds lists of the above entries and can be edited in the Config window.

## Extending Functionality

- Add more color rules by editing the config window or via context menu.
- You can expand the system by editing the C# files in the `Editor` folder.
