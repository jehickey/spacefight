using UnityEngine;
using UnityEngine.UI;

public class PlanetTestOverlay : MonoBehaviour
{

    public Text txtRadii;
    public Text txtSphereDetail;
    public Text txtVertices;
    public Text txtFPS;
    public Text txtMode;

    private Body body;
    private CameraCursor cursor;

    void OnEnable()
    {
        body = FindFirstObjectByType<Body>();
        cursor = FindFirstObjectByType<CameraCursor>();
    }

    void Update()
    {
        if (body)
        {
            txtSphereDetail.text = $"Sphere Detail: {body.SphereDetail}";
            if (body.mesh) txtVertices.text = $"Mesh Vertices: {body.mesh.vertices.Length}";
        }
        if (cursor)
        {
            txtRadii.text = $"Radii: {cursor.Radii:#.00}";
            txtMode.text = "";
            if (cursor.OrbitMode) txtMode.text = "ROTATE MODE";
            if (cursor.LightMode) txtMode.text = "LIGHT MODE";
            if (cursor.AngleMode) txtMode.text = "ANGLE MODE";
            //if (cursor
        }
        if (Game.I)
        {
            txtFPS.text = $"FPS: {Game.I.FPS:#.0}";
        }
        
    }
}
