using UnityEngine;
using UnityEngine.UI;

public class HUD : MonoBehaviour
{

    [Header("Reticule")]
    public Image Reticule;
    public Image ReticuleMask;
    public Image StickPosition;
    public RectTransform StickPositionLine;

    [Header("Horizon Indicator")]
    public RectTransform HorizonLine;
    public Color HorizonColor = Color.white;
    public float HorizonInstabilityMin = .94f;
    public float HorizonInstabilityMax = 0.95f;
    private Image horizonImage;

    public Ship ship;

    private Canvas canvas;
    private RectTransform canvasRect;
    private Simulation sim;

    private float screenSize;
    private float reticleSize;

    private SteeringSystem steering;


    private void OnEnable()
    {
        if (!sim) sim = FindFirstObjectByType<Simulation>();
        if (!canvas) canvas = GetComponent<Canvas>();
        if (!canvasRect) canvasRect = canvas.GetComponent<RectTransform>();
        if (!steering) steering = GetComponentInParent<SteeringSystem>();
        if (!steering)
        {
            Debug.Log("HUD can't find SteeringSystem");
        }
    }

    void Update()
    {
        screenSize = Mathf.Min(canvasRect.rect.width, canvasRect.rect.height);
        UpdateReticule();
        UpdateStickPosition();
    }

    private void UpdateReticule()
    {
        if (!Reticule || !ship) return;
        reticleSize = sim.StickControlDeadzone * screenSize;
        Reticule.rectTransform.sizeDelta = Vector2.one * reticleSize;
        if (ReticuleMask) ReticuleMask.rectTransform.sizeDelta = Reticule.rectTransform.sizeDelta;
        UpdateHorizonIndicator();
    }

    private void UpdateStickPosition()
    {
        //work out radius for maximum stick position (so it's not square)
        if (!StickPosition) return;
        float StickMax = screenSize * sim.StickControlLimit * .5f;
        Vector2 pos = Vector2.zero;
        if (ship)
        {
            pos.x = steering.realStick.x * StickMax;
            pos.y = steering.realStick.z * StickMax;
        }
        StickPosition.rectTransform.anchoredPosition = pos;
    }

    private void UpdateHorizonIndicator()
    {
        //Create the HorizonLine and components if they don't exist yet
        if (!HorizonLine)
        {
            float horizonThickness = 2;
            HorizonLine = CreateLine(ReticuleMask.transform, horizonThickness);
            horizonImage = HorizonLine.GetComponent<Image>();
            //create "up" marker
            float UpMarkWidth = reticleSize * .1f;
            float UpMarkThicknes = horizonThickness * 2f;
            RectTransform upMarker = CreateLine(HorizonLine, 2);
            upMarker.anchoredPosition = new Vector2(0, UpMarkThicknes*2);
            upMarker.sizeDelta = new Vector2(10, UpMarkThicknes);
        }

        Quaternion orient = ship.transform.rotation;
        float reticleScale = reticleSize / 180f;

        //Roll
        // Project ecliptic up into ship space
        float ux = Vector3.Dot(Vector3.up, ship.transform.right);
        float uy = Vector3.Dot(Vector3.up, ship.transform.up);
        // 2D direction of “up” in the HUD
        Vector2 up2D = new Vector2(ux, uy);
        // Horizon line is perpendicular to this
        float angleDeg = Mathf.Atan2(up2D.y, up2D.x) * Mathf.Rad2Deg - 90f;
        HorizonLine.localRotation = Quaternion.Euler(0f, 0f, angleDeg);

        //Position
        // Pitch relative to ecliptic
        float pitchDeg = -Mathf.Asin(ship.transform.forward.y) * Mathf.Rad2Deg;

        // How much the ship is rolled relative to the ecliptic
        float rollInfluence = Vector3.Dot(ship.transform.right, Vector3.up);

        // Convert pitch and heading to UI offset
        float yOffset = pitchDeg * reticleScale;
        float xOffset = rollInfluence * (pitchDeg * reticleScale);
        HorizonLine.anchoredPosition = new Vector2(xOffset, yOffset);

        //fade horizon when at unstable angles
        HorizonColor.a = 1;
        float horizonInstability = Mathf.Abs(Vector3.Dot(ship.transform.forward, Vector3.up));
        if (horizonInstability > HorizonInstabilityMin)
        {
            float t = Mathf.InverseLerp(HorizonInstabilityMin, HorizonInstabilityMax, horizonInstability);
            HorizonColor.a = 1f-t;
        }
        horizonImage.color = HorizonColor;

    }

    private RectTransform CreateLine(Transform parent, float thickness)
    {
        GameObject go = new GameObject("HorizonLine", typeof(Image));
        go.transform.SetParent(parent, false);
        Image img = go.GetComponent<Image>();
        img.sprite = CreatePixelSprite();
        img.color = Color.white;
        RectTransform rect = img.rectTransform;
        //rect.anchoredPosition = Vector2.zero;
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.sizeDelta = new Vector2(200, thickness);
        return rect;
    }


    private static Sprite CreatePixelSprite()
    {
        Texture2D tex = new Texture2D(1, 1);
        tex.SetPixel(0, 0, Color.white);
        tex.Apply();
        return Sprite.Create(tex, new Rect(0, 0, 1, 1), Vector2.one * .5f);
    }


}
