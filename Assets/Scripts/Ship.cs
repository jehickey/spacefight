using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Ship : MonoBehaviour
{
    //flight controls
    public float Pitch;
    public float Roll;
    public float Yaw;
    public float Throttle;    //current thrust setting, 0-1
    public float StickResponse = .25f;
    public Vector3 Stick = Vector3.zero;    //controllable stick position, does not reflect actual position
    public Vector3 realStick = Vector3.zero; //actual stick position, influenced by controllable setting

    public float StickZeroRate = 0.5f; //rate stick returns to center without input

    //limits and capabilities
    public float MaxSpeed;      //meters per second
    public float TurnRate;      //handles pitch and yaw. degrees per second
    public float RollRate;      //handles roll. degrees per second
    public float ThrottleRate;    //speed of thrust change per second (0-1)

    //current status
    public float Speed;         //current velocity magnitude, meters per second
    public Vector3 Velocity;    //current velocity vector, meters per second

    public List<Weapon> weapons = new List<Weapon>();
    public bool IsFiring = false;
    public float IsFiringCooldown = .25f;
    private float lastFireTime = 0;


    public Team team;

    private Simulation sim;

    void Start()
    {
    }

    private void OnEnable()
    {
        if (!sim) sim = FindFirstObjectByType<Simulation>();
        if (weapons.Count == 0)
        {
            foreach (Weapon weapon in GetComponentsInChildren<Weapon>())
            {
                weapons.Add(weapon);
            }
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

        Speed = Throttle * MaxSpeed;
        Velocity = transform.forward * Speed;
        transform.position += Velocity * Time.deltaTime;
        UpdateFiringStatus();
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


    public void SetThrottle(float throttle)
    {
        if (throttle == 0) return;
        throttle = Mathf.Clamp01(throttle);
        Throttle = Mathf.MoveTowards(Throttle, throttle, ThrottleRate * Time.deltaTime);
    }

    public void Fire()
    {
        //check for weapons and fire them
        foreach (Weapon weapon in weapons)
        {
            weapon.Fire();
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

}
