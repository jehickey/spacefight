using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.TextCore.Text;
using UnityEngine;


[Serializable]
public class ParameterDefinition
{
    public string name;
    public string type;
    public float defaultVal;
    public float value;
}


[Serializable]
public class ImagingModule
{
    public string name;
    //public ParameterDefinition[] parameters;
    public List<ParameterDefinition> parameters = new List<ParameterDefinition>();
    

    public int id;

    //ui info
    public bool fullDisplay;
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

    public ImagingModule(ImagingModule original)
    {
        name = original.name;
        rect = Rect.zero;
        isSelected = false;
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
        float width = 100;
        float height = 30;
        //Rect rect = Rect.zero;
        //ImagingModule mod = pipelineModules[i];
        if (index > -1 && !floating) lastIndex = index;

        GUILayout.BeginVertical(EditorStyles.helpBox, GUILayout.Width(width));
        GUILayout.Label(name, EditorStyles.boldLabel);

        //GUI.Box(rect, name, EditorStyles.helpBox);
        foreach (ParameterDefinition p in parameters)
        {
            GUILayout.BeginHorizontal();
            GUILayout.Label(p.name, smallLabel, GUILayout.Width(50));
            p.value = EditorGUILayout.FloatField(p.value, smallField, GUILayout.Width(50));
            GUILayout.EndHorizontal();
        }
        GUILayout.EndVertical();

        if (pos == Vector2.zero)
        {
            //rect = GUILayoutUtility.GetRect(width, height, GUILayout.ExpandWidth(true));
        }
        else
        {
            //rect = new Rect(pos.x, pos.y, width, height);
        }


        Rect rect = GUILayoutUtility.GetLastRect();
        return rect;
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
