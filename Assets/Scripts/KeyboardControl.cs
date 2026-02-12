using UnityEngine;
using static UnityEngine.Rendering.DebugUI;

public class KeyboardControl : MonoBehaviour
{

    private Ship ship;
    private FlightControls controls;

    void Start()
    {
        ship = GetComponent<Ship>();
        if (!ship) Debug.LogError("KeyboardControl can't find a Ship to control");
        controls = new FlightControls();
        controls.Enable();
    }

    void Update()
    {
        if (ship)
        {
            ship.SetPitch(controls.Flight.Pitch.ReadValue<float>());
            ship.SetYaw(controls.Flight.Yaw.ReadValue<float>());
            ship.SetRoll(controls.Flight.Roll.ReadValue<float>());
            ship.SetThrottle(controls.Flight.Throttle.ReadValue<float>());
            if (controls.Flight.Fire.IsPressed()) ship.Fire();

            Vector2 pitchyaw = controls.Flight.PitchYaw.ReadValue<Vector2>();
            pitchyaw.x = ((pitchyaw.x / Screen.width) * 2f - 1f);
            pitchyaw.y = -((pitchyaw.y / Screen.height) * 2f - 1f);

            Vector2 smooth = Vector2.zero;
            smooth.y = SmoothAxis(pitchyaw.y, .1f, 1.5f);
            smooth.x = SmoothAxis(pitchyaw.x, .1f, 1.5f);
            if (smooth.magnitude > 0)
            {
                ship.SetPitch(smooth.y);
                ship.SetYaw(smooth.x);
            }

        }

    }


    float SmoothAxis(float value, float deadzone, float exponent)
    {
        float abs = Mathf.Abs(value);

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
