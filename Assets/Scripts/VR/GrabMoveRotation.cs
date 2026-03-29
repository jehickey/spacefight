using UnityEngine;

public class GrabMoveRotation : GrabMove
{
    private Quaternion initialObjectRot;
    private Quaternion originalRotation;
    public Quaternion rotationDelta;

    //object stays in position but registers angle of hand
    //angle is always relative to base angle
    //base angle is whatever the angle was at start of grab

    protected override void Start()
    {
        base.Start();
        //originalRotation = transform.localRotation;
    }

    protected override void Update()
    {
        base.Update();
        if (Grabbed)
        {
            rotationDelta = GetGrabberRotation() * Quaternion.Inverse(originalRotation);
            //transform.rotation = offsetRotation * initialObjectRot;
        }

    }

    public override void DoGrab(Grabber grabbedBy)
    {
        base.DoGrab(grabbedBy);
        initialObjectRot = transform.rotation;
        originalRotation = grabbedBy.transform.localRotation;
    }
}
