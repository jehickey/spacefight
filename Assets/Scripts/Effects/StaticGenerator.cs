using UnityEngine;

public static class StaticGenerator
{
    public static int FrameCount = 10;
    public static int Resolution = 128;
    public static float NoiseLevel = .5f;
    private static Texture2D[] noiseFrames;
    private static RenderTexture target;

    public static void Generate(int count = 0, int res = 0, float noise = 0)
    {
        if (count>0) FrameCount = count;
        if (res > 0) Resolution = res;
        if (noise > 0) NoiseLevel = noise;
        FrameCount = Mathf.Clamp(FrameCount, 1, 100);
        Resolution = Mathf.Clamp(Resolution, 16, 2048);
        NoiseLevel = Mathf.Clamp01(NoiseLevel);

        noiseFrames = new Texture2D[FrameCount];

        for (int i = 0; i < FrameCount; i++)
        {
            var tex = new Texture2D(Resolution, Resolution, TextureFormat.RGBA32, false);
            var pixels = new Color32[Resolution * Resolution];

            for (int p = 0; p < pixels.Length; p++)
            {
                byte v = (byte)(Random.value < NoiseLevel ? 255 : 0);
                pixels[p] = new Color32(v, v, v, 255);
            }

            tex.SetPixels32(pixels);
            tex.Apply();
            noiseFrames[i] = tex;
        }
    }

    public static Texture2D Get()
    {
        if (noiseFrames.Length == 0)
        {
            Debug.Log("no static frames");
            return null;
        }
        int index = Random.Range(0, noiseFrames.Length);
        //Graphics.Blit(noiseFrames[index], target);
        return noiseFrames[index];
    }

}
