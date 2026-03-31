using UnityEngine;


public class KeyboardControl : MonoBehaviour
{

    public bool MouseSteering = true;

    [Tooltip("Defines how rapidly a keypress influences the throttle")]
    public float ThrottlePush = 1;

    private PlayerController player;
    private Ship ship;
    private FlightControls flightControls;

    private float screenSize;

    public ThrottleBox throttle;
    public JoystickBox joystick;
    public JumpDrivePanel jumpDrive;


    private FlightControls controls
    {
        get {
            if (flightControls == null) flightControls = new FlightControls();
            return flightControls;
        }
    }

    private void OnEnable()
    {
        //if (!ship) ship = GetComponent<Ship>();
        if (flightControls == null) flightControls = new FlightControls();
        flightControls.Enable();


        Cursor.lockState = CursorLockMode.Locked;
        Cursor.lockState = CursorLockMode.None;
    }

    private void OnDisable()
    {
        flightControls?.Disable();
    }

    

    void Start()
    {
    }

    void Update()
    {
        if (Game.I)
        {
            if (Game.I.Paused) flightControls.Flight.Disable();
            if (!Game.I.Paused) flightControls.Flight.Enable();
        }
        ConnectToCockpit();
        screenSize = Mathf.Min(Screen.width, Screen.height);
        if (ship)
        {
            //steering.SetPitch(controls.Flight.Pitch.ReadValue<float>());
            //ship.SetYaw(controls.Flight.Yaw.ReadValue<float>());
            if (joystick) joystick.SetRoll(-controls.Flight.Roll.ReadValue<float>());

            //throttle control
            if (throttle && !Game.I.VRHeadset)
            {
                if (controls.Flight.Throttle.IsPressed())
                {
                    float input = controls.Flight.Throttle.ReadValue<float>();
                    input *= ThrottlePush * Time.deltaTime;
                    throttle.InputPosition += input;
                }

                throttle.Boost = controls.Flight.Boost.IsPressed();
            }

            //work the fire button, but only if not in VR
            if (joystick && !Game.I.VRHeadset)
            {
                joystick.TriggerPressed = controls.Flight.Fire.IsPressed();
            }

            if (jumpDrive)
            {
                if (controls.Flight.Jump.WasPressedThisFrame()) jumpDrive.Engage();
            }

            MouseToStickVector();
        }

    }


    private void ConnectToCockpit()
    {
        if (!player)                                        //connect to Player
        {
            player = GetComponent<PlayerController>();
            if (!player)
            {
                ship = null;
                return;
            }
        }
        if (!ship)                                          //connect to Ship
        {
            ship = player.ship;
            if (!ship)
            {
                throttle = null;
                joystick = null;
                jumpDrive = null;
                return;
            }
        }
        //Connect to each component as needed
        if (!throttle) throttle = ship.GetComponentInChildren<ThrottleBox>();
        if (!joystick) joystick = ship.GetComponentInChildren<JoystickBox>();
        if (!jumpDrive) jumpDrive = ship.GetComponentInChildren<JumpDrivePanel>();
    }


    public void MouseToStickVector()
    {
        if (!MouseSteering) return;
        if (Game.I.VRHeadset) return;
        Vector2 mousePos = controls.Flight.PitchYaw.ReadValue<Vector2>();

        //convert mouse position into a centered coordinate system
        mousePos = mousePos - new Vector2(Screen.width * 0.5f, Screen.height * 0.5f);

        //compute radius in pixels
        float deadzoneRadius = screenSize * Game.I.StickControlDeadzone;
        float limitRadius = screenSize * Game.I.StickControlLimit;

        //distance from center
        float dist = mousePos.magnitude;
        if (dist < deadzoneRadius) dist=0;

        //clamp to limit radius
        if (dist > limitRadius)
            mousePos = mousePos.normalized * limitRadius;

        //normalize into 0..1 range between deadzone and limit
        float t = Mathf.InverseLerp(deadzoneRadius, limitRadius, mousePos.magnitude);

        //direction (unit vector)
        Vector2 dir = mousePos.normalized;

        //final circular stick vector (x = yaw, z = pitch)
        joystick.SetStick2D(new Vector3(dir.x * t, -dir.y * t));
    }



    float SmoothAxis(float value, float deadzone, float exponent)
    {
        //needs to be rewritten to use the sim's deadzone and limit settings
        float abs = Mathf.Abs(value);
        //float deadzone = sim.StickControlDeadzone;

        // Inside deadzone - no movement
        if (abs < deadzone)
            return 0f;

        // Remove deadzone and normalize to 0..1
        float normalized = (abs - deadzone) / (1f - deadzone);

        // Apply response curve (exponent > 1 = softer center, stronger edges)
        float curved = Mathf.Pow(normalized, exponent);

        // Restore sign
        return Mathf.Sign(value) * curved;
    }

}
