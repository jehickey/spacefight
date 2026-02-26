using UnityEngine;
using static UnityEngine.GraphicsBuffer;

public class IndicatorLight : MonoBehaviour
{
    public bool On = false;
    public float SetLevel = 1;
    public float Level = 0;
    public float LevelRate = 2; //level delta per second
    [ColorUsage(true,true)]
    public Color color = Color.white;

    private Renderer render;
    private MaterialPropertyBlock mpb;

    void Start()
    {
    }

    private void OnEnable()
    {
        if (!render) render = GetComponent<Renderer>();
        if (mpb == null) mpb = new MaterialPropertyBlock();
    }

    void Update()
    {
        float targetLevel = On ? SetLevel : 0f;
        if (LevelRate > 0) Level = Mathf.Lerp(Level, targetLevel, Time.deltaTime * LevelRate);
        render.GetPropertyBlock(mpb);
        mpb.SetColor("_EmissionColor", color * Level);
        render.SetPropertyBlock(mpb);

    }
}
