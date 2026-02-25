using System.Collections.Generic;
using System.Threading;
using UnityEngine;

public class RadarBox : MonoBehaviour
{
    public RenderTexture texture;
    public int TextureRes = 256;
    public int RadarMaterialIndex = 0;
    public float MaxRange = 10f;

    public Renderer render;
    public Material screenMaterial;
    public Texture2D screenTex;

    private int oldRes;
    private int oldIndex;

    public Ship myShip;
    public List<Ship> ships;

    private void OnEnable()
    {
        oldRes = TextureRes;
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
        UpdateTexture();

    }


    #region Drawing Functions
    private void UpdateTexture()
    {
        if (!texture || !screenTex) return;
        ClearScreen();
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

    private void PlotShips()
    {

        foreach (Ship ship in FindObjectsByType<Ship>(FindObjectsSortMode.None))
        {
            if (ship && ship != myShip)
            {
                Vector2 p = PolarProject2(ship.transform.position);
                DrawDot((int)p.x, (int)p.y, ship.team.color);

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


    /// <summary>
    /// Projects a world-space point onto a forward-centric polar map.
    /// Forward = center, behind = edge.
    /// </summary>
    /// <param name="observer">Transform of the observer</param>
    /// <param name="worldPos">World position of the target</param>
    /// <returns>UV coordinate in 0–1 range</returns>
    public Vector2 PolarProject(Vector3 worldPos)
    {
        // Convert world position into observer-local space
        Vector3 local = transform.InverseTransformPoint(worldPos);

        // Normalize for angular calculations
        Vector3 dir = local.normalized;

        // Bearing around the observer (left/right)
        float bearing = Mathf.Atan2(dir.x, dir.z); // radians

        // Angular distance from forward (0 = forward, 180 = behind)
        float angleFromForward = Vector3.Angle(-transform.forward, dir); // degrees

        // Convert angle to radius: 0 = forward, 1 = behind
        float radius = angleFromForward / 180f;

        // Convert polar - UV (0–1 range)
        float u = 0.5f + Mathf.Cos(bearing) * radius * 0.5f;
        float v = 0.5f + Mathf.Sin(bearing) * radius * 0.5f;

        int px = Mathf.RoundToInt(u * TextureRes);
        int py = Mathf.RoundToInt(v * TextureRes);
        return new Vector2(px, py);
    }


    public Vector2 PolarProject2(Vector3 worldPos)
    {
        // 1. World - local (ship/cockpit space)
        Vector3 local = transform.InverseTransformPoint(worldPos);

        // 2. Direction in local space
        Vector3 dir = local.normalized;

        // 3. Bearing: ignore vertical so pitch doesn't skew left/right
        Vector3 flat = new Vector3(dir.x, 0f, dir.z);
        if (flat.sqrMagnitude < 1e-6f)
            flat = Vector3.forward; // avoid NaN when target is exactly above/below

        float bearing = Mathf.Atan2(flat.x, flat.z); // radians

        // 4. Radius: angular distance from *local* forward (0 = forward, 1 = behind)
        // In local space, forward is (0,0,1), so just use dir.z
        float angleFromForward = Mathf.Acos(Mathf.Clamp(dir.z, -1f, 1f)); // radians
        float radius = angleFromForward / Mathf.PI;                       // 0–1

        // 5. Polar - UV
        float u = 0.5f + Mathf.Cos(bearing) * radius * 0.5f;
        float v = 0.5f + Mathf.Sin(bearing) * radius * 0.5f;

        // 6. UV - pixels (with your vertical flip)
        int px = Mathf.RoundToInt(u * TextureRes);
        int py = Mathf.RoundToInt((1f - v) * TextureRes); // if 0 = top, max = bottom

        return new Vector2(px, py);
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

    private void ClearScreen()
    {
        if (!screenTex) return;
        Color32 clear = new Color32(0, 0, 50, 255);
        var pixels = screenTex.GetPixels32();
        for (int i = 0; i < pixels.Length; i++)
        {
            //pixels[i].a = (byte)(pixels[i].a * fadeFactor);  //fade
            pixels[i] = clear;
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
        texture.filterMode = FilterMode.Point;
        texture.wrapMode = TextureWrapMode.Clamp;
        texture.Create();
        if (screenMaterial)
        {
            screenMaterial.EnableKeyword("_EMISSION");
            screenMaterial.SetTexture("_EmissionMap", texture);
            screenMaterial.SetColor("_EmissionColor", Color.white);
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
