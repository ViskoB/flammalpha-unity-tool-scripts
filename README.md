# FlammAlpha Unity Tools

A comprehensive collection of Unity Editor tools designed to streamline game development workflows, with specialized utilities for VRChat avatar creation and general Unity development.

## 🚀 Features

### 🎨 Hierarchy Management

- **Hierarchy Highlighting**: Advanced color coding system for GameObjects based on components, names, or custom properties
- **Hierarchy Reorder**: Tools for organizing and restructuring hierarchy

### 📦 Component & Object Tools

- **Component Lister**: Debug utility to list all components on selected GameObjects
- **Bounding Box Finder**: Visual editor for SkinnedMeshRenderer bounds with real-time scene view gizmos

### 🎭 Animation Utilities

- **Animation Rename**: Batch rename animation clips with slash notation conversion
- **Animation Hierarchy Editor**: Advanced animation workflow tools

### 🎨 Material Management

- **Material Instance Fix**: Resolve material instance issues by reverting to assets or creating new instances

### 🔧 Mesh Tools

- **Update Skinned Mesh**: Tools for updating and managing skinned mesh renderers

## 📋 Installation

### Unity Package Manager (Recommended)

1. Open Unity Package Manager
2. Click "+" and select "Add package from git URL"
3. Enter: `https://github.com/flammalpha/unity-tool-scripts.git`

### Manual Installation

1. Download the latest release from the [GitHub repository](https://github.com/flammalpha/unity-tool-scripts)
2. Extract and place the `Assets/Tools` folder in your Unity project

## 🛠 Usage

### Accessing Tools

All tools are organized under the Unity menu:

```text
Tools/FlammAlpha/
├── Hierarchy/
│   ├── Label Quick Selector
│   └── Hierarchy Coloring Config
├── Bounding Box Finder
├── Material Instance Fix
├── Animation Rename/
└── Utilities/
    └── Force Refresh All
```

### Quick Start Examples

#### Hierarchy Highlighting

1. Go to `Tools/FlammAlpha/Hierarchy Coloring Config`
2. Add component types or name patterns to highlight
3. Choose colors and enable highlighting
4. Objects matching your criteria will be colored in the hierarchy

#### Bounding Box Finder

1. Select a GameObject with SkinnedMeshRenderers
2. Open `Tools/FlammAlpha/Bounding Box Finder`
3. Assign your target root object
4. Visualize and adjust bounds in real-time

#### Material Instance Fix

1. Open `Tools/FlammAlpha/Material Instance Fix`
2. Choose mode (Revert Instances or Create Instances)
3. Assign target root object
4. Scan for issues and apply fixes

## 📚 Tool Documentation

### Hierarchy Tools

- **HierarchyHighlighting**: Configurable color coding system with async processing
- **HierarchyReorder**: Organize hierarchy structure

### Component Tools

- **ComponentLister**: Right-click any GameObject → FlammAlpha → List Components
- **BoundingBoxFinder**: Visual bounds editor with scene gizmos

### Animation Tools

- **AnimationRename**: Convert between underscore and slash naming conventions
- **AnimationHierarchyEditor**: Advanced animation management

### Material Tools

- **MaterialInstanceFix**: Batch material instance management

### Mesh Tools

- **UpdateSkinnedMesh**: SkinnedMeshRenderer utilities

## 🔧 Configuration

Tools can be configured through:

- Unity Preferences (some tools)
- ScriptableObject configs (Hierarchy Highlighting)
- Tool-specific windows and inspectors

## 🐛 Issues & Support

- **Bug Reports**: [GitHub Issues](https://github.com/flammalpha/unity-tool-scripts/issues)
- **Feature Requests**: [GitHub Issues](https://github.com/flammalpha/unity-tool-scripts/issues)
- **Documentation**: [GitHub Repository](https://github.com/flammalpha/unity-tool-scripts)

## 📈 Version History

### v1.0.0

- Initial release
- Hierarchy highlighting system
- Bounding box finder
- Component management tools
- Material instance management
- Animation utilities

---

**Note**: This tool collection is designed primarily for Unity Editor workflows. Runtime functionality is limited to editor-time operations.
