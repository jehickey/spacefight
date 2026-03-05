using UnityEngine;

[DisallowMultipleComponent]
public class Simulation : MonoBehaviour
{
    public static Simulation I {get; private set; }
    public float TimeScale = 1f;
    public float TimeDelta => Time.deltaTime * TimeScale;
    //public Ship PlayerShip;


    public float SpeedUnit = 1f;

    public float ForceDecayRate = 1f;
    public float ImpactDamageMultiplier = .25f;
    public float ImpactForceMultiplier = 2f;
    public float ImpactDisplacementMultiplier = 2f;


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
        
    }
}
