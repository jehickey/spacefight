using UnityEngine;

public class GrabMoveRotation : GrabMove
{
    private Quaternion initialObjectRot;
    private Quaternion originalRotation;
    public Quaternion rotationDelta;
    public Transform ReferenceTransform;    //used for stable directional reference (or it uses this object)

    //object stays in position but registers angle of hand
    //angle is always relative to base angle
    //base angle is whatever the angle was at start of grab

    protected override void Start()
    {
        base.Start();
        //originalRotation = transform.localRotation;

        //default reference transform to self if not set
        if (!ReferenceTransform) ReferenceTransform = transform;
    }

    protected override void LateUpdate()
    {
        base.LateUpdate();
        if (Grabbed)
        {
            //rotationDelta = GetGrabberRotation() * Quaternion.Inverse(originalRotation);
            Quaternion currentLocal = Quaternion.Inverse(ReferenceTransform.rotation) * grabbers[0].transform.rotation;
            rotationDelta = currentLocal * Quaternion.Inverse(originalRotation);
            //transform.rotation = offsetRotation * initialObjectRot;
        }

    }

    public override void DoGrab(Grabber grabbedBy)
    {
        base.DoGrab(grabbedBy);
        //initialObjectRot = transform.rotation;
        //originalRotation = grabbedBy.transform.localRotation;
        originalRotation = Quaternion.Inverse(ReferenceTransform.rotation) * grabbedBy.transform.rotation;
    }
}
