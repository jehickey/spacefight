using UnityEngine;
using UnityEngine.SceneManagement;

public class Game : MonoBehaviour
{
    public Team PlayerTeam;

    private FlightControls controls;

    void OnEnable()
    {
        controls = new FlightControls();
    }

    void Update()
    {
        if (controls.Game.Exit.IsPressed()) Application.Quit();
        if (controls.Game.Restart.IsPressed()) SceneManager.LoadSceneAsync(SceneManager.GetActiveScene().name);
        
    }
}
