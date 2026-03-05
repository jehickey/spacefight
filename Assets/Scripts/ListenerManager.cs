using UnityEngine;

public class ListenerManager : MonoBehaviour
{
    private AudioListener listener;


    private void OnEnable()
    {
        listener = GetComponent<AudioListener>();
    }

    void Update()
    {
        if (!Game.I) return;
        if (Game.I.PlayerShip)
        {
            transform.position = Game.I.PlayerShip.transform.position;
        }
    }
}
