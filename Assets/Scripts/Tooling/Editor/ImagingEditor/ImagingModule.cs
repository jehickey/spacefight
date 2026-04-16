using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;


[Serializable]
public class ImagingModule
{
    public int id;
    public string name;
    //public ParameterDefinition[] parameters;
    public List<ParameterDefinition> parameters = new List<ParameterDefinition>();
    public bool breakpoint;

    //ui info
    public bool fullDisplay;
    public bool highlight;
    public int lastIndex;
    public Rect rect;
    public bool isSelected;
    public bool floating;


    GUIStyle smallLabel;
    GUIStyle smallField;


    public ImagingModule()
    {
    }

    public ImagingModule(string _name)
    {
        name = _name;
        GenerateID();
    }

    public ImagingModule(ModuleDefinition module)
    {
        name = module.name;
        id = module.id;
        if (id==0) GenerateID();
        parameters.Clear();
        parameters.AddRange( module.parameters);


    }

    public ImagingModule(ImagingModule original)
    {
        name = original.name;
        rect = Rect.zero;
        isSelected = false;
        breakpoint = original.breakpoint;
        GenerateID();
        foreach (ParameterDefinition parameter in original.parameters)
        {
            ParameterDefinition p = new ParameterDefinition();
            p.name = parameter.name;
            p.type = parameter.type;
            p.defaultVal = parameter.defaultVal;
            p.value = parameter.value;
            parameters.Add(p);
        }
    }

    private void GenerateID()
    {
        id = UnityEngine.Random.Range(0, 9999);
    }


    public void AssignDefaultValues()
    {
        foreach (ParameterDefinition p in parameters)
        {
            p.value = p.defaultVal;
        }
    }

    public Rect Draw(int index, Vector2 pos)
    {
        InitStyles();
        float width = 190;
        //float height = 30;
        //Rect rect = Rect.zero;
        //ImagingModule mod = pipelineModules[i];
        if (index > -1 && !floating) lastIndex = index;

        GUILayout.BeginVertical(EditorStyles.helpBox, GUILayout.Width(width));
        //highlight = GUILayout.Toggle(highlight, "Highlight");
        GUIStyle titleStyle = new GUIStyle(EditorStyles.boldLabel);
        if (highlight)
        {
            titleStyle.normal.textColor = Color.red;
            titleStyle.hover.textColor = Color.red;
            titleStyle.fontSize = 14;
        }
        GUILayout.Label(name, titleStyle);

        //GUI.Box(rect, name, EditorStyles.helpBox);
        foreach (ParameterDefinition p in parameters)
        {
            GUILayout.BeginHorizontal();
            GUILayout.Label(p.name, smallLabel, GUILayout.Width(50));
            switch (p.type) {
                case "float":
                    p.value = EditorGUILayout.FloatField(p.value, smallField, GUILayout.Width(50));
                    break;
                case "int":
                    p.value = EditorGUILayout.IntField((int)p.value, smallField, GUILayout.Width(50));
                    break;
                default:
                    break;
            }
            GUILayout.EndHorizontal();
        }
        breakpoint = GUILayout.Toggle(breakpoint, "Breakpoint");
        GUILayout.EndVertical();
        if (Event.current.type == EventType.Repaint) rect = GUILayoutUtility.GetLastRect();
        //detecting mouseover in IMGUI is a complete nightmare. Forget that.
        return rect;
    }


    Texture2D MakeTex(int width, int height, Color col)
    {
        Color[] pix = new Color[width * height];
        for (int i = 0; i < pix.Length; i++) pix[i] = col;
        Texture2D result = new Texture2D(width, height);
        result.SetPixels(pix);
        result.Apply();
        return result;
    }


    public Rect DrawFloating(int index, Vector2 pos)
    {
        float width = 100f;

        // Reserve a rect with flexible height
        Rect container = new Rect(pos.x, pos.y, width, 0);

        GUILayout.BeginArea(container, EditorStyles.helpBox);

        GUILayout.Label(name, EditorStyles.boldLabel);

        foreach (ParameterDefinition p in parameters)
            p.value = EditorGUILayout.FloatField(p.name, p.value);

        GUILayout.EndArea();

        // After EndArea, Unity knows the height
        container.height = GUILayoutUtility.GetLastRect().height;

        return container;
    }


    void InitStyles()
    {
        if (smallLabel == null)
        {
            smallLabel = new GUIStyle(EditorStyles.label);
            smallLabel.fontSize = 8;
            smallLabel.alignment = TextAnchor.MiddleLeft;

            smallField = new GUIStyle(EditorStyles.numberField);
            smallField.fontSize = 8;
            smallField.alignment = TextAnchor.MiddleLeft;
            smallField.normal.background = EditorStyles.numberField.normal.background;
            smallField.focused.background = EditorStyles.numberField.focused.background;
            smallField.active.background = EditorStyles.numberField.active.background;
            smallField.border = new RectOffset(1, 1, 1, 1);
            smallField.padding = new RectOffset(1, 1, 0, 0);
            smallField.margin = new RectOffset(0, 0, 0, 0);
            smallField.stretchHeight = false;


        }
    }


}
