using UnityEngine;

public class Body : MonoBehaviour
{
    public static int MinDetailGlobal = 1;
    public static int MaxDetailGlobal = 7;

    [Header("Body Settings")]
    public bool TerrainDeformation = false;
    public float Radius = 50;
    public float DistanceFromPlayer;
    public float RotationPeriod = 10; //degrees per second

    public int SphereDetail;
    public int MaxDetail = 0;
    public float actualTerrainMagnitude;
    public float TerrainSmoothness = 1;

    public Texture2D heightmap;

    public bool Regenerate = false;

    public Material material;
    public Mesh mesh;
    protected MeshFilter filter;
    protected MeshRenderer render;


    protected virtual void Start()
    {

    }

    protected virtual void OnEnable()
    {
        filter = GetComponent<MeshFilter>();
        if (!filter) filter = gameObject.AddComponent<MeshFilter>();

        render = GetComponent<MeshRenderer>();
        if (!render) render = gameObject.AddComponent<MeshRenderer>();
        
        //mesh = Icosphere.Generate(SphereDetail);
        //filter.sharedMesh = mesh;

        if (!material) material = new Material(Shader.Find("Unlit/Color"));
        render.sharedMaterial = material;

        SphereDetail = 0;
        /*
        mesh = filter.sharedMesh;
        if (material) { render.sharedMaterial = material; }
        else { material = render.sharedMaterial; }
        */

    }

    protected virtual void Update()
    {
        //forced regeneration
        if (Regenerate)
        {
            SphereDetail = 0;
            Regenerate = false;
        }

        //Set a hard minimum on MinGlobalDetail (to prevent regeneration every frame)
        if (MinDetailGlobal == 0) MinDetailGlobal = 1;

        //no detail setting defaults to class maximum
        if (MaxDetail == 0) MaxDetail = MaxDetailGlobal;
        //don't let this object's settings go beyond class minimum and maximum
        MaxDetail = Mathf.Clamp(MaxDetail, MinDetailGlobal, MaxDetailGlobal);
        //apply instance limits for this specific body
        SphereDetail = Mathf.Clamp(SphereDetail, MinDetailGlobal, MaxDetail);

        SetScale();
        DoRotation();
        int detail = Mathf.Clamp(GetDistanceDetail(), MinDetailGlobal, MaxDetail);
        if (detail != SphereDetail || !mesh)
        {
            SphereDetail = detail;
            mesh = Shapes.Icosphere.Generate(SphereDetail);
            if (mesh)
            {
                if (filter) filter.sharedMesh = mesh;
                DeformMesh();
                if (render) render.sharedMaterial = material;
            }
        }
    }

    protected virtual void OnValidate()
    {
        //SetScale();
    }

    protected virtual void OnDrawGizmos()
    {
        if (!Application.isPlaying)
        {
            Gizmos.color = Color.red;
            Gizmos.DrawSphere(transform.position, Radius);
        }
        else
        {
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(transform.position, Radius / 2f);
        }
    }


    private int GetDistanceDetail()
    {
        if (Camera.main)
        {
            DistanceFromPlayer = Vector3.Distance(transform.position, Camera.main.transform.position);
            if (DistanceFromPlayer < Radius * 1.5f) return 8;
            if (DistanceFromPlayer < Radius * 2) return 7;
            if (DistanceFromPlayer < Radius * 3) return 6;
            if (DistanceFromPlayer < Radius * 10) return 5;
        }
        return 3;
    }

    private void SetScale()
    {
        if (Radius <= 0) Radius = .01f;
        Vector3 scale = Vector3.one * Radius;
        if (transform.parent)
        {
            scale.x /= transform.parent.lossyScale.x;
            scale.y /= transform.parent.lossyScale.y;
            scale.z /= transform.parent.lossyScale.z;
        }
        transform.localScale = scale;
    }


    void DoRotation() {
        if (!Simulation.I || Simulation.I.TimeScale==0) return;
        float degreesPerSimSecond = 360f / RotationPeriod;
        transform.Rotate(Vector3.up, degreesPerSimSecond * Simulation.I.TimeDelta, Space.Self);
    }


    void DeformMesh()
    {
        if (!TerrainDeformation) return;
        if (!mesh) return;
        actualTerrainMagnitude = Simulation.I.TerrainMagnitudeScale * TerrainSmoothness * Radius;

        //may have this re-grab a virgin sphere if needed

        Vector3[] verts = mesh.vertices;
        Vector2[] uvs = mesh.uv;

        if (!heightmap) heightmap = (Texture2D)material.mainTexture;

        for (int i = 0; i < verts.Length; i++)
        {
            Vector3 dir = verts[i].normalized;

            //get sample from grayscale
            Color c = heightmap.GetPixelBilinear(uvs[i].x, uvs[i].y);
            float h = c.grayscale - .5f;

            //apply displacement
            //float offset = Random.Range(-actualTerrainMagnitude, actualTerrainMagnitude);
            //float noiseAmount = 0;
            //float noise = Random.Range(-noiseAmount, noiseAmount);
            float displacedRadius = 1 + h * actualTerrainMagnitude;
            verts[i] = dir * displacedRadius;   //(verts[i].magnitude + offset);
        }

        mesh.vertices = verts;
        mesh.RecalculateNormals();
        mesh.RecalculateBounds();
    }

}
