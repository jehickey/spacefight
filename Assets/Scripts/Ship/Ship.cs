using NUnit.Framework.Internal;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using Unity.Burst;
using UnityEngine;
using static UnityEngine.Rendering.DebugUI.Table;

public class Ship : MonoBehaviour
{
    //flight controls
    public Team team;
    public Vector3 Velocity;    //current velocity vector, meters per second
    public float Health = 10f;
    public float MaxHealth = 10f;
    public float Mass = 100;

    [SerializeField]
    private float RumbleIntensity = 0;
    [SerializeField]
    private float RumbleFade = 1;
    [SerializeField]
    private float RumbleMax = 5;
    private     Vector3 baseCamLocalPos;
    private Quaternion baseCamLocalRot;


    [Header("Weapons System")]
    public List<Weapon> weapons = new List<Weapon>();
    public int WeaponIndex = 0; //index of next gun in the sequence
    public float WeaponIndexDelay = .1f; //delay between continuation of sequence
    public bool WeaponsFireInterlinked = false; //guns all fire together
    public bool IsFiring = false;
    public float IsFiringCooldown = .25f;
    private float lastFireTime = 0;

    private Simulation sim;
    private ThrottleSystem Throttle;
    private SteeringSystem Steering;
    private Camera cam;

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

        if (!sim) sim = FindFirstObjectByType<Simulation>();
        if (!Throttle) Throttle = GetComponent<ThrottleSystem>();
        if (!Steering) Steering = GetComponent<SteeringSystem>();

        if (weapons.Count == 0)
        {
            foreach (Weapon weapon in GetComponentsInChildren<Weapon>())
            {
                weapons.Add(weapon);
            }
        }
        SelectWeapon();
        //add ship to its team if not already present
        if (team && !team.Ships.Contains(this)) team.Ships.Add(this);

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
        CollisionChecks();
        UpdateFiringStatus();
        UpdateHealth();
        DoRumble();
    }

    private void LateUpdate()
    {
        Vector3 posDelta = forcedDisplacement;
        Vector3 rotDelta = Vector3.zero;

        //apply steering
        if (Steering) rotDelta += Steering.Result;

        //throttle and propulsion
        //Velocity = Vector3.zero;
        if (Throttle) Velocity = transform.forward * Throttle.Thrust * sim.SpeedUnit;
        posDelta += Velocity * Time.deltaTime;

        //apply external forces
        if (Mass <= 0) Mass = .001f;
        posDelta += (externalForce / Mass) * Time.deltaTime;
        rotDelta += (externalAngularForce / Mass) * Time.deltaTime;

        //apply all movement
        transform.position += posDelta;
        transform.Rotate(rotDelta, Space.Self);

        forcedDisplacement = Vector3.zero;
        externalForce *= Mathf.Exp( -sim.ForceDecayRate * Time.deltaTime);
        externalAngularForce *= Mathf.Exp(-sim.ForceDecayRate * Time.deltaTime);
    }

    private void CollisionChecks()
    {
        //compare against every other ship
        foreach (Ship ship in FindObjectsByType<Ship>(FindObjectsSortMode.None))
        {
            if (ship && ship != this)
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
                    forcedDisplacement += (direction * (distance*.5f+.0001f));
                    externalForce += direction.normalized * impact * sim.ImpactForceMultiplier;
                    TakeDamage(sim.ImpactDamageMultiplier * impact);
                    Debug.Log($"Impact: {impact}");
                }
            }
        }
    }

    private void CollisionCheck()
    {
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


    //There's only one weapon right now
    public void SelectWeapon()
    {
        //set firing index delay to keep fire continuous
        if (weapons.Count > 0)
        {
            float rateAvg = 0;
            foreach (Weapon weapon in weapons) rateAvg += weapon.FireRate;
            rateAvg /= weapons.Count;
            if (rateAvg > 0)
            {
                WeaponIndexDelay = (1f / rateAvg) / weapons.Count;
            }
        }

        //if (Time.time - lastFireTime < 1/FireRate) return; //enforce fire rate

    }

    public void Fire()
    {
        if (weapons.Count == 0) return;
        if (Time.time - lastFireTime < WeaponIndexDelay) return;

        //check for weapons and fire them
        if (WeaponsFireInterlinked)
        {
            foreach (Weapon weapon in weapons) weapon.Fire();
        }
        else
        {
                if (WeaponIndex >= weapons.Count) WeaponIndex = 0;
                weapons[WeaponIndex].Fire();
                WeaponIndex++;
        }

        IsFiring = true;
        lastFireTime = Time.time;
    }


    private void UpdateFiringStatus()
    {
        if (IsFiring && Time.time - lastFireTime >= IsFiringCooldown)
        {
            IsFiring = false;
        }
    }

    public void TakeDamage(float damage)
    {
        if (damage <= 0) return;
        Health -= damage;
        AddRumble(10);
    }

    private void UpdateHealth()
    {
        Health = Mathf.Clamp(Health, 0, MaxHealth);
        if (Health <= 0)
        {
            Flare.Spawn(transform.position, Color.white, 1f, 0.15f, 0.025f, 0.25f);
            Destroy(gameObject);
        }
    }

}
