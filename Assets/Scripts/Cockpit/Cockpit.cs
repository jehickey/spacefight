using UnityEngine;

public class Cockpit : MonoBehaviour
{
    private Ship ship;

    private JoystickBox joystick;
    private WeaponsSystem weaponsSystem;
    private ThrottleBox throttle;
    private ThrottleSystem throttleSystem;


    private void OnEnable()
    {
        InitComponents();
    }


    void Update()
    {
        if (weaponsSystem)
        {
            if (joystick)
            {
                //if (joystick.TriggerPressed) weaponsSystem.Fire();
                //Debug.Log("FIRE!");
            }
        }

        if (throttleSystem)
        {
            if (throttle)
            {
            }
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
        if (!weaponsSystem) weaponsSystem = GetComponentInParent<WeaponsSystem>();
        if (!joystick) joystick = GetComponentInChildren<JoystickBox>();

        if (!throttleSystem) throttleSystem = GetComponentInParent<ThrottleSystem>();
        if (!throttle) throttle = GetComponentInChildren<ThrottleBox>();

    }
}
