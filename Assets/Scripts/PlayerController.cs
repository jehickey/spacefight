using UnityEditor;
using UnityEngine;

public class PlayerController : MonoBehaviour
{
    public Ship ship;
    private PlayerMountPoint mountPoint;

    public Camera InsideCam;
    public Camera OutsideCam;

    public bool doAlign = false;

    void Start()
    {
        
    }

    void LateUpdate()
    {
        AlignToShip();
    }

    private void OnValidate()
    {
        if (doAlign)
        {
            AlignToShip ();
            doAlign = false;
        }
    }

    private void AlignToShip()
    {
        //if we don't have a ship, try to find a mount point somewhere
        if (!ship)
        {
            mountPoint = GameObject.FindFirstObjectByType<PlayerMountPoint>();
            if (mountPoint) ship = mountPoint.GetComponentInParent<Ship>();
        }
        //is still no ship, give up
        if (!ship)
        {
            mountPoint = null;
            return;
        }
        //if we have a ship and no mount point, get the mount point
        if (!mountPoint) mountPoint = ship.GetComponentInChildren<PlayerMountPoint>();
        //if still no mountpoint...
        if (!mountPoint) return;

        transform.position = mountPoint.transform.position;
        transform.rotation = mountPoint.transform.rotation;
    }

}
