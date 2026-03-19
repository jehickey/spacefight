using UnityEngine;

public class EngineSounds : MonoBehaviour
{
    [Header("Throttle")]
    public float PitchLow = .5f;
    public float PitchHigh = 1.5f;
    public float VolumeLow = .5f;
    public float VolumeHigh = 1.0f;

    [Header("Boost")]
    public SoundMachine ThrottleSound;
    private AudioSource BoostSound;
    public bool BoostRunning = false;
    private bool BoostReadied = false;
    private bool BoostFailed = false;
    //    public bool Boosting;
    //    public bool BoostStarting;
    //    public bool BoostRunning;
    //    public bool BoostStopping;

    private ThrottleSystem throttle;



    private void OnEnable()
    {
        throttle = GetComponentInParent<ThrottleSystem>();

        if (!ThrottleSound) ThrottleSound = GetComponent<SoundMachine>();

        BoostSound = gameObject.AddComponent<AudioSource>();
        BoostSound.clip = DefaultAV.I?.BoostRunning;
        BoostSound.loop = true;
    }

    void Update()
    {
        //ToggleAudio();    
        if (!throttle || !ThrottleSound) return;
        float pitch = Mathf.Lerp(PitchLow, PitchHigh, throttle.Actual);
        float volume = Mathf.Lerp(VolumeLow, VolumeHigh, throttle.Actual);
        ThrottleSound.Pitch = pitch;
        ThrottleSound.Volume = volume * Game.I.AudioLevelEngines;
        ThrottleSound.Looping = true;
        ThrottleSound.IsPlaying = true;
        ManageBoost();
    }

    private void ManageBoost()
    {
        if (!throttle) return;          //no throttle system
        if (!BoostSound) return;        //no boost sound set up
        if (!DefaultAV.I) return;       //required

        BoostSound.loop = true;

        if (throttle.Boosting && !BoostRunning)
        {
            BoostRunning = true;
            BoostSound.Play();
            BoostSound.PlayOneShot(DefaultAV.I.BoostStart);
        }

        if (!throttle.Boosting && BoostRunning)
        {
            BoostSound.Stop();
            BoostRunning = false;
            BoostSound.PlayOneShot(DefaultAV.I.BoostEnd);
        }

        if (!throttle.BoostFail) BoostFailed = false;       //reset fail sound if not in failure mode
        if (throttle.BoostFail && !BoostFailed)             //play fail sound if entering failure mode
        {
            BoostSound.PlayOneShot(DefaultAV.I?.BoostFail);
            BoostFailed = true;
        }

        if (!throttle.BoostReady) BoostReadied = false;     //reset ready sound if not ready
        if (throttle.BoostReady && !BoostReadied)           //play ready sound if ready becomes ready
        {
            BoostSound.PlayOneShot(DefaultAV.I?.BoostReady);
            BoostReadied = true;
        }


    }


}
