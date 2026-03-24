using System.Collections.Generic;
using UnityEngine;


[CreateAssetMenu(menuName = "Solar System Data")]
public class SolarSystemData : ScriptableObject
{
    public List<PlanetData> planets;


    private void Reset()
    {
        planets = new List<PlanetData>();

        // -------------------------
        // Helper functions
        // -------------------------


        // -------------------------
        // PLANETS + MOONS
        // -------------------------

        // Mercury
        var mercury = AddPlanet("Mercury", 4879f, 5.07e6f, 0.03f, 5.79e7f, 7.0f, "");

        // Venus
        var venus = AddPlanet("Venus", 12104f, -2.10e7f, 177.4f, 1.082e8f, 3.4f, "");
        venus.HasAtmosphere = true;

        // Earth
        var earth = AddPlanet("Earth", 12756f, 8.62e4f, 23.4f, 1.496e8f, 0f, "");
        earth.HasAtmosphere = true;
        AddMoon(earth, "Moon", 3475f, 2.36e6f, 6.7f, 3.844e5f, 5.1f, "");

        // Mars
        var mars = AddPlanet("Mars", 6792f, 8.86e4f, 25.2f, 2.279e8f, 1.85f, "");
        mars.HasAtmosphere = true;
        AddMoon(mars, "Phobos", 22.2f, 2.66e4f, 0f, 9.376e3f, 1.1f, "");
        AddMoon(mars, "Deimos", 12.6f, 1.09e5f, 0f, 2.3463e4f, 1.8f, "");

        // Jupiter
        var jupiter = AddPlanet("Jupiter", 142984f, 3.57e4f, 3.1f, 7.783e8f, 1.31f, "Jupiter");
        jupiter.HasAtmosphere = true;
        AddMoon(jupiter, "Io", 3643f, 1.53e5f, 0f, 4.218e5f, 0.04f, "Io");
        AddMoon(jupiter, "Europa", 3122f, 3.07e5f, 0f, 6.711e5f, 0.47f, "Europa");
        AddMoon(jupiter, "Ganymede", 5268f, 6.18e5f, 0f, 1.0704e6f, 0.20f, "Ganymede");
        AddMoon(jupiter, "Callisto", 4821f, 1.44e6f, 0f, 1.8827e6f, 0.19f, "Callisto");
        AddMoon(jupiter, "Amalthea", 250f, 1.20e5f, 0f, 1.81e5f, 0.37f, "");
        AddMoon(jupiter, "Himalia", 170f, 2.90e6f, 0f, 1.14e7f, 27.5f, "");
        AddMoon(jupiter, "Elara", 80f, 2.90e6f, 0f, 1.17e7f, 26.6f, "");
        AddMoon(jupiter, "Pasiphae", 60f, -3.20e7f, 0f, 2.35e7f, 151.4f, "");
        AddMoon(jupiter, "Sinope", 38f, -3.20e7f, 0f, 2.38e7f, 158.1f, "");
        AddMoon(jupiter, "Lysithea", 36f, 1.17e6f, 0f, 5.92e6f, 28.3f, "");
        AddMoon(jupiter, "Carme", 46f, -2.30e7f, 0f, 2.01e7f, 164.9f, "");
        AddMoon(jupiter, "Ananke", 29f, -2.10e7f, 0f, 1.83e7f, 148.9f, "");
        AddMoon(jupiter, "Leda", 20f, 1.10e6f, 0f, 5.40e6f, 27.5f, "");

        // Saturn
        var saturn = AddPlanet("Saturn", 120536f, 3.84e4f, 26.7f, 1.427e9f, 2.49f, "");
        saturn.HasAtmosphere = true;
        AddMoon(saturn, "Mimas", 396f, 8.29e4f, 0f, 1.8554e5f, 1.6f, "");
        AddMoon(saturn, "Enceladus", 504f, 1.18e5f, 0f, 2.37948e5f, 0f, "");
        AddMoon(saturn, "Tethys", 1062f, 1.90e5f, 0f, 2.94672e5f, 1.1f, "");
        AddMoon(saturn, "Dione", 1123f, 2.37e5f, 0f, 3.77415e5f, 0.02f, "");
        AddMoon(saturn, "Rhea", 1528f, 3.90e5f, 0f, 5.27108e5f, 0.35f, "");
        var titan = AddMoon(saturn, "Titan", 5151f, 1.38e6f, 0f, 1.22187e6f, 0.33f, "");
        titan.HasAtmosphere = true;
        AddMoon(saturn, "Iapetus", 1470f, 6.85e6f, 0f, 3.56082e6f, 7.5f, "");

        // Uranus
        var uranus = AddPlanet("Uranus", 51118f, -6.21e4f, 97.8f, 2.871e9f, 0.77f, "");
        uranus.HasAtmosphere = true;
        AddMoon(uranus, "Miranda", 472f, 1.22e5f, 0f, 1.2939e5f, 4.2f, "");
        AddMoon(uranus, "Ariel", 1158f, 2.09e5f, 0f, 1.909e5f, 0.26f, "");
        AddMoon(uranus, "Umbriel", 1169f, 2.64e5f, 0f, 2.66e5f, 0.36f, "");
        AddMoon(uranus, "Titania", 1578f, 7.53e5f, 0f, 4.3591e5f, 0.14f, "");
        AddMoon(uranus, "Oberon", 1523f, 1.13e6f, 0f, 5.8352e5f, 0.07f, "");

        // Neptune
        var neptune = AddPlanet("Neptune", 49528f, 5.80e4f, 28.3f, 4.498e9f, 1.77f, "");
        neptune.HasAtmosphere = true;
        AddMoon(neptune, "Triton", 2707f, -5.08e5f, 0f, 3.54759e5f, 156.9f, "");

        // Pluto
        var pluto = AddPlanet("Pluto", 2376f, 5.52e5f, 119.6f, 5.906e9f, 17.2f, "");
        AddMoon(pluto, "Charon", 1212f, 5.52e5f, 0f, 1.957e4f, 0f, "");
        AddMoon(pluto, "Styx", 16f, 1.06e6f, 0f, 4.29e4f, 0f, "");
        AddMoon(pluto, "Nix", 49f, 1.15e6f, 0f, 4.87e4f, 0f, "");
        AddMoon(pluto, "Kerberos", 19f, 1.83e6f, 0f, 5.77e4f, 0f, "");
        AddMoon(pluto, "Hydra", 65f, 1.23e6f, 0f, 6.47e4f, 0f, "");

        //set GMs
        mercury.GM = 2.2032e13f;
        venus.GM = 3.24859e14f;
        earth.GM = 3.986004418e14f;
        mars.GM = 4.282837e13f;
        jupiter.GM = 1.26686534e17f;
        saturn.GM = 3.7931187e16f;
        uranus.GM = 5.793939e15f;
        neptune.GM = 6.836529e15f;
        pluto.GM = 8.71e11f;

    }



