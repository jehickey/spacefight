using UnityEngine;

public class EnginePlume : MonoBehaviour
{
    public float Level = 1;
    public float Cycle = 0;

    [Header("Visual Settings")]
    public float CycleDelta = .1f;
    public float CycleRateMax = 1.0f;           //how quickly does it cycle at full blast
    public float CycleRateMin = .5f;            //how quickly does it cycle at minimum
    public Color ColorCold = Color.white;
    public Color ColorHot = Color.yellow;
    public float MaxSize = 1;                   //how big is it at full blast
    public float LongAxisBias = 2;
    public float MaxTransparency = .1f;         //how transparent is it at full blast
    public float MaxEmission = 1f;
    public float MinEmission = .1f;

    [Header("Sound Settings")]
    public float MaxPitch = 1.0f;
    public float MinPitch = .5f;
    public float MaxVolume = .5f;
    public float MinVolume = 0f;


    private Renderer render;
    private Material material;


    void OnEnable()
    {
        render = GetComponent<Renderer>();
        material = render.sharedMaterial;
        material.EnableKeyword("_EMISSION");
    }

    void Update()
    {
        //range checking
        Level = Mathf.Clamp01(Level);

        //update cycle
        float CycleRate = Mathf.Lerp(CycleRateMin, CycleRateMax, Level);
        Cycle = Mathf.Sin(Time.time * Mathf.PI * 2 * CycleRate) * CycleDelta * Level;

        //set color
        Color color = Color.Lerp(ColorCold, ColorHot, Level + Cycle);
        color.a = Level + Cycle;
        material.color = color;
        float emission = Mathf.Lerp(MinEmission, MaxEmission, Level + Cycle);
        //color.a = MaxTransparency * Level;
        material.SetColor("_EmissionColor", color * emission);

        //set size
        float size = Mathf.Lerp(0, MaxSize, Level) + Cycle;
        Vector3 scale = new Vector3(size, size, size*LongAxisBias);
        transform.localScale = Vector3.one * size;
    }
}
