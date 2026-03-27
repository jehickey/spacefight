using System.Collections.Generic;
using UnityEngine;

public class GrabMoveLinear : MonoBehaviour
{
    public Vector3 movementDir = Vector3.zero;
    public float linearDelta;
    private List<Grabber> grabbers = new List<Grabber>();

    void Update()
    {
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

    public void Grabbed(Grabber grabbedBy)
    {
        if (!grabbers.Contains(grabbedBy)) grabbers.Add(grabbedBy);
    }

    public void Released(Grabber grabbedBy)
    {
        if (grabbers.Contains(grabbedBy)) grabbers.Remove(grabbedBy);
    }

    private void GetGrabberDelta()
    {
        if (grabbers.Count==0)
        {
            linearDelta = 0;
            return;
        }

        //get averaged grabcount position
        Vector3 grabPos = grabbers[0].transform.position;
        if (grabbers.Count > 1)
        {
            for (int i = 1; i < grabbers.Count; i++)
            {
                grabPos += grabbers[i].transform.position;
            }
            grabPos /= grabbers.Count;
        }

        movementDir.Normalize();
        Vector3 worldDelta = grabPos - transform.position;
        Vector3 localDelta = transform.InverseTransformDirection(worldDelta);
        linearDelta = Vector3.Dot(localDelta, movementDir);

    }


}
