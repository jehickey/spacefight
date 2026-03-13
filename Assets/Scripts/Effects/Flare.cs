using Unity.VisualScripting;
using UnityEngine;

public class Flare : MonoBehaviour
{
    public float TTL = 2f;
    public float PeakFraction = .25f;
    public float MinRadius = .01f;
    public float MaxRadius = .1f;
    public Color color = Color.white;
    public float EmissionLevel;
    //public float MinAlpha = .5f;
    private float startTime;
    private float peakTime;

    public bool useLight = true;
    public float LightRadiusMultiplier = 10f;


    public bool useShockwave;
    public Shockwave shockwave;
    public float shockwaveRadiusMultiplier = 2;
    public float shockwaveRadiusFactor = 2;

    public int SphereDetail = 3;
    public AudioClip clipSound;

    protected MeshFilter filter;
    protected MeshRenderer render;

    public Mesh mesh;
    private Material material;
    private SoundMachine sound;

    private new Light light;

    private void OnEnable()
    {
        mesh = Shapes.Icosphere.Generate(SphereDetail);

        material = new Material(Simulation.I?.FlareMaterial);
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


        if (useLight)
        {
            light = GetComponent<Light>();
            if (!light) light = gameObject.AddComponent<Light>();
            light.type = LightType.Point;
            light.shadows = LightShadows.None;
            light.color = Color.white;
            light.intensity = 3;
        }

        Init();
    }

    void Start()
    {
        if (useShockwave)
        {
            shockwave = Shockwave.Spawn(transform.position, Color.white, TTL*.5f, MaxRadius * shockwaveRadiusMultiplier);
        }

        if (useLight)
        {
            light.color = Color.white;
            light.range = MaxRadius * LightRadiusMultiplier;
        }

    }

    private void Init()
    {
        if (TTL <= 0) TTL = .01f;               //thou shalt not divide by zero
        startTime = Time.time;
        peakTime = TTL * PeakFraction;
        transform.localScale = Vector3.one * MinRadius;
        if (clipSound)
        {
            if (!sound) sound = gameObject.AddComponent<SoundMachine>();
            if (sound)
            {
                sound.Sound = clipSound;
                sound.Volume = Game.I.AudioLevelExplosions;
                sound.PlayOnStart = true;
                //sound.Play();
                //Debug.Log("BOOM");
            }
        }
    }

    void Update()
    {
        float age = Time.time - startTime;
        float multiplier = FlareCurve(age);
        float radius = Mathf.Lerp(MinRadius, MaxRadius, multiplier);
        transform.localScale = Vector3.one * radius * 2f;
        if (light) light.intensity = (1-multiplier) * 2f;
        if (material)
        {
            color.a = multiplier;
            material.SetColor("_Color", color*multiplier);
            material.color = color*multiplier;
            material.SetColor("_EmissionColor", color*multiplier*5f);
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


    // Returns a smoothed 0𢴎 multiplier
    float FlareCurve(float t)
    {
        peakTime = TTL * PeakFraction;

        // Linear rise/fall
        float linear = 0;
        if (t <= peakTime) linear = t / peakTime;
        if (t > peakTime) linear = 1f - ((t - peakTime) / (TTL - peakTime));
        linear = Mathf.Clamp01(linear);

        // Smoothstep for softer motion
        return linear * linear * (3f - 2f * linear);
    }


    public static Flare Spawn(Vector3 position, Color color, float ttl = .5f, float peakFraction = .25f, float minRadius = .01f, float maxRadius = .05f, AudioClip soundEffect = null)
    {
        GameObject obj = new GameObject("Flare");
        obj.transform.position = position;
        Flare flare = obj.AddComponent<Flare>();
        flare.TTL = ttl;
        flare.PeakFraction = peakFraction;
        flare.MinRadius = minRadius;
        flare.MaxRadius = maxRadius;
        flare.color = color;
        flare.clipSound = soundEffect;
        flare.Init();
        return flare;
    }
}
