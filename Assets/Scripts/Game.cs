using Shapes;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.XR;

[DisallowMultipleComponent]
public class Game : MonoBehaviour
{
    public static Game I { get; private set; }

    public PlayerController player;
    public GameObject PlayerPrefab;
    public Ship PlayerShip;
    public GameObject PlayerShipPrefab;
    public Team PlayerTeam;
    public bool Paused = false;
    public int KillCount = 0;
    public int DeathCount = 0;

    [Header("VR Settings")]
    public bool EnableVR = true;
    public float VRInputTimeout = 5;
    [ReadOnly]
    public bool VRHeadset = false;
    public bool EnableVRMountEditing = false;
    private UnityEngine.XR.InputDevice headDevice;
    private Vector3 lastHeadsetPosition = Vector3.zero;
    private Quaternion lastHeadsetRotation = Quaternion.identity;
    private float headsetDeadTime = 0;


    [Header("Jump Drive")]
    [SerializeField]
    private JumpLocation JumpLoc;
    [ReadOnly]
    public int JumpLocationCount;
    public float JumpSelectionAngle = 5;       //how close do they have to get to trigger selection?
    public bool doNewJump = false;
    public bool doClearJump = false;


    [Header("Gameplay Settings")]
    public bool useInvulnerability;
    public bool useInfiniteBoost;
    public bool useSpawnEnemies;
    public bool useSpawnPlayer;

    [Header("Respawn")]
    public bool forceRespawn = false;
    public Body RespawnTarget;
    public string RespawnTargetName;
    public float RespawnRadii = 10;
    public float RespawnCountdown = 3;
    private float respawnCount;
    private float respawnCountdownStart;
    private Ship lastPlayerShip;

    [Header("Engine Settings")]
    public bool useMonitorScreens;
    public bool usePrecache;
    public float precacheStatus;

    private FlightControls controls;
    private OverlayManager overlay;

    //fps info
    private float updateInterval = .5f;
    private int frames = 0;
    private float timeAccumulator = 0;
    public float FPS = 0;


    [Header("Flight Control Settings")]
    public bool InvertPitchAxis = false;
    public bool TurnStickToRoll = false;
    public bool PlanetaryRollAdjustment = true;  //use auto-roll to stay vertical near a planet
    public float StickControlLimit = 0.5f;       //this is a percentage of the screen
    public float StickControlDeadzone = 0.25f;   //this is a percentage of the screen


    [Header("Jump Drive Effect Settings")]
    public float JumpFOVNormal = 64;
    public float JumpFOVFull = 100;
    public float JumpFOVFactor = .3f;
    public float JumpMinTime = 5;                //minimum time a jump can take
    public float JumpMaxTime = 15;               //maximum time a jump can take
    public float JumpBuildupTime = 5;            //how long for effect to build up?
    public float JumpRampdownTime = 5;           //how long for effect to die down?
    public float JumpEffectsStrength = 1;        //manages visual effects
    public float JumpMinHue = 0;
    public float JumpMaxHue = .66f;
    public float JumpSaturationDelta = .75f;
    public float JumpBrightnessBoost = .5f;
    public float JumpBrightnessRange = .5f;
    public float JumpEnemyDespawnProgress = .25f;
    public float JumpChargeTime = 3f;


    public float ActivationCountdown = 3;       //how long after spawn before enemies "wake up"


    [Header("Audio Settings")]
    public float AudioCutoffRange = 3;         //how close before audio is activated
    public float AudioCutoffPadding = 1;        //how far out of range before audio is deactivated
    public float AudioExternalSuppression = .5f;    //How much to suppress audio from outside ship
    public float AudioLevelWeapons = 1;
    public float AudioLevelEngines = 1;
    public float AudioLevelExplosions = 1;
    public AudioClip defaultSoundHit;
    public AudioClip defaultSoundExplosion;


    private void Awake()
    {
        if (!Application.isPlaying) return;
        if (I && I!=this)
        {
            Debug.Log("An instance of Game already exists!");
            //Destroy(gameObject);
            return;
        }
        I = this;
    }

    private void OnDestroy()
    {
        if (I == this) I = null;
        Shapes.Icosphere.Cleanup();     //make sure all running jobs are killed
    }

