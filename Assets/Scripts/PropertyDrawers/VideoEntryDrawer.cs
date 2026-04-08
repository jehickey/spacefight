using UnityEngine;
using UnityEditor;

[CustomPropertyDrawer(typeof(VideoEntry))]
public class VideoEntryDrawer : PropertyDrawer
{
    public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
    {
        EditorGUI.BeginProperty(position, label, property);

        // Remove indent so the line is flush
        int indent = EditorGUI.indentLevel;
        EditorGUI.indentLevel = 0;

        // Calculate rects
        float nameWidth = position.width * 0.3f;
        float clipWidth = position.width * 0.7f;

        Rect nameRect = new Rect(position.x, position.y, nameWidth - 4, position.height);
        Rect clipRect = new Rect(position.x + nameWidth, position.y, clipWidth, position.height);

        // Draw fields
        SerializedProperty nameProp = property.FindPropertyRelative("Name");
        SerializedProperty clipProp = property.FindPropertyRelative("Clip");

        EditorGUI.PropertyField(nameRect, nameProp, GUIContent.none);
        EditorGUI.PropertyField(clipRect, clipProp, GUIContent.none);

        EditorGUI.indentLevel = indent;
        EditorGUI.EndProperty();
    }

    public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
    {
        return EditorGUIUtility.singleLineHeight;
    }
}
