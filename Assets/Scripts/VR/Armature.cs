using System.Timers;
using Unity.VisualScripting;
using UnityEngine;

public class Armature : MonoBehaviour
{
    public Grabber grabber;
    public Transform handJoint;

    [Header("Bones")]
    public Transform shoulder;   // Root, fixed position, rotatable
    public Transform elbow;      // Mid joint
    public Transform wrist;      // End joint (hand base)

//    [Header("Target")]
    public Transform target;     // Desired hand pose

    [Header("Smoothing")]
    public float posSmooth = 20f;
    public float rotSmooth = 20f;

    private Vector3 smoothedPos;
    private Quaternion smoothedRot;

    [Header("Joint Limits")]
    public float minElbowDeg = 5f;     // Slight bend even when extended
    public float maxElbowDeg = 130f;   // Deep bend limit

    public Vector3 wristMinAngles = new Vector3(-45, -30, -60);
    public Vector3 wristMaxAngles = new Vector3(45, 30, 60);

    [Header("Elbow Bend Direction")]
    public Vector3 elbowHintLocal = new Vector3(-0.2f, -1f, 0f);
    // Down + slightly inward (chicken-like)


    [Header("Hand Grip")]
    public Transform Finger1;
    public Transform Finger2;
    public Transform Finger3;
    public Transform Thumb;
    public float PointLevel = 0;
    public float GripLevel = 0;
    private float actualGripLevel = 0;
    public float GripLevelRate = .5f;       //time to complete per second
    public float MinGripAngle = 0;
    public float MaxGripAngle = 15;
    public float FingerLevelFactor = 1.25f;
    public bool GripTestMode = false;
    private Quaternion finger1BaseRot;
    private Quaternion finger2BaseRot;
    private Quaternion finger3BaseRot;
    private Quaternion thumbBaseRot;


    private float upperLen;
    private float lowerLen;
    private Vector3 previousHint;

    private Transform visibleHand;

    void Start()
    {
        upperLen = Vector3.Distance(shoulder.position, elbow.position);
        lowerLen = Vector3.Distance(elbow.position, wrist.position);

        //smoothedPos = target.position;
        //smoothedRot = target.rotation;
        //smoothedPos = grabber.transform.position;
        //smoothedRot = grabber.transform.rotation;
        if (grabber && grabber.HandObject) visibleHand = grabber.HandObject.transform;
        if (Finger1) finger1BaseRot = Finger1.transform.localRotation;
        if (Finger2) finger2BaseRot = Finger2.transform.localRotation;
        if (Finger3) finger3BaseRot = Finger3.transform.localRotation;
        if (Thumb) thumbBaseRot = Thumb.transform.localRotation;

    }

    void LateUpdate()
    {
        if (handJoint && grabber)
        {
            //handJoint.position = grabber.transform.position;
            //handJoint.rotation = grabber.transform.rotation;
            target = visibleHand;// grabber.transform;

            //AdjustHand();   //may do this before solver

            //SmoothTarget();
            SolveIK();
            UpdateGrip();
            AdjustHand();
        }
    }


    void UpdateGrip()
    {
        if (!GripTestMode) GripLevel = (grabber.Gripping ? 1 : 0);
        GripLevel = Mathf.Clamp01(GripLevel);

        actualGripLevel = Mathf.MoveTowards(actualGripLevel, GripLevel, GripLevelRate * Time.deltaTime);

        GripFinger(Finger1, finger1BaseRot, actualGripLevel);
        GripFinger(Finger2, finger2BaseRot, actualGripLevel);
        GripFinger(Finger3, finger3BaseRot, actualGripLevel);
        GripFinger(Thumb, thumbBaseRot, -actualGripLevel);
    }

    void GripFinger(Transform fingerSeg, Quaternion baseRot, float level)
    {
        if (fingerSeg == null) return;
        fingerSeg.localRotation = baseRot * Quaternion.AngleAxis(MaxGripAngle * level, Vector3.right);
        Quaternion zeroBase = Quaternion.Euler(0, 0, 0);
        foreach (Transform t in fingerSeg.transform)
        {
            GripFinger(t, zeroBase, +level*FingerLevelFactor);
        }
    }


    void AdjustHand()
    {
        //adjust hand to offset
        if (visibleHand)
        {
            target.position += visibleHand.position;
            wrist.rotation = target.rotation;

            //rotate hand to offset
            /*
            Quaternion offset =
                Quaternion.AngleAxis(handRotationFlex, target.right) *
                Quaternion.AngleAxis(handRotationSideways, target.up) *
                Quaternion.AngleAxis(handRotationAxial, target.forward);
                */
        }

    }


    void SmoothTarget()
    {
        smoothedPos = Vector3.Lerp(smoothedPos, grabber.transform.position, Time.deltaTime * posSmooth);
        smoothedRot = Quaternion.Slerp(smoothedRot, grabber.transform.rotation, Time.deltaTime * rotSmooth);
        //smoothedPos = grabber.transform.position;
        //smoothedRot = grabber.transform.rotation;
    }


    void SolveIK()
    {
        //get arm lengths
        float upperLen = (elbow.position - shoulder.position).magnitude;
        float lowerLen = (wrist.position - elbow.position).magnitude;
        float targetLen = (target.position - shoulder.position).magnitude;

        //work out elbow angle (law of cosines)
        float elbowCosine = (upperLen*upperLen + lowerLen * lowerLen - targetLen * targetLen) / (2 * upperLen * lowerLen);
        elbowCosine = Mathf.Clamp(elbowCosine, -1, 1);
        float elbowVertical = Mathf.Acos(-elbowCosine) * Mathf.Rad2Deg * .5f;

        Vector3 shoulderToTarget = target.position - shoulder.position;
        float shoulderCosine = (upperLen * upperLen + targetLen * targetLen - lowerLen * lowerLen) / (2 * upperLen * targetLen);
        shoulderCosine = Mathf.Clamp(shoulderCosine, -1, 1);

        //base pitch toward target
        float flat = new Vector2(shoulderToTarget.x, shoulderToTarget.z).magnitude;
        float alpha = Mathf.Atan2(shoulderToTarget.y, flat);   // radians

        //triangle correction
        float beta = Mathf.Acos(shoulderCosine);               // radians

        //final shoulder pitch - subtract alpha from beta to bias down, add to bias up
        float shoulderVertical = (-(alpha - beta) * Mathf.Rad2Deg);

        float shoulderHorizontal = Mathf.Atan2(shoulderToTarget.x, shoulderToTarget.z) * Mathf.Rad2Deg;

        //Debug.Log($"{shoulderVertical} / {shoulderHorizontal}"); 
        shoulder.localEulerAngles = new Vector3(shoulderVertical, 0, -shoulderHorizontal);

        //aim forearm at wrist
        Vector3 elbowToTarget = (target.position - elbow.position).normalized;
        elbow.up = elbowToTarget;
        //elbow.rotation = Quaternion.LookRotation(elbowToTarget, shoulder.right);

        //correct elbow rotation
        Vector3 elbowEuler = elbow.localEulerAngles;
        elbowEuler.y = 0;
        //elbow.localEulerAngles = elbowEuler;

    }


}


