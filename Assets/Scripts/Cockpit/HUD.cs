using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class HUD : MonoBehaviour
{

    [Header("Reticule")]
    public RectTransform ReticuleContainer;
    public Image Reticule;
    public Image ReticuleMask;
    public Color ReticleColor = Color.white;
    public bool useAltimeter = true;
    public Text Altimeter;
    public Color AltimeterColor= Color.white;
    public float AltimeterHeight = .9f;
    private RectTransform reticuleRect;


    [Header("Stick Position")]
    public Image StickPosition;
    public RectTransform StickPositionLine;
    public Color StickPositionColor = Color.white;

    [Header("Horizon Indicator")]
    public RectTransform HorizonLine;
    public Color HorizonColor = Color.white;
    public float HorizonInstabilityMin = .94f;
    public float HorizonInstabilityMax = 0.95f;
    private Image horizonImage;

    [Header("Text Display")]
    public Text TextDisplay;
    public string TextContent;
    public Color TextColor = Color.white;
    public float TextCPS = 5;           //characters per second
    public bool TextFade = true;
    public float TextFadeDelay = .1f;   //time to wait before fading (per character)
    public float TextFadeSpeed = 2;     //how many seconds it takes to fade (once it starts)
    private float textFadeAlpha;
    private float textFinishTime;       //used for tracking fade-out
    public bool ShowCursor = true;
    public float CursorBlinkRate = 5;  //how fast the cursor blinks
    private string actualText;          //text after teletype effect
    private string oldText;             //used for identifying if text has changed
    private float lastCharTime;         //used for timing


    [Header("Jump Destinations")]
    public float JumpReticleSize = 10;
    public float JumpReticlePulseRate = 3;

    private Ship ship;
    private Canvas canvas;
    private RectTransform canvasRect;
    private float screenSize;
    private float reticleSize;

    //ship components HUD needs to access
    private SteeringSystem steering;
    private WeaponsSystem weapons;
    private Jumpdrive jump;
    private List<RectTransform> jumpReticles = new List<RectTransform>();

    private new Camera camera;


    private void OnEnable()
    {
        if (!ship) ship = GetComponentInParent<Ship>(); 
        if (!canvas) canvas = GetComponent<Canvas>();
        if (!canvasRect) canvasRect = canvas.GetComponent<RectTransform>();
        if (!steering) steering = GetComponentInParent<SteeringSystem>();
        if (!weapons) weapons = GetComponentInParent<WeaponsSystem>();
        if (!steering)
        {
            Debug.Log("HUD can't find SteeringSystem");
        }
        if (!jump) jump = GetComponentInParent<Jumpdrive>();

        if (ReticuleContainer) reticuleRect = ReticuleContainer.GetComponent<RectTransform>();
    }

    void LateUpdate()
    {
        if (!camera)
        {
            if (Game.I && Game.I.VRHeadset)
            {
                Headset headset = FindFirstObjectByType<Headset>();
                if (headset) camera = headset.camera;
            }
            if (!camera) camera = Camera.main;
        }

        screenSize = Mathf.Min(canvasRect.rect.width, canvasRect.rect.height);
        UpdateTextDisplay();
        UpdateReticule();
        UpdateStickPositionReticule();
        UpdateJumpLocations();
    }


    public void DoText(string text)
    {
        TextContent = text;
        //needs functionality for fade control, blinking/pulsing, etc
    }

    private void UpdateTextDisplay()
    {
        if (!TextDisplay) return;

        //see if text contents have changed
        if (TextContent != oldText)
        {
            actualText = "";
            lastCharTime = 0;
            oldText = TextContent;
            textFinishTime = 0;
            textFadeAlpha = 1;
        }

        if (TextCPS == 0) actualText = TextContent;

        //update text content
        if (actualText != TextContent)
        {
            int index = actualText.Length;
            if (TextContent.Length > index)
            {
                if (Time.time - lastCharTime > 1 / TextCPS)
                {
                    actualText += TextContent.Substring(index, 1);
                    lastCharTime = Time.time;
                }
            }
        }

        //append cursor
        string cursor = "|";
        if (TextContent=="" || textFadeAlpha < 1) cursor = "";  //no cursor when nothing to show (or fading)
        if (actualText == TextContent && TextContent!="" && textFadeAlpha == 1)   //only blink when entire string is printed
        {
            if (Mathf.Sin(Time.time * Mathf.PI * 2 * CursorBlinkRate) < 0) cursor = "";
        }

        //text fading:
        if (TextFade) {
            if (actualText == TextContent && TextContent != "")      //no fading till it's done
            {
                if (textFinishTime == 0) textFinishTime = Time.time;
                if (Time.time - textFinishTime >= TextFadeDelay * actualText.Length)    //is it time to fade?
                {
                    textFadeAlpha -= (1f / TextFadeSpeed) * Time.deltaTime;
                    if (textFadeAlpha <= 0) TextContent = "";
                }
            }
        }

        TextDisplay.text = actualText + cursor;
        Color actualColor = TextColor;
        actualColor.a = textFadeAlpha;
        TextDisplay.color = actualColor;


    }

    private void UpdateReticule()
    {
        if (!Reticule || !ship) return;
        UpdateReticulePosition();
        reticleSize = Game.I.StickControlDeadzone * screenSize * 2f;
        Reticule.rectTransform.sizeDelta = Vector2.one * reticleSize;
        Reticule.color = ReticleColor;
        if (ReticuleMask) ReticuleMask.rectTransform.sizeDelta = Reticule.rectTransform.sizeDelta;
        UpdateHorizonIndicator();
        UpdateAltimeter();
    }

    private void UpdateReticulePosition()
    {
        if (!ReticuleContainer || !ship || !camera) return;
        Plane plane = new Plane(canvas.transform.forward, canvas.transform.position);
        Ray ray = new Ray(camera.transform.position, ship.transform.forward);   //raycast onto canvas plane
        if (!plane.Raycast(ray, out float dist)) return;                        //get a raycast distance
        Vector3 worldHit = ray.GetPoint(dist);                                  //where did it hit? (world)
        Vector3 localHit = canvas.transform.InverseTransformPoint(worldHit);    //localize to canvas
        reticuleRect.anchoredPosition = new Vector2(localHit.x, localHit.y);    //move reticle to hit point
    }

    private void UpdateAltimeter()
    {
        if (!Altimeter || !ship) return;
        if (ship.bodyProximity)
        {
            Altimeter.text = $"{ship.bodyAltitude:0}m";
            Altimeter.color = Altimeter.color;
            Altimeter.rectTransform.anchoredPosition = new Vector2(0, reticleSize*.5f*AltimeterHeight);
        }
        else
        {
            Altimeter.text = "";
        }
    }

    private void UpdateStickPositionReticule()
    {
        if (!StickPosition || !ship) return;
        //float StickMin = screenSize * Game.I.StickControlDeadzone * .5f;
        //float StickMax = screenSize * Game.I.StickControlLimit * .5f;
        Vector2 pos = new Vector2(steering.realStick.x, steering.realStick.z);
        if (!Game.I.InvertPitchAxis) pos.y *= -1;
        if (pos.magnitude >= 0)
        {
            float t = Mathf.InverseLerp(Game.I.StickControlDeadzone, Game.I.StickControlLimit, pos.magnitude);
            t += Game.I.StickControlDeadzone*2f;
            pos = pos.normalized * t * screenSize * .5f;
        }
        else
        {
            pos = Vector2.zero;
        }
        StickPosition.rectTransform.anchoredPosition = pos;
        StickPosition.color = StickPositionColor;
    }

    private void UpdateHorizonIndicator()
    {
        if (!ship) return;
        //Create the HorizonLine and components if they don't exist yet
        if (!HorizonLine)
        {
            float horizonThickness = 2;
            HorizonLine = CreateLine(ReticuleMask.transform, horizonThickness, HorizonColor);
            horizonImage = HorizonLine.GetComponent<Image>();
            horizonImage.color = HorizonColor;
            //create "up" marker
            float UpMarkWidth = reticleSize * .1f;
            float UpMarkThicknes = horizonThickness * 2f;
            RectTransform upMarker = CreateLine(HorizonLine, 2, HorizonColor);
            upMarker.anchoredPosition = new Vector2(0, UpMarkThicknes*2);
            upMarker.sizeDelta = new Vector2(10, UpMarkThicknes);
            
        }

        Quaternion orient = ship.transform.rotation;
        float reticleScale = reticleSize / 180f;

        Vector3 upDir = Vector3.up;
        if (ship.bodyProximity) upDir = ship.bodyFrom;

        //Roll
        // Project ecliptic up into ship space
        float ux = Vector3.Dot(upDir, ship.transform.right);
        float uy = Vector3.Dot(upDir, ship.transform.up);
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
        Color actualColor = HorizonColor;
        float horizonInstability = Mathf.Abs(Vector3.Dot(ship.transform.forward, Vector3.up));
        if (horizonInstability > HorizonInstabilityMin)
        {
            float t = Mathf.InverseLerp(HorizonInstabilityMin, HorizonInstabilityMax, horizonInstability);
            actualColor.a = HorizonColor.a-t;
        }
        horizonImage.color = actualColor;

    }

    private void UpdateJumpLocations()
    {
        if (!jump) return;
        foreach (JumpLocation loc in jump.Locations) {
            if (loc.Available)
            {
                if (!loc.reticle) loc.reticle = CreateBox(transform, Color.white, new Color(1, 1, 1, .25f));
                Vector3 worldPos = loc.Target.transform.position;

                // 1. Project world point onto canvas plane
                Plane plane = new Plane(canvas.transform.forward, canvas.transform.position);

                // Ray from camera to the target
                Ray ray = new Ray(camera.transform.position, (worldPos - camera.transform.position).normalized);

                if (!plane.Raycast(ray, out float dist))
                {
                    // Target is behind the camera or off-plane
                    loc.reticle.gameObject.SetActive(false);
                    return;
                }
                loc.reticle.gameObject.SetActive(true);

                Vector3 hitPoint = ray.GetPoint(dist);

                // 2. Convert world to canvas local
                Vector3 localHit = canvas.transform.InverseTransformPoint(hitPoint);

                // 3. Apply to UI
                loc.reticle.anchoredPosition = new Vector2(localHit.x, localHit.y);

                // 4. Size logic stays the same
                float size = JumpReticleSize;
                if (loc.Selected)
                    size = JumpReticleSize * .5f + Mathf.Sin(Time.time * Mathf.PI * 2 * JumpReticlePulseRate) * JumpReticleSize * .5f;

                loc.reticle.sizeDelta = Vector2.one * size * canvas.scaleFactor;

                //turn it to always face the camera
                loc.reticle.transform.rotation = Quaternion.LookRotation(
                    loc.reticle.transform.position - camera.transform.position,
                    camera.transform.up);


            }
        }

    }

    private void CreateJumpReticles()
    {
    }



    private RectTransform CreateLine(Transform parent, float thickness, Color color)
    {
        GameObject go = new GameObject("HorizonLine", typeof(Image));
        go.transform.SetParent(parent, false);
        Image img = go.GetComponent<Image>();
        img.sprite = CreatePixelSprite(color);
        img.color = color;
        RectTransform rect = img.rectTransform;
        //rect.anchoredPosition = Vector2.zero;
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.sizeDelta = new Vector2(200, thickness);
        return rect;
    }


    
    private RectTransform CreateBox(Transform parent, Color borderColor, Color fillColor)
    {
        GameObject obj = new GameObject("Box");
        obj.transform.SetParent(parent, false);
        Image img = obj.AddComponent<Image>();
        img.sprite = CreateBorderSprite(borderColor);
        img.type = Image.Type.Sliced;
        img.color = fillColor;
        img.rectTransform.localScale = Vector3.one;// * canvas.scaleFactor;
        img.rectTransform.anchoredPosition = new Vector2(0, 0);
        return img.rectTransform;
    }
    


    private static Sprite CreateBorderSprite1(Color color)
    {
        Texture2D tex = new Texture2D(3, 3);
        tex.SetPixel(0, 0, color);
        tex.Apply();
        Vector4 border = new Vector4(1, 1, 1, 1) * 100;
        return Sprite.Create(
            tex,
            new Rect(0, 0, 1, 1),
            Vector2.one * .5f,
            100f,
            0,
            SpriteMeshType.FullRect,
            border);
    }


    private static Sprite CreateBorderSprite(Color color)
    {
        Texture2D tex = new Texture2D(3, 3);

        // Fill border pixels
        for (int x = 0; x < 3; x++)
        {
            for (int y = 0; y < 3; y++)
            {
                bool isBorder = (x == 0 || x == 2 || y == 0 || y == 2);
                tex.SetPixel(x, y, isBorder ? color : new Color(0, 0, 0, 0));
            }
        }

        tex.Apply();

        // Border is 1 pixel on each side
        Vector4 border = new Vector4(1, 1, 1, 1);

        return Sprite.Create(
            tex,
            new Rect(0, 0, 3, 3),
            new Vector2(0.5f, 0.5f),
            100f,
            0,
            SpriteMeshType.FullRect,
            border
        );
    }



    private static Sprite CreatePixelSprite(Color color)
    {
        Texture2D tex = new Texture2D(1, 1);
        tex.SetPixel(0, 0, color);
        tex.Apply();
        return Sprite.Create(tex, new Rect(0, 0, 1, 1), Vector2.one * .5f);
    }


}
