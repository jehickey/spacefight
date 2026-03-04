using System.Collections.Generic;
using UnityEngine;

public class PropulsionSystem : MonoBehaviour
{
    public ThrottleSystem throttle;
    public List<Engine> engines = new List<Engine>();

    private void OnEnable()
    {
        throttle = GetComponent<ThrottleSystem>();
        engines.Clear();
        engines.AddRange(GetComponentsInChildren<Engine>());
    }

    void Update()
    {
        UpdateEngines();
        
    }

    void UpdateEngines()
    {
        foreach (Engine engine in engines)
        {
            if (engine)
            {
                engine.Level = throttle ? throttle.Actual : 0;
            }
        }
    }

}
