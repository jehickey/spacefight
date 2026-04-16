using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using UnityEditor;
using UnityEngine;

public class ImagingEditorWindow : EditorWindow
{
    //main settings
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
    bool scriptRunning = false;

    List<ImagingModule> availableModules = new List<ImagingModule>();

    //console
    Vector2 consoleScroll;
    List<string> consoleLines = new List<string>();
    int lastConsoleLineCount = 0;


    ImagingUIPreview preview;
    ImagingUIPipeline pipeline;

    //editor audio effects
    public AudioClip clip;
    bool allowAudio = true;
    MethodInfo audioPlayMethod;


    [MenuItem("Tools/Map Imaging Tool")]
    public static void ShowWindow()
    {
        GetWindow<ImagingEditorWindow>("Map Imaging Tool");
    }

    private void OnEnable()
    {
        InitAudio();
        GetAvailableModules();
        preview = new ImagingUIPreview();
        pipeline = new ImagingUIPipeline(this);
    }

    void InitAudio()
    {
        var audio = typeof(AudioImporter).Assembly.GetType("UnityEditor.AudioUtil");
        audioPlayMethod = audio.GetMethod("PlayPreviewClip", BindingFlags.Static | BindingFlags.Public,
            null, new Type[] { typeof(AudioClip), typeof(int), typeof(bool) }, null);
    }

