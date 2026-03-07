using UnityEngine;

public class ThrottleBox : MonoBehaviour
{
    public float InputPosition = 0;

    public float NeutralPosition = .5f;
    public float NeutralDriftRate = .5f;
    public bool Boost = false;

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


    private ThrottleSystem throttleSystem;

    void Start()
    {
        
    }

    private void OnEnable()
    {
        if (!throttleSystem) throttleSystem = GetComponentInParent<ThrottleSystem>();
    }

    void Update()
    {
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
