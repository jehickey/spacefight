using UnityEngine;

public class ThrottleSystem: MonoBehaviour
{
    [Header("Throttle Settings")]
    public float Input = 0;
    public float Actual = 0;
    public float Rate = 1;                  //rate of change
    public float Thrust = 0;
    public float MaxThrust = 1;

    [Header("Boost Settings")]
    public bool Boost = false;
    public bool Boosting = false;
    public float BoostMultiplier = 3;
    public float BoostCharge = 0;
    public float BoostConsumeRate = .25f;
    public float BoostChargeRate = .1f;
    public float BoostMinCharge = .25f;

    public float MaxRumble = 1;

    public float MinActual = 0.01f;         //cutoff avoids being unable to reach zero

    private Ship ship;

    private void OnEnable()
    {
        ship = GetComponent<Ship>();
    }

    void Update()
    {
        Input = Mathf.Clamp01(Input);
        ManageBoost();
        
        //actual goes down faster when input is over 1
        float actualRate = (Input > 1) ? Rate * Input : Rate;

        //adjust actual if not the same as input
        if (Actual != Input) Actual = Mathf.MoveTowards(Actual, Input, Rate * Time.deltaTime);

        //actual becomes input if it gets close enough (to avoid a hovering value)
        if (Mathf.Abs(Actual - Input) < MinActual) Actual = Input;

        //Apply actual to output thrust
        Thrust = Actual * MaxThrust;

        //ship effects
        if (ship)
        {
            float rumble = Mathf.Clamp( Actual * MaxRumble, 0, MaxRumble );
            ship.AddRumble(rumble);
            //if (Boost) ship.AddRumble(.1f);
        }
    }

    void ManageBoost()
    {
        //apply Infinite Boost setting
        if (Game.I.useInfiniteBoost && ship == Game.I.PlayerShip) BoostCharge = 1;

        //only allow boost to start if they're at a minimum charge
        if (Boost && !Boosting && BoostCharge < BoostMinCharge) Boost = false;

        //verify they can boost right now
        Boosting = (Boost && BoostCharge > 0);

        if (Boosting)
        {
            //consume charge and apply multiplier
            BoostCharge -= BoostConsumeRate * Time.deltaTime;
            Input = BoostMultiplier;
        }
        else
        {
            //charge up the booster
            BoostCharge += BoostChargeRate * Time.deltaTime;
        }

        //keep boost charge within limits
        BoostCharge = Mathf.Clamp01(BoostCharge);
    }


}
