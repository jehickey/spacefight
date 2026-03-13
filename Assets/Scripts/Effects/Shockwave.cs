using UnityEngine;

public class Shockwave : MonoBehaviour
{
    public float TTL = 2f;
    public float MinRadius = .01f;
    public float MaxRadius = .1f;
    public Color color = Color.white;
    //public float MinAlpha = .5f;
    private float startTime;

    public int SphereDetail = 3;

    protected MeshFilter filter;
    protected MeshRenderer render;

    public Mesh mesh;
    private Material material;

    private void OnEnable()
    {
        mesh = Shapes.Icosphere.Generate(SphereDetail);

        material = new Material(Simulation.I?.FlareShockwaveMaterial);
        if (!material) material = new Material(Shader.Find("Unlit/Color"));
        material.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
        material.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
        material.SetInt("_ZWrite", 0);
        material.DisableKeyword("_ALPHATEST_ON");
        material.EnableKeyword("_ALPHABLEND_ON");
        material.DisableKeyword("_ALPHAPREMULTIPLY_ON");
        material.renderQueue = 3000;
        //material.color = Color.blue;
        material.EnableKeyword("_EMISSION");


        filter = GetComponent<MeshFilter>();
        if (!filter) filter = gameObject.AddComponent<MeshFilter>();
        filter.sharedMesh = mesh;


        render = GetComponent<MeshRenderer>();
        if (!render) render = gameObject.AddComponent<MeshRenderer>();
        render.sharedMaterial = material;


        Init();
    }

    private void Init()
    {
        if (TTL <= 0) TTL = .01f;               //thou shalt not divide by zero
        startTime = Time.time;
        transform.localScale = Vector3.one * MinRadius;
    }

    void Update()
    {
        float age = Time.time - startTime;
        float multiplier = ShockwaveCurve(age);
        float radius = Mathf.Lerp(MinRadius, MaxRadius, multiplier);
        transform.localScale = Vector3.one * radius * 2f;
        if (material)
        {
            color.a = multiplier;
            material.SetColor("_Color", color * multiplier);
            material.color = color * multiplier;
            material.SetColor("_EmissionColor", color * multiplier);
        }

        if (age >= TTL) Destroy(gameObject);
    }

    protected virtual void OnDrawGizmos()
    {
        if (!Application.isPlaying)
        {
            Gizmos.color = color;
            Gizmos.DrawSphere(transform.position, MaxRadius);
        }
    }


    float ShockwaveCurve(float t)
    {
        // Linear rise/fall
        float linear = 0;
        linear = t / TTL;
        linear = Mathf.Clamp01(linear);

        // Smoothstep for softer motion
        return linear * linear * (3f - 2f * linear);
    }


    public static Shockwave Spawn(Vector3 position, Color color, float ttl = .5f, float maxRadius = .05f)
    {
        GameObject obj = new GameObject("Shockwave");
        obj.transform.position = position;
        Shockwave shock = obj.AddComponent<Shockwave>();
        shock.TTL = ttl;
        shock.MaxRadius = maxRadius;
        shock.color = color;
        shock.Init();
        return shock;
    }

}
