using UnityEditor;
using System.IO;

public static class PythonScriptCreator
{
    [MenuItem("Assets/Create/Python Script")]
    public static void CreatePythonScript()
    {
        string path = AssetDatabase.GetAssetPath(Selection.activeObject);
        if (string.IsNullOrEmpty(path))
            path = "Assets";

        if (!Directory.Exists(path))
            path = Path.GetDirectoryName(path);

        string fullPath = AssetDatabase.GenerateUniqueAssetPath(Path.Combine(path, "NewPythonScript.py"));
        File.WriteAllText(fullPath, "#!/usr/bin/env python3\n\n");

        AssetDatabase.Refresh();
    }
}
