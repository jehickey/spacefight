using System.Runtime.CompilerServices;
using Unity.XR.CoreUtils;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Windows;
using UnityEngine.XR;
using UnityEngine.XR.Management;
using static UnityEngine.GraphicsBuffer;

public class Headset : MonoBehaviour
{
    public TrackingOriginModeFlags TrackingMode = TrackingOriginModeFlags.Device;
    public float CameraYOffset = 0;
    public Transform CameraOffsetObject;
    public new Camera camera;

    private XRInputSubsystem xrInput;
    private FlightControls flightControls;
    private PlayerController playerController;

    public float recenterHoldTime = 1.0f;
    private bool didRecenter = false;
    private float recenterTimer = 0f;

    private Vector3 headsetPosition = Vector3.zero;

    private FlightControls controls
     {
         get {
             if (flightControls == null) flightControls = new FlightControls();
             return flightControls;
         }
     }
     private void OnEnable()
     {
         controls.Enable();
     }
     private void OnDisable()
     {
         controls.Disable();
    }

    void Start()
    {
        playerController = GetComponentInParent<PlayerController>();
        xrInput = XRGeneralSettings.Instance.Manager.activeLoader.GetLoadedSubsystem<XRInputSubsystem>();
        if (xrInput != null)
        {
            xrInput.TrySetTrackingOriginMode(TrackingMode);     //use device mode, but offer others
            Recenter();
        }
        camera = transform.GetComponentInChildren<Camera>();
    }

    void LateUpdate()
    {
        CheckRecenterButtons();
        if (CameraOffsetObject)
        {
            headsetPosition = controls.VR.HeadPosition.ReadValue<Vector3>();
            //CameraOffsetObject.localRotation = controls.VR.HeadRotation.ReadValue<Quaternion>();
            //CameraOffsetObject.localPosition += new Vector3(0, CameraYOffset, 0);
        }
    }


    void CheckRecenterButtons()
    {

        if (xrInput == null) return;
        bool leftPressed = controls.VR.LeftThumbstickPress.IsPressed();
        bool rightPressed = controls.VR.RightThumbstickPress.IsPressed();


        if (leftPressed && rightPressed)
        {
            if (!didRecenter)
            {
                recenterTimer += Time.deltaTime;
                if (recenterTimer >= recenterHoldTime)
                {
                    recenterTimer = 0f;
                    Recenter();
                    didRecenter = true;

                }
                return;
            }
        }
        else
        {
            recenterTimer = 0f;
            didRecenter = false;
        }
    }


    void Recenter()
    {
        if (!camera) camera = transform.GetComponentInChildren<Camera>();

        if (xrInput != null)
        {
            //xrInput.TryRecenter();
        }

        Transform player = playerController.transform;
        Transform rig = transform;
        Transform cam = camera.transform;

        //realign rig to match camera position
        if (camera)
        {
            //aim headset at ship forward (on horizontal plane only)
            Vector3 up = player.up;
            Vector3 camFwd = Vector3.ProjectOnPlane(cam.forward, up).normalized;
            Vector3 targetFwd = Vector3.ProjectOnPlane(player.forward, up).normalized;
            Quaternion rot = Quaternion.FromToRotation(camFwd, targetFwd);
            rig.rotation = rot * rig.rotation;

            //position headset back at mount point
            //Get a world coordinate for the headset position (to avoid position abberation from using camera pos)
            Vector3 worldHeadPos = CameraOffsetObject.TransformPoint(headsetPosition);
            Vector3 headOffsetWorld = worldHeadPos - rig.position;
            rig.position = player.position - headOffsetWorld;
        }
    }

}