    void OnEnable()
    {
        if (!Application.isPlaying) return;
        if (!I) I = this;       //so it runs on domain reload
        if (controls==null) controls = new FlightControls();
        controls.Enable();
        overlay = GetComponentInChildren<OverlayManager>();
        //controls.Flight.Enable();

        //if (!PlayerShipPrefab) Debug.Log("No player ship prefab set in Game!");
        //if (!PlayerTeam) Debug.Log("No player team assigned in Game!");
    }

    private void OnDisable()
    {
        controls?.Disable();
    }

    private void Start()
    {
        if (usePrecache) Icosphere.PreCache(Body.MaxDetailGlobal);
        StaticGenerator.Generate(20,256,.05f);
        //NewJump();
    }

    void Update()
    {
        //run this to keep the job system moving
        if (usePrecache) precacheStatus = Shapes.Icosphere.GetStatus();

        if (controls.Game.Exit.WasPressedThisFrame()) Application.Quit();
        if (controls.Game.Restart.WasPressedThisFrame()) SceneManager.LoadSceneAsync(SceneManager.GetActiveScene().name);
        if (controls.Game.Pause.WasPressedThisFrame()) Paused = !Paused;
        if (controls.Game.ShowFPS.WasPressedThisFrame()) overlay.ShowFPS = !overlay.ShowFPS;
        if (controls.Game.ToggleEnemies.WasPressedThisFrame()) useSpawnEnemies = !useSpawnEnemies;

        if (Paused)
        {
            Cursor.lockState = CursorLockMode.Confined;
            Cursor.visible = false;
        }
        else
        {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }

        if (overlay)
        {
            overlay.Paused = Paused;
            overlay.scoreKills = KillCount;
            overlay.scoreDeaths = DeathCount;
            overlay.FPS = FPS;
        }
        if (Paused)
        {
            Time.timeScale = 0;
        }
        else
        {
            Time.timeScale = 1;
        }

        SetupPlayer();
        UpdateRespawn();

        //fps management
        frames++;
        timeAccumulator += Time.unscaledDeltaTime;
        if (timeAccumulator >= updateInterval)
        {
            FPS = frames / timeAccumulator;
            frames = 0;
            timeAccumulator = 0;
        }

        if (doNewJump) NewJump();
        if (doClearJump) ClearJump();
        JumpLocationCount = JumpLoc ? 1 : 0;

        VRHeadset = IsHeadsetWorn();
    }


    private void OnDrawGizmos()
    {
        if (!JumpLoc) return;
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(JumpLoc.Coordinate, 5);
        //draw line leading to jump point
        Gizmos.DrawLine(PlayerShip.transform.position, JumpLoc.Coordinate);
    }


    public static bool IsVRActive()
    {
        List<XRDisplaySubsystem> displays = new List<XRDisplaySubsystem>();
        SubsystemManager.GetSubsystems(displays);
        foreach (var d in displays)
        {
            if (d.running)
            return true;
        }
        return false;
    }

    public bool IsHeadsetWorn()
    {
        if (!EnableVR) return false;
        if (!IsVRActive()) return false;

        if (!headDevice.isValid)
        {
            List<UnityEngine.XR.InputDevice> devices = new List<UnityEngine.XR.InputDevice>();
            InputDevices.GetDevicesAtXRNode(XRNode.Head, devices);
            if (devices.Count > 0)
            {
                headDevice = devices[0];
            }
        }
        if (!headDevice.isValid) return false;
        bool userPresent = false;
        headDevice.TryGetFeatureValue(UnityEngine.XR.CommonUsages.userPresence, out userPresent);

        if (!userPresent) return false;
        return CheckVRInputDetected();
    }


    private bool CheckVRInputDetected()
    {


        //get a position update
        Vector3 pos = controls.VR.HeadPosition.ReadValue<Vector3>();
        Quaternion rot = controls.VR.HeadRotation.ReadValue<Quaternion>();

        //compare to previous position
        if (pos != lastHeadsetPosition || rot != lastHeadsetRotation)   //movement detected
        {
            headsetDeadTime = 0;
            lastHeadsetPosition = pos;
            lastHeadsetRotation = rot;
            return true;
        }

        headsetDeadTime += Time.deltaTime;
        return headsetDeadTime < VRInputTimeout;
    }


