using UnityEngine;

public class SequentialLightPanel : MonoBehaviour
{
    public IndicatorLight[] lights;
    public float Min = 0;
    public float Max = 0;
    public float Value = 0;
    public float LevelRates = 10f;
    [ColorUsage(true, true)]
    public Color LoColor;
    public Color HiColor;

    void Update()
    {
        // Normalize Value into 0–1 range
        float t = Mathf.InverseLerp(Min, Max, Value);

        for (int i = 0; i < lights.Length; i++)
        {
            var light = lights[i];

            // Push shared settings
            light.On = true;
            light.color = Color.Lerp(LoColor, HiColor*2f, t);
            light.LevelRate = LevelRates;

            // Determine whether this light should be lit
            float threshold = (i + 1f) / lights.Length;
            light.SetLevel = t >= threshold ? 1f : 0f;
        }

        
    }
}
