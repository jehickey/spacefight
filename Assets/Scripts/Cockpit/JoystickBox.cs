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

    private Vector3 targetStick = Vector3.zero;
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


    private void UpdateJoystickPosition()
    {
        if (!Joystick) return;

        
        //get VR control
        if (grabHandle && grabHandle.Grabbed && (!Game.I || Game.I.VRHeadset))
        {
            //Vector3 stick = grabHandle.Delta * GrabPushRate;
            //stick.z = stick.y;
            //stick.y = 0;
            //StickPosition.x = stick.x;
            //StickPosition.y = 0;
            //StickPosition.z = stick.z;

            Vector3 rot = grabHandle.rotationDelta.eulerAngles;
            targetStick.x = -Mathf.Clamp(Mathf.DeltaAngle(0, rot.z) / MovementRange, -1, 1);
            targetStick.y = Mathf.Clamp(Mathf.DeltaAngle(0, rot.y) / MovementRange, -1, 1);
            targetStick.z = Mathf.Clamp(Mathf.DeltaAngle(0, rot.x) / MovementRange, -1, 1);
        }


        //impose safe limits
        targetStick.x = Mathf.Clamp(targetStick.x, -1, 1);
        targetStick.y = Mathf.Clamp(targetStick.z, -1, 1);
        targetStick.z = Mathf.Clamp(targetStick.z, -1, 1);

        StickPosition = Vector3.MoveTowards(StickPosition, targetStick, MaxAngularChange * Time.deltaTime);


        //work out deflection (stick position limited by movement range)
        Vector3 offset = StickPosition * MovementRange;
        //move the stick
        //Joystick.localRotation = Quaternion.FromToRotation(StickNeutral, StickNeutral-offset);
        Joystick.localEulerAngles = new Vector3(StickPosition.z, 0, -StickPosition.x) * MovementRange;
        OutputStick.x = StickPosition.x;
        OutputStick.y = 0;
        OutputStick.z = StickPosition.y;

        targetStick = Vector3.zero;
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
        OutputStick.y = Mathf.Clamp(value, -1f, 1f);
    }


}