    void OnGUI()
    {
        //Upper Region
        DrawSettings();
        GUILayout.Space(10);

        //Execution Button
        using (new EditorGUI.DisabledScope(!allowExecution || scriptRunning))
        {
            if (GUILayout.Button("Run Python Upscaler"))
            {
                EditorApplication.delayCall += RunPipeline;
            }
        }

        GUILayout.Space(10);
        
        //Central Region
        EditorGUILayout.BeginHorizontal();
        GUILayout.Space(5);
        preview.inputTexture = inputTexture;
        preview.outputTexture = outputTexture;
        preview.Draw();
        GUILayout.Space(10);
        pipeline.Draw();
        GUILayout.Space(5);
        DrawModuleList();
        GUILayout.Space(5);
        EditorGUILayout.EndHorizontal();
        
        DrawConsole();
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

        EditorGUILayout.BeginHorizontal();
        isNormalMap = GUILayout.Toggle(isNormalMap, "Normal Map");
        allowAudio = GUILayout.Toggle(allowAudio, "Editor Sound FX");
        EditorGUILayout.EndHorizontal();

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





    void DrawModuleList()
    {
        EditorGUILayout.BeginVertical(GUILayout.Width(100));
        GUILayout.Label("Modules", EditorStyles.boldLabel);
        foreach (ImagingModule module in availableModules)
        {
            Rect rect = GUILayoutUtility.GetRect(100, 30, GUILayout.ExpandWidth(true));
            module.rect = rect;
            
            GUI.Box(rect, module.name, EditorStyles.helpBox);

            Event e = Event.current;
            if (e.type == EventType.MouseDown && rect.Contains(e.mousePosition))
            {
                ImagingModule mod = new ImagingModule(module);
                mod.AssignDefaultValues();
                //UnityEngine.Debug.Log($"Starting drag for module {module.Name}");
                DragAndDrop.PrepareStartDrag();
                DragAndDrop.SetGenericData("Module", mod);
                DragAndDrop.StartDrag(name);
                e.Use();
            }

        }
        EditorGUILayout.EndVertical();
    }




    void DrawConsole()
    {
        consoleScroll = GUILayout.BeginScrollView(consoleScroll, GUILayout.Height(200));
        string consoleText = string.Join("\n", consoleLines);
        EditorGUILayout.TextArea(consoleText, EditorStyles.textArea);
        GUILayout.EndScrollView();
        if (consoleLines.Count > lastConsoleLineCount)
        {
            consoleScroll.y = Mathf.Infinity;
        }
        lastConsoleLineCount = consoleLines.Count;
    }


    private void AddToConsole(string data)
    {
        consoleLines.Add(data);
        //UnityEngine.Debug.Log($"Console: {data}");
    }




    void GetAvailableModules()
    {
        if (availableModules.Count > 0) return;
        RunScript("--list-modules",
            onJsonLine: (json) =>
                {
                    string wrapped = "{\"modules\":" + json + "}";
                    ModuleList list = JsonUtility.FromJson<ModuleList>(wrapped);
                    EditorApplication.delayCall += () =>
                    {
                        availableModules.Clear();
                        foreach (ModuleDefinition mod in list.modules)
                        {
                            //UnityEngine.Debug.Log(mod.name);
                            availableModules.Add(new ImagingModule(mod));
                        }
                    };
                },
            onComplete: () =>
            {
                UnityEngine.Debug.Log("Module list loaded.");
            }
            );
    }



    void RunPipeline()
    {
        consoleLines.Clear();
        string inputPath = Path.GetFullPath(AssetDatabase.GetAssetPath(inputTexture));
        string folderPath = Path.GetFullPath(AssetDatabase.GetAssetPath(outputFolder));
        string outputPath = Path.Combine(folderPath, outputFilename);
        outputPath = outputPath.Replace("\\", "/");

        ModuleList pipelineOut = new ModuleList(pipeline.pipelineModules);
        string pipelineJson = JsonUtility.ToJson(pipelineOut);//.Trim('{', '}');
        string jsonPath = $"{folderPath}/pipeline.json";
        File.WriteAllText(jsonPath, pipelineJson);
        //pipelineJson.Replace("\"", "\\\"");


        RunScript($"--input-path \"{inputPath}\" --output-path \"{outputPath}\" --modules-file \"{jsonPath}\"",
            onJsonLine: (line) =>
            {
                EditorApplication.delayCall += () =>
                {
                    //UnityEngine.Debug.Log($"Received json line: {line}");
                    PipelineEvent evt = JsonUtility.FromJson<PipelineEvent>(line);
                    switch (evt.type)
                    {
                        case "pipeline_start":
                            pipeline.HighlightModule(0);
                            break;

                        case "pipeline_end":
                            pipeline.HighlightModule(0);
                            break;

                        case "module_start":
                            pipeline.HighlightModule(evt.moduleid);
                            break;

                        case "module_end":
                            pipeline.HighlightModule(0);
                            break;

                        case "module_error":
                            UnityEngine.Debug.Log($"Module Error: {evt.type} msg:{evt.message}");
                            break;
                        case "error":
                            UnityEngine.Debug.Log($"Error: {evt.type} msg:{evt.message}");
                            break;
                        default:
                            UnityEngine.Debug.Log($"Unknown Signal: {evt.type}");
                            break;
                    }
                };
            },
            onComplete: () =>
            {
                EditorApplication.delayCall += () =>
                {
                    ImportImage(outputPath);
                };
            });
        PlaySound(clip);
    }

    void ImportImage(string path) {

        AssetDatabase.Refresh();
        outputTexture = null;
        string projectRelativePath = FileUtil.GetProjectRelativePath(path);
        outputTexture = AssetDatabase.LoadAssetAtPath<Texture2D>(projectRelativePath);

        if (outputTexture == null)
        {
            //UnityEngine.Debug.LogWarning($"Generated file exists but Unity has not imported it yet. {projectRelativePath}");
        }
        else
        {
            //UnityEngine.Debug.Log("Doing Import");
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
            preview.showProcessed = true;
        }
    }


    Process process;
    //Action<string> onJsonLine;     // called for each JSON line
    //Action onComplete;             // called when process ends

    public void RunScript(string arguments, Action<string> onJsonLine, Action onComplete)
    {
        string scriptPath = Path.Combine(Application.dataPath, "Scripts/Tooling/Python/imager.py");
        string pythonExe = "python";

        //this.onJsonLine = onJsonLine;
        //this.onComplete = onComplete;

        var psi = new ProcessStartInfo()
        {
            FileName = pythonExe,
            Arguments = $"\"{scriptPath}\" {arguments}",
            UseShellExecute = false,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            CreateNoWindow = true
        };

        AddToConsole($"> {psi.FileName} {psi.Arguments}");
        process = new Process();
        process.StartInfo = psi;

        //process.OutputDataReceived += HandleOutput;
        process.OutputDataReceived += (s, e) =>
        {
            if (!string.IsNullOrEmpty(e.Data)) onJsonLine?.Invoke(e.Data);
            //onConsoleLine?.Invoke(e.Data); 
            AddToConsole(e.Data);
        };

        process.ErrorDataReceived += (s, e) => {
            if (!string.IsNullOrEmpty(e.Data)) UnityEngine.Debug.LogError(e.Data);
            AddToConsole(e.Data);
        };
        process.Exited += (s, e) =>
        {
            scriptRunning = false;
            onComplete?.Invoke();
        };
        process.EnableRaisingEvents = true;

        process.Start();
        scriptRunning = true;
        process.BeginOutputReadLine();
        process.BeginErrorReadLine();
    }


    public void Kill()
    {
        if (process != null && !process.HasExited)
            process.Kill();
    }





    public void PlaySound(AudioClip clip)
    {
        if (!allowAudio) return;
        if (clip == null) return;
        audioPlayMethod.Invoke(null, new object[] { clip, 0, false });
    }


}