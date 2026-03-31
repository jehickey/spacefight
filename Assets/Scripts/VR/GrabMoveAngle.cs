using System.Collections.Generic;
using UnityEngine;

public class GrabMoveAngle : GrabMove
{
    //public Vector3 movementDir = Vector3.zero;
    //public Vector3 Delta;

    protected override void LateUpdate()
    {
        base.LateUpdate();
        GetGrabberDelta();
    }

    private void OnDrawGizmos()
    {
        //if (movementDir.magnitude > 0)
        {
            //movementDir.Normalize();
            float gizmoLength = .1f;
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(transform.position, gizmoLength * .25f);
            //Gizmos.DrawLine(transform.position, transform.position + transform.TransformDirection(movementDir) * .25f);
            //Gizmos.DrawLine(transform.position, transform.position - transform.TransformDirection(movementDir) * .25f);
            Gizmos.DrawLine(transform.position, transform.position + Delta *  gizmoLength);
        }
    }

    private void GetGrabberDelta()
    {
        if (grabbers.Count == 0)
        {
            Delta = Vector3.zero;
            return;
        }

        //movementDir.Normalize();
        Vector3 worldDelta = GetGrabberPosition() - transform.position;
        //Delta = transform.InverseTransformDirection(worldDelta);
        //worldDelta.y = transform.position.y;
        //worldDelta -= Vector3.Project(worldDelta, transform.up);

        //Delta = transform.InverseTransformDirection(GetGrabberPosition());
        Delta = transform.InverseTransformPoint(GetGrabberPosition());
        linearDelta = Delta.magnitude;

        //linearDelta = Vector3.Dot(localDelta, movementDir);

    }


}
