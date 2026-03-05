using UnityEngine;

public class EngineSounds : MonoBehaviour
{
    public float PitchLow = .5f;
    public float PitchHigh = 1.5f;
    public float VolumeLow = .5f;
    public float VolumeHigh = 1.0f;

    private ThrottleSystem throttle;

    public SoundMachine sound;

    private void OnEnable()
    {
        throttle = GetComponentInParent<ThrottleSystem>();

        if (!sound) sound = GetComponent<SoundMachine>();
    }

    void Update()
    {
        //ToggleAudio();    
        if (!throttle || !sound) return;
        float pitch = Mathf.Lerp(PitchLow, PitchHigh, throttle.Actual);
        float volume = Mathf.Lerp(VolumeLow, VolumeHigh, throttle.Actual);
        sound.Pitch = pitch;
        sound.Volume = volume * Game.I.AudioLevelEngines;
        sound.Looping = true;
        sound.IsPlaying = true;
    }

}
