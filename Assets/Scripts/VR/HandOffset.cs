using System;
using System.IO;
using UnityEditor.Build.Content;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.UIElements;


[System.Serializable]
public struct HandOffsetEntry
{
    public Vector3 position;
    public Vector3 rotation;

    public HandOffsetEntry(Vector3 pos, Vector3 rot)
    {
        position = pos;
        rotation = rot;
    }
}

public class HandOffset : MonoBehaviour
{
    //public Transform hand;
    //public Transform forearm;

    [Header("Status")]
    public bool Activate = false;
    public HandEditModes Mode = HandEditModes.PosXZ;
    public HandOffsetEntry data = new HandOffsetEntry();
    public bool Reset = false;

    [Header("Settings")]
    public float LableSideOffset = -.1f;
    public float PositionMoveRate = .1f;        //per second
    public float RotationMoveRate = .1f;        //per second
    public float StickDoubleclickSec = .25f;
    [SerializeField]
    [ReadOnly]
    private LeftRight handedness = LeftRight.None;

    [Header("Data File")]
    public string fileBase = "handData-";
    [SerializeField]
    [ReadOnly]
    private string filePath = "";

    [Header("Audio")]
    public AudioClip soundActivate;
    public AudioClip soundDeactivate;
    public AudioClip soundModeChange;
    public AudioClip soundAdjustment;
    public float PitchX = .75f;
    public float PitchY = 1.25f;
    public float BaseVolume = .5f;


    private Vector2 stickDelta = new Vector2();
    private bool stickClick = false;
    private bool stickDoubleClick = false;
    private float stickClickTime = 0;

    private bool stickDown = false;
    private float stickDownTime = 0;
    private float stickDownElapsed = 0;

    private Grabber grabber;
    private Canvas canvas;
    private Text textbox;
    private FlightControls controls;
    private new AudioSource audio;


    public enum HandEditModes
    {
        PosXZ,
        PosY,
        RotXZ,
        RotY
    }


    public Quaternion GetRotation(Transform target)
    {
        return Quaternion.AngleAxis(data.rotation.x, Vector3.right) *
            Quaternion.AngleAxis(data.rotation.y, Vector3.up) *
            Quaternion.AngleAxis(data.rotation.z, target.forward);
    }

    private void OnEnable()
    {
        if (controls == null) controls = new FlightControls();
        controls.Enable();
    }

    private void OnDisable()
    {
        controls?.Disable();
    }


    void Start()
    {
        canvas = GetComponentInChildren<Canvas>();
        textbox = GetComponentInChildren<Text>();
        audio = GetComponent<AudioSource>();
        //preload audio
        soundActivate.LoadAudioData();
        soundDeactivate.LoadAudioData();
        soundModeChange.LoadAudioData();
        soundAdjustment.LoadAudioData();

        grabber = GetComponentInParent<Grabber>();
        if (grabber) handedness = grabber.Hand;
        filePath = Path.Combine(Application.persistentDataPath, $"{fileBase}{handedness}.json");
        Load();

    }

    void Update()
    {
        GetInput();

        if (Game.I && Game.I.EnableVRHandPositionSettings && Activate)
        {
            canvas.gameObject.SetActive(true);
        }
        else
        {
            canvas.gameObject.SetActive(false);
            return;
        }

        UpdatePosition();
        UpdateMode();
        EnactMode();
    }

    private void OnValidate()
    {
        Reset = false;
        UpdatePosition();


    }

    private void GetInput()
    {
        //collect input
        if (handedness == LeftRight.Right)
        {
            stickDelta = controls.VR.RightThumbstick.ReadValue<Vector2>();
            stickDown = controls.VR.RightThumbstickPress.IsPressed();
            stickClick = controls.VR.RightThumbstickPress.WasPressedThisFrame();
        }
        if (handedness == LeftRight.Left)
        {
            stickDelta = controls.VR.LeftThumbstick.ReadValue<Vector2>();
            stickDown = controls.VR.LeftThumbstickPress.IsPressed();
            stickClick = controls.VR.LeftThumbstickPress.WasPressedThisFrame();
        }

        //detect double-click
        stickDoubleClick = false;
        if (stickClick)
        {
            //is this a doubleclick?
            if (Time.time - stickClickTime < StickDoubleclickSec)
            {
                stickDoubleClick = true;
                stickClick = false;             //avoid double-triggering on both events
            }
            //note the time of this click
            stickClickTime = Time.time;
            stickDownTime = Time.time;
        }

        //process input
        if (stickDown)
        {
            stickDownElapsed = Time.time - stickDownTime;
        }
        else
        {
            stickDownTime = 0;
            stickDownElapsed = 0;
        }

        //turn on and off
        bool wasActive = Activate;
        if (stickDoubleClick) Activate = !Activate;
        if (!wasActive && Activate)     //turning it on
        {
            Mode = 0;
            PlaySound(soundActivate);
        }
        if (wasActive && !Activate)     //turning it off
        {
            PlaySound(soundDeactivate);
            Save();
        }
    }

