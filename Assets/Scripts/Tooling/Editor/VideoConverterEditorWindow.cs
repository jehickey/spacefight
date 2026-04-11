using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using System.Transactions;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

public class VideoConverterEditorWindow : EditorWindow
{
    /*
   	* set up output
        * what type of output(videoconf or normal)
		* checkbox for audio or no-audio
        * what folder they go into
	* drag a video file into the window(or multiple)
		* show list of files to be converted
        * display selected video(if possible)
	* show converted videos(if a list)
	* display results/errors from script
    */

    DefaultAsset outputFolder;
    string outputPath;
    List<string> inputFiles = new List<string>();
    Process scriptProcess;
    Vector2 inputScroll;
    string scriptLog = "";
    Vector2 logScroll;
    int lastLogLength = 0;

    [MenuItem("Tools/Video Converter")]
    public static void ShowWindow()
    {
        GetWindow<VideoConverterEditorWindow>("Video Converter");
    }

    private void OnGUI()
    {
        GUILayout.Label("Settings", EditorStyles.boldLabel);
        outputFolder = (DefaultAsset)EditorGUILayout.ObjectField("Output Folder", outputFolder, typeof(DefaultAsset), false);
        if (GUILayout.Button("Convert"))
        {
            if (outputFolder)
            {
                outputPath = AssetDatabase.GetAssetPath(outputFolder);
                EditorApplication.delayCall += RunScript;
            }
            else
            {
                UnityEngine.Debug.Log("No output folder provided");
            }
        }

        DrawInputSection();
        DrawOutputSection();
        GUILayout.Label("Processing Results", EditorStyles.boldLabel);


    }


    private void DrawInputSection()
    {
        GUILayout.Label("Input Files", EditorStyles.boldLabel);

        //drop zone
        Rect dropArea = GUILayoutUtility.GetRect(0, 60, GUILayout.ExpandWidth(true));
        GUI.Box(dropArea, "Drag video files here");
        HandleDragAndDrop(dropArea);

        //scrollable list
        inputScroll = EditorGUILayout.BeginScrollView(inputScroll, GUILayout.Height(120));
        foreach (string file in inputFiles)
        {
            GUILayout.Label(Path.GetFileName(file));
        }
        EditorGUILayout.EndScrollView();

    }


    private void DrawOutputSection()
    {
        //scroll to bottom whenever new text comes in
        if (scriptLog.Length != lastLogLength)
        {
            logScroll.y = float.MaxValue;
            lastLogLength = scriptLog.Length;
        }

        GUILayout.Label("Output", EditorStyles.boldLabel);
        logScroll = EditorGUILayout.BeginScrollView(logScroll, GUILayout.Height(120));
        GUILayout.TextArea(scriptLog, GUILayout.ExpandHeight(true));
        EditorGUILayout.EndScrollView ();
    }

    private async void RunScript()
    {
        scriptLog = "";
        //kill a running script if necessary
        if (scriptProcess != null && !scriptProcess.HasExited)
        {
            try
            {
                scriptProcess.Kill();
                scriptProcess.WaitForExit();
                scriptLog += "Killed existing process...\n";
            }
            catch (System.Exception e)
            {
                scriptLog += $"Error killing process: {e.Message}\n";
            }
        }

        //string inputPath = "";// Path.GetFullPath(AssetDatabase.GetAssetPath(inputTexture));
        string scriptPath = Path.Combine(Application.dataPath, "Scripts/Tooling/Python/video_converter.py");

        // Python executable (adjust if using venv)
        string pythonExe = "python";

        ProcessStartInfo psi = new ProcessStartInfo
        {
            FileName = pythonExe,
            Arguments = $"\"{scriptPath}\"",// --input \"{inputPath}\" --output \"{outputPath}\"",
            UseShellExecute = false,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            CreateNoWindow = true
        };

        scriptProcess = new Process();
        scriptProcess.StartInfo = psi;

        await Task.Run(() =>
        {
            bool started;
            try
            {
                started = scriptProcess.Start();
            }
            catch (System.Exception e)
            {
                scriptLog += $"Failed to start process: {e.Message}\n";
                return;
            }

            if (!started)
            {
                scriptLog += "Process did not start.\n";
                return;
            }

            scriptLog += $"Running (process {scriptProcess.Id})\n";
            try
            {
                scriptLog += scriptProcess.StandardOutput.ReadToEnd();
            }
            catch (System.Exception e)
            {
                scriptLog += $"Error reading stdout: {e.Message}\n";
            }

            //if (scriptLog != "") scriptLog += "\n";
            try
            {
                scriptLog += scriptProcess.StandardError.ReadToEnd();
            }
            catch (System.Exception e)
            {
                scriptLog += $"Error reading stderr: {e.Message}\n";
            }

            scriptProcess.WaitForExit();
        });
        Repaint();
    }


    private void HandleDragAndDrop(Rect dropArea)
    {
        Event evt = Event.current;

        //duck out if they aren't over the sensitive spot
        if (!dropArea.Contains(evt.mousePosition)) return;

        if (evt.type == EventType.DragUpdated)
        {
            DragAndDrop.visualMode = DragAndDropVisualMode.Copy;
            evt.Use();
        }
        if (evt.type == EventType.DragPerform)
        {
            DragAndDrop.AcceptDrag();
            foreach (string path in DragAndDrop.paths)
            {
                //if it exists and isn't already in the list, add it
                if (File.Exists(path) && !inputFiles.Contains(path)) inputFiles.Add(path);
                evt.Use();
            }
        }
    }

}
