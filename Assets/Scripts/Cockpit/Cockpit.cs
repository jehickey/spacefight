using UnityEngine;

public class Cockpit : MonoBehaviour
{
    public JoystickBox Joystick;
    private Ship ship;

    public SequentialLightPanel ThrottleLightPanel;

    private ThrottleSystem throttle;


    private void OnEnable()
    {
        InitComponents();
        throttle = GetComponentInParent<ThrottleSystem>();

    }


    void Update()
    {
        if (ThrottleLightPanel) ThrottleLightPanel.Value = throttle.Actual;
        if (Joystick)
        {
            //Joystick.StickPosition = ship.realStick;
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

        if (!Joystick) Joystick = GetComponentInChildren<JoystickBox>();

        if (!Joystick)
        {
            Debug.Log("Cockpit can't find joystick box!");
            return;
        }


    }
}
