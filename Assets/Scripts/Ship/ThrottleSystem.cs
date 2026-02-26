using NUnit.Framework.Internal;
using UnityEngine;
using static UnityEngine.Rendering.DebugUI.Table;

[System.Serializable]
public class ThrottleSystem: MonoBehaviour
{
    public float Input = 0;
    public float Actual =0;
    public float Rate = 1;
    public float Thrust = 0;
    public float MaxThrust = 1;

    public float MaxRumble = 1;

    private Ship ship;

    private void OnEnable()
    {
        ship = GetComponent<Ship>();
    }

    void Update()
    {
        Input = Mathf.Clamp01(Input);
        Actual = Mathf.MoveTowards(Actual, Input, Rate * Time.deltaTime);
        Thrust = Actual * MaxThrust;

        if (ship) ship.AddRumble(Actual * MaxRumble);
    }

}
