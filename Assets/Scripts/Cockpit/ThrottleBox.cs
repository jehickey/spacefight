using UnityEngine;

public class ThrottleBox : MonoBehaviour
{
    public float ThrowPosition = 0;
    public Transform ThrottleBar;
    public Vector3 ThrowCenter = Vector3.zero;
    public Vector3 ThrowAxis = Vector3.forward;
    public float ThrowMin = -1.0f;
    public float ThrowMax = 1.0f;

    void Start()
    {
        
    }

    void Update()
    {
        UpdateThrowbar();

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
        ThrowPosition = Mathf.Clamp(ThrowPosition, 0f, 1.0f);
        //float relPos = Mathf.InverseLerp(0,1, ThrowPosition);
        float pos = Mathf.Lerp(ThrowMin, ThrowMax, ThrowPosition);
        //ThrowAxis = transform.forward;
        Vector3 center = ThrowCenter;
        Vector3 throwPos = center + ThrowAxis.normalized * pos;
        ThrottleBar.localPosition = throwPos;
    }

}
