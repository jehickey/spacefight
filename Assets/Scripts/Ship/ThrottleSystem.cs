using TMPro;
using UnityEngine;

public class ThrottleSystem: MonoBehaviour
{
    public bool Locked = false;

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
    public bool BoostFail;                  //tells the sound system that a boost attempt failed
    public bool BoostReady;                 //tells the sound system that boost is ready
    private bool BoostPress;

    public float MaxRumble = .25f;
    public float BoostShake = .5f;

    public float MinActual = 0.01f;         //cutoff avoids being unable to reach zero

    private Ship ship;

    private void OnEnable()
    {
        ship = GetComponent<Ship>();
    }

    void Update()
    {
        if (Locked)
        {
            Actual = 0;
            Boosting = false;
            return;
        }
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
            ship.AddShake( rumble );
            if (Boosting) ship.AddShake(BoostShake);
        }
    }

    void ManageBoost()
    {
        //apply Infinite Boost setting
        if (Game.I.useInfiniteBoost && ship == Game.I.PlayerShip) BoostCharge = 1;
        BoostReady = BoostCharge >= BoostMinCharge;

        if (!Boost) BoostPress = false;
        if (Boost && !BoostPress) DoBoost();
        if (BoostReady || Boost) BoostFail = false;


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


    public bool DoBoost()
    {
        BoostPress = true;
        if (!BoostReady && !Game.I.useInfiniteBoost)
        {
            BoostFail = true;
            Boost = false;
            return false;
        }
        Boost = true;
        BoostFail = false;
        return true;
    }

}
