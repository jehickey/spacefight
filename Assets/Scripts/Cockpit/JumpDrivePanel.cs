using UnityEngine;
using UnityEngine.UI;

public class JumpDrivePanel : MonoBehaviour
{
    public Text Readout;
    public Color readoutColor;

    public Button button;
    public new IndicatorLight light;
    public Color LightColor = Color.white;
    public float LightBrightness = 5;

    private Jumpdrive jumpdrive;

    private void OnEnable()
    {
        if (button) button.Pressed += OnJumpPressed;
        jumpdrive = GetComponentInParent<Jumpdrive>();
    }

    private void OnDisable()
    {
        if (button) button.Pressed -= OnJumpPressed;
    }

    void Start()
    {
        
    }

    void Update()
    {
        ManageLight();
        ManageReadout();
        
    }

    public void Engage()
    {
        if (button) button.Press();
    }

    private void OnJumpPressed() 
    {
        if (!jumpdrive) return;
        jumpdrive.Activate();
    }

    private void ManageLight()
    {
        if (!light) return;
        if (!jumpdrive)
        {
            light.On = false;
            light.color = LightColor * .25f;
            return;
        }
        light.color = LightColor;
        if (!jumpdrive.Available) light.color = LightColor * .25f;
        light.Level = LightBrightness;
        light.On = jumpdrive.Available;
    }

    private void ManageReadout()
    {
        if (!Readout) return;
        if (!jumpdrive.Available)
        {
            Readout.text = "";
            return;
        }

        if (jumpdrive.TimeRemaining > 0)
        {
            Readout.text = $"{jumpdrive.TimeRemaining} sec";
        }
        else
        {
            Readout.text = "---";
        }
    }

}
