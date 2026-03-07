using UnityEngine;

public class ThrottleSystem: MonoBehaviour
{

    public float Input = 0;
    public float Actual =0;
    public float Rate = 1;
    public float Thrust = 0;
    public float MaxThrust = 1;

    public bool Boost = false;
    public float BoostMultiplier = 3;

    public float MaxRumble = 1;

    public float MinThrust = 0.01f;

    private Ship ship;

    private void OnEnable()
    {
        ship = GetComponent<Ship>();
    }

    void Update()
    {
        Input = Mathf.Clamp01(Input);
        if (Boost) Input = BoostMultiplier;
        Actual = Mathf.MoveTowards(Actual, Input, Rate * Time.deltaTime);
        Thrust = Actual * MaxThrust;
        if (Thrust < MinThrust) Thrust = 0;

        if (ship)
        {
            float rumble = Mathf.Clamp( Actual * MaxRumble, 0, MaxRumble );
            ship.AddRumble(rumble);
            //if (Boost) ship.AddRumble(.1f);
        }
    }

}
