using UnityEngine;

public class Body : MonoBehaviour
{
    [Header("Body Settings")]
    public float Radius = 50;
    public float DistanceFromPlayer;
    public float RotationPeriod = 10; //degrees per second

    public int SphereDetail;
    public float TerrainMagnitudeScale = .01f;       //a factor of radius
    public float actualTerrainMagnitude;

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
        SetScale();
        DoRotation();
        int detail = GetDistanceDetail();
        if (detail != SphereDetail)
        {
            SphereDetail = detail;
            mesh = Icosphere.Generate(SphereDetail);
            if (filter) filter.sharedMesh = mesh;
            ModifyMesh();
            render.sharedMaterial = material;
        }
    }

    protected virtual void OnValidate()
    {
        SetScale();
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
            if (DistanceFromPlayer < Radius * 1.5f) return 6;
            if (DistanceFromPlayer < Radius * 2) return 5;
            if (DistanceFromPlayer < Radius * 3) return 4;
            if (DistanceFromPlayer < Radius * 10) return 3;
        }
        return 2;
    }

    private void SetScale()
    {
        if (Radius <= 0) Radius = .01f;
        Vector3 scale = Vector3.one * Radius * 2;
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


    void ModifyMesh()
    {
        if (!mesh) return;
        actualTerrainMagnitude = TerrainMagnitudeScale * Radius;

        Vector3[] verts = mesh.vertices;

        for (int i = 0; i < verts.Length; i++)
        {
            Vector3 dir = verts[i].normalized;
            float offset = Random.Range(-actualTerrainMagnitude, actualTerrainMagnitude);
            verts[i] = dir * (verts[i].magnitude + offset);
        }

        mesh.vertices = verts;
        mesh.RecalculateNormals();
        mesh.RecalculateBounds();
    }

}
