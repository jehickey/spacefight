using UnityEditor;
using UnityEngine;
using System.Diagnostics;
using System.IO;

public static class PythonContextMenu
{
    [MenuItem("Assets/Open Command Window Here", true)]
    private static bool ValidateOpenCmd()
    {
        return Selection.activeObject != null &&
               AssetDatabase.GetAssetPath(Selection.activeObject).EndsWith(".py");
    }

    [MenuItem("Assets/Open Command Window Here")]
    private static void OpenCmdHere()
    {
        string assetPath = AssetDatabase.GetAssetPath(Selection.activeObject);
        string fullPath = Path.GetFullPath(assetPath);
        string directory = Path.GetDirectoryName(fullPath);

#if UNITY_EDITOR_WIN
        Process.Start("cmd.exe", $"/K cd /d \"{directory}\"");
#else
        Process.Start("/bin/bash", $"-c \"cd '{directory}'; exec bash\"");
#endif
    }
}