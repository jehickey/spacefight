using UnityEngine;

public class GrabTrigger : MonoBehaviour
{
    public bool Grabbed = false;
    public bool Pressed = false;

    void Start()
    {
        
    }

    void Update()
    {
        
    }

    public void DoPress()
    {
        Pressed = true;
        //Debug.Log("Press Fire button");
    }

    public void DoRelease()
    {
        Pressed = false;
    }

}
