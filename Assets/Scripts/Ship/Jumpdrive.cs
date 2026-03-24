using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.UIElements;

public class Jumpdrive : MonoBehaviour
{
    [Header("Jump Status")]
    public bool Available = false;                      //is the jump system available for use?
    public bool InTransit = false;                      //is the player in transit right now?
    public float Progress = 0;                          //how close are they to their destination? (0-1)
    public float TimeRemaining = 0;
    public float TransitTime = 0;
    public Vector3 StartPoint = Vector3.zero;
    public JumpLocation CurrentLocation;                //where are we right now (or leaving from)
    public JumpLocation Destination;                    //the selected destination
    public List<JumpLocation> Locations = new List<JumpLocation>();
    public float ProgressCloseEnough = .01f;

    [Header("Jump Effects")]
    public Camera OutsideCam;                           //the cam that covers all external effects
    private float JumpChargeStartTime;
    private float JumpStartTime;
    private float JumpElapsedTime;
    private bool despawned;


    private new AudioSource audio;
    private AudioClip clipAvailable;
    private AudioClip clipHighlighted;
    private AudioClip clipEngaging;
    private AudioClip clipStart;
    private AudioClip clipRunning;
    private AudioClip clipEnd;
    private AudioClip clipFail;

    private Ship ship;
    private ThrottleSystem throttle;
    private SteeringSystem steering;
    private WeaponsSystem weapons;
    private RelativisticDopplerFeature doppler;

    void Start()
    {
        SetupAudio();
        Destination = null;
        CurrentLocation = null;
        ship = GetComponentInParent<Ship>();
        throttle = GetComponentInParent<ThrottleSystem>();
        steering = GetComponentInParent<SteeringSystem>();
        weapons = GetComponentInParent<WeaponsSystem>();
        RelativisticDopplerFeature.DopplerStrength = 0;

    }

    void Update()
    {
        if (!InTransit)
        {
            UpdateDestinations();
            ManageSelection();
            Available = Locations.Count > 0;
        }
        if (InTransit)
        {
            Available = false;
            ManageProgress();
            ManageEffectDefaults();
        }
    }

    private bool DoActivate()
    {
        //is the system available?
        if (!Available) return false;
        Destination = null;
        foreach (JumpLocation loc in Locations)
        {
            if (loc.Available && loc.Selected) Destination = loc;
        }

        //verify there's a location and it IS on the list
        if (!Destination || !Destination.Available)
        {
            Debug.Log("No jump destination set!");
            return false;
        }
        /*
        if (!Locations.Contains(Destination))
        {
            Debug.Log("Jump Destination is not in the list");
            return false;
        }
        */

        //verify there's Line-Of-Sight
        if (!HasLineOfSight(Destination.Target.transform))
        {
            Debug.Log("No line of sight!");
            return false;
        }

        //verify we're not already at this destination
        //Destination = location;
        EventDeparture();
        return true;
    }

    //a public wrapper for jump drives to be activated by other components
    public bool Activate()
    {
        bool result = DoActivate();
        if (!result && clipFail) audio.PlayOneShot(clipFail);
        return result;
    }

    private void ManageSelection()
    {
        if (!ship) return;

        Destination = null;
        foreach (JumpLocation loc in Locations)
        {
            if (loc)
            {
                //see if the ship is pointing at it
                Vector3 toTarget = (loc.Target.transform.position - ship.transform.position).normalized;
                float angle = Vector3.Angle(ship.transform.forward, toTarget);
                loc.Selected = angle < Game.I.JumpSelectionAngle;
                if (loc.Selected) Destination = loc;
            }
        }
        GetTimeRemaining();
    }


    private void UpdateDestinations()
    {
        Locations.Clear();
        foreach (JumpLocation loc in Game.I?.GetJumps()) {
            if (loc.Available) Locations.Add(loc);
        }
    }

    private void ManageProgress()
    {
        if (!InTransit) return;
        //update progress and stats
        TimeRemaining -= Time.deltaTime;
        JumpElapsedTime = Time.time - JumpStartTime;
        Progress = Mathf.Clamp01(Mathf.InverseLerp(TransitTime, 0, TimeRemaining));
        if (1-Progress <= ProgressCloseEnough) Progress = 1;        //ensure we reach 1

        ship.transform.position = Vector3.Lerp(StartPoint, Destination.Coordinate, Progress);

        //ensure buildup and rampdown times are compatible with transit time
        float actualBuildupTime = Game.I.JumpBuildupTime;
        float actualRampdownTime = Game.I.JumpRampdownTime;
        if (Game.I.JumpBuildupTime + Game.I.JumpRampdownTime > TransitTime)       //need to shorten build/ramp times
        {
            actualBuildupTime = TransitTime * .5f;
            actualRampdownTime = TransitTime * .5f;
        }

        //manage visual effect transitions
        float buildup = 0;
        buildup = Mathf.InverseLerp(0, actualBuildupTime, JumpElapsedTime);
        if (TimeRemaining <= actualRampdownTime)
        {
            buildup = Mathf.InverseLerp(0, actualRampdownTime, TimeRemaining);
        }
        buildup = Mathf.Clamp01(buildup);
        ship.AddShake(buildup*.5f);
        if (OutsideCam)
        {
            if (ship) RelativisticDopplerFeature.DopplerCameraForward = OutsideCam.transform.InverseTransformDirection(ship.transform.forward);
            OutsideCam.fieldOfView = Mathf.Lerp(Game.I.JumpFOVNormal, Game.I.JumpFOVFull, buildup);
            RelativisticDopplerFeature.DopplerMaxAngle = OutsideCam.fieldOfView * Game.I.JumpFOVFactor;
            RelativisticDopplerFeature.DopplerStrength = buildup * Game.I.JumpEffectsStrength;
        }

        if (!despawned && Progress >= Game.I.JumpEnemyDespawnProgress)
        {
            Game.I.DespawnEnemies();
            despawned = true;
        }

        if (Progress == 1 || TimeRemaining < 0)
        {
            EventArrival();
        }
    }

