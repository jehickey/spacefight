using UnityEngine;


public class Sun : Body
{
    [Header("Sun Settings")]
    public float EmissionBrightness = 20;
    public float LightIntensity = 1;

    public new Light light;

    protected override void Start()
    {
        base.Start();
        if (material)
        {
            material.EnableKeyword("_EMISSION");
            material.SetColor("_EmissionColor", Color.white * EmissionBrightness);
        }
        light = GetComponentInChildren<Light>();
    }

    protected override void Update()
    {
        base.Update();

        //aim light source at camera
        if (light)
        {
            light.intensity = LightIntensity;
            if (Camera.main)
            {
                light.transform.LookAt(Camera.main.transform);      //aim at the camera
            }
            else
            {
                light.transform.LookAt(Vector3.zero);               //aim at the center
            }
        }
    }
}
