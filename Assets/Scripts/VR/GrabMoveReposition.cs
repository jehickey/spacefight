using UnityEngine;

public class GrabMoveReposition : GrabMove
{
    public bool AllowReposition = true;
    public bool AllowRotation = false;
    public float moveSpeed = 1f;
    public float rotateSpeed = 1f;
    public float rotateAngleDeg = 5f;


    public Vector3 originalGrabPosition = Vector3.zero;
    public Vector3 originalPosition = Vector3.zero;
    public Vector3 newPosition = Vector3.zero;
    public Transform moveObject;
    public Quaternion originalRotation;
    public Quaternion rotationDelta;

    public event System.Action OnPositionChanged;

    public bool Moved = false;

    private Grabber grabber;

    protected override void Start()
    {
        base.Start();
        if (!moveObject) moveObject = transform.parent;
    }


    protected override void LateUpdate()
    {
        base.LateUpdate();
        if (grabber)
        {
            //Delta = grabber.transform.position - originalGrabPosition;
            //newPosition = originalPosition + Delta;
            //moveObject.position = newPosition;

            //Vector3 currentGrabPosition = moveObject.parent.InverseTransformPoint(grabber.transform.position);
            //Delta = currentGrabPosition - originalGrabPosition;
            //moveObject.localPosition = originalPosition + Delta;

            if (AllowReposition)
            {
                Vector3 grabLocal = moveObject.parent.InverseTransformPoint(grabber.transform.position);

                // Compute delta relative to the original offset
                Delta = (grabLocal - originalGrabPosition) - originalPosition;
                Delta *= moveSpeed;

                // Apply movement
                moveObject.localPosition = originalPosition + Delta;
            }


            if (AllowRotation)
            {
                //Quaternion currentLocal = Quaternion.Inverse(moveObject.rotation) * grabbers[0].transform.rotation;
                //rotationDelta = currentLocal * Quaternion.Inverse(originalRotation);
                //moveObject.rotation = rotationDelta * moveObject.rotation;

                // Current grabber rotation in parent-local space
                Quaternion grabLocal = Quaternion.Inverse(moveObject.parent.rotation) * grabbers[0].transform.rotation;

                // Compute the desired object rotation
                Quaternion targetLocalRotation = grabLocal * Quaternion.Inverse(originalRotation);

                //slow rotation down by rotateSpeed
                targetLocalRotation = Quaternion.Slerp(moveObject.localRotation,targetLocalRotation, rotateSpeed);
                Quaternion rotationDelta = targetLocalRotation * Quaternion.Inverse(moveObject.localRotation);

                //quantize rotation
                if (rotateAngleDeg > 0)
                {
                    Vector3 euler = targetLocalRotation.eulerAngles;
                    euler.x = SnapAngle(euler.x, rotateAngleDeg);
                    euler.y = SnapAngle(euler.y, rotateAngleDeg);
                    euler.z = SnapAngle(euler.z, rotateAngleDeg);
                    targetLocalRotation = Quaternion.Euler(euler);
                }

                // Apply it
                moveObject.localRotation = targetLocalRotation;

            }

        }
    }


    public override void DoGrab(Grabber grabbedBy)
    {
        base.DoGrab(grabbedBy);
        /*
        grabber = grabbedBy;
        originalGrabPosition = grabber.transform.position;
        if (moveObject) originalPosition = moveObject.position;
        newPosition= originalPosition;      //just in case there's a problem
        */
        /*
        grabber = grabbedBy;
        originalGrabPosition = moveObject.parent.InverseTransformPoint(grabber.transform.position);
        originalPosition = moveObject.localPosition;
        */
        grabber = grabbedBy;

        Vector3 grabLocal = moveObject.parent.InverseTransformPoint(grabber.transform.position);
        Vector3 objectLocal = moveObject.localPosition;

        // Store the offset between grabber and object
        originalGrabPosition = grabLocal - objectLocal;

        // Store the object’s starting position
        originalPosition = objectLocal;


        //get rotation info
        Quaternion rotLocal = Quaternion.Inverse(moveObject.parent.rotation) * grabbedBy.transform.rotation;
        // Store the object’s local rotation
        Quaternion rotObjectLocal = moveObject.localRotation;
        // Store the offset between grabber and object
        originalRotation = Quaternion.Inverse(rotObjectLocal) * rotLocal;

        //originalRotation = Quaternion.Inverse(moveObject.rotation) * grabbedBy.transform.rotation;


    }

    public override void DoRelease(Grabber grabbedBy)
    {
        base.DoRelease(grabbedBy);
        //if (moveObject) moveObject.position = newPosition;
        grabber= null;
        OnPositionChanged?.Invoke();
    }

    private float SnapAngle(float angle, float increment)
    {
        return Mathf.Round(angle / increment) * increment;
    }


}
