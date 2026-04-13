using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Net.Configuration;
using Unity.VisualScripting;
using UnityEditor;
using UnityEngine;

public class ImagingEditorWindow : EditorWindow
{
    Texture2D inputTexture;
    Texture2D outputTexture;
    DefaultAsset outputFolder;   // Unity's folder asset type
    int outputWidth = 4096;
    int outputHeight = 4096;
    string outputFilename = "upscaled.png";

    int maxResolution = 8192;
    bool resOverride = false;
    bool allowExecution = false;
    bool isNormalMap = false;

    //preview controls
    bool showProcessed = false;
    Vector2 scrollPos;
    float zoom = 1;
    float zoomMin = .1f;
    float zoomMax = 8;

    //pipeline list
    List<ImagingModule> pipelineModules = new List<ImagingModule>();
    Vector2 pipelineScroll;

    //available pipeline modules
    List<ImagingModule> availableModules = new List<ImagingModule>();



    [MenuItem("Tools/Map Imaging Tool")]
    public static void ShowWindow()
    {
        GetWindow<ImagingEditorWindow>("Map Imaging Tool");
    }

    void OnGUI()
    {
        GetAvailableModules();
        DrawSettings();
        GUILayout.Space(10);

        using (new EditorGUI.DisabledScope(!allowExecution))
        {
            if (GUILayout.Button("Run Python Upscaler"))
            {
                EditorApplication.delayCall += RunPythonScript;
            }
        }

        GUILayout.Space(10);
        EditorGUILayout.BeginHorizontal();
        GUILayout.Space(5);
        EditorGUILayout.BeginVertical();
        DrawPreview();
        EditorGUILayout.EndVertical();
        EditorGUILayout.BeginVertical(GUILayout.Width(200));
        DrawPipeline();
        EditorGUILayout.EndVertical();
        EditorGUILayout.BeginVertical(GUILayout.Width(100));
        DrawModuleList();
        EditorGUILayout.EndVertical();
        GUILayout.Space(5);
        EditorGUILayout.EndHorizontal();
    }

    void DrawSettings()
    {
        allowExecution = true;      //assume true until something says otherwise

        EditorGUILayout.BeginHorizontal();
        //Output Settings
        EditorGUILayout.BeginVertical();
        GUILayout.Label("Upscaler Settings", EditorStyles.boldLabel);
        outputFolder = (DefaultAsset)EditorGUILayout.ObjectField(
            "Output Folder",
            outputFolder,
            typeof(DefaultAsset),
            false
        );
        if (outputFolder == null) allowExecution = false;

        outputFilename = EditorGUILayout.TextField("Output Filename", outputFilename);
        if (outputFilename == null) allowExecution = false;

        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.BeginVertical();
        outputWidth = EditorGUILayout.IntField("Output Width", outputWidth);
        outputHeight = EditorGUILayout.IntField("Output Height", outputHeight);
        EditorGUILayout.EndVertical();
        EditorGUILayout.Space(5);
        EditorGUILayout.BeginVertical();
        resOverride = GUILayout.Toggle(resOverride, "Override Limits");
        EditorGUILayout.EndVertical();
        EditorGUILayout.EndHorizontal();

        isNormalMap = GUILayout.Toggle(isNormalMap, "Normal Map");

        if (outputWidth <= 0 || outputHeight <= 0) allowExecution = false;
        if ((outputWidth & (outputWidth - 1)) != 0)
        {
            EditorGUILayout.HelpBox($"Resolutions are not in powers of two!", MessageType.Warning);
        }
        if (!resOverride && (outputWidth > maxResolution || outputHeight > maxResolution))
        {
            EditorGUILayout.HelpBox($"Resolution exceeds maximum of {maxResolution}!", MessageType.Error);
            allowExecution = false;
        }
        EditorGUILayout.EndVertical ();

        EditorGUILayout.Space(10);

        //Input texture
        EditorGUILayout.BeginVertical(GUILayout.Width(50));
        GUILayout.Label("Input Texture", EditorStyles.boldLabel);
        inputTexture = (Texture2D)EditorGUILayout.ObjectField(
            GUIContent.none,
            inputTexture,
            typeof(Texture2D),
            false
        );
        if (inputTexture == null) allowExecution = false;
        EditorGUILayout.EndVertical();

        EditorGUILayout.EndHorizontal();
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

        if (showImage) GUILayout.Label($"Zoom: {zoom:0.00}x  ({showImage.width}x{showImage.height})");

        //preview click-and-drag panning
        if (e.type == EventType.MouseDrag && previewRect.Contains(e.mousePosition))
        {
            scrollPos -= e.delta;
            e.Use();
        }
    }


