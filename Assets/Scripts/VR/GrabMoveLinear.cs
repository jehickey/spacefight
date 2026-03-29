using System.Collections.Generic;
using UnityEngine;

public class GrabMoveLinear : GrabMove
{
    public Vector3 movementDir = Vector3.zero;
    //public float linearDelta;

    protected override void Update()
    {
        base.Update();
        GetGrabberDelta();   
    }

    private void OnDrawGizmos()
    {
        if (movementDir.magnitude > 0)
        {
            movementDir.Normalize();
            float gizmoLength = .1f;
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(transform.position, gizmoLength * .25f);
            Gizmos.DrawLine(transform.position, transform.position+ transform.TransformDirection(movementDir) *.25f);
            Gizmos.DrawLine(transform.position, transform.position - transform.TransformDirection(movementDir) * .25f);
        }
    }


    private void GetGrabberDelta()
    {
        if (grabbers.Count==0)
        {
            linearDelta = 0;
            return;
        }
        movementDir.Normalize();
        Vector3 worldDelta = GetGrabberPosition() - transform.position;
        Delta = transform.InverseTransformDirection(worldDelta);
        linearDelta = Vector3.Dot(Delta, movementDir);

    }


}
