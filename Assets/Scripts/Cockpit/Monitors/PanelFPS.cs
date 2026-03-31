using UnityEngine;
using UnityEngine.UI;

public class PanelFPS : MonoBehaviour
{

    public Text txtFPS;

    void Start()
    {
        
    }

    void Update()
    {
        if (txtFPS)
        {
            int fps = 0;
            if (Game.I) fps = (int)Game.I.FPS;
            txtFPS.text = $"{fps} FPS";
        }
    }
}
