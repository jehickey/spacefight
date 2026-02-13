using System.Collections.Generic;
using UnityEngine;

public class Icosphere : MonoBehaviour
{
    struct Triangle
    {
        public int v1, v2, v3;
        public Triangle(int a, int b, int c)
        {
            v1 = a; v2 = b; v3 = c;
        }
    }

    public static Mesh Generate(int subdivisions)
    {
        subdivisions = Mathf.Clamp(subdivisions, 0, 6);

        // Golden ratio
        float t = (1f + Mathf.Sqrt(5f)) / 2f;

        // Initial 12 vertices of an icosahedron
        List<Vector3> vertices = new List<Vector3>
        {
            new Vector3(-1,  t,  0),
            new Vector3( 1,  t,  0),
            new Vector3(-1, -t,  0),
            new Vector3( 1, -t,  0),

            new Vector3( 0, -1,  t),
            new Vector3( 0,  1,  t),
            new Vector3( 0, -1, -t),
            new Vector3( 0,  1, -t),

            new Vector3( t,  0, -1),
            new Vector3( t,  0,  1),
            new Vector3(-t,  0, -1),
            new Vector3(-t,  0,  1)
        };

        // Normalize to radius 0.5 (unit sphere diameter = 1)
        for (int i = 0; i < vertices.Count; i++)
            vertices[i] = vertices[i].normalized * 0.5f;

        // 20 faces of an icosahedron
        List<Triangle> faces = new List<Triangle>
        {
            new Triangle(0, 11, 5),
            new Triangle(0, 5, 1),
            new Triangle(0, 1, 7),
            new Triangle(0, 7, 10),
            new Triangle(0, 10, 11),

            new Triangle(1, 5, 9),
            new Triangle(5, 11, 4),
            new Triangle(11, 10, 2),
            new Triangle(10, 7, 6),
            new Triangle(7, 1, 8),

            new Triangle(3, 9, 4),
            new Triangle(3, 4, 2),
            new Triangle(3, 2, 6),
            new Triangle(3, 6, 8),
            new Triangle(3, 8, 9),

            new Triangle(4, 9, 5),
            new Triangle(2, 4, 11),
            new Triangle(6, 2, 10),
            new Triangle(8, 6, 7),
            new Triangle(9, 8, 1)
        };

        // Cache for midpoint vertices
        Dictionary<long, int> midpointCache = new Dictionary<long, int>();

        int GetMidpoint(int a, int b)
        {
            long key = ((long)Mathf.Min(a, b) << 32) + Mathf.Max(a, b);

            if (midpointCache.TryGetValue(key, out int index))
                return index;

            Vector3 mid = (vertices[a] + vertices[b]) * 0.5f;
            mid.Normalize();
            mid *= 0.5f;

            int newIndex = vertices.Count;
            vertices.Add(mid);
            midpointCache[key] = newIndex;

            return newIndex;
        }

        // Subdivide
        for (int i = 0; i < subdivisions; i++)
        {
            List<Triangle> newFaces = new List<Triangle>();

            foreach (var tri in faces)
            {
                int a = GetMidpoint(tri.v1, tri.v2);
                int b = GetMidpoint(tri.v2, tri.v3);
                int c = GetMidpoint(tri.v3, tri.v1);

                newFaces.Add(new Triangle(tri.v1, a, c));
                newFaces.Add(new Triangle(tri.v2, b, a));
                newFaces.Add(new Triangle(tri.v3, c, b));
                newFaces.Add(new Triangle(a, b, c));
            }

            faces = newFaces;
        }

        // Build mesh
        Mesh mesh = new Mesh();
        mesh.indexFormat = (vertices.Count > 65000)
            ? UnityEngine.Rendering.IndexFormat.UInt32
            : UnityEngine.Rendering.IndexFormat.UInt16;

        mesh.vertices = vertices.ToArray();

        // Normals = normalized vertex positions
        Vector3[] normals = new Vector3[vertices.Count];
        for (int i = 0; i < normals.Length; i++)
            normals[i] = vertices[i].normalized;
        mesh.normals = normals;

        // UVs (simple spherical projection)
        Vector2[] uvs = new Vector2[vertices.Count];
        for (int i = 0; i < vertices.Count; i++)
        {
            Vector3 n = normals[i];
            float u = Mathf.Atan2(n.x, n.z) / (2f * Mathf.PI) + 0.5f;
            float v = n.y * 0.5f + 0.5f;
            uvs[i] = new Vector2(u, v);
        }
        mesh.uv = uvs;

        // Triangles
        int[] tris = new int[faces.Count * 3];
        for (int i = 0; i < faces.Count; i++)
        {
            tris[i * 3 + 0] = faces[i].v1;
            tris[i * 3 + 1] = faces[i].v2;
            tris[i * 3 + 2] = faces[i].v3;
        }
        mesh.triangles = tris;

        mesh.RecalculateBounds();
        return mesh;
    }

}
