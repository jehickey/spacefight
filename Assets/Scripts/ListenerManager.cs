using UnityEngine;

public class ListenerManager : MonoBehaviour
{
    private Game game;
    private AudioListener listener;


    private void OnEnable()
    {
        game = GetComponentInParent<Game>();
        listener = GetComponent<AudioListener>();
    }

    void Update()
    {
        if (!game) return;
        if (game.PlayerShip)
        {
            transform.position = game.PlayerShip.transform.position;
        }
    }
}
