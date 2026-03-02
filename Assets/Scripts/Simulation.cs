using UnityEngine;

public class Simulation : MonoBehaviour
{
    public float TimeScale = 1f;
    public float TimeDelta => Time.deltaTime * TimeScale;
    //public Ship PlayerShip;

    [Header("Config Settings")]
    public float StickControlLimit = 0.5f;       //this is a percentage of the screen
    public float StickControlDeadzone = 0.25f;   //this is a percentage of the screen

    public float SpeedUnit = 1f;

    public float ForceDecayRate = 1f;
    public float ImpactDamageMultiplier = .25f;
    public float ImpactForceMultiplier = 2f;

    public float AudioCutoffRange = 10;         //how close before audio is activated
    public float AudioCutoffPadding = 1;        //how far out of range before audio is deactivated
    public float AudioExternalSuppression = .5f;    //How much to suppress audio from outside ship

    public float AudioLevelWeapons = 1;
    public float AudioLevelEngines = 1;
    public float AudioLevelExplosions = 1;


    void Start()
    {
        //look for any competing instances and warn (don't delete)
        foreach (Simulation sim in FindObjectsByType<Simulation>(FindObjectsSortMode.None))
        {
            if (sim != this) Debug.LogError("Multiple Simulation instances found!");
        }
    }

    void Update()
    {
        
    }
}
