using UnityEngine;

public class Cockpit : MonoBehaviour
{
    public ThrottleBox Throttle;
    public JoystickBox Joystick;
    private Ship ship;

    private void OnEnable()
    {
        InitComponents();
    }


    void Update()
    {
        if (Throttle) Throttle.ThrowPosition = ship.Throttle;
        if (Joystick)
        {
            Joystick.StickPosition = ship.realStick;
        }
    }


    private void InitComponents()
    {
        if (!ship) ship = GetComponentInParent<Ship>();
        if (!ship)
        {
            Debug.Log("Cockpit can't find ship!");
            return;
        }

        if (!Throttle) Throttle = GetComponentInChildren<ThrottleBox>();
        if (!Throttle)
        {
            Debug.Log("Cockpit can't find throttle box!");
            return;
        }

        if (!Joystick) Joystick = GetComponentInChildren<JoystickBox>();

        if (!Joystick)
        {
            Debug.Log("Cockpit can't find joystick box!");
            return;
        }


    }
}
