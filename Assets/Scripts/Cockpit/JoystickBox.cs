using TMPro;
using UnityEngine;

public class JoystickBox : MonoBehaviour
{
    public Transform Joystick;
    public Vector3 ReferenceDir = Vector3.up;
    public Vector3 StickNeutral = Vector3.zero;
    public float MovementRange = 0.5f;
    public float MaxAngularChange = 90;                     //per second
    public Quaternion NeutralPosition;
    public Vector3 OutputStick = Vector3.zero;              //final output

    public float VRDeadzone = 0.1f;                             //input below this is ignored

    public float RollRate = 1;                              //how fast rolls can be performed
    public float outputRoll;                                 //for external roll input
    private float targetRoll;

    [SerializeField]
    [ReadOnly]
    private Vector3 targetStick = Vector3.zero;
    [SerializeField]
    [ReadOnly]
    private Vector3 StickPosition = Vector3.zero;

    public float GrabPushRate = 2;
    public bool TriggerPressed= false;      //need a timeout for this if it's not reinforced regularly
    public Transform TriggerObject;
    public float TriggerDepressRange = 20;

    private SteeringSystem steeringSystem;
    private WeaponsSystem weaponsSystem;
    private GrabMoveRotation grabHandle;
    private GrabTrigger trigger;



    private void OnEnable()
    {
        if (!steeringSystem) steeringSystem = GetComponentInParent<SteeringSystem>();
        if (!weaponsSystem) weaponsSystem = GetComponentInParent<WeaponsSystem>();
        if (!grabHandle) grabHandle = GetComponentInChildren<GrabMoveRotation>();
        if (!trigger) trigger = GetComponentInChildren<GrabTrigger>();
    }

    void Update()
    {
        UpdateJoystickPosition();
        UpdateTriggerButton();
        if (steeringSystem) steeringSystem.Input = OutputStick;
    }

    private void OnValidate()
    {
        UpdateJoystickPosition();
    }

    public void SetStick(Vector3 set)
    {
        targetStick = set;
    }

    public void SetStick2D(Vector2 set)
    {
        targetStick.x = set.x;
        targetStick.z = set.y;
    }



    private void UpdateJoystickPosition()
    {
        if (!Joystick) return;


        //get VR control
        if (grabHandle && grabHandle.Grabbed && (!Game.I || Game.I.VRHeadset))
        {
            Vector3 rot = grabHandle.rotationDelta.eulerAngles;
            targetStick.x = -Mathf.Clamp(Mathf.DeltaAngle(0, rot.z) / MovementRange, -1, 1);
            targetStick.y = Mathf.Clamp(Mathf.DeltaAngle(0, -rot.y) / MovementRange, -1, 1);
            targetStick.z = Mathf.Clamp(Mathf.DeltaAngle(0, rot.x) / MovementRange, -1, 1);

            targetStick.x = ApplyVRDeadzone(targetStick.x);
            targetStick.y = ApplyVRDeadzone(targetStick.y);
            targetStick.z = ApplyVRDeadzone(targetStick.z);

            //block stick rotation if not enabled
            if (Game.I && !Game.I.TurnStickToRoll) targetStick.y = 0;


            /*
            Vector3 axis;
            float angle;
            grabHandle.rotationDelta.ToAngleAxis(out angle, out axis);
            if (angle>180f) angle -= 360f;
            targetStick = axis * angle;
            */


        }




        //impose safe limits
        targetStick.x = Mathf.Clamp(targetStick.x, -1, 1);
        targetStick.y = Mathf.Clamp(targetStick.y, -1, 1);
        targetStick.z = Mathf.Clamp(targetStick.z, -1, 1);

        //move stick towards target position
        //StickPosition = Vector3.MoveTowards(StickPosition, targetStick, MaxAngularChange * Time.deltaTime);
        StickPosition.x = Mathf.MoveTowards(StickPosition.x, targetStick.x, MaxAngularChange * Time.deltaTime);
        StickPosition.y = Mathf.MoveTowards(StickPosition.y, targetStick.y, MaxAngularChange * Time.deltaTime);
        StickPosition.z = Mathf.MoveTowards(StickPosition.z, targetStick.z, MaxAngularChange * Time.deltaTime);

        //drift target stick towards zero
        targetStick.x = Mathf.MoveTowards(targetStick.x, 0, MaxAngularChange * .5f * Time.deltaTime);
        targetStick.y = Mathf.MoveTowards(targetStick.y, 0, MaxAngularChange * .5f * Time.deltaTime);
        targetStick.z = Mathf.MoveTowards(targetStick.z, 0, MaxAngularChange * .5f * Time.deltaTime);




        //drift roll input towards zero
        targetRoll = Mathf.MoveTowards(targetRoll, 0, RollRate * Time.deltaTime);

        //adjust output roll towards target
        outputRoll = Mathf.MoveTowards(outputRoll, targetRoll, RollRate * Time.deltaTime);
        //StickPosition.y = outputRoll;
        
        //work out deflection (stick position limited by movement range)
        //Vector3 offset = StickPosition * MovementRange;

        //move the stick
        Joystick.localEulerAngles = new Vector3(StickPosition.z, -StickPosition.y, -StickPosition.x) * MovementRange;

        //set output
        OutputStick.x = StickPosition.x;
        OutputStick.y = StickPosition.y;
        OutputStick.z = StickPosition.z;
    }


    private void UpdateTriggerButton()
    {
        //Check grabtrigger status, but only interfere if it's grabbed
        if (trigger && trigger.Grabbed) TriggerPressed = trigger.Pressed;

        //fire if triggered
        if (TriggerPressed && weaponsSystem) weaponsSystem.Fire();

        //update the visual trigger object
        if (TriggerObject)
        {
            Vector3 current = TriggerObject.localEulerAngles;
            if (TriggerPressed)
            {
                current.x = TriggerDepressRange;
            }
            else
            {
                current.x = 0;
            }
            TriggerObject.localEulerAngles = current;
        }
    }

    public void SetRoll(float value)
    {

        //targetRoll = Mathf.Clamp(value, -1f, 1f);
        targetStick.y = Mathf.Clamp(value, -1f, 1f);
    }


    float ApplyVRDeadzone(float v)
    {
        if (Mathf.Abs(v) < VRDeadzone)
            return 0f;

        // Optional: rescale so output ramps smoothly to 1
        return Mathf.Sign(v) * Mathf.InverseLerp(VRDeadzone, 1f, Mathf.Abs(v));
    }


}
