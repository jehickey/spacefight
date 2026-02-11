using System.Collections.Generic;
using UnityEditor.Rendering;
using UnityEngine;

public class Starfield : MonoBehaviour
{
    public TextAsset StarDataSource;
    public float MaxMagnitude = 10f;
    public float FieldRadius = 1000f;
    public float starSizeMin = .5f;
    public float starSizeMax = 3f;

    public Camera referenceCamera;

    public Mesh quadMesh;
    public Material starMaterial;

    [System.Serializable]
    public struct StarData
    {
        public Vector3 direction;
        public float magnitude;
    }

    public List<StarData> Stars = new List<StarData>();
    public List<Matrix4x4[]> batches = new List<Matrix4x4[]>();
    const int batchSize = 1023; // Max instances per batch for DrawMeshInstanced



    void Start()
    {
        LoadStarData();
        quadMesh = CreateQuad();
    }

    void Update()
    {
        
    }

    private void LateUpdate()
    {
        if (!referenceCamera)
        {
            Debug.Log("Starfield has no reference camera");
            return;
        }
        BuildMatrices();
        foreach (var batch in batches)
        {
            Graphics.DrawMeshInstanced(quadMesh, 0, starMaterial, batch);
        }
    }

    void LoadStarData()
    {
        Stars.Clear();
        if (StarDataSource == null)
        {
            Debug.LogError("No star CSV assigned.");
            return;
        }

        string[] lines = StarDataSource.text.Split('\n');

        // Skip header if present
        int startIndex = lines[0].StartsWith("x") ? 1 : 0;

        for (int i = startIndex; i < lines.Length; i++)
        {
            string line = lines[i].Trim();
            if (string.IsNullOrWhiteSpace(line))
                continue;

            string[] parts = line.Split(',');
            if (parts.Length < 4)
                continue;

            float x = float.Parse(parts[0]);
            float y = float.Parse(parts[1]);
            float z = float.Parse(parts[2]);
            float mag = float.Parse(parts[3]);
            if (mag > MaxMagnitude) continue;

            Stars.Add(new StarData
            {
                direction = new Vector3(x, y, z).normalized,
                magnitude = mag
            });
        }

        Debug.Log($"Loaded {Stars.Count} stars.");
    }


    void BuildMatrices()
    {
        batches.Clear();
        Quaternion starRotation = referenceCamera.transform.rotation;
        List<Matrix4x4> currBatch = new List<Matrix4x4>(batchSize);
        foreach (var star in Stars)
        {
            float magFactor = Mathf.InverseLerp(7, 0, star.magnitude);
            Vector3 scale = Vector3.one * Mathf.Lerp(starSizeMin, starSizeMax, magFactor);
            Vector3 pos = transform.position + star.direction * FieldRadius;
            Matrix4x4 matrix = Matrix4x4.TRS(pos, starRotation, scale);
            currBatch.Add(matrix);
            if (currBatch.Count >= batchSize)
            {
                batches.Add(currBatch.ToArray());
                currBatch.Clear();
            }
        }
        if (currBatch.Count > 0)
        {
            batches.Add(currBatch.ToArray());
        }
    }


    public Mesh CreateQuad()
    {
        Mesh m = new Mesh();

        m.vertices = new Vector3[]
        {
        new Vector3(-0.5f, -0.5f, 0f),
        new Vector3( 0.5f, -0.5f, 0f),
        new Vector3(-0.5f,  0.5f, 0f),
        new Vector3( 0.5f,  0.5f, 0f)
        };

        m.uv = new Vector2[]
        {
        new Vector2(0f, 0f),
        new Vector2(1f, 0f),
        new Vector2(0f, 1f),
        new Vector2(1f, 1f)
        };

        m.triangles = new int[]
        {
        0, 2, 1,
        2, 3, 1
        };

        m.RecalculateNormals();
        return m;
    }

}
