using UnityEngine;

public class Cockpit : MonoBehaviour
{
    private Ship ship;

    public SequentialLightPanel ThrottleLightPanel;

    private ThrottleSystem throttle;


    private void OnEnable()
    {
        InitComponents();
        throttle = GetComponentInParent<ThrottleSystem>();

    }


    void Update()
    {
        //if (ThrottleLightPanel) ThrottleLightPanel.Value = throttle.Actual;
    }


    private void InitComponents()
    {
        if (!ship) ship = GetComponentInParent<Ship>();
        if (!ship)
        {
            Debug.Log("Cockpit can't find ship!");
            return;
        }

    }
}
