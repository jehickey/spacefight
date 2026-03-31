using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.XR;
using UnityEngine.XR;
using UnityEngine.XR.Management;

public class Headset : MonoBehaviour
{
    public TrackingOriginModeFlags TrackingMode = TrackingOriginModeFlags.Device;
    public float CameraYOffset = 0;
    public Transform CameraOffsetObject;
    public new Camera camera;
    private List<Camera> cameras = new List<Camera>();

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
        Application.onBeforeRender += BeforeRender;

    }
     private void OnDisable()
     {
         controls.Disable();
        Application.onBeforeRender -= BeforeRender;
    }

    void Start()
    {
        playerController = GetComponentInParent<PlayerController>();
        cameras.AddRange(GetComponentsInChildren<Camera>());
        if (cameras.Count > 0) camera = cameras[0];
        //InitVR();
    }

    private void Update()
    {
        InitVR();
    }

    void LateUpdate()
    {
        CheckRecenterButtons();
        if (CameraOffsetObject)
        {
            headsetPosition = controls.VR.HeadPosition.ReadValue<Vector3>();
        }
    }


    void BeforeRender()
    {
        HeadTracking();
    }

    private void InitVR()
    {
        if (xrInput != null) return;        //already initialized
        xrInput = XRGeneralSettings.Instance.Manager.activeLoader?.GetLoadedSubsystem<XRInputSubsystem>();
        if (xrInput != null)                //it has just initialized
        {
            xrInput.TrySetTrackingOriginMode(TrackingMode);     //use device mode, but offer others
            Recenter();
            Debug.Log("INIT");
        }
    }


    void HeadTracking()
    {
        //if we're not in VR, reset cameras to centered and forward
        if (Game.I && !Game.I.VRHeadset)
        {
            foreach (Camera cam in cameras)
            {
                cam.transform.localPosition = Vector3.zero;
                cam.transform.localRotation = Quaternion.identity;
            }
            return;
        }

        var hmd = InputSystem.GetDevice<XRHMD>();
        if (hmd == null)
        {
            Debug.Log("Got null HMD");
            return;
        }

        Vector3 pos = hmd.centerEyePosition.ReadValue();
        Quaternion rot = hmd.centerEyeRotation.ReadValue();

        foreach (Camera cam in cameras)
        {
            cam.transform.localPosition = Vector3.Lerp(
                cam.transform.localPosition,
                pos,
                1f - Mathf.Exp(-20f * Time.deltaTime));
            //cam.transform.localPosition = pos;

            /*
            cam.transform.localRotation = Quaternion.Slerp(
                cam.transform.localRotation,
                rot,
                1f - Mathf.Exp(-5f * Time.deltaTime));
            */
            cam.transform.localRotation = rot;
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