    private void SetupPlayer()
    {
        if (!player) player = GameObject.FindFirstObjectByType<PlayerController>();
        if (!player)
        {
            if (!PlayerPrefab)
            {
                Debug.Log("Player Prefab not assigned in Game!");
                return;
            }
            GameObject obj = Instantiate(PlayerPrefab);
            if (obj)
            {
                player = obj.GetComponent<PlayerController>();
                if (player) player.transform.position = Vector3.zero;
            }
        }
    }


    public void AddKill()
    {
        KillCount++;
    }

    public void AddDeath()
    {
        DeathCount++;
    }

    public void DespawnEnemies()
    {
        foreach (BotControl bot in GameObject.FindObjectsByType<BotControl>( FindObjectsSortMode.None))
        {
            Destroy(bot.gameObject);
        }
    }


    private void NewJump()
    {
        Debug.Log("Setting up a new jump location");
        doNewJump = false;
        if (!PlayerShip) return;
        JumpLoc = null;
        string targetName = "Europa";
        GameObject obj = GameObject.Find(targetName);
        if (!obj) 
        {
            Debug.Log($"NewJump couldn't find {targetName}");
            return; 
        }
        Body body = obj.GetComponent<Body>();
        if (!body)
        {
            Debug.Log($"NewJump couldn't find a Body in {targetName}");
            return;
        }
        Vector3 dir = PlayerShip.transform.position - body.transform.position;
        float dist = dir.magnitude;
        dir.Normalize();
        JumpLoc = new JumpLocation(body.name, dist, true);
        Vector3 offset = dir * body.Radius * 3;
        JumpLoc.Coordinate = body.transform.position + offset;
        JumpLoc.Distance = (JumpLoc.Coordinate - PlayerShip.transform.position).magnitude;
        JumpLoc.Target = body;
        JumpLoc.Selected = false;
        JumpLoc.Available = true;
    }

    public List<JumpLocation> GetJumps()
    {
        List<JumpLocation> result = new List<JumpLocation>();
        if (JumpLoc != null && JumpLoc.Available) result.Add(JumpLoc);
        return result;
    }

    public void ClearJump()
    {
        doClearJump = false;
        JumpLoc = null;

    }

    private void UpdateRespawn() {
        //player respawn
        //lastPlayerShip = PlayerShip;
        if (!useSpawnPlayer) return;
        if (Paused) return;

        if (forceRespawn)
        {
            forceRespawn = false;
            if (PlayerShip)
            {
                Destroy(PlayerShip.gameObject);
                PlayerShip = null;
                respawnCount = .001f;
            }
        }

        if (overlay) overlay.Countdown = Mathf.CeilToInt(respawnCount);
        if (PlayerShip) return;
        if (!PlayerShip)
        {
            //first see if there is a Player Ship somewhere
            PlayerMountPoint mount = FindFirstObjectByType<PlayerMountPoint>();
            if (mount)        //found one
            {
                PlayerShip = mount.GetComponentInParent<Ship> ();
                forceRespawn = false;
                return;
            }

            if (respawnCount == 0)                      //no countdown has started yet
            {
                //respawnCount = RespawnCountdown;
                respawnCountdownStart = Time.time;
            }
            respawnCount = RespawnCountdown - (Time.time - respawnCountdownStart);
            if (respawnCount <= 0) RespawnPlayerShip();
        }


    }

    private void RespawnPlayerShip()
    {
        if (PlayerShip) return;
        if (!PlayerShipPrefab) return;
        respawnCount = 0;

        if (RespawnTargetName == string.Empty) RespawnTargetName = "Jupiter";
        RespawnTarget = GameObject.Find(RespawnTargetName)?.GetComponent<Body>();

        GameObject obj = Instantiate(PlayerShipPrefab);
        if (obj)
        {
            PlayerShip = obj.GetComponent<Ship>();
            if (PlayerShip)
            {
                PlayerShip.team = PlayerTeam;
                //pick a spot and orientation
                if (RespawnTarget)
                {
                    PlayerShip.transform.position = RespawnTarget.transform.position + Random.onUnitSphere * RespawnRadii * RespawnTarget.Radius;
                    PlayerShip.transform.LookAt(RespawnTarget.transform.position);
                }
                else
                {
                    PlayerShip.transform.position = Vector3.zero;
                }

            }
        }

    }

}