    private void UpdatePosition()
    {
        if (handedness == LeftRight.Left) transform.localPosition = Camera.main.transform.right * LableSideOffset;
        if (handedness == LeftRight.Right) transform.localPosition = -Camera.main.transform.right * LableSideOffset;
        if (Camera.main && canvas) canvas.transform.LookAt(Camera.main.transform.position);
    }

    private void UpdateMode()
    {
        //bail if the mode control hasn't been hit
        if (stickClick)
        {
            Mode = (HandEditModes)(((int)Mode + 1) % 4);
            textbox.text = GetModeName();
            PlaySound(soundModeChange);
            Save();
        }
    }

    private void EnactMode()
    {
        switch (Mode)
        {
            case HandEditModes.PosXZ: EnactPosXZ(); break;
            case HandEditModes.PosY: EnactPosY(); break;
            case HandEditModes.RotXZ: EnactRotXZ(); break;
            case HandEditModes.RotY: EnactRotY(); break;
            default: return;
        }
    }

    private void EnactPosXZ()
    {
        //if (stickDelta.magnitude == 0) return;
        stickDelta *= PositionMoveRate * Time.deltaTime;
        if (stickDelta.magnitude > 0) PlaySound(soundAdjustment);
        data.position.x += stickDelta.x;
        data.position.z += stickDelta.y;
    }

    private void EnactPosY()
    {
        //if (stickDelta.magnitude == 0) return;
        stickDelta *= PositionMoveRate * Time.deltaTime;
        if (stickDelta.magnitude > 0) PlaySound(soundAdjustment);
        data.position.y += stickDelta.y;
    }

    private void EnactRotXZ()
    {
        //if (stickDelta.magnitude == 0) return;
        stickDelta *= RotationMoveRate * Time.deltaTime;
        if (stickDelta.magnitude > 0) PlaySound(soundAdjustment);
        data.rotation.x += stickDelta.x;
        data.rotation.z += stickDelta.y;
    }

    private void EnactRotY()
    {
        //if (stickDelta.magnitude == 0) return;
        stickDelta *= RotationMoveRate * Time.deltaTime;
        if (stickDelta.magnitude > 0) PlaySound(soundAdjustment);
        data.rotation.y += stickDelta.x;
    }

    private string GetModeName()
    {
        switch (Mode)
        {
            case HandEditModes.PosXZ: return "Position\n(XZ)";
            case HandEditModes.PosY: return "Position\n(Y)";
            case HandEditModes.RotXZ: return "Rotate\n(XZ)";
            case HandEditModes.RotY: return "Rotate\n(Y)";
            default: return "";
        }
    }


    void PlaySound(AudioClip clip, float pitch = 1)
    {
        if (!audio) return;
        if (!clip) return;
        audio.pitch = pitch;
        audio.PlayOneShot(clip, BaseVolume);
    }


    private void Load()
    {
        string json = null;

        //try the custom file first
        if (File.Exists(filePath))
        {
            try
            {
                json = File.ReadAllText(filePath);
            }
            catch (System.Exception e)
            {
                Debug.LogError($"Failed to read custom data for {handedness} hand: " + e.Message);
            }

            //custom file loaded, parse it
            if (json != null)
            {
                try
                {
                    data = JsonUtility.FromJson<HandOffsetEntry>(json);
                }
                catch (System.Exception e)
                {
                    Debug.LogError("Failed to parse custom VRMounts JSON: " + e.Message);
                }
            }
        }
    }

    private void Save()
    {
        string json = JsonUtility.ToJson(data, true);
        File.WriteAllText(filePath, json);
    }

}
