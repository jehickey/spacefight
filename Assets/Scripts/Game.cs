using System.Collections.Generic;
using Shapes;
using Unity.VisualScripting;
using UnityEditor.ShaderGraph.Internal;
using UnityEngine;
using UnityEngine.SceneManagement;

[DisallowMultipleComponent]
public class Game : MonoBehaviour
{
    public static Game I { get; private set; }

    public Ship PlayerShip;
    public GameObject PlayerShipPrefab;
    public Team PlayerTeam;
    public bool Paused = false;
    public int KillCount = 0;
    public int DeathCount = 0;

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
    public GameObject RespawnTarget;
    public float RespawnDistance = 10;
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

    //deathcam - if it's ever used
    private Vector3 deathcamPos = Vector3.zero;
    private Quaternion deathcamRot = Quaternion.identity;
    private Camera deathcam;

    //fps info
    private float updateInterval = .5f;
    private int frames = 0;
    private float timeAccumulator = 0;
    public float FPS = 0;


    [Header("Control Settings")]
    public bool InvertPitchAxis = false;
    public float StickControlLimit = 0.5f;       //this is a percentage of the screen
    public float StickControlDeadzone = 0.25f;   //this is a percentage of the screen


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


        


        //maintain info for deathcam
        if (Camera.main)
        {
            deathcamPos = Camera.main.transform.position;
            deathcamRot = Camera.main.transform.rotation;
        }
        if (!PlayerShip && !deathcam)
        {
            /*
            deathcam=new GameObject("Deathcam").AddComponent<Camera>();
            deathcam.transform.position=deathcamPos;
            deathcam.transform.rotation=deathcamRot;
            deathcam.backgroundColor = Color.black;
            deathcam.clearFlags = CameraClearFlags.SolidColor;
            deathcam.nearClipPlane = 0.01f;
            */
        }
        if (PlayerShip && deathcam) Destroy(deathcam);

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

    }


    private void OnDrawGizmos()
    {
        if (!JumpLoc) return;
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(JumpLoc.Coordinate, 5);
        //draw line leading to jump point
        Gizmos.DrawLine(PlayerShip.transform.position, JumpLoc.Coordinate);
    }

    public void AddKill()
    {
        KillCount++;
    }

    public void AddDeath()
    {
        DeathCount++;
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
        if (overlay) overlay.Countdown = Mathf.CeilToInt(respawnCount);
        if (PlayerShip) return;
        if (!PlayerShip)
        {
            //first see if there is a Player somewhere
            KeyboardControl playerInput = FindFirstObjectByType<KeyboardControl>();
            if (playerInput)        //found one
            {
                PlayerShip = playerInput.GetComponentInParent<Ship> ();
                return;
            }

            if (respawnCount == 0)                      //no countdown has started yet
            {
                //respawnCount = RespawnCountdown;
                respawnCountdownStart = Time.time;
            }
            respawnCount = RespawnCountdown - (Time.time - respawnCountdownStart);
            if (respawnCount <= 0) RespawnPlayer();
        }
    }

    private void RespawnPlayer()
    {
        if (PlayerShip) return;
        if (!PlayerShipPrefab) return;
        respawnCount = 0;
        if (deathcam) Destroy(deathcam.gameObject);

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
                    PlayerShip.transform.position = RespawnTarget.transform.position + Random.onUnitSphere * RespawnDistance;
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
