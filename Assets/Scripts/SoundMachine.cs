using UnityEngine;

public class SoundMachine : MonoBehaviour
{
    public AudioClip Sound;
    public float Pitch = 1;
    public float Volume = 1;
    public bool IsPlayer = false;       //is this sound coming from the player?
    public bool Looping = false;        //should this audio play continuously?
    public bool IsStereo = false;

    public bool IsPlaying = false;
    public bool PlayOnStart = false;
    private bool played = false;

    private new AudioSource audio;
    private AudioListener listener;
    private float distanceFromPlayer;

    private void OnEnable()
    {
        //create audio source
        audio = gameObject.AddComponent<AudioSource>();
        audio.playOnAwake= false;

        //judge if audio source is player or not
        Ship ship = GetComponentInParent<Ship>();
        IsPlayer = (ship && ship == Game.I.PlayerShip);
    }

    void Update()
    {
        if (!listener) listener = FindFirstObjectByType<AudioListener>();
        ToggleAudio();
        if (audio && audio.enabled)
        {
            audio.clip = Sound;
            audio.pitch = Pitch;
            audio.volume = Volume;
            if (IsPlayer)
            {
                audio.spatialize = false;
                audio.spatialBlend = 0;
                if (IsStereo)
                {
                    float offsetX = (transform.position - listener.transform.position).x;
                    if (offsetX > 0) audio.panStereo = 1;
                    if (offsetX == 0) audio.panStereo = 0;
                    if (offsetX < 0) audio.panStereo = -1;
                }
                else
                {
                    audio.panStereo = 0;
                }
            }
            else
            {
                audio.spatialize = true;
                audio.spatialBlend = 1;
                audio.maxDistance = Game.I.AudioCutoffRange;
                audio.panStereo = 0;
                audio.volume = Volume * Game.I.AudioExternalSuppression;
            }

            //maintain looping audio
            if (!Looping) IsPlaying = audio.isPlaying;
            if (Looping && IsPlaying && !audio.isPlaying) Play();

            if (!Looping && PlayOnStart && !played) Play();
        }
    }

    public void Play()
    {
        if (!audio || !audio.enabled) return;
        IsPlaying = true;
        audio.Play();
        played = true;
    }

    public void Stop()
    {
        if (!audio || !audio.enabled) return;
        audio.Stop();
    }

    //turn audio on or off depending on distance and enemy or player
    void ToggleAudio()
    {
        //player-based audio is always active
        if (IsPlayer)
        {
            audio.enabled = true;
            distanceFromPlayer = 0;

            return;
        }
        if (listener) distanceFromPlayer = Vector3.Distance(transform.position, listener.transform.position);

        //have they entered audio range?
        if (!audio.isActiveAndEnabled)
        {
            if (distanceFromPlayer <= Game.I.AudioCutoffRange)
            {
                audio.enabled = true;
            }
        }
        //have they left audio range? (always a larger value)
        if (audio.isActiveAndEnabled)
        {
            if (distanceFromPlayer > Game.I.AudioCutoffRange + Game.I.AudioCutoffPadding)
            {
                audio.enabled = false;
            }
        }
    }



}
