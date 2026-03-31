using UnityEngine;

public class Grabber : MonoBehaviour
{
    [Header("Settings")]
    public LeftRight Hand;
    public GameObject HandObject;
    public Transform ControllerObject;
    public Transform TriggerArea;
    public Vector3 Offset = new Vector3(0, -0.025f, -0.025f);
    public float GrabTriggerRadius = .025f;

    [Header("Status")]
    [ReadOnly]
    public GrabMove hovering;
    [ReadOnly]
    public GrabMove holding;

    [ReadOnly]
    public Color HandColor = Color.white;

    [ReadOnly]
    public bool GripStart;
    [ReadOnly]
    public bool Gripping;
    [ReadOnly]
    public bool GripRelease;

    private FlightControls controls;
    private Material handMaterial;
    private new SphereCollider collider;
    private GrabTrigger holdTrigger;

    public enum LeftRight
    {
        Left,
        Right
    }


    void Start()
    {
        if (controls == null) controls = new FlightControls();
        controls.Enable();

        collider = GetComponent<SphereCollider>();
        collider.isTrigger = true;

        if (!HandObject)
        {
            if (transform.childCount > 0) HandObject = transform.GetChild(0).gameObject;
        }

        if (HandObject)
        {
            Renderer rend = HandObject.GetComponent<Renderer>();
            if (rend) handMaterial = rend.material;
        }
    }

    void LateUpdate()
    {
        bool wasGripping = Gripping;
        GripStart = false;

        //set hands visible only if we're in VR
        HandObject.SetActive(Game.I && Game.I.VRHeadset);
        ControllerObject.gameObject.SetActive(Game.I && Game.I.VRHeadset);
        TriggerArea.gameObject.SetActive(Game.I && Game.I.VRHeadset);

        UpdatePositions();

        if (GripStart && hovering)
        {
            if (holding) Debug.Log($"Trying to hold while already holding \"{holding.name}\"");
            if (!holding)
            {
                holding = hovering;
                holding.DoGrab(this);
                holdTrigger = holding.GetComponent<GrabTrigger>();
                //Debug.Log($"Grabbing {holding.name}");
            }
        }

        if (!Gripping && holding)
        {
            //Debug.Log($"Releasing {holding.name}");
            holding.DoRelease(this);
            holding = null;
            if (holdTrigger)
            {
                holdTrigger.Pressed = false;
                holdTrigger = null;
            }
        }

        UpdateButtons();
        UpdateColor();
        LockGrabbingHand();
        UpdateCollider();

    }


    private void OnTriggerEnter(Collider other)
    {
        //Debug.Log($"TriggerEnter {other.name}");
        GrabMove grab = other.GetComponent<GrabMove>();
        if (!grab) return;
        if (!holding) hovering = grab;
    }

    private void OnTriggerExit(Collider other)
    {
        //Debug.Log($"TriggerExit {other.name}");
        GrabMove grab = other.GetComponent<GrabMove>();
        if (!grab) return;
        hovering = null;
    }

    private void OnValidate()
    {
        if (!collider) collider = GetComponent<SphereCollider>();
        UpdateCollider();
    }


    private void UpdatePositions()
    {
        if (Hand == LeftRight.Left)
        {
            transform.localPosition = controls.VR.LeftPosition.ReadValue<Vector3>() + Offset;
            transform.localRotation = controls.VR.LeftRotation.ReadValue<Quaternion>();
            GripStart = controls.VR.GripLeft.WasPressedThisFrame();
            Gripping = controls.VR.GripLeft.IsPressed();
        }
        if (Hand == LeftRight.Right)
        {
            transform.localPosition = controls.VR.RightPosition.ReadValue<Vector3>() + Offset;
            transform.localRotation = controls.VR.RightRotation.ReadValue<Quaternion>();
            GripStart = controls.VR.GripRight.WasPressedThisFrame();
            Gripping = controls.VR.GripRight.IsPressed();
        }
    }

    private void UpdateButtons()
    {
        if (!holding) return;
        if (Hand == LeftRight.Left)
        {
            holding.triggerPressed = controls.VR.TriggerLeft.IsPressed();
            if (holdTrigger)
            {
                holdTrigger.Pressed = controls.VR.TriggerLeft.IsPressed();
                if (controls.VR.TriggerLeft.WasPressedThisFrame()) holdTrigger.DoPress();
            }
        }
        if (Hand == LeftRight.Right)
        {
            holding.triggerPressed = controls.VR.TriggerRight.IsPressed();
            if (holdTrigger)
            {
                holdTrigger.Pressed = controls.VR.TriggerRight.IsPressed();
                if (controls.VR.TriggerRight.WasPressedThisFrame()) holdTrigger.DoPress();
            }
        }
    }

    private void UpdateColor()
    {
        HandColor = Color.white;
        if (Gripping) HandColor = Color.red;
        if (hovering) HandColor = Color.yellow;
        if (holding) HandColor = Color.green;

        if (handMaterial) handMaterial.color = HandColor;
    }

    private void LockGrabbingHand()
    {
        if (!HandObject) return;
        if (!holding)
        {
            HandObject.transform.localPosition = Vector3.zero;
            return;
        }
        HandObject.transform.position = holding.transform.position;

    }


    private void UpdateCollider()
    {
        if (collider) collider.radius = GrabTriggerRadius;
        if (TriggerArea)
        {
            if (Gripping)
            {
                TriggerArea.localScale = Vector3.zero;
            }
            else
            {
                TriggerArea.localScale = Vector3.one * GrabTriggerRadius * 2;
            }
        }
    }

}


