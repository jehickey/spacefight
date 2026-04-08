using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Video;

[System.Serializable]
public class VideoEntry
{
    public string Name;
    public VideoClip Clip;
    public VideoEntry(string name, VideoClip clip)
    {
        Name= name;
        Clip = clip;
    }
}

public class VideoCharacter : MonoBehaviour
{
    public int ConfID = 0;
    public Monitor monitor;
    public float videoTimer = 3;
    public float videoPlayTime = 0;

    public VideoEntry current;

    public AudioClip audioClip;

    public bool AudioRunning = false;
    public bool isTalking = false;
    public float TalkThreshhold = 0.02f;
    public int TalkSampleSize = 4096;
    public float TalkCooldown = .5f;        //seconds
    private float lastTalkTime = 0;


    public string NeutralClip;
    public string TalkClip;
    public List<VideoEntry> clips = new List<VideoEntry>();

    public List<VideoCharacter> characters = new List<VideoCharacter>();
    public VideoCharacter Talker;

    private FlightControls controls;
    private new AudioSource audio;
    private GameObject talkIndicator;
    private Material talkIndicatorMaterial;

    void Start()
    {


    }

    private void OnEnable()
    {
        audio = GetComponent<AudioSource>();
        BuildList();
        if (!monitor) monitor = GetComponentInParent<Monitor>();
        if (monitor)
        {
            transform.position = monitor.transform.position;
            monitor.useClearScreen = false;
            monitor.LoopVideo = false;
            monitor.TextContent = getText();
        }
        controls = new FlightControls();
        controls?.Enable();
        audio.clip = audioClip;
        FindOtherCharacters();
        talkIndicator = transform.GetChild(0)?.gameObject;
        if (talkIndicator && monitor)
        {
            talkIndicator.transform.position = monitor.transform.position + new Vector3(0, .08f, 0);
            Renderer rend = talkIndicator.GetComponent<Renderer>();
            if (rend) talkIndicatorMaterial = rend.material;
        }
    }

    private void OnDisable()
    {
        controls?.Disable();
    }

    void Update()
    {
        if (!monitor) return;
        /*
        if (Time.time - videoPlayTime > videoTimer || !monitor.VideoIsPlaying) 
        {
            videoPlayTime = Time.time;
            current = GetRandomClip();
            if (current.Clip == null) Debug.Log($"Got a Null Clip on '{current.Name}'!");
            monitor.PlayVideo(current.Clip);
        }
        */

        ToggleAudio();
        WhoIsTalking();
        AmITalking();


        if (isTalking)
        {
            Play(TalkClip);
        }
        else
        {
            if (Talker)
            {
                if (Talker.transform.position.x < transform.position.x) Play("lookleft");
                if (Talker.transform.position.x > transform.position.x) Play("lookright");
                //should add up/down as well
            }
            else
            {
                Play(NeutralClip);
            }
        }

    }

    private void FindOtherCharacters()
    {
        characters.Clear();
        foreach (var c in GameObject.FindObjectsByType<VideoCharacter>(FindObjectsSortMode.None)) {
            if (c != this) characters.Add(c);
        }
    }

    private void WhoIsTalking()
    {
        Talker = null;
        foreach (var c in characters)
        {
            if (c.isTalking) Talker = c;
        }
    }

    private void AmITalking()
    {
        bool hearTalking = DetectTalking(audio);
        if (hearTalking)
        {
            isTalking = true;
            lastTalkTime = Time.time;
        }
        else
        {
            if (Time.time - lastTalkTime > TalkCooldown)    //talk timeout
            {
                isTalking = false;
            }
        }

        if (talkIndicatorMaterial)
        {
            talkIndicatorMaterial.color = isTalking ? Color.white : Color.black;
        }
    }

    private void ToggleAudio()
    {
        int audioSelect = 0;
        if (controls.Testing._1.WasPressedThisFrame()) audioSelect = 1;
        if (controls.Testing._2.WasPressedThisFrame()) audioSelect = 2;
        if (controls.Testing._3.WasPressedThisFrame()) audioSelect = 3;
        if (audioSelect == ConfID)
        {
            AudioRunning = !AudioRunning;
            audio.loop = true;
            if (!AudioRunning) audio.Pause();
            if (AudioRunning) audio.Play();
        }
    }

    private void Play(string name)
    {
        VideoEntry queued = GetClip(name);
        if (queued == null)
        {
            Debug.Log($"Can't find a clip for '{name}'!");
            return;
        }

        if (queued.Name == current.Name && monitor.VideoIsPlaying) return;
        current = queued;
        monitor.PlayVideo(current.Clip);
    }

    //Retrieve a clip by the given name.  Returns a random selection if multiple.
    private VideoEntry GetClip(string name) 
    {
        if (name == "") return null;
        List<VideoEntry> results = new List<VideoEntry>();
        foreach (var clip in clips)
        {
            if (clip.Name.ToLower() == name.ToLower()) results.Add(clip);
        }
        if (results.Count == 0) return null;
        if (results.Count == 1) return results[0];
        return results[Random.Range(0, results.Count)];
    }

    void BuildList()
    {
        //clips.Clear();
    }

    VideoEntry GetRandomClip()
    {
        return clips[Random.Range(0, clips.Count - 1)];
    }


    string getText()
    {
        return "The chicken(Gallus gallus domesticus) is a domesticated form of the red junglefowl(Gallus gallus), originally native to Southeast Asia.It was first domesticated around 8,000 years ago and is one of the most common and widespread domesticated animals in the world. Chickens are primarily kept for their meat and eggs, though they are also kept as pets.[1]\n\n" +
               "As of 2023, the global chicken population exceeds 26.5 billion, with more than 50 billion birds produced annually for consumption.Specialized breeds such as broilers and laying hens have been developed for meat and egg production, respectively.A hen bred for laying can produce over 300 eggs per year.Chickens are social animals with complex vocalizations and behaviors, and feature in folklore, religion, and literature across many societies.Their economic importance makes them a central component of global animal husbandry.";
    }


    private float GetAudioLevel(AudioSource source, int sampleSize = 256)
    {
        float[] samples = new float[sampleSize];
        source.GetOutputData(samples, 0); // channel 0

        float sum = 0f;
        for (int i = 0; i < sampleSize; i++)
            sum += samples[i] * samples[i];

        return Mathf.Sqrt(sum / sampleSize); // RMS amplitude
    }

    bool DetectTalking(AudioSource src)
    {
        float level = GetAudioLevel(src, TalkSampleSize);
        //Debug.Log(level);
        return level > TalkThreshhold;
    }


}
