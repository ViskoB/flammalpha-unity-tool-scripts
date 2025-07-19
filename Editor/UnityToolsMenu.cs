using UnityEditor;
using UnityEngine;

namespace FlammAlpha.UnityTools
{
    /// <summary>
    /// Main menu provider for FlammAlpha Unity Tools.
    /// Organizes all tools under a unified menu structure.
    /// </summary>
    public static class UnityToolsMenu
    {
        private const string MENU_ROOT = "Tools/FlammAlpha/";
        
        [MenuItem(MENU_ROOT + "Utilities/Force Refresh All", false, 900)]
        private static void ForceRefreshAll()
        {
            // Force refresh hierarchy coloring
            Hierarchy.Highlight.HierarchyHighlighting.ForceRecache();
            
            // Refresh asset database
            AssetDatabase.Refresh();
            
            // Repaint all windows
            EditorApplication.RepaintHierarchyWindow();
            EditorApplication.RepaintProjectWindow();
            
            Debug.Log("FlammAlpha Unity Tools: Forced refresh of all systems completed.");
        }

        [MenuItem(MENU_ROOT + "Documentation/Open Documentation", false, 1001)]
        private static void OpenDocumentation()
        {
            Application.OpenURL("https://github.com/flammalpha/unity-tool-scripts");
        }

        [MenuItem(MENU_ROOT + "Documentation/Report Issue", false, 1002)]
        private static void ReportIssue()
        {
            Application.OpenURL("https://github.com/flammalpha/unity-tool-scripts/issues");
        }

        [MenuItem(MENU_ROOT + "About", false, 1003)]
        private static void ShowAbout()
        {
            EditorUtility.DisplayDialog(
                "FlammAlpha Unity Tools",
                "A comprehensive collection of Unity Editor tools.\n\n" +
                "Version: 1.0.0\n" +
                "Author: FlammAlpha\n" +
                "Repository: github.com/flammalpha/unity-tool-scripts\n\n" +
                "Features:\n" +
                "• Hierarchy Management\n" +
                "• Mesh and Component Tools\n" +
                "• Animation Utilities\n" +
                "• VRChat Specific Tools\n" +
                "• Material Management",
                "OK"
            );
        }
    }
}