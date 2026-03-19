using UnityEngine;

public class HealthIndicator : SequentialLightPanel
{
    [Header("Health Indicator")]
    public float WarningLevel = .25f;

    private Destructable destructable;

    protected override void Update()
    {
        base.Update();
        if (!destructable) destructable = GetComponentInParent<Destructable>();
        if (!destructable) return;

        Max = destructable.MaxHealth;
        Value = destructable.Health;

        if (Value <= WarningLevel)
        {
            DoBlink();
        }
        else
        {
            Blinking = false;
        }
        
    }
}
