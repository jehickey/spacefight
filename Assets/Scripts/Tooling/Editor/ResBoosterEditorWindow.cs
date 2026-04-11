using UnityEngine;
using UnityEditor;
using System.Diagnostics;
using System.IO;

public class ResBoosterEditorWindow : EditorWindow
{
    Texture2D inputTexture;
    Texture2D outputTexture;
    DefaultAsset outputFolder;   // Unity's folder asset type
    int outputWidth = 4096;
    int outputHeight = 4096;
    string outputFilename = "upscaled.png";

    bool showProcessed = false;

    //preview zooming
    Vector2 scrollPos;
    float zoom = 1;
    float zoomMin = .1f;
    float zoomMax = 8;

    [MenuItem("Tools/ResBooster")]
    public static void ShowWindow()
    {
        GetWindow<ResBoosterEditorWindow>("Terrain Upscaler");
    }

    void OnGUI()
    {

        GUILayout.Label("Python Terrain Upscaler", EditorStyles.boldLabel);

        // Input texture
        inputTexture = (Texture2D)EditorGUILayout.ObjectField(
            "Input Texture",
            inputTexture,
            typeof(Texture2D),
            false
        );

        // Output folder (drag a folder from Project window)
        outputFolder = (DefaultAsset)EditorGUILayout.ObjectField(
            "Output Folder",
            outputFolder,
            typeof(DefaultAsset),
            false
        );

        outputFilename = EditorGUILayout.TextField("Output Filename", outputFilename);

        outputWidth = EditorGUILayout.IntField("Output Width", outputWidth);
        outputHeight = EditorGUILayout.IntField("Output Height", outputHeight);

        GUILayout.Space(10);

        if (GUILayout.Button("Run Python Upscaler"))
        {
            RunPythonScript();
        }

        GUILayout.Space(20);
        DrawPreview();

    }


    void DrawPreview()
    {
        Texture2D showImage = showProcessed ? outputTexture : inputTexture;

        GUILayout.Label("Preview", EditorStyles.boldLabel);

        if (inputTexture == null)
        {
            GUILayout.Label("No textures available");
            return;
        }


        float maxWidth = position.width - 20;
        //float aspect = (float)outputTexture.width / outputTexture.height;
        float width = inputTexture.width * zoom;
        float height = inputTexture.height * zoom;

        if (outputTexture != null)
        {
            width = outputTexture.width* zoom;
            height = outputTexture.height* zoom;
        }

        //preview vs original
        GUILayout.BeginHorizontal();
        showProcessed = GUILayout.Toggle(showProcessed, "Show Processed");
        GUILayout.EndHorizontal();

        //scale settings
        GUILayout.BeginHorizontal();
        if (GUILayout.Button("1:1"))
            zoom = 1f;
        if (GUILayout.Button("Fit"))
        {
            maxWidth = position.width - 40;
            zoom = maxWidth / outputTexture.width;
        }
        GUILayout.EndHorizontal();

        if (showProcessed && outputTexture == null)
        {
            GUILayout.Label("No preview available");
            return;
        }

        //preview image
        scrollPos = GUILayout.BeginScrollView(scrollPos, true, true);
        Rect previewRect = GUILayoutUtility.GetRect(width, height, GUILayout.ExpandWidth(false), GUILayout.ExpandHeight(false));
        GUI.DrawTexture(previewRect, showImage, ScaleMode.StretchToFill);
        Event e = Event.current;
        Rect texRect = new Rect(0, 0, width, height);

        //preview zooming
        if (e.type == EventType.ScrollWheel)
        {
            Vector2 pixelPos = e.mousePosition / zoom;
            Vector2 screenPos = (e.mousePosition - scrollPos);
            float zoomDelta = -e.delta.y * .05f;
            zoom = Mathf.Clamp(zoom + zoomDelta, zoomMin, zoomMax);
            scrollPos = pixelPos * zoom - (screenPos);
            e.Use();
        }
        GUILayout.EndScrollView();

        GUILayout.Label($"Zoom: {zoom:0.00}x  ({showImage.width}x{showImage.height})");

        //preview click-and-drag panning
        if (e.type == EventType.MouseDrag && previewRect.Contains(e.mousePosition))
        {
            scrollPos -= e.delta;
            e.Use();
        }
    }

    void RunPythonScript()
    {
        if (inputTexture == null)
        {
            UnityEngine.Debug.LogError("No input texture selected.");
            return;
        }

        if (outputFolder == null)
        {
            UnityEngine.Debug.LogError("No output folder selected.");
            return;
        }

        // Resolve paths
        string inputPath = Path.GetFullPath(AssetDatabase.GetAssetPath(inputTexture));

        string folderPath = Path.GetFullPath(AssetDatabase.GetAssetPath(outputFolder));
        string outputPath = Path.Combine(folderPath, outputFilename);
        outputPath = outputPath.Replace("\\", "/");

        // Path to your Python script
        string scriptPath = Path.Combine(Application.dataPath, "Scripts/Tooling/Python/ResBooster.py");

        // Python executable (adjust if using venv)
        string pythonExe = "python";

        ProcessStartInfo psi = new ProcessStartInfo
        {
            FileName = pythonExe,
            Arguments = $"\"{scriptPath}\" --input \"{inputPath}\" --output \"{outputPath}\" --width {outputWidth} --height {outputHeight}",
            UseShellExecute = false,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            CreateNoWindow = true
        };

        Process p = Process.Start(psi);
        string stdout = p.StandardOutput.ReadToEnd();
        string stderr = p.StandardError.ReadToEnd();
        p.WaitForExit();

        UnityEngine.Debug.Log(stdout);
        if (!string.IsNullOrEmpty(stderr))
            UnityEngine.Debug.LogError(stderr);

        if (p.ExitCode != 0)
        {
            UnityEngine.Debug.LogError($"Python script failed with exit code {p.ExitCode}");
            outputTexture = null;
            return;
        }

        AssetDatabase.Refresh();
        if (p.ExitCode == 0)
        {
            //string projectRelativePath = outputPath.Replace(Application.dataPath, "Assets");
            string projectRelativePath = FileUtil.GetProjectRelativePath(outputPath);
            outputTexture = AssetDatabase.LoadAssetAtPath<Texture2D>(projectRelativePath);
            //UnityEngine.Debug.Log($"relpath: {projectRelativePath}");
            //UnityEngine.Debug.Log("Application.dataPath = " + Application.dataPath);
            //UnityEngine.Debug.Log("outputPath = " + outputPath);
            //UnityEngine.Debug.Log("Project root = " + Directory.GetParent(Application.dataPath).FullName);

            if (outputTexture == null)
            {
                UnityEngine.Debug.LogWarning($"Generated file exists but Unity has not imported it yet. {projectRelativePath}");
            }
            else
            {
                TextureImporter importer = (TextureImporter)AssetImporter.GetAtPath(projectRelativePath);
                if (importer)
                {
                    importer.maxTextureSize = Mathf.Max(outputWidth, outputHeight);
                    importer.textureCompression = TextureImporterCompression.Uncompressed;
                    importer.SaveAndReimport();
                }
                showProcessed = true;
            }
        }
        UnityEngine.Debug.Log($"Generated: {outputPath}");
    }
}