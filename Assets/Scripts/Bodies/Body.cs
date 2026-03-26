using System.Linq;
using UnityEngine;

public class Body : MonoBehaviour
{
    public static int MinDetailGlobal = 1;
    public static int MaxDetailGlobal = 8;

    [Header("Generation Values")]
    public BodyData Data;

    [Header("Body Settings")]
    public Body parentBody;
    public float Radius = 50;
    public float RotationPeriod = 10;   //degrees per second
    public float OrbitRadius = 100;
    public float OrbitPeriod = 10;
    [Range(0f, 1f)]
    public float OrbitPhase = 0; //0-1, current orbital period position
    public bool TidalLock = false;

    [Header("Tracking")]
    public float DistanceFromPlayer;


    [Header("Terrain Settings")]
    public bool TerrainDeformation = false;
    public bool Regenerate = false;     //do a full regeneration of mesh
    public bool DoDeform = false;       //just do basic deformation
    [ReadOnly]
    public int SphereDetail;
    public int MaxDetail = 0;
    public float TerrainSmoothness = 1;
    public float actualTerrainMagnitude;
    public Texture2D heightmap;
    private float[] heightData;
    public int heightDataCount;
    public float TextureDetailMinRadius = 2;
    public float TextureDetailMaxRadius = 3;
    [ReadOnly]
    public float TextureDetail;

    [Header("Atmosphere")]
    public Atmosphere atmosphere;


    public Material material;
    private MaterialPropertyBlock materialBlock;
    [ReadOnly]
    public Mesh mesh;
    protected MeshFilter filter;
    protected MeshRenderer render;
    protected new SphereCollider collider;

    //backup copy of original sphere mesh (for easier editing)
    [ReadOnly]
    public Mesh baseSphereMesh;





    protected virtual void Start()
    {
        InitFromData();

        if (!material) material = new Material(Shader.Find("Universal Render Pipeline/Lit"));
        render.sharedMaterial = material;

        SphereDetail = 0;

        materialBlock = new MaterialPropertyBlock();

        atmosphere = GetComponentInChildren<Atmosphere>();

        if (!collider)
        {
            collider = GetComponent<SphereCollider>();
            if (!collider)
            {
                collider = gameObject.AddComponent<SphereCollider>();
                collider.radius = 1;
            }
        }

        if (OrbitPhase == 0) OrbitPhase = Random.Range(0f, 1f);

    }

    protected virtual void OnEnable()
    {
        filter = GetComponent<MeshFilter>();
        if (!filter) filter = gameObject.AddComponent<MeshFilter>();

        render = GetComponent<MeshRenderer>();
        if (!render) render = gameObject.AddComponent<MeshRenderer>();


    }

    protected virtual void Update()
    {
        if (!Simulation.I) return;
        //forced regeneration
        if (Regenerate)
        {
            SphereDetail = 0;
            Regenerate = false;
        }

        //Set a hard minimum on MinGlobalDetail (to prevent regeneration every frame)
        if (MinDetailGlobal == 0) MinDetailGlobal = 1;

        //no detail setting defaults to class maximum
        if (MaxDetail == 0) MaxDetail = MaxDetailGlobal;
        //don't let this object's settings go beyond class minimum and maximum
        MaxDetail = Mathf.Clamp(MaxDetail, MinDetailGlobal, MaxDetailGlobal);
        //apply instance limits for this specific body
        SphereDetail = Mathf.Clamp(SphereDetail, MinDetailGlobal, MaxDetail);

        SetScale();
        DoRotation();
        int detail = Mathf.Clamp(GetDistanceDetail(), MinDetailGlobal, MaxDetail);
        if (detail != SphereDetail || !mesh)            //did detail level change (or no mesh?)
        {
            SphereDetail = detail;
            mesh = Shapes.Icosphere.Generate(SphereDetail);
            if (mesh)
            {
                baseSphereMesh = Shapes.Icosphere.CloneMesh(mesh);  //keep backup
                GetHeightmapData();
                if (filter) filter.sharedMesh = mesh;
                DoDeform = true;
                if (render) render.sharedMaterial = material;
            }
        }

        if (DoDeform)
        {
            DeformMesh();
            DoDeform = false;
        }
        heightDataCount = heightData!=null ? heightData.Count() : 0;
        AdjustTextureDetails();


        //Orbital updates and positioning
        if (TidalLock && parentBody)
        {
            RotationPeriod = OrbitPeriod;
        }

        if (Application.isPlaying && OrbitPeriod > 0)
        {
            if (Simulation.I)
            {
                OrbitPhase += Simulation.I.TimeDelta / OrbitPeriod;
            }
        }
        SetOrbitalPosition();

    }

