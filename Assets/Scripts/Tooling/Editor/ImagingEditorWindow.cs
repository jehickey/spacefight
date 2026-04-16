using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using UnityEditor;
using UnityEngine;

public class ImagingEditorWindow : EditorWindow
{
    public AudioClip clip;

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
    Rect previewScrollRect;

    //pipeline list
    List<ImagingModule> pipelineModules = new List<ImagingModule>();
    Vector2 pipelineScroll;
    ImagingModule movingModule;
    Rect pipelineRect;
    int currentBreakpoint = 0;


    //available pipeline modules
    List<ImagingModule> availableModules = new List<ImagingModule>();
    //List<ModuleDefinition> availableModules = new List<ModuleDefinition> ();

    //editor audio effects
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
    }

    void InitAudio()
    {
        var audio = typeof(AudioImporter).Assembly.GetType("UnityEditor.AudioUtil");
        audioPlayMethod = audio.GetMethod("PlayPreviewClip", BindingFlags.Static | BindingFlags.Public,
            null, new Type[] { typeof(AudioClip), typeof(int), typeof(bool) }, null);
    }

    void OnGUI()
    {
        Event e = Event.current;
        if (e.type == EventType.DragPerform)
        {
            //UnityEngine.Debug.Log($"GLOBAL DragPerform at mouse={e.mousePosition}");
        }

        if (e.type == EventType.DragExited)
        //if (DragAndDrop.GetGenericData("ModuleMove") == null)
        {
            //UnityEngine.Debug.Log($"Global DragExit at mouse={e.mousePosition}");
            if (movingModule != null)
            {
                //UnityEngine.Debug.Log("Drag terminated");
                movingModule.floating = false;
                int index = movingModule.lastIndex;
                RemoveModuleById(movingModule.id);
                if (index < pipelineModules.Count)
                {
                    //UnityEngine.Debug.Log($"Re-insert at {index} ({pipelineModules.Count})");
                    pipelineModules.Insert(index, movingModule);
                }
                else
                {
                    pipelineModules.Add(movingModule);
                }
                movingModule = null;
                e.Use();
            }
        }

        DrawSettings();
        GUILayout.Space(10);

        using (new EditorGUI.DisabledScope(!allowExecution))
        {
            if (GUILayout.Button("Run Python Upscaler"))
            {
                EditorApplication.delayCall += RunPipeline;
            }
        }

        GUILayout.Space(10);
        EditorGUILayout.BeginHorizontal();
        GUILayout.Space(5);
        EditorGUILayout.BeginVertical();
        DrawPreview();
        EditorGUILayout.EndVertical();
        GUILayout.Space(10);
        EditorGUILayout.BeginVertical(GUILayout.Width(200));
        DrawPipeline();
        EditorGUILayout.EndVertical();
        GUILayout.Space(5);
        EditorGUILayout.BeginVertical(GUILayout.Width(100));
        DrawModuleList();
        EditorGUILayout.EndVertical();
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

        //preview zooming
        Event e = Event.current;
        if (e.type == EventType.ScrollWheel && previewScrollRect.Contains(e.mousePosition))
        {
            Vector2 localMouse = e.mousePosition - previewScrollRect.position;
            //localmouse seems to be stable at all pab and zoom
            Vector2 pixelPos = (localMouse + scrollPos) / zoom;
            Vector2 screenPos = (localMouse - scrollPos);
            UnityEngine.Debug.Log($"{localMouse} / {pixelPos} / {screenPos}");
            float zoomDelta = -e.delta.y * .05f;
            zoom = Mathf.Clamp(zoom + zoomDelta, zoomMin, zoomMax);
            scrollPos = pixelPos * zoom - (screenPos);
            e.Use();
        }

        //preview click-and-drag panning
        if (e.type == EventType.MouseDrag && previewScrollRect.Contains(e.mousePosition))
        {
            scrollPos -= e.delta;
            e.Use();
        }


        //preview image
        scrollPos = GUILayout.BeginScrollView(scrollPos, true, true);
        Rect previewRect = GUILayoutUtility.GetRect(width, height, GUILayout.ExpandWidth(false), GUILayout.ExpandHeight(false));
        GUI.DrawTexture(previewRect, showImage, ScaleMode.StretchToFill);
        Rect texRect = new Rect(0, 0, width, height);
        GUILayout.EndScrollView();

        if (e.type == EventType.Repaint)  previewScrollRect = GUILayoutUtility.GetLastRect();

        if (showImage) GUILayout.Label($"Zoom: {zoom:0.00}x  ({showImage.width}x{showImage.height})");
    }


    void DrawPipeline()
    {
        //UnityEngine.Debug.Log($"Drawing pipeline");
        EditorGUILayout.BeginHorizontal();
        GUILayout.Label("Pipeline", EditorStyles.boldLabel);
        if (GUILayout.Button("Clear"))
        {
            pipelineModules.Clear();
        }
        EditorGUILayout.EndHorizontal();
        pipelineScroll = GUILayout.BeginScrollView(pipelineScroll, GUILayout.Height(300));
        Event e = Event.current;

        for (int i=0; i<pipelineModules.Count; i++)
        {
            //UnityEngine.Debug.Log($"Drawing module {pipelineModules[i].name} id {pipelineModules[i].id} at index {i}");
            GUILayout.Space(5);
            //Rect rect = DrawImagingModule(pipelineModules[i], i, Vector2.zero);
            Rect rect = pipelineModules[i].Draw (i, Vector2.zero);

            if (e.type == EventType.MouseDrag && rect.Contains(e.mousePosition))
            {
                //UnityEngine.Debug.Log($"Starting drag for pipeline module {mod.Name}");
                DragAndDrop.PrepareStartDrag();
                DragAndDrop.SetGenericData("ModuleMove", pipelineModules[i]);
                movingModule = pipelineModules[i];
                //movingModule.lastIndex = i;
                movingModule.floating = true;
                DragAndDrop.StartDrag("PipelineReorder");
                //pipelineModules.RemoveAt(i);
                e.Use();
            }

        }

        //ImagingModule moving = DragAndDrop.GetGenericData("ModuleMove") as ImagingModule;
        if (movingModule != null)
        {
            //UnityEngine.Debug.Log($"Dragging module {DragAndDrop.GetGenericData("ModuleMove")} from pipeline");
            //DrawImagingModule(movingModule, -1, e.mousePosition);
        }
        TrackBreakpoint();
        GUILayout.EndScrollView();
        if (e.type == EventType.Repaint)            pipelineRect = GUILayoutUtility.GetLastRect();
        HandlePipelineDrop();
    }


    void HandlePipelineDrop()
    {
        Event e = Event.current;
        //Rect dropArea = GUILayoutUtility.GetRect(0, 40, GUILayout.ExpandWidth(true));
        Rect dropArea = pipelineRect; //GUILayoutUtility.GetLastRect();
        //GUI.Box(dropArea, "Drag modules here");


        if (!dropArea.Contains(e.mousePosition))
            return;


        //if (e!=null && e.type != EventType.Repaint && e.type != EventType.Layout)
        //UnityEngine.Debug.Log($"event {e}");

        if (e.type == EventType.DragUpdated)
        {
            //UnityEngine.Debug.Log("Drag updated over pipeline");
            if (DragAndDrop.GetGenericData("Module") is ImagingModule)
            {
                //UnityEngine.Debug.Log("Dragging new module over pipeline");
                DragAndDrop.visualMode = DragAndDropVisualMode.Copy;
                e.Use();
            }
            if (DragAndDrop.GetGenericData("ModuleMove") != null)
            {
                //UnityEngine.Debug.Log("Dragging existing module over pipeline");
                DragAndDrop.visualMode = DragAndDropVisualMode.Move;
                e.Use();
                //see what slot it belongs in
                int toIndex = GetPipelineInsertIndex(e.mousePosition.y - dropArea.y);
                //UnityEngine.Debug.Log($"Moving module from {movingModule.lastIndex} to {toIndex}");
                //if it's already in the right slot, do nothing
                if (movingModule.lastIndex == toIndex) return;
                if (toIndex < pipelineModules.Count)
                {
                    RemoveModuleById(movingModule.id);
                    pipelineModules.Insert(toIndex, movingModule);
                    PlaySound(clip);
                }

            }

        }

        if (e.type == EventType.DragPerform)
        {
            //UnityEngine.Debug.Log("DROP!  Drag performed on pipeline");
            ImagingModule incoming = DragAndDrop.GetGenericData("Module") as ImagingModule;
            if (incoming != null)
            {
                //UnityEngine.Debug.Log($"Adding module {incoming.Name} to pipeline");
                //identify where this is in the list based on mouse position
                int index = GetPipelineInsertIndex(e.mousePosition.y - dropArea.y);
                pipelineModules.Insert(index, incoming);
                //pipelineModules.Add(new ImagingModule(incoming));
                DragAndDrop.AcceptDrag();
                e.Use();
                PlaySound(clip);
                return;
            }
            ImagingModule move = DragAndDrop.GetGenericData("ModuleMove") as ImagingModule;
            if (move != null && movingModule != null)
            {
                //UnityEngine.Debug.Log($"Dropping module {move.Name}");
                //int toIndex = GetPipelineInsertIndex(e.mousePosition.y - dropArea.y);
                //pipelineModules.Insert(toIndex, movingModule);
                movingModule.floating = false;
                movingModule = null;
                DragAndDrop.AcceptDrag();
                e.Use();
                PlaySound(clip);
                return;
            }
        }
    }

    int GetPipelineInsertIndex(float insertY)
    {
        //find the index just below the insertion point
        //assumes they are listed in order, vertically
        foreach (ImagingModule mod in pipelineModules)
        {
            if (insertY < mod.rect.center.y)
            {
                return pipelineModules.IndexOf(mod);
            }
        }
        return pipelineModules.Count;
    }


    void TrackBreakpoint()
    {
        foreach (ImagingModule mod in pipelineModules)
        {
            if (mod.breakpoint)
            {
                if (currentBreakpoint > 0 && currentBreakpoint != mod.id)
                {
                    ImagingModule currentBreak = FindModuleByID(currentBreakpoint);
                    if (currentBreak != null) currentBreak.breakpoint = false;
                    currentBreakpoint = mod.id;
                }
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

    }



    Vector2 consoleScroll;
    List<string> consoleLines = new List<string>();

    void DrawConsole()
    {
        consoleScroll = GUILayout.BeginScrollView(consoleScroll, GUILayout.Height(200));
        string consoleText = string.Join("\n", consoleLines);
        EditorGUILayout.TextArea(consoleText, EditorStyles.textArea);
        GUILayout.EndScrollView();
    }


    private void AddToConsole(string data)
    {
        consoleLines.Add(data);
        //UnityEngine.Debug.Log($"Console: {data}");
    }


    ImagingModule FindModuleByID(int id)
    {
        if (id==0) return null;
        return pipelineModules.Find(m => m.id == id);
    }
     
    void RemoveModuleById(int id)
    {
        ImagingModule mod = FindModuleByID(id);
        if (mod != null) pipelineModules.Remove(mod);
    }

    void HighlightModule (int id)
    {
        UnityEngine.Debug.Log($"Highlighting module id {id}");
        foreach (ImagingModule mods in pipelineModules)
        {
             mods.highlight = false;
        }
        ImagingModule mod = FindModuleByID(id);
        if (mod != null)
        {
            mod.highlight = true;
                UnityEngine.Debug.Log($"Module {mod.name} #{mod.id} is now highlighted");
        }
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

        ModuleList pipelineOut = new ModuleList(pipelineModules);
        string pipelineJson = JsonUtility.ToJson(pipelineOut);//.Trim('{', '}');
        string jsonPath = $"{folderPath}/pipeline.json";
        File.WriteAllText(jsonPath, pipelineJson);
        //pipelineJson.Replace("\"", "\\\"");

        UnityEngine.Debug.Log($"JSON: {pipelineJson}");

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
                            HighlightModule(0);
                            break;

                        case "pipeline_end":
                            HighlightModule(0);
                            break;

                        case "module_start":
                            HighlightModule(evt.moduleid);
                            break;

                        case "module_end":
                            HighlightModule(0);
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
                    UnityEngine.Debug.Log($"Generated: {outputPath}");
                };
            });


        //int result = RunPythonScript($"--input \"{inputPath}\" --output \"{outputPath}\" --width {outputWidth} --height {outputHeight}");
        PlaySound(clip);
        //if (result != 0) return;
    }

    void ImportImage(string path) {

        AssetDatabase.Refresh();
        outputTexture = null;
        string projectRelativePath = FileUtil.GetProjectRelativePath(path);
        outputTexture = AssetDatabase.LoadAssetAtPath<Texture2D>(projectRelativePath);

        if (outputTexture == null)
        {
            UnityEngine.Debug.LogWarning($"Generated file exists but Unity has not imported it yet. {projectRelativePath}");
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
            showProcessed = true;
        }
    }


    Process process;
    Action<string> onJsonLine;     // called for each JSON line
    Action onComplete;             // called when process ends

    public void RunScript(string arguments, Action<string> onJsonLine, Action onComplete)
    {
        string scriptPath = Path.Combine(Application.dataPath, "Scripts/Tooling/Python/imager.py");
        string pythonExe = "python";

        this.onJsonLine = onJsonLine;
        this.onComplete = onComplete;

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
        process.Exited += (s, e) => onComplete?.Invoke();
        process.EnableRaisingEvents = true;

        process.Start();
        process.BeginOutputReadLine();
        process.BeginErrorReadLine();
    }


    void HandleOutput(object sender, DataReceivedEventArgs e)
    {
        if (!string.IsNullOrEmpty(e.Data))
            onJsonLine?.Invoke(e.Data);
    }

    public void Kill()
    {
        if (process != null && !process.HasExited)
            process.Kill();
    }





    void PlaySound(AudioClip clip)
    {
        if (!allowAudio) return;
        if (clip == null) return;
        audioPlayMethod.Invoke(null, new object[] { clip, 0, false });
    }


}