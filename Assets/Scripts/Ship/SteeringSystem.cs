using System;
using UnityEngine;

public class SteeringSystem : MonoBehaviour
{
    public bool Locked = false;

    public float TurnRate;      //handles pitch and yaw. degrees per second
    public float RollRate;      //handles roll. degrees per second
    public float StickZeroRate = 0.5f; //rate stick returns to center without input
    public float StickResponse = .25f;
    public Vector3 Input = Vector3.zero;    //controllable stick position, does not reflect actual position
    public Vector3 realStick = Vector3.zero; //actual stick position, influenced by controllable setting

    public Vector3 Result = Vector3.zero;


    void Update()
    {
        if (Locked)
        {
            realStick = Vector3.zero;
            Result = Vector3.zero;
            return;
        }
        StickInput();
        ApplySteering();
    }



    private void StickInput()
    {

        //Input.x = Yaw;
        //Input.y = Roll;
        //Input.z = Pitch;

        //Limit steering - keep x and z within a circular range
        Vector2 stickLimit = new Vector2(Input.x, Input.z);
        if (stickLimit.sqrMagnitude > 1f) stickLimit = stickLimit.normalized;
        Input.x = stickLimit.x;
        Input.z = stickLimit.y;

        //push the virtual stick towards zero
        Input = Vector3.MoveTowards(Input, Vector3.zero, StickZeroRate * Time.deltaTime);

        //impose forced limits
        Input.x = Mathf.Clamp(Input.x, -1f, 1f);
        Input.y = Mathf.Clamp(Input.y, -1f, 1f);
        Input.z = Mathf.Clamp(Input.z, -1f, 1f);

        if (Game.I.InvertPitchAxis) Input.z *= -1;

        //apply Stick value to realStick with easing
        realStick = Vector3.MoveTowards(realStick, Input, StickResponse * Time.deltaTime);
    }

    private void ApplySteering()
    {
        //apply roll and turn rates to movement
        //Result = realStick;
        Result = Vector3.zero;
        Result.x = realStick.z * TurnRate;  //yaw
        Result.y = realStick.x * TurnRate;  //pitch
        Result.z = realStick.y * RollRate;  //roll
        Result *= Time.deltaTime;
    }
}
