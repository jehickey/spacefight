using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;

public class GrabMove : MonoBehaviour
{
    public float linearDelta;
    public Vector3 Delta;
    public bool Grabbed = false;



    protected List<Grabber> grabbers = new List<Grabber>();
    protected GrabTrigger grabTrigger;


    public virtual void DoGrab(Grabber grabbedBy)
    {
        if (!grabbers.Contains(grabbedBy)) grabbers.Add(grabbedBy);
        Grabbed = true;
    }

    public virtual void DoRelease(Grabber grabbedBy)
    {
        if (grabbers.Contains(grabbedBy)) grabbers.Remove(grabbedBy);
        if (grabbers.Count == 0) Grabbed = false;
    }


    protected virtual void Start()
    {
        if (!grabTrigger) grabTrigger = GetComponent<GrabTrigger>();
    }

    protected virtual void Update()
    {
        //update button grabbers with grab state (for activation)
        if (grabTrigger) grabTrigger.Grabbed = Grabbed;
    }

    protected Vector3 GetGrabberPosition()
    {
        //get averaged grabcount position
        Vector3 grabPos = Vector3.zero;
        if (grabbers.Count == 1) grabPos = grabbers[0].transform.position;
        if (grabbers.Count > 1)
        {
            for (int i = 1; i < grabbers.Count; i++)
            {
                grabPos += grabbers[i].transform.position;
            }
            grabPos /= grabbers.Count;
        }
        return grabPos;
    }


    protected Quaternion GetGrabberRotation()
    {
        //get averaged grabcount position
        Quaternion sum = Quaternion.identity;
        if (grabbers.Count == 1) sum = grabbers[0].transform.localRotation;
        //ignore other grabbers for now, go with the first
        /*
        if (grabbers.Count > 1)
        {
            for (int i = 1; i < grabbers.Count; i++)
            {
                Quaternion rot = grabbers[i].transform.rotation;
                if (Quaternion.Dot(rot, sum) < 0)
                {
                    sum.x -= rot.x;
                }
                else
                {
                    sum.x -= rot.x;
                }
                sum.y += rot.y;
                sum.z += rot.z;
                sum.w += rot.w;
            }
            sum.Normalize();
        }*/
        return sum;
    }


}
