using UnityEngine;

public class StellarBackground : MonoBehaviour
{

    public float Radius = 50;

    [Range(0, 10)]
    public int SphereDetail;
    [Range(0,5)]
    public float Brightness;

    public Material material;
    public Mesh mesh;

    private MeshFilter filter;
    private MeshRenderer render;

    private void OnEnable()
    {
        filter = GetComponent<MeshFilter>();
        if (!filter) filter = gameObject.AddComponent<MeshFilter>();
        //mesh = Icosphere.Generate(2);
        //filter.sharedMesh = mesh;

        render = GetComponent<MeshRenderer>();
        if (!render) render = gameObject.AddComponent<MeshRenderer>();
        //filter.sharedMesh = Icosphere.Generate(SphereDetail);

        //if (!material) material = new Material(Shader.Find("Unlit/Color"));
        //render.sharedMaterial = material;
        //SphereDetail = 0;
        Init();
    }


    void Update()
    {
        transform.localScale = Vector3.one * Radius * 2;
        if (material)
        {
            material.SetColor("_EmissionColor", Color.white * Brightness);
        }
    }

    private void OnValidate()
    {
        //Init();
    }

    private void Init()
    {
        if (!filter || !render)
        {
            Debug.Log("StellarBackground needs a renderer and a meshfilter");
            return;
        }
        else
        {
            mesh = Shapes.Icosphere.Generate(SphereDetail);
            Shapes.Icosphere.FlipMesh(mesh);
            filter.sharedMesh = mesh;
        }
        if (!material)
        {
            Debug.Log("StellarBackground has no material!");
            return;
        }
        else
        {
            render.sharedMaterial = material;
        }
        transform.localScale = Vector3.one * Radius * 2;
    }

}
