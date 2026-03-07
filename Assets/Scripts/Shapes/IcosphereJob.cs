using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;

namespace Shapes
{
    [BurstCompile]
    public struct IcosaJob : IJob
    {
        public int subdivisions;

        public NativeList<float3> vertices;
        public NativeList<int3> faces;
        public NativeHashMap<long, int> midpointCache;

        public NativeArray<float3> outVerts;
        public NativeArray<int> outTris;
        public NativeArray<float3> outNormals;
        public NativeArray<float2> outUVs;

        private int GetMidpoint(int a, int b)
        {
            int min = math.min(a, b);
            int max = math.max(a, b);
            long key = ((long)min << 32) + (uint)max;

            //check the cache
            if (midpointCache.TryGetValue(key, out int index)) return index;

            //compute midpoint
            float3 mid = math.normalize((vertices[a] + vertices[b]) * 0.5f);

            //add new vertex
            int newIndex = vertices.Length;
            vertices.Add(mid);
            //cache it
            midpointCache.TryAdd(key, newIndex);

            return newIndex;
        }


        public void Execute()
        {
            float t = (1f + math.sqrt(5f)) * .5f;       //golden ratio

            //12 vertices of an icosahedron
            vertices.Add(new float3(-1, t, 0));
            vertices.Add(new float3(1, t, 0));
            vertices.Add(new float3(-1, -t, 0));
            vertices.Add(new float3(1, -t, 0));

            vertices.Add(new float3(0, -1, t));
            vertices.Add(new float3(0, 1, t));
            vertices.Add(new float3(0, -1, -t));
            vertices.Add(new float3(0, 1, -t));

            vertices.Add(new float3(t, 0, -1));
            vertices.Add(new float3(t, 0, 1));
            vertices.Add(new float3(-t, 0, -1));
            vertices.Add(new float3(-t, 0, 1));


            //normalize to radius 1 (unit vectors - faster manipulation)
            for (int i = 0; i < vertices.Length; i++)
                vertices[i] = math.normalize(vertices[i]);

            //20 faces of an icosahedron
            faces.Add(new int3(0, 11, 5));
            faces.Add(new int3(0, 5, 1));
            faces.Add(new int3(0, 1, 7));
            faces.Add(new int3(0, 7, 10));
            faces.Add(new int3(0, 10, 11));

            faces.Add(new int3(1, 5, 9));
            faces.Add(new int3(5, 11, 4));
            faces.Add(new int3(11, 10, 2));
            faces.Add(new int3(10, 7, 6));
            faces.Add(new int3(7, 1, 8));

            faces.Add(new int3(3, 9, 4));
            faces.Add(new int3(3, 4, 2));
            faces.Add(new int3(3, 2, 6));
            faces.Add(new int3(3, 6, 8));
            faces.Add(new int3(3, 8, 9));

            faces.Add(new int3(4, 9, 5));
            faces.Add(new int3(2, 4, 11));
            faces.Add(new int3(6, 2, 10));
            faces.Add(new int3(8, 6, 7));
            faces.Add(new int3(9, 8, 1));

            //subdivide
            for (int i = 0; i < subdivisions; i++)
            {
                NativeList<int3> newFaces = new NativeList<int3>(faces.Length * 4, Allocator.Temp);
                for (int f = 0; f < faces.Length; f++)
                {
                    int3 tri = faces[f];
                    int a = GetMidpoint(tri.x, tri.y);
                    int b = GetMidpoint(tri.y, tri.z);
                    int c = GetMidpoint(tri.z, tri.x);

                    newFaces.Add(new int3(tri.x, a, c));
                    newFaces.Add(new int3(tri.y, b, a));
                    newFaces.Add(new int3(tri.z, c, b));
                    newFaces.Add(new int3(a, b, c));
                }

                faces.Clear();
                for (int n = 0; n < newFaces.Length; n++) faces.Add(newFaces[n]);
                newFaces.Dispose();
            }

            //export the vertices
            for (int i = 0; i < vertices.Length; i++)
                outVerts[i] = vertices[i];

            //create the normals
            for (int i = 0; i < vertices.Length; i++)
                outNormals[i] = math.normalize(vertices[i]);

            //UVs (simple spherical projection)
            for (int i = 0; i < vertices.Length; i++)
            {
                float3 n = outNormals[i];
                float u = math.atan2(n.x, n.z) / (2f * math.PI) + 0.5f;
                float v = n.y * 0.5f + 0.5f;
                outUVs[i] = new float2(u, v);
            }

            //triangles
            for (int i = 0; i < faces.Length; i++)
            {
                int3 tri = faces[i];
                int baseIndex = i * 3;
                outTris[baseIndex + 0] = tri.x;
                outTris[baseIndex + 1] = tri.y;
                outTris[baseIndex + 2] = tri.z;
            }
        }
    }
}