using UnityEngine;
using UnityEngine.SceneManagement;

public class Game : MonoBehaviour
{
    public Ship PlayerShip;
    public GameObject PlayerShipPrefab;
    public Team PlayerTeam;
    public GameObject RespawnTarget;
    public float RespawnDistance = 10;
    public float RespawnCountdown = 3;
    private float respawnCount;
    private float respawnCountdownStart;
    private Ship lastPlayerShip;

    public bool Paused = false;
    public int KillCount = 0;
    public int DeathCount = 0;

    private FlightControls controls;
    private OverlayManager overlay;

    private Vector3 deathcamPos = Vector3.zero;
    private Quaternion deathcamRot = Quaternion.identity;
    private Camera deathcam;

    void OnEnable()
    {
        if (controls==null) controls = new FlightControls();
        controls.Enable();
        overlay = GetComponentInChildren<OverlayManager>();
        //controls.Flight.Enable();

        if (!PlayerShipPrefab) Debug.Log("No player ship prefab set in Game!");
        if (!PlayerTeam) Debug.Log("No player team assigned in Game!");
    }

    private void OnDisable()
    {
        controls?.Disable();
    }

    void Update()
    {

        if (controls.Game.Exit.WasPressedThisFrame()) Application.Quit();
        if (controls.Game.Restart.WasPressedThisFrame()) SceneManager.LoadSceneAsync(SceneManager.GetActiveScene().name);
        if (controls.Game.Pause.WasPressedThisFrame()) Paused = !Paused;

        if (overlay)
        {
            overlay.Paused = Paused;
            overlay.scoreKills = KillCount;
            overlay.scoreDeaths = DeathCount;
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
        overlay.Countdown = Mathf.CeilToInt(respawnCount);
        if (PlayerShip) return;
        if (!PlayerShip)
        {
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
