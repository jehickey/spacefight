using UnityEngine;

public class HealthIndicator : SequentialLightPanel
{
    private Destructable destructable;

    protected override void Update()
    {
        base.Update();
        if (!destructable) destructable = GetComponentInParent<Destructable>();
        if (destructable)
        {
            Max = destructable.MaxHealth;
            Value = destructable.Health;
        }
    }
}
