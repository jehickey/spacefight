using System.Collections.Generic;
using UnityEngine;

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
    public float CloseEnough = .1f;

    [Header("Jump Effects")]
    public Camera OutsideCam;                           //the cam that covers all external effects
    public float FOVNormal = 64;
    public float FOVFull = 100;
    public float MinTime = 5;                           //minimum time a jump can take
    public float MaxTime = 15;                          //maximum time a jump can take
    public float JumpBuildupTime = 5;                   //how long for effect to build up?
    public float JumpBuildDownTime = 5;                 //how long for effect to die down?
    public float EffectsStrength = 0;                   //manages visual effects
    private float JumpStartTime;
    private float JumpElapsedTime;

    //public bool ForceAvailable = false;

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

    void Start()
    {
        SetupAudio();
        Destination = null;
        CurrentLocation = null;
        ship = GetComponentInParent<Ship>();
        throttle = GetComponentInParent<ThrottleSystem>();
        steering = GetComponentInParent<SteeringSystem>();
        weapons = GetComponentInParent<WeaponsSystem>();
    }

    void Update()
    {
        if (!InTransit)
        {
            UpdateDestinations();
            ManageSelection();
            Available = Locations.Count > 0;
            //if (ForceAvailable) Available = true;
        }
        if (InTransit)
        {
            Available = false;
            ManageProgress();
            ManageEffects();
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
        TimeRemaining -= Time.deltaTime;
        Progress = Mathf.InverseLerp(TransitTime, 0, TimeRemaining);
        ship.transform.position = Vector3.Lerp(StartPoint, Destination.Coordinate, Progress);
        if (Vector3.Distance(ship.transform.position, Destination.Coordinate) <= CloseEnough)
        {
            Progress = 1;
        }

        //visuals
        JumpElapsedTime = Time.time - JumpStartTime;
        float buildup = Mathf.InverseLerp(0, JumpBuildupTime, JumpElapsedTime);
        if (OutsideCam)
        {
            OutsideCam.fieldOfView = Mathf.Lerp(FOVNormal, FOVFull, buildup);
        }

        if (Progress == 1 || TimeRemaining < 0)
        {
            EventArrival();
        }
    }

    private void ManageEffects()
    {
        if (EffectsStrength <= 0) return;
    }

    private void EventDeparture()
    {
        if (!Destination)
        {
            Debug.LogWarning("Jump Departure without a destination!");
            return;
        }
        Debug.Log($"Jump Departure to {Destination.Name}");
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
        if (TimeRemaining > MaxTime) Debug.Log($"Jump time of {TimeRemaining}s exceeds maximum of {MaxTime}s");
        TimeRemaining = Mathf.Clamp(TimeRemaining, MinTime, MaxTime);

    }

    //Called when arriving at the destination
    private void EventArrival()
    {
        Debug.Log("Jump Arrival");
        ship.transform.position = Destination.Coordinate;
        CurrentLocation = Destination;
        TimeRemaining = 0;
        TransitTime = 0;
        InTransit = false;
        StartPoint = Vector3.zero;
        audio.Stop();
        if (clipEnd) audio.PlayOneShot(clipEnd);
        ship.DoMessage($"Arriving at {Destination.Name}");
        Destination= null;
        EnableControls();
        if (OutsideCam) OutsideCam.fieldOfView = FOVNormal;

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