    protected virtual void OnValidate()
    {
        //SetScale();
        SetOrbitalPosition();

    }

    protected virtual void OnDrawGizmos()
    {
        if (!Application.isPlaying)
        {
            //show a sphere to represent full radius
            Gizmos.color = Color.red;
            Gizmos.DrawSphere(transform.position, Radius);
        }
        else
        {
            //show gizmo sphere (just in case the rendered one is not working)
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(transform.position, Radius / 2f);
            //show planet AOI radius
            Gizmos.color = Color.blue;
            if (Simulation.I) Gizmos.DrawWireSphere(transform.position, Radius * Simulation.I.BodyProximityRadii);
        }

        //draw orbit
        if (parentBody)
        {
            int segments = 36;
            Gizmos.color = Color.yellow;
            Vector3 prev = parentBody.transform.position + new Vector3(OrbitRadius, 0f, 0f);
            float step = 2f * Mathf.PI / segments;
            for (int i = 1; i <= segments; i++)
            {
                float angle = i * step;
                Vector3 nextPoint = parentBody.transform.position + new Vector3(
                    Mathf.Cos(angle) * OrbitRadius,
                    0f,
                    Mathf.Sin(angle) * OrbitRadius
                );
                Gizmos.DrawLine(prev, nextPoint);
                prev = nextPoint;
            }
        }
    }


    private void InitFromData()
    {
        if (!Data) return;
        //Debug.Log($"Initializing body \"{Data.Name}\" from BodyData");
        float sunGM = 1.32712440018e20f;

        name = Data.Name;

        Radius = GetFactoredRadius( Data.DiameterKm/2);
        OrbitRadius = GetFactoredDistance( Data.OrbitalRadiusKm/2);
        //if (Data.IsMoon) OrbitRadius /= Simulation.I.RadiusFactor;

        if (OrbitPhase==0) OrbitPhase = Data.OrbitalPhase;
        OrbitPeriod = GetOrbitalPeriodSeconds(Data.OrbitalRadiusKm, sunGM);

        //setup material
        if (Data.MaterialName != string.Empty)
        {
            material = Resources.Load<Material>("PlanetMaterials/" + Data.MaterialName);
            if (!material) Debug.Log($"Cannot find material {Data.MaterialName} for body {name}");
        }
        else
        {
            //Debug.Log($"Body \"{name}\" has no material in Data");
        }

        //setup heightmap
        heightmap = Resources.Load<Texture2D>("PlanetHeightmaps/" + Data.HeightmapName);

        //create an atmosphere if it should have one
        if (Data.HasAtmosphere)
        {
            GameObject obj = new GameObject("Atmosphere");
            obj.transform.parent = transform;
            atmosphere = obj.AddComponent<Atmosphere>();
        }

        /*
    public float AxialTiltDeg = 0;      //planets to ecliptic, moons to parent

    //orbital characteristics
    public float InclinationDeg = 0;

    //sirface characteristics
    public bool HasAtmosphere;
    public bool TerrainDeformation;
    public float TerrainSmoothness = 0;
        */

    }


    private float GetFactoredRadius(float radiusKm)
    {
        //boost factor: 0=fully logarithmic, 1=real (huge jupiter tiny moons)
        return Mathf.Lerp(
            Mathf.Log10((float)radiusKm),
            (float)radiusKm,
            Simulation.I.RadiusCompression) * Simulation.I.RadiusFactor;
    }

    private float GetFactoredDistance(float distanceKm)
    {
        //boost factor: 0=fully logarithmic, 1=real (huge jupiter tiny moons)
        return Mathf.Lerp(
            Mathf.Log10((float)distanceKm),
            (float)distanceKm,
            Simulation.I.DistanceCompression) * Simulation.I.DistanceFactor;
    }

    public static float GetOrbitalPeriodSeconds(double orbitalRadiusKm, double GM)
    {
        double r = orbitalRadiusKm * 1000.0;
        return 2.0f * Mathf.PI * Mathf.Sqrt((float)((r * r * r) / GM));
    }

