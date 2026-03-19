using UnityEngine;

public class Monitor : MonoBehaviour
{
    [TextArea]
    public string TextContent = "Testing";
    public int FontSize = 50;
    public bool Scrolling;

    public Color BackgroundColor = Color.black;
    public float StaticStrength = .5f;

    public MonitorScreen screenDisplay;
    public MonitorTextDisplay textDisplay;

    void Start()
    {
        screenDisplay = GetComponentInChildren<MonitorScreen>();
        textDisplay = GetComponentInChildren<MonitorTextDisplay>();
        if (Scrolling) textDisplay.RandomizeScrolling();
    }

    void Update()
    {
        if (textDisplay)
        {
            textDisplay.TextContent = TextContent;
            textDisplay.FontSize = FontSize;
            textDisplay.Scrolling = Scrolling;
        }
        if (screenDisplay)
        {
            screenDisplay.backgroundColor = BackgroundColor;
            screenDisplay.staticStrength = StaticStrength;
        }
        
    }
}