    private void ManageEffectDefaults()
    {
        RelativisticDopplerFeature.DopplerMinHue = Game.I.JumpMinHue;
        RelativisticDopplerFeature.DopplerMaxHue = Game.I.JumpMaxHue;
        RelativisticDopplerFeature.DopplerSaturationDelta= Game.I.JumpSaturationDelta;
        RelativisticDopplerFeature.DopplerBrightnessBoost= Game.I.JumpBrightnessBoost;
        RelativisticDopplerFeature.DopplerBrightnessRange= Game.I.JumpBrightnessRange;
    }

private void EventDeparture()
    {
        if (!Destination)
        {
            Debug.LogWarning("Jump Departure without a destination!");
            return;
        }
        despawned= false;
        ship.DoMessage($"Jumping to {Destination.Name}");
        CurrentLocation = null;
        InTransit = true;
        GetTimeRemaining();
        TransitTime = TimeRemaining;
        StartPoint = ship.transform.position;
        JumpStartTime = Time.time;
        Destination.Selected = false;
        Game.I.ClearJump();
        Locations.Clear();
        if (clipStart) audio.PlayOneShot(clipStart);
        audio.Play();
        DisableControls();
        if (Destination.reticle) Destroy(Destination.reticle.gameObject);
    }

    private void GetTimeRemaining()
    {
        if (!Destination || !Destination.Available)
        {
            TimeRemaining = 0;
            return;
        }
        if (Destination.Distance == 0) return;
        TimeRemaining = Destination.Distance * .001f;
        //if (TimeRemaining < MinTime) Debug.Log($"Jump time of {TimeRemaining}s below minimum of {MinTime}s");
        if (TimeRemaining > Game.I.JumpMaxTime) Debug.Log($"Jump time of {TimeRemaining}s exceeds maximum of {Game.I.JumpMaxTime}s");
        TimeRemaining = Mathf.Clamp(TimeRemaining, Game.I.JumpMinTime, Game.I.JumpMaxTime);

    }

    //Called when arriving at the destination
    private void EventArrival()
    {
        audio.Stop();
        if (clipEnd) audio.PlayOneShot(clipEnd);
        CurrentLocation = Destination;
        ship.DoMessage($"Arriving at {Destination.Name}");
        EnableControls();

        //clean up values
        ship.transform.position = Destination.Coordinate;
        InTransit = false;
        Destination = null;
        TimeRemaining = 0;
        TransitTime = 0;
        StartPoint = Vector3.zero;

        //make sure all effects are cleaned up
        if (OutsideCam) OutsideCam.fieldOfView = Game.I.JumpFOVNormal;
        RelativisticDopplerFeature.DopplerStrength = 0;
        OutsideCam.fieldOfView = Game.I.JumpFOVNormal;
        RelativisticDopplerFeature.DopplerMaxAngle = OutsideCam.fieldOfView * Game.I.JumpFOVFactor;
    }


    private void DisableControls()
    {
        if (throttle) throttle.Locked = true;
        if (steering) steering.Locked = true;
        if (weapons) weapons.Locked = true;
    }

    private void EnableControls()
    {
        if (throttle) throttle.Locked = false;
        if (steering) steering.Locked = false;
        if (weapons) weapons.Locked = false;
    }


    private bool HasLineOfSight (Transform to)
    {
        int obstructionMask = ~(1 << LayerMask.NameToLayer("Inside"));
        Vector3 origin = transform.position;
        Vector3 direction = (to.position - origin);
        float distance = direction.magnitude;
        direction.Normalize();
        if (Physics.Raycast(origin, direction, out RaycastHit hit, distance, obstructionMask))
        {
            return hit.transform == to;
        }
        return true;        //returns true if nothing is hit at all (even target)
    }


    private void SetupAudio()
    {
        //get the default sounds from the AV controller
        if (!DefaultAV.I) return;
        clipAvailable = DefaultAV.I.JumpAvailable;
        clipHighlighted = DefaultAV.I.JumpHighlighted;
        clipEngaging = DefaultAV.I.JumpEngaging;
        clipStart = DefaultAV.I.JumpStart;
        clipRunning = DefaultAV.I.JumpRunning;
        clipEnd = DefaultAV.I.JumpEnd;
        clipFail = DefaultAV.I.JumpFail;

        if (!audio) audio = gameObject.AddComponent<AudioSource>();
        audio.loop = true;      //enabled for background Running sound
        audio.clip = clipRunning;

    }

}
