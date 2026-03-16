using UnityEngine;

public class Cockpit : MonoBehaviour
{
    private Ship ship;



    private void OnEnable()
    {
        InitComponents();
    }


    void Update()
    {
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
