using UnityEngine;

[System.Serializable]
public class BodyData
{
    public string Name;

    //physical characteristics
    public float DiameterKm = 1;
    public float RotationPeriodSec = 1;
    public float AxialTiltDeg = 0;      //planets to ecliptic, moons to parent
    public float GM;                    //G * mass

    //orbital characteristics
    public bool IsMoon;
    public float OrbitalRadiusKm = 1;
    public float InclinationDeg = 0;
    public float OrbitalPhase = 0;          //0=random positioning

    //sirface characteristics
    public string MaterialName;
    public string HeightmapName;
    public bool HasAtmosphere;
    public bool TerrainDeformation;
    public float TerrainSmoothness = 0;


    public static bool operator true(BodyData data)
    {
        if (data == null) return false;
        return data.Name != string.Empty;
    }

    public static bool operator false(BodyData data)
    {
        if (data == null) return true;
        return data.Name == string.Empty;
    }


    public static bool operator !(BodyData data)
    {
        if (data == null) return true;
        return data.Name == string.Empty;
    }



}
