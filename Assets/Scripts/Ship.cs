using UnityEngine;

public class Ship : MonoBehaviour
{
    //flight controls
    public float Pitch;
    public float Roll;
    public float Yaw;
    public float Throttle;    //current thrust setting, 0-1
    public float StickResponse = .25f;


    //limits and capabilities
    public float MaxSpeed;      //meters per second
    public float TurnRate;      //handles pitch and yaw. degrees per second
    public float RollRate;      //handles roll. degrees per second
    public float ThrottleRate;    //speed of thrust change per second (0-1)

    //current status
    public float Speed;         //current velocity magnitude, meters per second
    public Vector3 Velocity;    //current velocity vector, meters per second



    void Start()
    {

        
    }

    void Update()
    {
        float pitchTurn = Pitch * TurnRate * Time.deltaTime;
        float yawTurn = Yaw * TurnRate * Time.deltaTime;
        float rollTurn = Roll * RollRate * Time.deltaTime;
        transform.Rotate(pitchTurn, yawTurn, rollTurn, Space.Self);

        Speed = Throttle * MaxSpeed;
        Velocity = transform.forward * Speed;
        transform.position += Velocity * Time.deltaTime;

    }


    //interface:

    public void SetYaw(float value)
    {
        value = Mathf.Clamp(value, -1f, 1f);
        Yaw = Mathf.MoveTowards(Yaw, value, StickResponse*Time.deltaTime);
    }

    public void SetPitch(float value)
    {
        value = Mathf.Clamp(value, -1f, 1f);
        Pitch = Mathf.MoveTowards(Pitch, value, StickResponse * Time.deltaTime);
    }

    public void SetRoll(float value)
    {
        value = Mathf.Clamp(value, -1f, 1f);
        Roll = Mathf.MoveTowards(Roll, value, StickResponse * Time.deltaTime);
    }


    public void SetThrottle(float throttle)
    {
        if (throttle == 0) return;
        throttle = Mathf.Clamp01(throttle);
        Throttle = Mathf.MoveTowards(Throttle, throttle, ThrottleRate * Time.deltaTime);
    }

    public void Fire()
    {
    }


}
