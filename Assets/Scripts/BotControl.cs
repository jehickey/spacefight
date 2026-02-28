using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.UI;
using static UnityEngine.Rendering.DebugUI;

public class BotControl : MonoBehaviour
{
    private Ship ship;

    //public Vector3 TargetLocation = Vector3.zero;
    [Header("Target Information")]
    public GameObject TargetObject;
    [SerializeField, ReadOnly(true)]
    private float distanceToTarget = 0f;
    [SerializeField, ReadOnly(true)]
    private float angleToTarget = 0f;       //Angle of deflection of target from heading (deg: 0-180)
    [SerializeField, ReadOnly(true)]
    private Vector3 vectorToTarget;
    [SerializeField, ReadOnly(true)]
    private Vector3 localTargetDir;

    [Header("Firing Behavior Settings")]
    public bool FireOnTarget = false;
    public float FiringAngle = 10f;
    public List<Waypoint> Waypoints = new List<Waypoint>();

    [Header("Throttle Behavior Settings")]
    public float ThrottleUpMaxAngle = 90f;
    public float ThrottleDefault = .5f;
    public float ThrottleAimBias = .5f;     //0=aggressive, 1=gentle
    public float precisionAngle = 5f; // degrees


    [Header("Distance Baselines")]
    public float baselineRangeBreakoff = .1f;
    public float baselineRangeFiring = 2f;
    public float baselineRangeIdeal = .5f;

    [Header("Dynamic Distances")]
    [SerializeField]
    private float rangeBreakoff = .1f;
    [SerializeField]
    private float rangeFiring = 2f;
    [SerializeField]
    private float rangeIdeal = .5f;

    [Header("Interpretive Distances")]
    [SerializeField]
    private float factorBreakoff;
    [SerializeField]
    private float factorFiring;
    [SerializeField]
    private float factorNearIdeal;
    [SerializeField]
    private float factorInsideIdeal;


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
        /* fuzz testing
        float fuzz = 1f;
        float target = 5;
        for (float i = target-fuzz*2; i <= target+fuzz*2f; i+=fuzz/4f)
        {
            FuzzyRange(i, target, fuzz, +1);
        }
        */
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

        if (steering) steering.Stick = Vector3.zero;

        UpdateThreats();
        GetTargetInfo();    //this should have already been done during threat assessment
        ProcessRanges();

        //if (Waypoints.Count == 0) GetWaypoints();
        //if no waypoint, pick one at random
        if (!TargetObject)
        {
            distanceToTarget = 0;
            PickTarget();
            //PickRandomWaypoint();
        }

        AimAtTarget();
        TurnAway();
        Rolling();
        AdjustThrottle();
        AttackTarget();

