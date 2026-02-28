using UnityEngine;

public class SteeringSystem : MonoBehaviour
{
    public float TurnRate;      //handles pitch and yaw. degrees per second
    public float RollRate;      //handles roll. degrees per second
    public float StickZeroRate = 0.5f; //rate stick returns to center without input
    public float StickResponse = .25f;
    public float Pitch;
    public float Roll;
    public float Yaw;
    public Vector3 Stick = Vector3.zero;    //controllable stick position, does not reflect actual position
    public Vector3 realStick = Vector3.zero; //actual stick position, influenced by controllable setting

    public Vector3 Result = Vector3.zero;


    void Update()
    {
        StickManagement();
        ApplySteering();
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

        Stick.x = Mathf.Clamp(Stick.x, -1f, 1f);
        Stick.y = Mathf.Clamp(Stick.x, -1f, 1f);
        Stick.z = Mathf.Clamp(Stick.x, -1f, 1f);
        //clear control values
        Pitch = 0;
        Yaw = 0;
        Roll = 0;
        //Stick = Vector3.zero;
    }

    private void ApplySteering()
    {
        //apply roll and turn rates to movement
        Result = realStick;
        Result.y = realStick.x * TurnRate;  //pitch
        Result.x = realStick.z * TurnRate;  //yaw
        Result.z = realStick.y * RollRate;  //roll
        Result *= Time.deltaTime;
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


}
