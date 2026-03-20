using System;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEditor;
using UnityEngine;

public class Jumpdrive : MonoBehaviour
{
    [Header("Jump Status")]
    public bool Available = false;                      //is the jump system available for use?
    public bool InTransit = false;                      //is the player in transit right now?
    public float Progress = 0;                          //how close are they to their destination? (0-1)
    public float TimeRemaining = 0;
    public JumpLocation CurrentLocation;                //where are we right now (or leaving from)
    public JumpLocation Destination;                    //the selected destination
    public List<JumpLocation> Locations = new List<JumpLocation>();

    [Header("Jump Effects")]
    public float MinTime = 5;                           //minimum time a jump can take
    public float MaxTime = 15;                          //maximum time a jump can take
    public float JumpBuildupTime = 5;                   //how long for effect to build up?
    public float JumpBuildDownTime = 5;                 //how long for effect to die down?
    public float EffectsStrength = 0;                   //manages visual effects

    //public bool ForceAvailable = false;

    private new AudioSource audio;
    private AudioClip clipAvailable;
    private AudioClip clipHighlighted;
    private AudioClip clipEngaging;
    private AudioClip clipStart;
    private AudioClip clipRunning;
    private AudioClip clipEnd;
    private AudioClip clipFail;


    void Start()
    {
        SetupAudio();
        Destination = null;
        CurrentLocation = null;
    }

    void Update()
    {
        UpdateDestinations();
        Available = Locations.Count > 0;
        //if (ForceAvailable) Available = true;

        GetTimeRemaining();

        ManageProgress();
        ManageEffects();
    }

    private bool DoActivate()
    {
        //is the system available?
        if (!Available) return false;
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
        if (result && clipStart) audio.PlayOneShot(clipStart);
        return result;
    }

    public void Select()
    {
        
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
        if (TimeRemaining < 0) return;
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
        Debug.Log("Jump Departure");
        CurrentLocation = null;
        InTransit = true;
        GetTimeRemaining();
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
        if (TimeRemaining < MinTime) Debug.Log($"Jump time of {TimeRemaining}s below minimum of {MinTime}s");
        if (TimeRemaining > MaxTime) Debug.Log($"Jump time of {TimeRemaining}s exceeds maximum of {MaxTime}s");
        TimeRemaining = Mathf.Clamp(TimeRemaining, MinTime, MaxTime);

    }

    //Called when arriving at the destination
    private void EventArrival()
    {
        Debug.Log("Jump Arrival");
        CurrentLocation = Destination;
        TimeRemaining = 0;
        InTransit = false;
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
