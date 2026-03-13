using System.Collections.Generic;
using Unity.Mathematics;
using UnityEditor.SceneManagement;
using UnityEngine;

public class RadarBox : MonoBehaviour
{
    public int TextureRes = 256;
    public int RadarMaterialIndex = 0;
    public float MaxRange = 10f;

    public bool useLight;
    public Color lightColor = Color.white;
    public float lightRange = .1f;
    public float lightIntensity = .001f;
    public float lightHeight = .002f;
    private new Light light;

    public RenderTexture texture;
    public Renderer render;
    public Material screenMaterial;
    public Texture2D screenTex;

    public Color backgroundColor = Color.black;
    public int EdgeSizeBuffer = 1;
    public Color guideColor = Color.yellow;
    public float GuideAlpha = .5f;

    private int oldRes;
    private int oldIndex;

    public Ship myShip;
    public List<Ship> ships;

    private int centerX;
    private int centerY;

    private void OnEnable()
    {
        oldRes = 0;
        oldIndex = RadarMaterialIndex;

        myShip = FindFirstObjectByType<KeyboardControl>().GetComponent<Ship>();
    }

    void Start()
    {

    }

    void Update()
    {
        //force regeneration if key values change
        if (RadarMaterialIndex != oldIndex) { screenMaterial = null; }
        if (TextureRes != oldRes) { texture = null;  screenTex = null; }

        InitMaterial();
        InitTexture();
        InitScreenTexture();
        centerX = (int)(TextureRes * .5f);
        centerY = (int)(TextureRes * .5f);
        UpdateTexture();
        ManageLight ();

    }


    #region Drawing Functions
    private void UpdateTexture()
    {
        if (!texture || !screenTex) return;
        ClearScreen();
        DrawVisualGuides();
        //screenTex.SetPixel(0, 0, Color.white);
        //screenTex.SetPixel(10, 10, Color.red);
        //screenTex.SetPixel(TextureRes / 2, TextureRes / 2, Color.yellow);
        //DrawDot(TextureRes / 2, TextureRes / 2, Color.yellow);
        PlotShips();

        //DrawDot(64, 16, Color.white);
        //DrawDot(64, 64, Color.yellow);
        //DrawDot(16, 64, Color.green);

        //DrawDot((int)p.x, (int)p.y, Color.yellow);

        screenTex.Apply(false);
        Graphics.Blit(screenTex, texture);
    }

    private void DrawVisualGuides()
    {
        int half = TextureRes / 2;
        int edge = TextureRes/2  - EdgeSizeBuffer;
        
        //guideColor.a = GuideAlpha;
        //center point
        DrawCircle(centerX, centerY, 2, guideColor);
        //side zone
        DrawCircle(centerX, centerY, edge /2, guideColor);
        //back
        DrawCircle(centerX, centerY, edge, guideColor);
        DrawCircle(centerX, centerY, edge+1, guideColor);
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
            light.color = lightColor;
            light.intensity = lightIntensity;
            light.shadows = LightShadows.None;
        }

    }

    private void PlotShips()
    {

        foreach (Ship ship in FindObjectsByType<Ship>(FindObjectsSortMode.None))
        {
            if (ship && ship != myShip)
            {
                Vector2 p = PolarProject(ship.transform.position);
                if (ship.team) DrawDot(centerX+(int)p.x, centerY+(int)p.y, ship.team.color);

                /*
                //transform position to screen coordinate
                Vector3 local = transform.InverseTransformPoint(ship.transform.position);

                float bearing = Mathf.Atan2(local.x, local.z);//radians
                //angular distance from forward (0=forward, 180=back)
                float angleFromForward = Vector3.Angle(Vector3.forward, local.normalized);
                float radius = angleFromForward / 180f;     //normalized result
                float u = .5f + Mathf.Cos(bearing) * radius * .5f;
                float v = .5f + Mathf.Sin(bearing) * radius * .5f;
                int px = Mathf.RoundToInt(u * TextureRes);
                int py = Mathf.RoundToInt(v * TextureRes);
                DrawDot(px, py, ship.team.color);
                */
            }
        }
    }

    public Vector2 PolarProject(Vector3 worldPos)
    {
        //Oh how I hate this function
        //Debug.DrawRay(ansform.position, playerViewTransform.forward * 50f, Color.cyan);
        Vector3 local = myShip.transform.InverseTransformPoint(worldPos);
        Vector3 dir = local.normalized;
        float dist = local.magnitude;

        Vector2 offs = new Vector2(dir.x, dir.y);
        if (dir.z < 0)
        {
            offs.x = Mathf.Sign(dir.x) + (Mathf.Sign(dir.x) - dir.x);
            //offs.y = Mathf.Sign(dir.y) + (/*Mathf.Sign(dir.y) -*/ dir.y);
        }
        offs.y = -offs.y;

        float halfway = (TextureRes - EdgeSizeBuffer * 2) * .25f;
        //Debug.Log($"{offs.y}   z={dir.z}");
        offs *= halfway;
        //offs.y = 0;
        //Debug.Log($"X={offs.x} {offs.y}   z={dir.z}");
        return offs;
    }

    private void DrawDot(int x, int y, Color color)
    {
        y = TextureRes - y;
        screenTex.SetPixel(x, y, color);
        screenTex.SetPixel(x-1, y, color);
        screenTex.SetPixel(x+1, y, color);
        screenTex.SetPixel(x, y-1, color);
        screenTex.SetPixel(x, y+1, color);
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
#endregion

    #region Initialization
    private void InitMaterial()
    {
        if (!render) render = GetComponent<Renderer>();
        if (!render) return;
        if (screenMaterial) return;
        Material[] mats = render.materials;
        if (mats.Length == 0) return;
        RadarMaterialIndex = Mathf.Clamp(RadarMaterialIndex, 0, mats.Length - 1);
        screenMaterial = mats[RadarMaterialIndex];
        texture = null; 
        screenTex = null;
    }

    private void InitTexture()
    {
        if (texture) return;
        TextureRes = Mathf.Clamp(TextureRes, 1, 2048);
        texture = new RenderTexture(TextureRes, TextureRes, 0, RenderTextureFormat.ARGB32);
        texture.filterMode = FilterMode.Trilinear;
        texture.wrapMode = TextureWrapMode.Clamp;
        texture.Create();
        if (screenMaterial)
        {
            screenMaterial.EnableKeyword("_EMISSION");
            screenMaterial.SetTexture("_EmissionMap", texture);
            screenMaterial.SetColor("_EmissionColor", Color.white*5);
        }
    }

    private void InitScreenTexture() {
        if (screenTex) return;
        screenTex = new Texture2D(TextureRes, TextureRes, TextureFormat.RGBA32, false);
        screenTex.filterMode = FilterMode.Point;
        screenTex.wrapMode = TextureWrapMode.Clamp;
    }
    #endregion


}
