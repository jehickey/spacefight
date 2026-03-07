using UnityEngine.UI;
using UnityEngine;

public class OverlayManager : MonoBehaviour
{
    public int scoreKills = 0;
    public int scoreDeaths = 0;
    public int Countdown = 0;

    public bool Paused = false;
    public float PauseBrightness = .5f;


    public bool ShowFPS= false;
    public float FPS = 0;

    public Text txtKills;
    public Text txtDeaths;
    public Text txtPause;
    public Text txtKeys;
    public Text txtCountdown;
    public Text txtFPS;
    public Text txtPlanet;
    public Text txtAltitude;
    public Image background;

    public Color backgroundColor = Color.black;

    public AudioClip CountdownBeep;
    public AudioClip SpawnBeep;
    private new AudioSource audio;
    private int lastCountdown;
    
    private void OnEnable()
    {
        audio = GetComponent<AudioSource>();
    }

    void Update()
    {
        txtPause.enabled = Paused;
        txtKeys.enabled = Paused;
        background.enabled = Paused;
        if (Paused && Countdown == 0) backgroundColor.a = PauseBrightness;
        txtKills.text = $"Kills: {scoreKills}";
        txtDeaths.text = $"Deaths: {scoreDeaths}";

        if (Countdown > 0)
        {
            txtCountdown.text = $"{Countdown}";
            background.enabled = true;
            backgroundColor.a = 1;
            if (Countdown != lastCountdown && audio && CountdownBeep)
            {
                audio.PlayOneShot(CountdownBeep);
            }
        }
        else
        {
            txtCountdown.text = "";
        }

        if (Countdown == 0 && lastCountdown > 0 && audio && SpawnBeep)
        {
            audio.PlayOneShot(SpawnBeep);
        }
        lastCountdown = Countdown;

        background.color = backgroundColor;

        if (txtFPS)
        {
            if (ShowFPS)
            {
                txtFPS.text = $"FPS:\n{Mathf.RoundToInt(FPS)}";
            }
            else
            {
                txtFPS.text = "";
            }
        }

        //Display body in proximity
        if (Game.I?.PlayerShip?.bodyProximity)
        {
            txtPlanet.text = Game.I.PlayerShip.bodyProximity?.name;
            txtAltitude.text = $"{Game.I.PlayerShip.bodyAltitude:0}m";
        }
        else
        {
            txtPlanet.text = "";
            txtAltitude.text = "";
        }

    }
}
