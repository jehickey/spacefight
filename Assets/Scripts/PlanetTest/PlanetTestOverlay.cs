using Shapes;
using UnityEngine;
using UnityEngine.UI;

public class PlanetTestOverlay : MonoBehaviour
{

    [Header("Text Fields")]
    public Text txtRadii;
    public Text txtSphereDetail;
    public Text txtVertices;
    public Text txtMode;
    public Text txtTerrain;
    public Text txtTMagnitude;
    public Text txtTDistance;
    public Text txtCachingStatus;
    public Text txtCachedData;
    public Text txtCachedMeshes;


    [Header("FPS Meter")]
    public Text txtFPS;
    public Color FPSLoColor = Color.red;
    public Color FPSHiColor = Color.green;
    public float FPSLoValue = 30;
    public float FPSHiValue = 60;


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
            txtSphereDetail.text = $"Sphere Detail: {body.SphereDetail}/{body.MaxDetail}";
            if (body.mesh) txtVertices.text = $"Mesh Vertices: {body.mesh.vertices.Length}";
        }
        if (cursor)
        {
            txtRadii.text = $"Radii: {cursor.Radii:#.00}";
            txtMode.text = "";
            if (cursor.OrbitMode) txtMode.text = "ROTATE MODE";
            if (cursor.LightMode) txtMode.text = "LIGHT MODE";
            if (cursor.AngleMode) txtMode.text = "ANGLE MODE";
            if (cursor.TerrainMode) txtMode.text = "TERRAIN MODE";
            //if (cursor
        }

        UpdateFPS();
        UpdateCachingPanel();
        if (Simulation.I)
        {
            if (cursor.AllowDeformation)
            {
                txtTerrain.text = $"Terrain On";
                txtTMagnitude.text = $"Magnitude Scale: {Simulation.I.TerrainMagnitudeScale:#.000}";
                txtTDistance.text = $"Distance Scale: {Simulation.I.TerrainDistanceScale:#.000}";
            }
            else
            {
                txtTerrain.text = $"Terrain Off";
                txtTMagnitude.text = "";
                txtTDistance.text = "";
            }
        }

    }


    void UpdateCachingPanel()
    {
        txtCachingStatus.text = $"Caching: {Mathf.RoundToInt(Icosphere.PreCacheCompletion*100)}% (of {Icosphere.PreCacheCount})";
        txtCachedData.text = $"Data: {Icosphere.CachedDataCount()}";
        txtCachedMeshes.text = $"Meshes: {Icosphere.CachedMeshesCount()}";
    }

    void UpdateFPS()
    {
        if (!Game.I)        //Game instance is missing!
        {
            txtFPS.text = $"FPS: ?";
            txtFPS.color = FPSLoColor;
            return;
        }
        txtFPS.text = $"FPS: {Game.I.FPS:#.0}";
        float fpsFactor = Mathf.InverseLerp(FPSLoValue, FPSHiValue, Game.I.FPS);
        txtFPS.color = Color.Lerp(FPSLoColor, FPSHiColor, fpsFactor);
    }
}
