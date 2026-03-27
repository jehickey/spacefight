using System.Runtime.CompilerServices;
using UnityEngine;
using UnityEngine.UIElements;

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
    public GrabMoveLinear hovering;
    [ReadOnly]
    public GrabMoveLinear holding;

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
    private SphereCollider trigger;

    public enum LeftRight
    {
        Left,
        Right
    }


    void Start()
    {
        if (controls == null) controls = new FlightControls();
        controls.Enable();

        trigger = GetComponent<SphereCollider>();
        trigger.isTrigger = true;

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

    void Update()
    {
        bool wasGripping = Gripping;
        GripStart = false;
        UpdatePositions();
        if (GripStart) Debug.Log("Grip Start");
        //if (Gripping) Debug.Log("Gripping");
        if (wasGripping && !Gripping) Debug.Log("Grip Release");

        if (GripStart && hovering)
        {
            if (holding) Debug.Log($"Trying to hold while already holding \"{holding.name}\"");
            if (!holding)
            {
                holding = hovering;
                holding.Grabbed(this);
                //Debug.Log($"Grabbing {holding.name}");
            }
        }

        if (!Gripping && holding)
        {
            //Debug.Log($"Releasing {holding.name}");
            holding.Released(this);
            holding = null;
        }

        UpdateColor();
        LockGrabbingHand();
        UpdateTrigger();

    }


    private void OnTriggerEnter(Collider other)
    {
        //Debug.Log($"TriggerEnter {other.name}");
        GrabMoveLinear grab = other.GetComponent<GrabMoveLinear>();
        if (!grab) return;
        if (!holding) hovering = grab;
    }

    private void OnTriggerExit(Collider other)
    {
        //Debug.Log($"TriggerExit {other.name}");
        GrabMoveLinear grab = other.GetComponent<GrabMoveLinear>();
        if (!grab) return;
        hovering = null;
    }

    private void OnValidate()
    {
        if (!trigger) trigger = GetComponent<SphereCollider>();
        UpdateTrigger();
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


    private void UpdateTrigger()
    {
        if (trigger) trigger.radius = GrabTriggerRadius;
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


