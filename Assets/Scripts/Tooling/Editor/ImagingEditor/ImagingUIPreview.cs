using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

public class ImagingUIPreview
{

    public Texture2D inputTexture;
    public Texture2D outputTexture;


    //preview controls
    public bool showProcessed = false;
    Vector2 scrollPos;
    float zoom = 1;
    float zoomMin = .1f;
    float zoomMax = 8;
    Rect previewScrollRect;


    public void Draw()
    {
        Texture2D showImage = showProcessed ? outputTexture : inputTexture;

        EditorGUILayout.BeginVertical();
        GUILayout.Label("Preview", EditorStyles.boldLabel);

        if (inputTexture == null)
        {
            GUILayout.Label("No textures available");
            EditorGUILayout.EndVertical();
            return;
        }

        
        float maxWidth = previewScrollRect.width - 20;
        //float aspect = (float)outputTexture.width / outputTexture.height;
        float width = inputTexture.width * zoom;
        float height = inputTexture.height * zoom;

        if (outputTexture != null)
        {
            width = outputTexture.width * zoom;
            height = outputTexture.height * zoom;
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
            maxWidth = previewScrollRect.width - 40;
            zoom = maxWidth / (outputTexture ? outputTexture.width : inputTexture.width);
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

        if (e.type == EventType.Repaint) previewScrollRect = GUILayoutUtility.GetLastRect();

        if (showImage) GUILayout.Label($"Zoom: {zoom:0.00}x  ({showImage.width}x{showImage.height})");
        
        EditorGUILayout.EndVertical();
    }


}
