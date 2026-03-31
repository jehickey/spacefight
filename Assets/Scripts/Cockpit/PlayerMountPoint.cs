using UnityEngine;

public class PlayerMountPoint : MonoBehaviour
{
    public Vector3 PositionScreen = Vector3.zero;
    public Vector3 PositionVR = Vector3.zero;

    public bool TestPositionVR = false;

    void Start()
    {
        //if no positions are given, set defaults
        //if (PositionScreen == Vector3.zero) PositionScreen = transform.localPosition;
        //if (PositionVR == Vector3.zero) PositionVR = PositionScreen;
    }

    void Update()
    {
        //EnforcePosition();
    }

    private void OnValidate()
    {
        //EnforcePosition();
    }



    private void EnforcePosition()
    {
        if ((Game.I && Game.I.VRHeadset) || TestPositionVR)
        {
            transform.localPosition = PositionVR;
        }
        else
        {
            transform.localPosition = PositionScreen;
        }
    }

}
