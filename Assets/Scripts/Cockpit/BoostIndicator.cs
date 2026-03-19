using UnityEngine;

public class BoostIndicator : SequentialLightPanel
{
    [Header("Boost Indicator Settings")]
    public Color ReadyColor = Color.blue;
    public Color NotReadyColor = Color.red;
    public Color BoostingColor = Color.white;


    private ThrottleSystem throttleSystem;

    // Update is called once per frame
    protected override void Update()
    {
        base.Update();
        if (!throttleSystem) throttleSystem = GetComponentInParent<ThrottleSystem>();
        if (!throttleSystem) return;
        Value = throttleSystem.BoostCharge;
        if (throttleSystem.BoostReady)
        {
            LoColor = ReadyColor;
            HiColor = ReadyColor;
            Brightness = 1;
        }
        else
        {
            LoColor = NotReadyColor;
            HiColor = NotReadyColor;
            Brightness = 1;
        }
        if (throttleSystem.Boosting)
        {
            LoColor = BoostingColor;
            HiColor = BoostingColor;
            Brightness = 5;
        }

        if (throttleSystem.BoostFail) DoBlink();
        if (throttleSystem.BoostReady) Blinking = false;
    }
}
