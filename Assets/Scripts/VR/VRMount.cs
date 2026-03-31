using UnityEngine;
using UnityEngine.SceneManagement;

public class VRMount : MonoBehaviour
{

    public string MountName = "DefaultMount";
    public Vector3 PositionScreen = Vector3.zero;
    public Vector3 PositionVR = Vector3.zero;

    public Quaternion RotationScreen = Quaternion.identity;
    public Quaternion RotationVR = Quaternion.identity;


    public bool ForceCenter = false;

    public GrabMoveReposition grab;


    private Material material;


    void Start()
    {
        //set screen position to whatever was in editor
        if (PositionScreen == Vector3.zero) PositionScreen = transform.localPosition;
        if (RotationScreen == Quaternion.identity) RotationScreen = transform.localRotation;

        if (PositionVR == Vector3.zero) PositionVR = PositionScreen;
        if (RotationVR == Quaternion.identity) RotationVR = RotationScreen;

        PositionVR = VRMountManager.I.GetPosition(MountName, transform);
        RotationVR = VRMountManager.I.GetRotation(MountName, transform);

        if (grab)
        {
            grab.OnPositionChanged += HandleReposition;
            //set up grab handle's material
            Renderer rend = grab.GetComponent<Renderer>();
            if (rend)
            {
                material = rend.material;
                material.EnableKeyword("_EMISSION");
            }
        }
    }

    void Update()
    {
        if (Game.I && Game.I.VRHeadset)
        {
            //update position, but only if the handle isn't in use
            if (!grab || !grab.Grabbed)
            {
                transform.localPosition = PositionVR;
                transform.localRotation = RotationVR;
            }
        }

        //grabbers should only be active if in VR and they're enabled
        if (Game.I) grab.gameObject.SetActive(Game.I.VRHeadset && Game.I.EnableVRMountEditing);

        if (grab)
        {
            if (!grab.Grabbed) SetColor(Color.green, 1);
            if (grab.Grabbed) SetColor(Color.green, 5);
            //if (grab.triggerPressed) SetColor(Color.white, 5);
        }
    }

    private void HandleReposition() 
    {
        //transform.position = newPosition;
        Vector3 pos = transform.localPosition;
        if (ForceCenter)
        {
            pos.x = 0;
            transform.localPosition = pos;
        }
        PositionVR = pos;
        RotationVR = transform.localRotation;
        //the grabber handles repositioning, just save it
        VRMountManager.I.Set(MountName, transform);
    }


    private void SetColor(Color color, float brightness)
    {
        if (!material) return;
        color.a = brightness;
        material.SetColor("_EmissionColor", color);
    }

}
