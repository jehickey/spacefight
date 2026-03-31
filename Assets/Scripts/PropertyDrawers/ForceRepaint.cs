#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

public static class ForceRepaint
{
    [MenuItem("Tools/Force Full Repaint")]
    static void RepaintAll()
    {
        // These still exist in Unity 6.x
        SceneView.RepaintAll();
        EditorApplication.RepaintHierarchyWindow();
        EditorApplication.RepaintProjectWindow();
        EditorApplication.RepaintAnimationWindow();

        // Generic fallback: repaint all open EditorWindows
        foreach (var window in Resources.FindObjectsOfTypeAll<EditorWindow>())
        {
            window.Repaint();
        }
    }
}
#endif