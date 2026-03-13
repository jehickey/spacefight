using Shapes;
using UnityEngine;
using UnityEngine.SceneManagement;

[DisallowMultipleComponent]
public class Game : MonoBehaviour
{
    public static Game I { get; private set; }

    public Ship PlayerShip;
    public GameObject PlayerShipPrefab;
    public Team PlayerTeam;
    public GameObject RespawnTarget;
    public float RespawnDistance = 10;
    public float RespawnCountdown = 3;
    private float respawnCount;
    private float respawnCountdownStart;
    private Ship lastPlayerShip;

    public bool InvertPitchAxis = false;

    public bool Paused = false;
    public int KillCount = 0;
    public int DeathCount = 0;

    public bool SpawnEnemies;

    private FlightControls controls;
    private OverlayManager overlay;

    private Vector3 deathcamPos = Vector3.zero;
    private Quaternion deathcamRot = Quaternion.identity;
    private Camera deathcam;

    //fps info
    private float updateInterval = .5f;
    private int frames = 0;
    private float timeAccumulator = 0;
    public float FPS = 0;

    public float ActivationCountdown = 3;

    [Header("Control Settings")]
    public float StickControlLimit = 0.5f;       //this is a percentage of the screen
    public float StickControlDeadzone = 0.25f;   //this is a percentage of the screen


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
    }


    void OnEnable()
    {
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
        Icosphere.PreCache(Body.MaxDetailGlobal);
    }

    void Update()
    {
        float status = Icosphere.GetStatus();

        if (controls.Game.Exit.WasPressedThisFrame()) Application.Quit();
        if (controls.Game.Restart.WasPressedThisFrame()) SceneManager.LoadSceneAsync(SceneManager.GetActiveScene().name);
        if (controls.Game.Pause.WasPressedThisFrame()) Paused = !Paused;
        if (controls.Game.ShowFPS.WasPressedThisFrame()) overlay.ShowFPS = !overlay.ShowFPS;
        if (controls.Game.ToggleEnemies.WasPressedThisFrame()) SpawnEnemies = !SpawnEnemies;

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


    }

    public void AddKill()
    {
        KillCount++;
    }

    public void AddDeath()
    {
        DeathCount++;
    }

    private void UpdateRespawn() {
        //player respawn
        //lastPlayerShip = PlayerShip;
        if (Paused) return;
        if (overlay) overlay.Countdown = Mathf.CeilToInt(respawnCount);
        if (PlayerShip) return;
        if (!PlayerShip)
        {
            //first see if there is a Player somewhere
            Cockpit cockpit = FindFirstObjectByType<Cockpit>();
            if (cockpit)        //found one
            {
                PlayerShip = cockpit.GetComponentInParent<Ship> ();
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
