using System.Collections.Generic;
using UnityEngine;

public class BotControl : MonoBehaviour
{
    private Ship ship;

    //public Vector3 TargetLocation = Vector3.zero;
    public GameObject TargetObject;
    public float DistanceToTarget = 0f;
    public float AngleToTarget = 0f;
    public bool FireOnTarget = false;
    public float FiringRange = .5f;
    public float FiringAngle = 10f;
    public List<Waypoint> Waypoints = new List<Waypoint>();

    public float ThrottleUpMaxAngle = 90f;
    public float ThrottleDefault = .5f;
    public float BreakoffDistance = 1f;
    public float ThrottleAimBias = .5f;     //0=aggressive, 1=gentle


    void Start()
    {
        ship = GetComponent<Ship>();
        if (!ship) Debug.LogError("BotControl can't find a Ship to control");
    }

    void Update()
    {
        ship ??= GetComponent<Ship>();
        if (!ship) return;
        //simple throttle forward
        //ship.SetThrottle(1f);

        //if (Waypoints.Count == 0) GetWaypoints();
        //if no waypoint, pick one at random
        if (!TargetObject)
        {
            DistanceToTarget = 0;
            PickTarget();
            //PickRandomWaypoint();
        }

        PursueTarget();
        AdjustThrottle();
        AttackTarget();

    }


    private void OnDrawGizmos()
    {
        if (TargetObject)
        {
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(TargetObject.transform.position, 1f);
            Gizmos.DrawLine(transform.position, TargetObject.transform.position);
        }
        if (FireOnTarget)
        {
            Gizmos.color = Color.yellow;
            //Gizmos.DrawWireSphere(transform.position, FiringRange);
            Gizmos.DrawLine(transform.position, transform.position + transform.forward * FiringRange);
        }

    }

    void GetWaypoints()
    {
        GameObject wpObj = GameObject.Find("Waypoints");
        if (!wpObj) return;
        foreach (Waypoint wp in GameObject.FindObjectsByType<Waypoint>(FindObjectsSortMode.None))
        {
            Waypoints.Add(wp);
        }
    }

    void PickTarget()
    {
        //Waypoint[] targets = GameObject.FindObjectsByType<type>(FindObjectsSortMode.None);
        var targets = GameObject.FindObjectsByType<Ship>(FindObjectsSortMode.None);
        if (targets.Length == 0) return;
        TargetObject = targets[Random.Range(0, targets.Length)].gameObject;

    }

    void PickRandomWaypoint()
    {
        if (Waypoints.Count == 0) return;

        Waypoint target = null;

        while (target == null || target == TargetObject)
        {
            target = Waypoints[Random.Range(0, Waypoints.Count)];
        }
        if (target) TargetObject = target.gameObject;
    }


    void PursueTarget()
    {
        if (!TargetObject) return;
        Vector3 toTarget = (TargetObject.transform.position - transform.position).normalized;
        DistanceToTarget = Vector3.Distance(transform.position, TargetObject.transform.position);

        //convert to local space so roll is automatically accounted for
        Vector3 localDir = transform.InverseTransformDirection(toTarget);
        AngleToTarget = Vector3.Angle(Vector3.forward, localDir);

        float yaw = Mathf.Clamp(localDir.x, -1f, 1f);
        float pitch = Mathf.Clamp(localDir.y, -1f, 1f);

        //steering is proportional to angle to target
        float steerScale = Mathf.Clamp01(AngleToTarget / 90f);
        yaw *= steerScale;
        pitch *= steerScale;

        //signed roll angle around the forward axis
        float rollTurn = -yaw * 0.75f;      //roll a bit into the turn
        float rollAngle = Vector3.SignedAngle(transform.up, Vector3.up, transform.forward);
        float rollLevel = Mathf.Clamp(rollAngle / 45f, -1f, 1f);

        //bias rolling on turns, turn upward when not turning
        float turnAmount = Mathf.Abs(yaw);
        float roll = Mathf.Lerp(rollLevel, rollTurn, turnAmount);

        ship.Stick = new Vector3(yaw, roll, -pitch);

        if (DistanceToTarget < BreakoffDistance)
        {
            TargetObject = null;
        }
    }

    void AdjustThrottle()
    {
        ship.Throttle = ThrottleDefault;
        if (!TargetObject) return;
        if (AngleToTarget > ThrottleUpMaxAngle) return;
        float angNorm = 1f - (AngleToTarget / ThrottleUpMaxAngle);
        ship.Throttle = ThrottleDefault + (1f-ThrottleDefault) * Mathf.Pow(angNorm, ThrottleAimBias);
    }

    void AttackTarget()
    {
        if (!ship) return;
        if (!TargetObject) return;
        if (!FireOnTarget) return;
        if (DistanceToTarget > FiringRange) return;
        if (AngleToTarget > FiringAngle) return;
        ship.Fire();
        Debug.Log("Shoot!");
    }


}


