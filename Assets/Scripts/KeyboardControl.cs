using UnityEngine;


public class KeyboardControl : MonoBehaviour
{

    public bool MouseSteering = true;

    [Tooltip("Defines how rapidly a keypress influences the throttle")]
    public float ThrottlePush = 1;

    private Ship ship;
    private FlightControls flightControls;

    private float screenSize;

    public ThrottleBox throttleBox;
    public JoystickBox steering;
    public WeaponsSystem weapons;

    private FlightControls controls
    {
        get {
            if (flightControls == null) flightControls = new FlightControls();
            return flightControls;
        }
    }

    private void OnEnable()
    {
        if (!ship) ship = GetComponent<Ship>();
        if (flightControls == null) flightControls = new FlightControls();
        flightControls.Enable();

        throttleBox = GetComponentInChildren<ThrottleBox>();
        steering = GetComponentInChildren<JoystickBox>();
        weapons = GetComponentInChildren<WeaponsSystem>();

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
        screenSize = Mathf.Min(Screen.width, Screen.height);
        if (ship)
        {
            //steering.SetPitch(controls.Flight.Pitch.ReadValue<float>());
            //ship.SetYaw(controls.Flight.Yaw.ReadValue<float>());
            steering.SetRoll(-controls.Flight.Roll.ReadValue<float>());

            //throttle control
            if (throttleBox)
            {
                if (controls.Flight.Throttle.IsPressed())
                {
                    float input = controls.Flight.Throttle.ReadValue<float>();
                    input *= ThrottlePush * Time.deltaTime;
                    throttleBox.InputPosition += input;
                }
                throttleBox.Boost = controls.Flight.Boost.IsPressed();
            }
            if (controls.Flight.Fire.IsPressed() && weapons) weapons.Fire();

            MouseToStickVector();
        }

    }


    public void MouseToStickVector()
    {
        if (!MouseSteering) return;
        Vector2 mousePos = controls.Flight.PitchYaw.ReadValue<Vector2>();

        // Convert mouse position into a centered coordinate system
        mousePos = mousePos - new Vector2(Screen.width * 0.5f, Screen.height * 0.5f);

        // Compute radius in pixels
        float deadzoneRadius = screenSize * Game.I.StickControlDeadzone;
        float limitRadius = screenSize * Game.I.StickControlLimit;

        // Distance from center
        float dist = mousePos.magnitude;
        if (dist < deadzoneRadius) dist=0;

        // Clamp to limit radius
        if (dist > limitRadius)
            mousePos = mousePos.normalized * limitRadius;

        // Normalize into 0..1 range between deadzone and limit
        float t = Mathf.InverseLerp(deadzoneRadius, limitRadius, mousePos.magnitude);

        // Direction (unit vector)
        Vector2 dir = mousePos.normalized;

        // Final circular stick vector (x = yaw, z = pitch)
        //Vector3 result = new Vector3(dir.x * t, 0f, dir.y * t);
        steering.StickPosition.x = dir.x * t;
        steering.StickPosition.z = -dir.y * t;
        if (Game.I.InvertPitchAxis) steering.StickPosition.z *= -1;
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
