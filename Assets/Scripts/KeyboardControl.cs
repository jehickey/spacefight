using UnityEngine;

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
        }

    }
}
