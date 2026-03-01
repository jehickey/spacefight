using UnityEngine;

public class EngineSounds : MonoBehaviour
{
    public float PitchLow = .5f;
    public float PitchHigh = 1.5f;
    public float VolumeLow = .5f;
    public float VolumeHigh = 1.0f;

    private ThrottleSystem throttle;
    private new AudioSource audio;
    private Simulation sim;
    private Ship ship;

    private void OnEnable()
    {
        sim = FindFirstObjectByType<Simulation>();
        throttle = GetComponentInParent<ThrottleSystem>();
        audio = GetComponent<AudioSource>();
        ship = GetComponentInParent<Ship>();
    }

    void Update()
    {
        ToggleAudio();    
        if (!throttle || !audio) return;
        float pitch = Mathf.Lerp(PitchLow, PitchHigh, throttle.Actual);
        float volume = Mathf.Lerp(VolumeLow, VolumeHigh, throttle.Actual);
        if (audio && audio.enabled)
        {
            audio.pitch = pitch;
            audio.volume = volume;
            audio.loop = true;
            audio.maxDistance = sim.AudioCutoffRange;
            if (!audio.isPlaying) audio.Play();
        }
    }


    void ToggleAudio()
    {
        //turn audio on or off depending on distance
        if (ship)
        {
            if (ship == sim.PlayerShip)     //is this the player's ship?
            {
                audio.enabled = true;
            }
            else                            //not the player's ship
            {
                //have they entered audio range?
                if (!audio.isActiveAndEnabled)
                {
                    if (ship.DistanceFromPlayer <= sim.AudioCutoffRange)
                    {
                        audio.enabled = true;
                    }
                }
                //have they left audio range? (always a larger value)
                if (audio.isActiveAndEnabled)
                {
                    if (ship.DistanceFromPlayer > sim.AudioCutoffRange + sim.AudioCutoffPadding)
                    {
                        audio.enabled = false;
                    }
                }
            }
        }
    }

}
