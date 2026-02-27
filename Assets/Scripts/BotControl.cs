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

    public float precisionAngle = 5f; // degrees

    private ThrottleSystem throttle;
    private SteeringSystem steering;

    public class ThreatEntry
    {
        public Ship ship;
        public float score;
        public float distance;
        public float aspectAngle;   //relative direction (unsigned) - 0=heading towards, 180=heading away

        public ThreatEntry(Ship threatShip)
        {
            ship = threatShip;
            score = 0;
            distance = 0;
            aspectAngle = 0;
        }
    }
    public List<ThreatEntry> threats = new List<ThreatEntry>();


    void Start()
    {
    }

    private void OnEnable()
    {
        ship = GetComponent<Ship>();
        if (!ship) Debug.LogError("BotControl can't find a Ship to control");
        throttle = GetComponentInChildren<ThrottleSystem>();
        steering = GetComponentInChildren<SteeringSystem>();

    }

    void Update()
    {
        ship ??= GetComponent<Ship>();
        if (!ship) return;

        UpdateThreats();

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
        if (ship && ship.team)
        {
            Gizmos.color = ship.team.color;
            Gizmos.DrawWireSphere(transform.position, 0.25f);
        }

        if (TargetObject)
        {
            Gizmos.color = Color.red * .5f;
            if (ship.IsFiring) Gizmos.color = Color.red;
            //Gizmos.DrawWireSphere(TargetObject.transform.position, 1f);
            Vector3 fireDir = (transform.forward-TargetObject.transform.position).normalized;
            Gizmos.DrawLine(transform.position, transform.position + fireDir*FiringRange);
        }
        if (TargetObject && FireOnTarget)
        {
            Gizmos.color = Color.yellow;
            //Gizmos.DrawWireSphere(transform.position, FiringRange);
            //Gizmos.DrawLine(transform.position, transform.position + transform.forward * throttle.Thrust);
            Gizmos.DrawLine(transform.position, transform.position + ship.Velocity*Time.deltaTime);
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
        //var targets = GameObject.FindObjectsByType<Ship>(FindObjectsSortMode.None);
        //if (targets.Length == 0) return;
        //TargetObject = targets[Random.Range(0, targets.Length)].gameObject;

        if (threats.Count == 0) return;
        TargetObject = threats[0].ship.gameObject;

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
        /*
        float steerScale = Mathf.Clamp01(AngleToTarget / 90f);
        yaw *= steerScale;
        pitch *= steerScale;
        */
        
        if (AngleToTarget < precisionAngle)
        {
            float t = AngleToTarget / precisionAngle; // 0-1
            float precisionScale = Mathf.Lerp(0.5f, 1f, t);
            yaw *= precisionScale;
            pitch *= precisionScale;
        }



        //signed roll angle around the forward axis
        float rollTurn = -yaw * 0.75f;      //roll a bit into the turn
        float rollAngle = Vector3.SignedAngle(transform.up, Vector3.up, transform.forward);
        float rollLevel = Mathf.Clamp(rollAngle / 45f, -1f, 1f);

        //bias rolling on turns, turn upward when not turning
        float turnAmount = Mathf.Abs(yaw);
        float roll = Mathf.Lerp(rollLevel, rollTurn, turnAmount);

        steering.Stick = new Vector3(yaw, roll, -pitch);

        if (DistanceToTarget < BreakoffDistance)
        {
            TargetObject = null;
        }
    }

    void AdjustThrottle()
    {
        throttle.Input = ThrottleDefault;
        if (!TargetObject) return;
        if (AngleToTarget > ThrottleUpMaxAngle) return;
        float angNorm = 1f - (AngleToTarget / ThrottleUpMaxAngle);
        throttle.Input = ThrottleDefault + (1f-ThrottleDefault) * Mathf.Pow(angNorm, ThrottleAimBias);
    }

    void AttackTarget()
    {
        if (!ship) return;
        if (!TargetObject) return;
        if (!FireOnTarget) return;
        if (DistanceToTarget > FiringRange) return;
        if (AngleToTarget > FiringAngle) return;
        ship.Fire();
    }


    void UpdateThreats()
    {

        threats.Clear();
        if (!ship.team) return;
        foreach (Ship threat in ship.team.Threats)
        {
            if (threat == ship) continue;
            if (threat == null) continue;
            ThreatEntry entry = new ThreatEntry(threat);
            AssessThreat(entry);
            threats.Add(entry);
        }
        threats.Sort((a,b) => b.score.CompareTo(a.score));   //highest score first
    }

    //Examine a specific threat to collect data and score it
    void AssessThreat (ThreatEntry threat)
    {
        if (threat == null) return;
        if (threat.ship == null) return;
        //get distance to threat
        threat.distance = Vector3.Distance(transform.position, threat.ship.transform.position);
        if (threat.distance==0) threat.distance = .001f;    //apply a safe minimum

        //get aspect angle to threat
        Vector3 toObserver = (transform.position - threat.ship.transform.position).normalized;
        float dot = Vector3.Dot(threat.ship.transform.forward, toObserver);
        threat.aspectAngle = Mathf.Acos(dot) * Mathf.Rad2Deg;
        //threat.score = threat.distance + threat.aspectAngle * 0.5f - threat.ship.team.ShipsKilled * 0.25f;
        threat.score = 1f / threat.distance;
    }

}


