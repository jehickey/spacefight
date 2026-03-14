using UnityEngine;

[DisallowMultipleComponent]
public class Simulation : MonoBehaviour
{
    public static Simulation I {get; private set; }

    [Header("Time")]
    public float TimeScale = 1f;
    public float TimeDelta => Time.deltaTime * TimeScale;

    public Material FlareMaterial;
    public Material ShockwaveMaterial;
    public Material TrailMaterial;

    [Header("Standard Units")]
    public float SpeedUnit = 1f;

    [Header("Forces and Collisions")]
    public float ForceDecayRate = 1f;
    public float ImpactDamageMultiplier = .25f;
    public float ImpactForceMultiplier = 2f;
    public float ImpactDisplacementMultiplier = 2f;

    [Header("Body Proximity Settings")]
    public float BodyProximityRadii = 2f;
    public float BodyClosestApproachRadii = 1.025f;
    public float BodyProximityFactorCurve = 3;
    public float BodyProximityThrustFactor = .5f;

    [Header("Terrain Settings")]
    public float TerrainMagnitudeScale = .01f;
    public float TerrainDistanceScale = 1f;


    private void Awake()
    {
        if (I && I != this)
        {
            Debug.Log("An instance of Simulation already exists!");
            //Destroy(gameObject);
            return;
        }
        I = this;
    }

    private void OnEnable()
    {
        if (!I) I = this;   //so it runs on domain reload
    }

    private void OnDestroy()
    {
        if (I == this) I = null;
    }

    void Update()
    {
        if (TerrainMagnitudeScale < 0) TerrainMagnitudeScale = 0;
    }
}