    private void AdjustTextureDetails()
    {
        TextureDetail = Mathf.InverseLerp(TextureDetailMaxRadius*Radius, TextureDetailMinRadius*Radius, DistanceFromPlayer);
        TextureDetail = Mathf.Clamp01(TextureDetail);
        render.GetPropertyBlock(materialBlock);
        materialBlock.SetFloat("_DetailAlbedoMapScale", TextureDetail);
        materialBlock.SetFloat("_DetailNormalMapScale", TextureDetail);
        render.SetPropertyBlock(materialBlock);
    }


    private int GetDistanceDetail()
    {
        if (Camera.main)
        {
            DistanceFromPlayer = Vector3.Distance(transform.position, Camera.main.transform.position);
            if (DistanceFromPlayer < Radius * 1.5f) return 10;
            if (DistanceFromPlayer < Radius * 3f) return 9;
            if (DistanceFromPlayer < Radius * 4) return 8;
            if (DistanceFromPlayer < Radius * 6) return 7;
            if (DistanceFromPlayer < Radius * 10) return 6;
        }
        return 5;
    }

    private void SetScale()
    {
        if (Radius <= 0) Radius = .01f;
        Vector3 scale = Vector3.one * Radius;
        if (transform.parent)
        {
            scale.x /= transform.parent.lossyScale.x;
            scale.y /= transform.parent.lossyScale.y;
            scale.z /= transform.parent.lossyScale.z;
        }
        transform.localScale = scale;
    }


    void DoRotation() {
        if (!Simulation.I || Simulation.I.TimeScale==0) return;
        float degreesPerSimSecond = 360f / RotationPeriod;
        transform.Rotate(Vector3.up, degreesPerSimSecond * Simulation.I.TimeDelta, Space.Self);
    }





    /// <summary>
    /// Builts height data from a given Heightmap image (or from maintexture if none given)
    /// </summary>
    void GetHeightmapData()
    {
        if (!mesh) return;
        int count = mesh.vertexCount;       //total number of vertices to track
        Vector2[] uvs = mesh.uv;            //UV mapping for each vertex
        heightData = null;

        //Use the main texture if no heightmap availablen
        if (!heightmap && material) heightmap = (Texture2D)material.mainTexture;
        if (!heightmap) return;

        heightData = new float[count];      //height data for each vertex
        //Get texture info for each vertex and store height data
        for (int i = 0; i < count; i++)
        {
            //get sample from grayscale
            Color c = heightmap.GetPixelBilinear(uvs[i].x, uvs[i].y);
            heightData[i] = c.grayscale - .5f;
        }
    }

        void DeformMesh()
    {
        if (!TerrainDeformation) return;
        if (!mesh) return;
        if (!baseSphereMesh) return;

        if (heightData == null)
        {
            GetHeightmapData();
            if (heightData == null)
            {
                //Debug.Log("Failed to get heightmap data for deformation!");
                return;
            }
        }

        //calculate deformation scale for this specific body at this distance
        actualTerrainMagnitude = Simulation.I.TerrainMagnitudeScale * TerrainSmoothness * Radius;

        Vector3[] baseVerts = baseSphereMesh.vertices;      //vertices on original sphere
        Vector3[] verts = mesh.vertices;                    //vertices on this sphere
        Vector2[] uvs = mesh.uv;
        float[] heights = new float[verts.Length];

        //if (!heightmap) heightmap = (Texture2D)material.mainTexture;

        for (int i = 0; i < verts.Length; i++)
        {
            Vector3 original = baseVerts[i];

            //get sample from grayscale
            //Color c = heightmap.GetPixelBilinear(uvs[i].x, uvs[i].y);
            //float h = c.grayscale - .5f;

            //apply displacement
            //float offset = Random.Range(-actualTerrainMagnitude, actualTerrainMagnitude);
            //float noiseAmount = 0;
            //float noise = Random.Range(-noiseAmount, noiseAmount);
            float displacedRadius = 1 + heightData[i] * actualTerrainMagnitude;
            verts[i] = original * displacedRadius;   //(verts[i].magnitude + offset);
        }

        mesh.vertices = verts;
        mesh.RecalculateNormals();
        mesh.RecalculateBounds();
    }


    private void SetOrbitalPosition()
    {
        if (OrbitPhase > 1) OrbitPhase = 0;
        if (!parentBody) return;
        float angle = -OrbitPhase * 2f * Mathf.PI;
        transform.position = parentBody.transform.position + new Vector3(
            Mathf.Cos(angle) * OrbitRadius,
            0f,
            Mathf.Sin(angle) * OrbitRadius
        );
    }

}
