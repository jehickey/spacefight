using System.Collections.Generic;
using UnityEngine;

public class SequentialLightPanel : MonoBehaviour
{
    [Header("Sequential Light Panel Settings")]
    public List<IndicatorLight> lights = new List<IndicatorLight>();
    public float Min = 0;
    public float Max = 0;
    public float Value = 0;
    public float LevelRates = 10f;
    [ColorUsage(true, true)]
    public Color LoColor;
    public Color HiColor;
    public float Brightness = 1f;

    protected virtual void Start()
    {
        //If no lights have been pre-assigned, assign them
        if (lights.Count == 0)
        {
            lights.AddRange(GetComponentsInChildren<IndicatorLight>());
        }

    }

    protected virtual void Update()
    {
        // Normalize Value into 0–1 range
        Value = Mathf.Clamp(Value, Min, Max);
        float t = Mathf.InverseLerp(Min, Max, Value);

        for (int i = 0; i < lights.Count; i++)
        {
            var light = lights[i];

            // Push shared settings
            light.On = true;
            light.color = Color.Lerp(LoColor, HiColor*2f, t);
            light.LevelRate = LevelRates;

            // Determine whether this light should be lit
            float threshold = (i + 1f) / lights.Count;
            light.Level = (t >= threshold ? 1f : 0f) * Brightness;

        }
    }


}