    void DrawPipeline()
    {
        GUILayout.Label("Pipeline", EditorStyles.boldLabel);
        Event e = Event.current;

        pipelineScroll = GUILayout.BeginScrollView(pipelineScroll, GUILayout.Height(300));

        for (int i=0; i<pipelineModules.Count; i++)
        {
            ImagingModule mod = pipelineModules[i];
            Rect rect = GUILayoutUtility.GetRect(100, 30, GUILayout.ExpandWidth(true));
            mod.rect = rect;
            GUI.Box(rect, mod.Name, EditorStyles.helpBox);
        }
        GUILayout.EndScrollView();
        HandlePipelineDrop();
    }

    void HandlePipelineDrop()
    {
        Event e = Event.current;
        Rect dropArea = GUILayoutUtility.GetRect(0, 40, GUILayout.ExpandWidth(true));
        GUI.Box(dropArea, "Drag modules here");

        if (!dropArea.Contains(e.mousePosition))
            return;

        if (e.type == EventType.DragUpdated)
        {
            //UnityEngine.Debug.Log("Drag updated over pipeline");
            if (DragAndDrop.GetGenericData("Module") is ImagingModule)
            {
                //UnityEngine.Debug.Log("Dragging module over pipeline");
                DragAndDrop.visualMode = DragAndDropVisualMode.Copy;
                e.Use();
            }
        }

        if (e.type == EventType.DragPerform)
        {
            ImagingModule incoming = DragAndDrop.GetGenericData("Module") as ImagingModule;
            if (incoming != null)
            {
                //UnityEngine.Debug.Log($"Adding module {incoming.Name} to pipeline");
                pipelineModules.Add(new ImagingModule(incoming));
                DragAndDrop.AcceptDrag();
                e.Use();
            }
        }
    }



    void DrawModuleList()
    {

        GUILayout.Label("Modules", EditorStyles.boldLabel);
        foreach (ImagingModule module in availableModules)
        {
            Rect rect = GUILayoutUtility.GetRect(100, 30, GUILayout.ExpandWidth(true));
            module.rect = rect;
            
            GUI.Box(rect, module.Name, EditorStyles.helpBox);

            Event e = Event.current;
            if (e.type == EventType.MouseDown && rect.Contains(e.mousePosition))
            {
                //UnityEngine.Debug.Log($"Starting drag for module {module.Name}");
                DragAndDrop.PrepareStartDrag();
                DragAndDrop.SetGenericData("Module", module);
                DragAndDrop.StartDrag(name);
                e.Use();
            }

        }

    }


    void GetAvailableModules()
    {
        if (availableModules.Count > 0) return;
        //availableModules.Clear();
        availableModules.Add(new ImagingModule("ModuleA"));
        availableModules.Add(new ImagingModule("ModuleB"));
        availableModules.Add(new ImagingModule("ModuleC"));
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
            string projectRelativePath = FileUtil.GetProjectRelativePath(outputPath);
            outputTexture = AssetDatabase.LoadAssetAtPath<Texture2D>(projectRelativePath);

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
                    importer.mipmapEnabled = true;
                    importer.mipmapFilter = TextureImporterMipFilter.KaiserFilter;
                    importer.mipMapBias = 0;
                    importer.filterMode = FilterMode.Trilinear;
                    importer.textureCompression = TextureImporterCompression.Uncompressed;
                    importer.wrapMode = TextureWrapMode.Clamp;
                    importer.npotScale = TextureImporterNPOTScale.None;
                    importer.alphaSource = TextureImporterAlphaSource.None;
                    importer.alphaIsTransparency = false;

                    if (!isNormalMap)
                    {
                        importer.textureType = TextureImporterType.Default;
                        importer.sRGBTexture = true;
                        importer.anisoLevel = 4;
                    }
                    if (isNormalMap)
                    {
                        importer.textureType = TextureImporterType.NormalMap;
                        importer.sRGBTexture = false;
                        importer.anisoLevel = 8;
                    }
                    
                    importer.SaveAndReimport();
                }
                showProcessed = true;
            }
        }
        UnityEngine.Debug.Log($"Generated: {outputPath}");
    }
}