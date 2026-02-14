using System.Collections.Generic;
using UnityEngine;

public class BotControl : MonoBehaviour
{
    private Ship ship;

    public Vector3 TargetLocation = Vector3.zero;
    public float DistanceToTarget = 0f;
    public float AngleToTarget = 0f;
    public List<Vector3> Waypoints = new List<Vector3>();


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
        ship.SetThrottle(1f);

        if (Waypoints.Count == 0) GetWaypoints();
        //if no waypoint, pick one at random
        if (TargetLocation == Vector3.zero)
        {
            DistanceToTarget = 0;
            PickRandomWaypoint();
        }
        else
        {

            if (TargetLocation != Vector3.zero)
            {
                Vector3 toTarget = (TargetLocation - transform.position).normalized;
                DistanceToTarget = Vector3.Distance(transform.position, TargetLocation);

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

                if (DistanceToTarget < 3f)
                {
                    PickRandomWaypoint();
                }
            }
        }
    }


    private void OnDrawGizmos()
    {
        Gizmos.color = Color.red;
        if (TargetLocation != Vector3.zero)
        {
            Gizmos.DrawSphere(TargetLocation, 1f);
        }

    }

    void GetWaypoints()
    {
        GameObject wpObj = GameObject.Find("Waypoints");
        if (!wpObj) return;
        foreach (Transform child in wpObj.transform)
        {
            Waypoints.Add(child.position);
        }
    }

    void PickRandomWaypoint()
    {
        if (Waypoints.Count == 0) return;

        Vector3 target = Vector3.zero;

        while (target == Vector3.zero || target == TargetLocation)
        {
            target = Waypoints[Random.Range(0, Waypoints.Count)];
        }
        TargetLocation= target;
    }

}
