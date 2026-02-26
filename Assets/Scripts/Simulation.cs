using UnityEngine;

public class Simulation : MonoBehaviour
{
    public float TimeScale = 1f;
    public float TimeDelta => Time.deltaTime * TimeScale;
    public Ship PlayerShip;

    [Header("Config Settings")]
    public float StickControlLimit = 0.5f;       //this is a percentage of the screen
    public float StickControlDeadzone = 0.25f;   //this is a percentage of the screen

    public float SpeedUnit = 1f;

    void Start()
    {
        //look for any competing instances and warn (don't delete)
        foreach (Simulation sim in FindObjectsByType<Simulation>(FindObjectsSortMode.None))
        {
            if (sim != this) Debug.LogError("Multiple Simulation instances found!");
        }
    }

    void Update()
    {
        
    }
}
