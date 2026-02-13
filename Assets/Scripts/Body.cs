using UnityEngine;

public class Body : MonoBehaviour
{
    [Header("Body Settings")]
    public float Radius = 50;
    public float DistanceFromPlayer;
    public float RotationPeriod = 10; //degrees per second

    public int SphereDetail;

    public Material material;
    public Mesh mesh;

    protected MeshFilter filter;
    protected MeshRenderer render;

    protected Simulation sim;

    protected virtual void Start()
    {
        sim = FindFirstObjectByType<Simulation>();

        filter = GetComponent<MeshFilter>();
        if (!filter) filter = gameObject.AddComponent<MeshFilter>();
        mesh = Icosphere.Generate(2);
        filter.sharedMesh = mesh;

        render = GetComponent<MeshRenderer>();
        if (!render) render = gameObject.AddComponent<MeshRenderer>();
        filter.sharedMesh = Icosphere.Generate(SphereDetail);

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
            filter.sharedMesh = Icosphere.Generate(SphereDetail);
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
        if (!sim) return 3;
        DistanceFromPlayer = Vector3.Distance(transform.position, sim.PlayerShip.transform.position);
        if (DistanceFromPlayer < Radius * 3) return 4;
        if (DistanceFromPlayer < Radius * 10) return 3;
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
        if (!sim || sim.TimeScale==0) return;
        float degreesPerSimSecond = 360f / RotationPeriod;
        transform.Rotate(Vector3.up, degreesPerSimSecond * sim.TimeDelta, Space.Self);
    }

}
