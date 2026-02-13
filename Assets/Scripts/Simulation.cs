using UnityEngine;

public class Simulation : MonoBehaviour
{
    public float TimeScale = 1f;
    public float TimeDelta => Time.deltaTime * TimeScale;
    public Ship PlayerShip;

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
