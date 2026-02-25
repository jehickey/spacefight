using UnityEngine;

public class JoystickBox : MonoBehaviour
{
    public Transform Joystick;
    public Vector3 ReferenceDir = Vector3.up;
    public Vector3 StickPosition = Vector3.zero;
    public Vector3 StickNeutral = Vector3.zero;
    public float MovementRange = 0.5f;
    public Quaternion NeutralPosition;

    void Start()
    {
        
    }

    void Update()
    {
        UpdateJoystickPosition();

    }

    private void OnValidate()
    {
        UpdateJoystickPosition();
    }

    private void UpdateJoystickPosition()
    {
        if (!Joystick) return;
        //impose safe limits
        StickPosition.x = Mathf.Clamp(StickPosition.x, -1, 1);
        StickPosition.y = Mathf.Clamp(StickPosition.z, -1, 1);
        StickPosition.z = 0;// Mathf.Clamp(StickPosition.z, -1, 1);
        //work out deflection (stick position limited by movement range)
        Vector3 offset = StickPosition * MovementRange;
        //move the stick
        Joystick.localRotation = Quaternion.FromToRotation(StickNeutral, StickNeutral-offset);
    }
}
