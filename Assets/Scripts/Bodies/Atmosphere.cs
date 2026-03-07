using UnityEngine;
using UnityEngine.Experimental.GlobalIllumination;

public class Atmosphere : MonoBehaviour
{
    public float radii;          //radius, compared to parent body
    public Mesh mesh;           //sphere that handles the atmosphere
    public int SphereDetail;
    public Material material;   //material assigned to the atmosphere
    private Renderer render;
    private MeshFilter filter;

    private Body parentBody;
    private Sun sun;

    void OnEnable()
    {
        render = GetComponent<Renderer>();
        if (!render) render = gameObject.AddComponent<MeshRenderer>();
        filter = GetComponent<MeshFilter>();
        if (!filter) filter = gameObject.AddComponent<MeshFilter>();
        SphereDetail = 0;
        parentBody = GetComponentInParent<Body>();
        sun = FindFirstObjectByType<Sun>();
    }

    void Update()
    {
        if (!parentBody) return;
        ManageMesh();
        ManageMaterial();
        ManageScaling();
    }


    private void OnDrawGizmos()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, transform.parent.localScale.magnitude*.25f*radii);
    }

    void ManageMesh()
    {
        if (!filter) return;
        if (SphereDetail == 0) mesh = null;
        SphereDetail = parentBody.SphereDetail;


        if (mesh == null)
        {
            mesh = Shapes.Icosphere.Generate(SphereDetail);
            if (!mesh) return;
            filter.sharedMesh = mesh;
        }
    }

    void ManageMaterial()
    {
        if (!render) return;
        if (!material)
        {
            material = new Material(Shader.Find("Custom/AtmosphereGlow"));
        }
        if (!material) return;
        render.sharedMaterial = material;

        if (sun)
        {
            //adjust shader for sunlight direction
            material.SetVector("_LightDirection", -sun.light.transform.forward);
        }
    }

    void ManageScaling()
    {
        if (radii <= 0) radii = 1.1f;
        transform.localScale = Vector3.one * radii;
    }



}