        //forget current target (disabled for now)
        if (distanceToTarget < rangeBreakoff)
        {
            //TargetObject = null;
        }


    }


    private void OnDrawGizmos()
    {
        if (ship && ship.team)
        {
            Gizmos.color = ship.team.color;
            //Gizmos.DrawWireSphere(transform.position, 0.25f);
        }

        if (TargetObject)
        {
            Gizmos.color = Color.red * .5f;
            if (ship.IsFiring) Gizmos.color = Color.red;
            //Gizmos.DrawWireSphere(TargetObject.transform.position, 1f);
            Vector3 fireDir = (transform.forward-TargetObject.transform.position).normalized;
            Gizmos.DrawLine(transform.position, transform.position + fireDir*rangeFiring);
        }
        if (TargetObject && FireOnTarget)
        {
            Gizmos.color = Color.yellow;
            //Gizmos.DrawWireSphere(transform.position, FiringRange);
            //Gizmos.DrawLine(transform.position, transform.position + transform.forward * throttle.Thrust);
            Gizmos.DrawLine(transform.position, transform.position + ship.Velocity*Time.deltaTime);
        }

    }

    private void OnDrawGizmosSelected()
    {
        //if (!TargetObject) return;
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, rangeBreakoff);

        Gizmos.color = Color.green;
        Gizmos.DrawWireSphere(transform.position, rangeIdeal);

        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, rangeFiring);

    }

    private void OnValidate()
    {
        ProcessRanges();
    }

    void GetTargetInfo()
    {
        if (!TargetObject)
        {
            distanceToTarget = 0;
            angleToTarget = 0;
            vectorToTarget = Vector3.zero;
            localTargetDir = Vector3.zero;
            return;
        }
        distanceToTarget = Vector3.Distance(transform.position, TargetObject.transform.position);
        vectorToTarget = (TargetObject.transform.position - transform.position).normalized;

        //convert to local space so roll is automatically accounted for
        localTargetDir = transform.InverseTransformDirection(vectorToTarget);

        angleToTarget = Vector3.Angle(Vector3.forward, localTargetDir);
    }


    void ProcessRanges()
    {
        //reset values
        rangeFiring = baselineRangeFiring;
        rangeBreakoff = baselineRangeBreakoff;
        rangeIdeal = baselineRangeIdeal;
        factorBreakoff = 0;
        factorNearIdeal = 0;
        factorInsideIdeal = 0;
        factorFiring = 0;

        //maybe fuzz should be a percentage of value?
        if (!TargetObject) return;
        factorBreakoff = FuzzyRange(distanceToTarget, rangeBreakoff, .2f, -1);
        factorNearIdeal = FuzzyRange(distanceToTarget, rangeIdeal, .2f, 0);
        factorInsideIdeal = FuzzyRange(distanceToTarget, rangeIdeal, .2f, -1);
        factorFiring = FuzzyRange(distanceToTarget, rangeFiring, .2f, -1);

        //firing range should be influenced by projectile speed

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


    void AimAtTarget()
    {
        if (!TargetObject) return;

        //steering is proportional to angle to target
        float precisionScale = 1;
        if (angleToTarget < precisionAngle)
        {
            float t = angleToTarget / precisionAngle; // 0-1
            precisionScale = Mathf.Lerp(0.5f, 1f, t);
        }

        if (factorInsideIdeal>0 && angleToTarget >= 140) precisionScale = 0;


        precisionScale *= (1f-factorBreakoff);
        float yaw = Mathf.Clamp(localTargetDir.x, -1f, 1f) * precisionScale;
        float pitch = Mathf.Clamp(localTargetDir.y, -1f, 1f) * precisionScale;
        if (steering) steering.Stick += new Vector3(yaw, steering.Stick.y, -pitch);
    }

    void TurnAway()
    {
        if (!TargetObject) return;

        //steering is proportional to angle to target
        float precisionScale = 1;
        if (angleToTarget < precisionAngle)
        {
            float t = angleToTarget / precisionAngle; // 0-1
            //precisionScale = Mathf.Lerp(0.5f, 1f, t);
        }

        //strength is proportional to breakoff
        precisionScale *= factorBreakoff * 2f;// * (angleToTarget/180f);   

        float yaw = Mathf.Clamp(-localTargetDir.x, -1f, 1f) * precisionScale;
        float pitch = Mathf.Clamp(-localTargetDir.y, -1f, 1f) * precisionScale;
        if (steering) steering.Stick += new Vector3(yaw, steering.Stick.y, -pitch);
    }

    void Rolling()
    {
        float yaw = steering.Stick.x;
        //signed roll angle around the forward axis
        float rollTurn = -yaw * 0.75f;      //roll a bit into the turn
        float rollAngle = Vector3.SignedAngle(transform.up, Vector3.up, transform.forward);
        float rollLevel = Mathf.Clamp(rollAngle / 45f, -1f, 1f);

        //bias rolling on turns, turn upward when not turning
        float turnAmount = Mathf.Abs(yaw);
        float roll = Mathf.Lerp(rollLevel, rollTurn, turnAmount);
        if (steering) steering.Stick.y = roll;

    }

    void AdjustThrottle()
    {
        throttle.Input = ThrottleDefault;
        if (!TargetObject) return;
        if (angleToTarget > ThrottleUpMaxAngle) return;
        float angNorm = 1f - (angleToTarget / ThrottleUpMaxAngle);
        //angNorm *= 1 - factorBreakoff;
        //if (angNorm < .2f) angNorm = .2f;
        throttle.Input = ThrottleDefault* (1 - factorBreakoff) + (1f-ThrottleDefault) * Mathf.Pow(angNorm, ThrottleAimBias);
    }

    void AttackTarget()
    {
        if (!ship) return;
        if (!TargetObject) return;
        if (!FireOnTarget) return;
        //if (distanceToTarget > rangeFiring) return;
        if (factorFiring <= 0) return;
        if (angleToTarget > FiringAngle) return;
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



    //Bias=0  - result peaks at center of range
    //Bias=-1 - result peaks at bottom of range
    //Bias=+1 - result peaks at top of range
    float FuzzyRange(float value, float targetValue, float fuzz, float bias)
    {
        float min = targetValue - fuzz;
        float max = targetValue + fuzz;

        float result = 0;

        //catch anything beyond ranges
        /*
        if (bias < 0 && value <= min) result=1;
        if (bias > 0 && value >= max) result= 1;
        if (bias == 0 && (value <= min || value >= max)) result= 0;
        */

        if (bias < 0)       //peaks at min
        {
            result = 1f - Mathf.InverseLerp(min, max, value);
        }

        if (bias > 0)       //peaks at max
        {
            result = Mathf.InverseLerp(min, max, value);
        }

        if (bias == 0)
        {
            //peaks at center (targetValue)
            float half = fuzz;
            float delta = Mathf.Abs(value - targetValue);
            result = 1f - Mathf.Clamp01(delta / half);
        }
        //Debug.Log($"{value} ({min}/{max}) b{bias} = {result}");
        return Mathf.Clamp01(result);
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


}


