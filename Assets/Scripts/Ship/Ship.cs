using System.Collections.Generic;
using UnityEngine;

public class Ship : MonoBehaviour
{
    //flight controls
    public Team team;
    public Vector3 Velocity;    //current velocity vector, meters per second
    //public float Health = 10f;
    //public float MaxHealth = 10f;
    public float Mass = 100;

    public float DistanceFromPlayer;
    private AudioListener listener;

    [SerializeField]
    private float RumbleIntensity = 0;
    [SerializeField]
    private float RumbleFade = 1;
    [SerializeField]
    private float RumbleMax = 5;
    private     Vector3 baseCamLocalPos;
    private Quaternion baseCamLocalRot;

    public float FreshSpawnCountdown = 0;

    [Header("Ship Audio")]
    public SoundMachine soundGotHit;
    public AudioClip clipExplosion;

    [Header("Weapons System")]
    WeaponsSystem Weapons;

    private Transform lastHitBy;

    private ThrottleSystem Throttle;
    private SteeringSystem Steering;
    private Camera cam;
    private BotControl pilot;
    private Destructable destructable;

    public new Collider collider;

    [SerializeField]
    private Vector3 forcedDisplacement = Vector3.zero;
    [SerializeField]
    private Vector3 externalForce = Vector3.zero;
    [SerializeField]
    private Vector3 externalAngularForce = Vector3.zero;

    void Start()
    {
    }

    private void OnEnable()
    {
        collider = GetComponent<Collider>();

        if (!Throttle) Throttle = GetComponent<ThrottleSystem>();
        if (!Steering) Steering = GetComponent<SteeringSystem>();
        if (!Weapons) Weapons = GetComponent<WeaponsSystem>();
        if (!pilot) pilot = GetComponent<BotControl>();
        if (!destructable) destructable = GetComponent<Destructable>();

        if (!cam) cam = GetComponentInChildren<Camera>();
        if (cam)
        {
            baseCamLocalPos = cam.transform.localPosition;
            baseCamLocalRot = cam.transform.localRotation;
        }
    }

    private void OnDestroy()
    {
        if (team) team.Ships.Remove(this);
    }


    void Update()
    {
        
        //disable pilot until spawn countdown is complete
        if (pilot) pilot.enabled = (FreshSpawnCountdown == 0);
        if (!pilot.enabled && Throttle) Throttle.Input = 1;
        if (!pilot.enabled && Steering) Steering.Stick = Vector3.zero;
        if (FreshSpawnCountdown != 0)
        {
            FreshSpawnCountdown -= Time.deltaTime;
            if (FreshSpawnCountdown < 0) FreshSpawnCountdown = 0;
            return;
        }

        //add ship to its team if not already present
        if (team && !team.Ships.Contains(this)) team.Ships.Add(this);

        if (!listener) listener = FindFirstObjectByType<AudioListener>();
        DistanceFromPlayer = -1;    //default if no sim or no player
        if (listener) DistanceFromPlayer = Vector3.Distance(transform.position, listener.transform.position);

        CollisionChecks();
        DoRumble();
    }

    private void LateUpdate()
    {
        if (Mass <= 0) Mass = .001f;
        Vector3 posDelta = forcedDisplacement / Mass; 
        Vector3 rotDelta = Vector3.zero;

        //apply steering
        if (Steering) rotDelta += Steering.Result;

        //throttle and propulsion
        //Velocity = Vector3.zero;
        if (Throttle) Velocity = transform.forward * Throttle.Thrust * Simulation.I.SpeedUnit;
        posDelta += Velocity * Time.deltaTime;

        //apply external forces
        posDelta += (externalForce / Mass) * Time.deltaTime;
        rotDelta += (externalAngularForce / Mass) * Time.deltaTime;

        //apply all movement
        transform.position += posDelta;
        transform.Rotate(rotDelta, Space.Self);

        //update forces and displacement
        forcedDisplacement = Vector3.zero;
        externalForce *= Mathf.Exp( -Simulation.I.ForceDecayRate * Time.deltaTime);
        externalAngularForce *= Mathf.Exp(-Simulation.I.ForceDecayRate * Time.deltaTime);
    }

    private void CollisionChecks()
    {
        if (FreshSpawnCountdown > 0) return;
        //compare against every other ship
        foreach (Ship ship in FindObjectsByType<Ship>(FindObjectsSortMode.None))
        {
            if (ship && ship != this && ship.FreshSpawnCountdown==0)
            {
                if (Physics.ComputePenetration(
                    collider, transform.position, transform.rotation,
                    ship.collider, ship.transform.position, ship.transform.rotation,
                    out Vector3 direction, out float distance))
                {
                    Vector3 point = (transform.position + ship.transform.position) * .5f;
                    float impact = (Velocity + ship.Velocity).magnitude;
                    float flareSize = .03f * impact;
                    Flare.Spawn(point, Color.white, .15f, .25f, flareSize*.1f, flareSize);
                    //Debug.Log($"ship {name} struck ship {ship.name}");
                    forcedDisplacement += (direction * (distance * .5f + .0001f)) * Simulation.I.ImpactDisplacementMultiplier * impact;
                    externalForce += direction.normalized * impact * Simulation.I.ImpactForceMultiplier;
                    destructable?.TakeDamage(Simulation.I.ImpactDamageMultiplier * impact, ship.transform);
                    //Debug.Log($"Impact: {impact}");
                }
            }
        }
    }



    public void AddRumble(float value)
    {
        RumbleIntensity += value * Time.deltaTime;
        RumbleIntensity = Mathf.Clamp(RumbleIntensity, 0, RumbleMax);
    }

    private void DoRumble()
    {
        RumbleIntensity -= RumbleFade* Time.deltaTime;
        RumbleIntensity = Mathf.Clamp(RumbleIntensity, 0, RumbleMax);
        if (cam)
        {
            float shakeFrequency = 12f;
            float shakePosAmplitude = 0.02f;
            float shakeRotAmplitude = 0.5f;

            float t = Time.time * shakeFrequency;

            // Smooth Perlin noise offsets
            float px = (Mathf.PerlinNoise(t, 0f) - 0.5f) * shakePosAmplitude * RumbleIntensity;
            float py = (Mathf.PerlinNoise(0f, t) - 0.5f) * shakePosAmplitude * RumbleIntensity;

            float rx = (Mathf.PerlinNoise(t, t) - 0.5f) * shakeRotAmplitude * RumbleIntensity;

            cam.transform.localPosition = baseCamLocalPos + new Vector3(px, py, 0f);
            cam.transform.localRotation = baseCamLocalRot * Quaternion.Euler(rx, 0f, 0f);
        }
    }

       
    public void TakeDamage(float damage, Transform origin = null)
    {
        AddRumble(10);
        //need to make impact and rumble directional
    }

}
