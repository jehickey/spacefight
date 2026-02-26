using NUnit.Framework.Internal;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using static UnityEngine.Rendering.DebugUI.Table;

public class Ship : MonoBehaviour
{
    //flight controls
    public Team team;
    public Vector3 Velocity;    //current velocity vector, meters per second
    public float Health = 10f;
    public float MaxHealth = 10f;

    [SerializeField]
    private float RumbleIntensity = 0;
    [SerializeField]
    private float RumbleFade = 1;
    [SerializeField]
    private float RumbleMax = 5;
private     Vector3 baseCamLocalPos;
    private Quaternion baseCamLocalRot;



    [Header("Steering System")]
    public float TurnRate;      //handles pitch and yaw. degrees per second
    public float RollRate;      //handles roll. degrees per second
    public float StickZeroRate = 0.5f; //rate stick returns to center without input
    public float StickResponse = .25f;
    public float Pitch;
    public float Roll;
    public float Yaw;
    public Vector3 Stick = Vector3.zero;    //controllable stick position, does not reflect actual position
    public Vector3 realStick = Vector3.zero; //actual stick position, influenced by controllable setting

//    public float ThrottleActual;      //current thrust setting, 0-1
//    public float ThrottleRate;  //speed of thrust change per second (0-1)
//    public float Speed;         //current velocity magnitude, meters per second
//    public float MaxSpeed;      //meters per second

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
    private Camera cam;

    void Start()
    {
    }

    private void OnEnable()
    {
        if (!sim) sim = FindFirstObjectByType<Simulation>();
        if (!Throttle) Throttle = GetComponent<ThrottleSystem>();

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

        StickManagement();
        ApplySteering();

        //throttle and propulsion
        Velocity = transform.forward * Throttle.Thrust * sim.SpeedUnit;
        transform.position += Velocity * Time.deltaTime;

        UpdateFiringStatus();
        UpdateHealth();
        DoRumble();
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


    private void StickManagement()
    {

        //apply individual Pitch/Yaw/Roll commands (hard unprocessed values)
        if (Pitch != 0) Stick.z = Pitch;
        if (Yaw != 0) Stick.x = Yaw;
        if (Roll != 0) Stick.y = Roll;

        //Limit steering - keep x and z within a circular range
        Vector2 stickLimit = new Vector2(Stick.x, Stick.z);
        if (stickLimit.sqrMagnitude > 1f) stickLimit = stickLimit.normalized;
        Stick.x = stickLimit.x;
        Stick.z = stickLimit.y;

        //Limit roll
        Stick.y = Mathf.Clamp(Stick.y, -1f, 1f);


        //apply Stick value to realStick with easing
        realStick = Vector3.MoveTowards(realStick, Stick, StickResponse * Time.deltaTime);
        //push the virtual stick towards zero
        Stick = Vector3.MoveTowards(Stick, Vector3.zero, StickZeroRate * Time.deltaTime);

        //clear control values
        Pitch = 0;
        Yaw = 0;
        Roll = 0;
        //Stick = Vector3.zero;
    }

    private void ApplySteering()
    {
        //apply roll and turn rates to movement
        Vector3 result = realStick;
        result.y = realStick.x * TurnRate;  //pitch
        result.x = realStick.z * TurnRate;  //yaw
        result.z = realStick.y * RollRate;  //roll
        result *= Time.deltaTime;
        transform.Rotate(result, Space.Self);
    }

    public void SetYaw(float value)
    {
        Yaw = Mathf.Clamp(value, -1f, 1f);
    }

    public void SetPitch(float value)
    {
        Pitch = Mathf.Clamp(value, -1f, 1f);
    }

    public void SetRoll(float value)
    {
        Roll = Mathf.Clamp(value, -1f, 1f);
    }


    /*
    public void SetThrottle(float throttle)
    {
        if (throttle == 0) return;
        throttle = Mathf.Clamp01(throttle);
        ThrottleActual = Mathf.MoveTowards(ThrottleActual, throttle, ThrottleRate * Time.deltaTime);
    }
    */

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
