using UnityEngine;

public class ThrustIndicator : SequentialLightPanel
{

    private ThrottleSystem throttleSystem;


    protected override void Update()
    {
        base.Update();
        if (!throttleSystem) throttleSystem = GetComponentInParent<ThrottleSystem>();
        if (throttleSystem)
        {
            //set the value
            Value = throttleSystem.Actual;
        }
        
    }
}
