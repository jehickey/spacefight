using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEditor;
using UnityEngine;

public class ImagingUIPipeline 
{
    ImagingEditorWindow window;

    //pipeline list
    public List<ImagingModule> pipelineModules = new List<ImagingModule>();
    Vector2 pipelineScroll;
    ImagingModule movingModule;
    Rect pipelineRect;
    int currentBreakpoint = 0;



    public ImagingUIPipeline(ImagingEditorWindow window)
    {
        this.window = window;
    }


    public void Draw()
    {
        EditorGUILayout.BeginVertical(GUILayout.Width(200));

        //topbar
        EditorGUILayout.BeginHorizontal();
        GUILayout.Label("Pipeline", EditorStyles.boldLabel);
        if (GUILayout.Button("Clear"))
        {
            pipelineModules.Clear();
        }
        EditorGUILayout.EndHorizontal();

        pipelineScroll = GUILayout.BeginScrollView(pipelineScroll);//, GUILayout.Height(300));
        Event e = Event.current;
        for (int i = 0; i < pipelineModules.Count; i++)
        {
            GUILayout.Space(5);
            Rect rect = pipelineModules[i].Draw(i, Vector2.zero);

            if (e.type == EventType.MouseDrag && rect.Contains(e.mousePosition))
            {
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
        if (e.type == EventType.Repaint) pipelineRect = GUILayoutUtility.GetLastRect();
        EditorGUILayout.EndVertical();


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
                    window.PlaySound(window.clip);
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
                window.PlaySound(window.clip);
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
                window.PlaySound(window.clip);
                return;
            }
        }
        if (e.type == EventType.DragExited)
        {
            if (movingModule != null)
            {
                //UnityEngine.Debug.Log("Drag terminated");
                movingModule.floating = false;
                int index = movingModule.lastIndex;
                RemoveModuleById(movingModule.id);
                if (index < pipelineModules.Count)
                {
                    //UnityEngine.Debug.Log($"Re-insert at {index} ({pipelineModules.Count})");
                    //pipelineModules.Insert(index, movingModule);
                }
                else
                {
                    //pipelineModules.Add(movingModule);
                }
                movingModule = null;
                e.Use();
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
                //this breakpoint isn't the tracked one
                if (mod.id > 0 && currentBreakpoint != mod.id)
                {
                    //disable old breakpoint
                    ImagingModule currentBreak = FindModuleByID(currentBreakpoint);
                    if (currentBreak != null) currentBreak.breakpoint = false;
                    //set new breakpoint
                    currentBreakpoint = mod.id;
                }
            }
            else
            {
                //last tracked breakpoint is no longer a breakpoint
                if (currentBreakpoint == mod.id) currentBreakpoint = 0;
            }
        }
    }

    ImagingModule FindModuleByID(int id)
    {
        if (id == 0) return null;
        return pipelineModules.Find(m => m.id == id);
    }

    void RemoveModuleById(int id)
    {
        ImagingModule mod = FindModuleByID(id);
        if (mod != null) pipelineModules.Remove(mod);
    }

    public void HighlightModule(int id)
    {
        foreach (ImagingModule mods in pipelineModules)
        {
            mods.highlight = false;
        }
        ImagingModule mod = FindModuleByID(id);
        if (mod != null)
        {
            mod.highlight = true;
        }
    }


}
