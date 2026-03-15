using System.Runtime.InteropServices.WindowsRuntime;
using UnityEngine;
using UnityEngine.Video;

public class Monitor : MonoBehaviour
{

    //works similar to Radar
    
    public RenderTexture texture;
    public Renderer render;
    public Material screenMaterial;
    public Texture2D screenTex;
    public int TextureRes = 256;
    private int oldRes;
    private int centerX;
    private int centerY;

    public Color backgroundColor = Color.black;
    public int EdgeSizeBuffer = 1;

    public bool showNoise;

    public bool useLight;
    public bool useLightScreenColor;
    public Color lightColor = Color.white;
    public float lightRange = .1f;
    public float lightIntensity = .001f;
    public float lightHeight = .002f;
    private new Light light;

    public VideoPlayer video;

    public float RefreshRate = 10;   //refreshes per second avg
    private float lastUpdate;

    void Start()
    {
        video = GetComponent<VideoPlayer>();
    }

    void Update()
    {
        //force regeneration if key values change
        if (TextureRes != oldRes) { texture = null; screenTex = null; }


        InitMaterial();
        InitTexture();
        InitScreenTexture();

        //manage refresh rate
        if (RefreshRate <= 0 || Time.time - lastUpdate < 1 / RefreshRate) return;
        lastUpdate = Time.time;

        centerX = (int)(TextureRes * .5f);
        centerY = (int)(TextureRes * .5f);
        UpdateTexture();
        ManageLight();

        if (video)
        {
            //if (!video.targetTexture) video.targetTexture = texture;
            //if (!video.isPlaying) video.Play();
        }

    }


    private void UpdateTexture()
    {
        if (!texture || !screenTex) return;

        if (showNoise)
        {
            screenMaterial.SetTexture("_NoiseTex", StaticGenerator.Get());
            screenMaterial.SetFloat("_Strength", .5f);
            //Graphics.Blit(StaticGenerator.Get(), texture);
            //return;
        }
        else
        {
            screenMaterial.SetFloat("_Strength", 0);
        }
        ClearScreen();
        //screenTex.SetPixel(0, 0, Color.white);

        DrawCircle(0, 0, 10, Color.white);
        DrawCircle(0, 2, 10, Color.yellow);
        DrawCircle(2, 0, 10, Color.red);
        DrawCircle(centerX, centerY, 10, Color.white);


        screenTex.Apply(false);
        //screenMaterial.SetTexture("_MainTex", screenTex);
        Graphics.Blit(screenTex, texture, screenMaterial);
        //Graphics.Blit(screenTex, texture);
    }


    private void ManageLight()
    {
        if (!useLight)
        {
            if (light) Destroy(light.gameObject);
            return;
        }

        if (!light)
        {
            GameObject obj = new GameObject("Radar Light");
            obj.transform.parent = transform;
            light = obj.AddComponent<Light>();
        }

        if (light)
        {
            light.transform.localPosition = Vector3.zero + Vector3.up * lightHeight;
            light.range = lightRange;
            light.color = GetAverageColor();
            light.intensity = lightIntensity;
            light.shadows = LightShadows.None;
        }

    }

    private Color GetAverageColor()
    {
        if (!useLightScreenColor) return Color.white*.5f;
        if (!screenTex) return Color.black;
        Texture2D temp = new Texture2D(4, 4, TextureFormat.RGB24, false);

        //Graphics.Blit(screenTex, temp);
        RenderTexture.active = texture;
        temp.ReadPixels(new Rect(0,0,4,4), 0, 0);
        temp.Apply(false);
        temp.ReadPixels(new Rect(0, 0, 4, 4), 0, 0);
        temp.Apply();

        Color[] pixels = temp.GetPixels();
        Color avg = Color.black;

        foreach (var c in pixels) avg += c;

        avg /= pixels.Length;

        // use avg for your light
        return avg * .5f;
    }


    private void DrawDot(int x, int y, Color color)
    {
        y = TextureRes - y;
        screenTex.SetPixel(x, y, color);
        screenTex.SetPixel(x - 1, y, color);
        screenTex.SetPixel(x + 1, y, color);
        screenTex.SetPixel(x, y - 1, color);
        screenTex.SetPixel(x, y + 1, color);
    }

    private void DrawCircle(int cx, int cy, int radius, Color color)
    {
        cy = TextureRes - cy;       //flip vertical
        int x = radius;
        int y = 0;
        int err = 1 - x;

        while (x >= y)
        {
            //plot 8 octants
            screenTex.SetPixel(cx + x, cy + y, color);
            screenTex.SetPixel(cx + y, cy + x, color);
            screenTex.SetPixel(cx - y, cy + x, color);
            screenTex.SetPixel(cx - x, cy + y, color);
            screenTex.SetPixel(cx - x, cy - y, color);
            screenTex.SetPixel(cx - y, cy - x, color);
            screenTex.SetPixel(cx + y, cy - x, color);
            screenTex.SetPixel(cx + x, cy - y, color);
            y++;
            if (err < 0)
            {
                err += 2 * y + 1;
            }
            else
            {
                x--;
                err += 2 * (y - x + 1);
            }
        }
    }

    private void ClearScreen()
    {
        if (!screenTex) return;
        var pixels = screenTex.GetPixels32();
        for (int i = 0; i < pixels.Length; i++)
        {
            //pixels[i].a = (byte)(pixels[i].a * fadeFactor);  //fade
            pixels[i] = backgroundColor;
        }
        screenTex.SetPixels32(pixels);
    }


    #region Initialization
    private void InitMaterial()
    {
        if (!render) render = GetComponent<Renderer>();
        if (!render) return;
        if (screenMaterial) return;
        screenMaterial = render.material;
        texture = null;
        screenTex = null;
    }

    private void InitTexture()
    {
        if (texture) return;
        TextureRes = Mathf.Clamp(TextureRes, 1, 2048);
        oldRes = TextureRes;
        texture = new RenderTexture(TextureRes, TextureRes, 0, RenderTextureFormat.ARGB32);
        texture.filterMode = FilterMode.Trilinear;
        texture.wrapMode = TextureWrapMode.Clamp;
        texture.autoGenerateMips = false;

        texture.Create();
        if (screenMaterial)
        {
            screenMaterial.EnableKeyword("_EMISSION");
            screenMaterial.mainTexture = texture;
            screenMaterial.SetTexture("_EmissionMap", texture);
            screenMaterial.SetColor("_EmissionColor", Color.white * 5);
        }
    }

    private void InitScreenTexture()
    {
        if (screenTex) return;
        screenTex = new Texture2D(TextureRes, TextureRes, TextureFormat.RGBA32, false);
        screenTex.filterMode = FilterMode.Point;
        screenTex.wrapMode = TextureWrapMode.Clamp;
    }
    #endregion



}
