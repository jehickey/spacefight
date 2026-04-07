using System.Collections.Generic;
using System.ComponentModel;
using System.Numerics;
using Unity.Burst.Intrinsics;
using Unity.VisualScripting;
using UnityEditor.Localization.Editor;
using UnityEngine;
using UnityEngine.AdaptivePerformance.Provider;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.EnhancedTouch;
using UnityEngine.SocialPlatforms;
using UnityEngine.UIElements.Experimental;
using UnityEngine.Video;
using static UnityEngine.InputSystem.OnScreen.OnScreenStick;

public class VideoCharacter : MonoBehaviour
{
    public Monitor monitor;
    public float videoTimer = 3;
    public float videoPlayTime = 0;

    //emotes
    public VideoClip clipNeutral;
    public VideoClip clipTalking;
    public VideoClip clipLookLeft;
    public VideoClip clipLookRight;
    public VideoClip clipNod;
    public VideoClip clipPoint;
    public VideoClip clipPointUp1;
    public VideoClip clipPointUp2;
    public VideoClip clipSalute;
    public VideoClip clipGo1;
    public VideoClip clipGo2;
    public VideoClip clipAgree;
    public VideoClip clipWTF;

    private List<VideoClip> clips = new List<VideoClip>();


    void Start()
    {
        clips.Clear();
        clips.Add(clipNeutral);
        clips.Add(clipTalking);
        clips.Add(clipLookLeft);
        clips.Add(clipLookRight);
        clips.Add(clipNod);
        clips.Add(clipPoint);
        clips.Add(clipPointUp1);
        clips.Add(clipPointUp2);
        clips.Add(clipSalute);
        clips.Add(clipGo1);
        clips.Add(clipGo2);
        clips.Add(clipAgree);
        clips.Add(clipWTF);

        if (monitor)
        {
            monitor.useClearScreen = false;
            monitor.LoopVideo = false;
            monitor.TextContent = getText();
        }

    }

    void Update()
    {
        if (!monitor) return;
        if (Time.time - videoPlayTime > videoTimer) 
        {
            videoPlayTime = Time.time;
            VideoClip clip = clips[Random.Range(0,clips.Count-1)];
            monitor.PlayVideo(clip);
        }
        
    }


    string getText()
    {
        return "The chicken(Gallus gallus domesticus) is a domesticated form of the red junglefowl(Gallus gallus), originally native to Southeast Asia.It was first domesticated around 8,000 years ago and is one of the most common and widespread domesticated animals in the world. Chickens are primarily kept for their meat and eggs, though they are also kept as pets.[1]\n\n" +
               "As of 2023, the global chicken population exceeds 26.5 billion, with more than 50 billion birds produced annually for consumption.Specialized breeds such as broilers and laying hens have been developed for meat and egg production, respectively.A hen bred for laying can produce over 300 eggs per year.Chickens are social animals with complex vocalizations and behaviors, and feature in folklore, religion, and literature across many societies.Their economic importance makes them a central component of global animal husbandry.";
    }
}
