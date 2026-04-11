using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

public class ReadOnlyAttribute : PropertyAttribute
{
    public bool value;
    public ReadOnlyAttribute(bool value = true)
    {
        this.value = value;
    }
}

#if UNITY_EDITOR
[CustomPropertyDrawer(typeof(ReadOnlyAttribute))]
public class ReadOnlyDrawer : PropertyDrawer
{
    public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
    {
        GUI.enabled = false;
        EditorGUI.PropertyField(position, property, label, true);
        GUI.enabled = true;
    }
}
#endif
