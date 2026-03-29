using UnityEngine;

public class ThrottleBox : MonoBehaviour
{
    public float InputPosition = 0;

    public float NeutralPosition = .5f;
    public float NeutralDriftRate = .5f;
    public bool Boost = false;
    public float GrabPushRate = 2f;
    public bool TriggerPressed = false;      //need a timeout for this if it's not reinforced regularly


    [SerializeField]
    private Vector3 ThrowCenter = Vector3.zero;
    [SerializeField]
    private Vector3 ThrowAxis = Vector3.forward;
    [SerializeField]
    private float ThrowMin = -1.0f;
    [SerializeField]
    private float ThrowMax = 1.0f;

    [SerializeField]
    private Transform ThrottleBar;

    private GrabMoveLinear grabHandle;
    private GrabTrigger trigger;

    private ThrottleSystem throttleSystem;

    void Start()
    {
        
    }

    private void OnEnable()
    {
        if (!throttleSystem) throttleSystem = GetComponentInParent<ThrottleSystem>();
        if (!grabHandle) grabHandle = GetComponentInChildren<GrabMoveLinear>();
        if (!trigger) trigger = GetComponentInChildren<GrabTrigger>();
    }

    void Update()
    {
        //accept input from grabber
        if (grabHandle)
        {
            InputPosition += grabHandle.linearDelta * GrabPushRate * Time.deltaTime;
        }

        if (trigger)
        {
            Boost = trigger.Pressed;
            //if (trigger.Pressed) Debug.Log("Boost!");
        }

        //neutral drift
        InputPosition = Mathf.Lerp(InputPosition, NeutralPosition, Time.deltaTime * NeutralDriftRate);

        UpdateThrowbar();
        if (throttleSystem)
        {
            throttleSystem.Input = InputPosition;
            throttleSystem.Boost = Boost;
        }



    }

    private void OnValidate()
    {
        UpdateThrowbar();
    }

    private void OnDrawGizmos()
    {
        Vector3 center = ThrowCenter;
        Gizmos.color = Color.green;
        Gizmos.DrawSphere(transform.TransformPoint(center), 0.0025f);

        Gizmos.color = Color.red;
        Gizmos.DrawSphere(transform.TransformPoint(center + ThrowAxis.normalized * ThrowMin), 0.001f);
        Gizmos.color = Color.yellow;
        Gizmos.DrawSphere(transform.TransformPoint(center + ThrowAxis.normalized * ThrowMax), 0.001f);

        //Gizmos.DrawLine(transform.position + ThrowCenter, transform.position + ThrowCenter + ThrowAxis.normalized * 0.5f);

    }


    private void UpdateThrowbar()
    {
        if (!ThrottleBar) return;
        InputPosition = Mathf.Clamp(InputPosition, 0f, 1.0f);
        //float relPos = Mathf.InverseLerp(0,1, ThrowPosition);
        float pos = Mathf.Lerp(ThrowMin, ThrowMax, InputPosition);
        //ThrowAxis = transform.forward;
        Vector3 center = ThrowCenter;
        Vector3 throwPos = center + ThrowAxis.normalized * pos;
        ThrottleBar.localPosition = throwPos;
    }

}
