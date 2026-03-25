using System.Collections.Generic;
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

    [Header("Planetary Bodies")]
    public bool useSpawnPlanets = false;
    public bool useSpawnMoons = false;
    public bool doSpawnPlanets = false;
    public Sun sunPrefab;
    public Sun sun;
    public SolarSystemData systemData;
    public float RadiusCompression = .5f;
    public float DistanceCompression = .5f;
    public float RadiusFactor = 1;
    public float DistanceFactor = 1;

    //public List<Body> planetPrefabs = new List<Body>();

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
        if (!systemData)
        {
            Debug.Log("No solar system data assigned!");
        }
    }

    private void OnDestroy()
    {
        if (I == this) I = null;
    }

    private void Start()
    {
        if (useSpawnPlanets) SpawnPlanets();
    }

    void Update()
    {
        if (doSpawnPlanets) SpawnPlanets();
        if (TerrainMagnitudeScale < 0) TerrainMagnitudeScale = 0;
    }

    void SpawnPlanets()
    {
        doSpawnPlanets = false;
        if (!useSpawnPlanets) return;
        if (!systemData)
        {
            Debug.Log("No Solar System Data!");
            return;
        }
        DespawnPlanets();
        //set up sun
        sun = null;
        if (sunPrefab)
        {
            sun = Instantiate(sunPrefab);
            sun.transform.position = Vector3.zero;
        }

        int planetNumber = 0;
        foreach (PlanetData p in systemData.planets)
        {
            GameObject obj = new GameObject();
            Body body = obj.AddComponent<Body>();
            body.Data = p;
            body.parentBody = sun;
            body.OrbitPhase = SetStartingPhase( ++planetNumber);
            if (useSpawnMoons)
            {
                foreach (BodyData m in p.moons) {
                    GameObject moonObj = new GameObject();
                    Body moon = moonObj.AddComponent<Body>();
                    moon.Data = m;
                    moon.parentBody = body;
                    moon.OrbitPhase = 0;        //random positioning
                }
            }
        }

    }

    void DespawnPlanets()
    {
        foreach (Body body in GameObject.FindObjectsByType<Body>(FindObjectsSortMode.None))
        {
            Destroy(body.gameObject);
        }
    }


    float SetStartingPhase(int planetNumber)
    {
        float fullArc = .25f;
        float arc = fullArc / 9;

        return arc * planetNumber;
    }


}
