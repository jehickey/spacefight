using UnityEngine;
using UnityEngine.UI;

public class HUD : MonoBehaviour
{

    public Image Reticule;
    public Image StickPosition;
    public Image StickPositionLine;

    public Ship ship;

    private Canvas canvas;
    private RectTransform canvasRect;
    private Simulation sim;

    private float screenSize;

    void Start()
    {
        
    }

    private void OnEnable()
    {
        if (!sim) sim = FindFirstObjectByType<Simulation>();
        if (!canvas) canvas = GetComponent<Canvas>();
        if (!canvasRect) canvasRect = canvas.GetComponent<RectTransform>();
    }

    void Update()
    {
        screenSize = Mathf.Min(canvasRect.rect.width, canvasRect.rect.height);
        UpdateReticule();
        UpdateStickPosition();
    }

    private void UpdateReticule()
    {
        if (!Reticule) return;
        Reticule.rectTransform.sizeDelta = Vector2.one * sim.StickControlDeadzone * screenSize;
    }

    private void UpdateStickPosition()
    {
        //work out radius for maximum stick position (so it's not square)
        if (!StickPosition) return;
        float StickMax = screenSize * sim.StickControlLimit * .5f;
        Vector2 pos = Vector2.zero;
        if (ship)
        {
            pos.x = ship.realStick.x * StickMax;
            pos.y = ship.realStick.z * StickMax;
        }
        StickPosition.rectTransform.anchoredPosition = pos;


    }

}
