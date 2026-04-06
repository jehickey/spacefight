using UnityEditor;
using UnityEngine;

public class PlayerController : MonoBehaviour
{
    public Ship ship;
    private PlayerMountPoint mountPoint;

    public Camera InsideCam;
    public Camera OutsideCam;

    public bool doAlign = false;

    public Grabber LeftHand;
    public Grabber RightHand;

    public Transform joystickHandle;
    public Transform throttleHandle;

    void Start()
    {

        if (!joystickHandle)
        {
            JoystickBox stick = GameObject.FindFirstObjectByType<JoystickBox>();
            if (stick)
            {
                GrabMove grab = stick.GetComponentInChildren<GrabMove>();
                if (grab) joystickHandle = grab.transform;
            }
        }

        if (!throttleHandle)
        {
            ThrottleBox throttle = GameObject.FindFirstObjectByType<ThrottleBox>();
            if (throttle)
            {
                GrabMove grab = throttle.GetComponentInChildren<GrabMove>();
                if (grab) throttleHandle = grab.transform;
            }
        }


    }

    void LateUpdate()
    {
        AlignToShip();
        SetHandPosition();
    }

    private void OnValidate()
    {
        if (doAlign)
        {
            AlignToShip ();
            //doAlign = false;
        }
    }

    private void AlignToShip()
    {
        //if we don't have a ship, try to find a mount point somewhere
        if (!ship)
        {
            mountPoint = GameObject.FindFirstObjectByType<PlayerMountPoint>();
            if (mountPoint) ship = mountPoint.GetComponentInParent<Ship>();
        }
        //is still no ship, give up
        if (!ship)
        {
            mountPoint = null;
            return;
        }
        //if we have a ship and no mount point, get the mount point
        if (!mountPoint) mountPoint = ship.GetComponentInChildren<PlayerMountPoint>();
        //if still no mountpoint...
        if (!mountPoint) return;

        transform.position = mountPoint.transform.position;
        transform.rotation = mountPoint.transform.rotation;
    }


    private void SetHandPosition()
    {
        if (Game.I && Game.I.VRHeadset) return;     //don't mess with the hands when in VR
        if (RightHand && joystickHandle)
        {
            if (!RightHand.ForcedPosition)
            {
                RightHand.transform.position = joystickHandle.position;
                RightHand.transform.rotation = joystickHandle.rotation;
            }
        }
        if (LeftHand && throttleHandle)
        {
            if (!LeftHand.ForcedPosition)
            {
                LeftHand.transform.position = throttleHandle.position;
                LeftHand.transform.rotation = throttleHandle.rotation;
            }
        }
    }








}