    private PlanetData AddPlanet(string name, float diameter, float rot, float tilt, float orbitKm, float inc, string material)
    {
        var p = new PlanetData
        {
            Name = name,
            DiameterKm = diameter,
            RotationPeriodSec = rot,
            AxialTiltDeg = tilt,
            OrbitalRadiusKm = orbitKm,
            InclinationDeg = inc,
            OrbitalPhase = 0f,
            MaterialName = material,
            HeightmapName = "",
            HasAtmosphere = false,
            TerrainDeformation = false,
            TerrainSmoothness = 0f,
            moons = new List<BodyData>()
        };
        planets.Add(p);
        return p;
    }

    private BodyData AddMoon(PlanetData parent, string name, float diameter, float rot, float tilt, float orbitKm, float inc, string material)
    {
        var m = new BodyData
        {
            Name = name,
            IsMoon = true,
            DiameterKm = diameter,
            RotationPeriodSec = rot,
            AxialTiltDeg = tilt,
            OrbitalRadiusKm = orbitKm,
            InclinationDeg = inc,
            OrbitalPhase = 0f,
            MaterialName = material,
            HeightmapName = "",
            HasAtmosphere = false,
            TerrainDeformation = false,
            TerrainSmoothness = 0f
        };

        parent.moons.Add(m);
        return m;
    }

}

