using System.Collections.Generic;
using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;

namespace Shapes
{
    public static class Icosphere
    {
        public const int MaxSubdivisions = 8;


        //tracks how many entires are being pre-cached
        public static int PreCacheCount = 0;
        public static float PreCacheCompletion = 0;

        private readonly static Dictionary<int, MeshData> cacheData = new Dictionary<int, MeshData>();
        private readonly static Dictionary<int, Mesh> cacheMesh = new Dictionary<int, Mesh>();
        private static Dictionary<int, JobHandle> jobHandles = new Dictionary<int, JobHandle>();

        public static MeshDataNative[] nativeData = new MeshDataNative[MaxSubdivisions+1];

        //stored references to temporary arrays to dispose of after their job ends
        private static NativeList<float3>[] workingVerts = new NativeList<float3>[MaxSubdivisions+1];
        private static NativeList<int3>[] workingFaces = new NativeList<int3>[MaxSubdivisions+1];
        private static NativeHashMap<long, int>[] workingCaches = new NativeHashMap<long, int>[MaxSubdivisions+1];


        struct Triangle
        {
            public int v1, v2, v3;
            public Triangle(int a, int b, int c)
            {
                v1 = a; v2 = b; v3 = c;
            }
        }

        public struct MeshData
        {
            public Vector3[] vertices;
            public int[] triangles;
            public Vector2[] uvs;
            public Vector3[] normals;
        }

        public struct MeshDataNative
        {
            public NativeArray<float3> vertices;
            public NativeArray<int> triangles;
            public NativeArray<float2> uvs;
            public NativeArray<float3> normals;

            public MeshDataNative(int vertexCount, int triCount, Allocator alloc)
            {
                vertices = new NativeArray<float3>(vertexCount, alloc);
                triangles = new NativeArray<int>(triCount, alloc);
                uvs = new NativeArray<float2>(vertexCount, alloc);
                normals = new NativeArray<float3>(vertexCount, alloc);
            }

            public void Dispose()
            {
                if (vertices.IsCreated) vertices.Dispose();
                if (triangles.IsCreated) triangles.Dispose();
                if (uvs.IsCreated) uvs.Dispose();
                if (normals.IsCreated) normals.Dispose();
            }
        }


        //constructor
        static Icosphere()
        {
            //init
        }


        public static int CachedDataCount()
        {
            return cacheData.Count;
        }

        public static int CachedMeshesCount()
        {
            return cacheMesh.Count;
        }



        public static MeshData ConvertNativeToMeshData(MeshDataNative native)
        {
            MeshData data = new MeshData();
            int vCount = native.vertices.Length;
            int tCount = native.triangles.Length;
            int uvCount = native.uvs.Length;
            int nCount = native.normals.Length;
            data.vertices = new Vector3[vCount];
            data.triangles = new int[tCount];
            data.uvs = new Vector2[uvCount];
            data.normals = new Vector3[nCount];

            for (int i = 0; i < vCount; i++)
                data.vertices[i] = new Vector3(native.vertices[i].x, native.vertices[i].y, native.vertices[i].z);
            for (int i = 0; i < tCount; i++)
                data.triangles[i] = native.triangles[i];
            for (int i = 0; i < uvCount; i++)
                data.uvs[i] = new Vector2(native.uvs[i].x, native.uvs[i].y);
            for (int i = 0; i < nCount; i++)
                data.normals[i] = new Vector3(native.normals[i].x, native.normals[i].y, native.normals[i].z);
            return data;
        }

        private static MeshDataNative AllocateNativeMeshData(int subdivisions)
        {
            int vertexCount = ComputeVertexCount(subdivisions);
            int triCount = ComputeTriangleIndexCount(subdivisions);

            return new MeshDataNative(vertexCount, triCount, Allocator.Persistent);
        }

        private static int ComputeTriangleIndexCount(int subdivisions)
        {
            int faces = 20;                                         //20 faces in an icosahedron
            for (int i = 0; i < subdivisions; i++) faces *= 4;      //each face splits into 4
            return faces * 3;                                       //3 indices per face
        }
        private static int ComputeVertexCount(int subdivisions)
        {
            int verts = 12;
            int faces = 20;
            for (int i = 0; i < subdivisions; i++)
            {
                verts += faces * 3;
                faces *= 4;
            }
            return verts;
        }



        
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
                Debug.Log("Job executing");
                // 1. Generate base icosahedron
                // 2. Normalize
                // 3. Subdivide
                // 4. Write final arrays

                //Golden ratio - I guess burst doesn't like division?
                float t = (1f + math.sqrt(5f)) * .5f;

                // Initial 12 vertices of an icosahedron
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


                // Normalize to radius 1 (unit vectors - faster manipulation)
                for (int i = 0; i < vertices.Length; i++)
                    vertices[i] = math.normalize(vertices[i]);

                // 20 faces of an icosahedron
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

                // Subdivide
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
                    for (int n=0; n<newFaces.Length; n++) faces.Add(newFaces[n]);
                    newFaces.Dispose();
                }

                //export the vertices
                for (int i=0; i<vertices.Length; i++)
                    outVerts[i] = vertices[i];

                // Normals = normalized vertex positions
                for (int i = 0; i < vertices.Length; i++)
                    outNormals[i] = math.normalize(vertices[i]);

                // UVs (simple spherical projection)
                for (int i = 0; i < vertices.Length; i++)
                {
                    float3 n = outNormals[i];
                    float u = math.atan2(n.x, n.z) / (2f * math.PI) + 0.5f;
                    float v = n.y * 0.5f + 0.5f;
                    outUVs[i] = new float2(u, v);
                }

                // Triangles
                for (int i = 0; i < faces.Length; i++)
                {
                    int3 tri = faces[i];
                    int baseIndex = i * 3;
                    outTris[baseIndex + 0] = tri.x;
                    outTris[baseIndex + 1] = tri.y;
                    outTris[baseIndex + 2] = tri.z;
                }
                Debug.Log("Job completing");
            }
        }


        private static JobHandle ScheduleIcosaJob(int subdivisions)
        {
            //Set up buffers for Native (since it can't be done in the job)
            MeshDataNative native = AllocateNativeMeshData(subdivisions);
            //save a reference to it for later pickup
            nativeData[subdivisions] = native;

            //These are passed into the job, but must be defined outside of it
            NativeList<float3> verts = new NativeList<float3>(Allocator.Persistent);
            NativeList<int3> faces = new NativeList<int3>(Allocator.Persistent);
            NativeHashMap<long, int> midpointCache = new NativeHashMap<long, int>(1024, Allocator.Persistent);

            //store references to these to dispose of after the job is done
            workingVerts[subdivisions] = verts;
            workingFaces[subdivisions] = faces;
            workingCaches[subdivisions] = midpointCache;


            // 3. Create the job
            Debug.Log("Creating job");
            var job = new IcosaJob
            {
                subdivisions = subdivisions,

                // working containers
                vertices = verts,
                faces = faces,
                midpointCache = midpointCache,

                // final output
                outVerts = native.vertices,
                outTris = native.triangles,
                outNormals = native.normals,
                outUVs = native.uvs
            };

            // 4. Schedule the job
            JobHandle handle = job.Schedule();
            Debug.Log($"Scheduling job, got handle {handle}");
            // 5. Store the handle
            jobHandles[subdivisions] = handle;
            return handle;
        }

    
        /// <summary>
        /// launches all the jobs, returns 1 on full completion.
        /// </summary>
        /// <param name="levelMax"></param>
        public static void PreCache(int levelMax)
        {
            PreCacheCount = levelMax;
            for (int i = 1; i <= levelMax; i++) GenerateData(i);
                //nativeData[i] = AllocateNativeMeshData(i);
                //jobHandles[i] = ScheduleGenerateJob(i, nativeData[i]);
        }

        /// <summary>
        /// Gets status on the jobs (and also triggers caching the results when done)
        /// </summary>
        public static float GetStatus()
        {
            if (PreCacheCount == 0) return -1;       //not triggered yet
            int readyData = 0;
            int readyMeshes = 0;
            for (int i = 1; i <= PreCacheCount; i++)
            {
                if (cacheMesh.ContainsKey(i))       //Mesh is cached.  Good to go.
                {
                    readyMeshes++;
                    readyData++;
                    continue;
                }
                if (cacheData.ContainsKey(i))   //Data is ready but no Mesh yet
                {
                    readyData++;
                    Generate(i);
                    continue;
                }
                if (!jobHandles.ContainsKey(i) || jobHandles[i].IsCompleted)
                {
                    GenerateData(i);
                    continue;
                }
                if (!jobHandles[i].IsCompleted)
                {
                    //job in progress, be patient
                    continue;
                }
                //if we're still here something is broken
                Debug.Log($"Icosphere.GetStatus ran into a weird state on item {i}");
            }
            //compute percentage
            PreCacheCompletion = (float)(readyData + readyMeshes) / (float)(PreCacheCount * 2);
            return PreCacheCompletion;
        }



        /// <summary>
        /// Generates a spherical Mesh, from cache where available
        /// </summary>
        /// <param name="subdivisions"></param>
        /// <returns></returns>
        public static Mesh Generate(int subdivisions)
        {
            Debug.Log($"Running Generate({subdivisions})");
            subdivisions = Mathf.Clamp(subdivisions, 0, MaxSubdivisions);
            //check cache and return a clone from it if present
            if (cacheMesh.TryGetValue(subdivisions, out Mesh cached) && cached) return CloneMesh(cached);

            //check MeshData cache to see if we need to generate it
            MeshData data = GenerateData(subdivisions);

            //no data - in progress
            if (data.vertices == null) return null;

            //build mesh from data
            Mesh mesh = new Mesh();
            mesh.indexFormat = (data.vertices.Length > 65000)
                ? UnityEngine.Rendering.IndexFormat.UInt32
                : UnityEngine.Rendering.IndexFormat.UInt16;

            mesh.vertices = data.vertices;
            mesh.normals = data.normals;
            mesh.uv = data.uvs;
            mesh.triangles = data.triangles;

            mesh.RecalculateBounds();
            mesh.RecalculateNormals();
            cacheMesh[subdivisions] = mesh;     //store this data in the cache
            return CloneMesh(mesh);         //return a clone so we don't contaminate cache
        }



        public static MeshData GenerateData(int subdivisions)
        {
            Debug.Log($"Running GenerateData({subdivisions})");
            //range checking
            subdivisions = Mathf.Clamp(subdivisions, 0, MaxSubdivisions);
            //check cache and return a clone from it if present
            if (cacheData.TryGetValue(subdivisions, out MeshData cached)) return cached;

            //not cached.  See if there's a job in progress.
            if (!jobHandles.ContainsKey(subdivisions))          //no job in progress
            {
                Debug.Log($"GenerateData launching Job ({subdivisions})");
                //launch job
                jobHandles[subdivisions] = ScheduleIcosaJob(subdivisions);
                Debug.Log($"GenerateData got job handle ({subdivisions})");
                //return empty data for now
                return new MeshData();                          //or we could just let it bleed through
            }

            if (jobHandles[subdivisions].IsCompleted)           //job completed
            {
                Debug.Log($"Job Complete ({subdivisions})");
                //should verify nativedata is present
                jobHandles[subdivisions].Complete();
                MeshData meshData = ConvertNativeToMeshData(nativeData[subdivisions]);
                //gotta get rid of it now that we're done with it
                nativeData[subdivisions].Dispose();
                workingVerts[subdivisions].Dispose();
                workingFaces[subdivisions].Dispose();
                workingCaches[subdivisions].Dispose();

                cacheData[subdivisions] = meshData;     //cache it
                return meshData;                        //return it
            }

            //if we're still here, the job is still in progress.
            return new MeshData();
        }



        public static Mesh CloneMesh(Mesh source)
        {
            Mesh m = new Mesh();
            m.indexFormat = source.indexFormat;

            // Deep copy vertex attributes
            m.vertices = (Vector3[])source.vertices.Clone();
            m.normals = source.normals != null && source.normals.Length > 0
                            ? (Vector3[])source.normals.Clone()
                            : null;
            m.tangents = source.tangents != null && source.tangents.Length > 0
                            ? (Vector4[])source.tangents.Clone()
                            : null;
            m.colors = source.colors != null && source.colors.Length > 0
                            ? (Color[])source.colors.Clone()
                            : null;

            // Deep copy UV channels
            m.uv = source.uv != null && source.uv.Length > 0
                            ? (Vector2[])source.uv.Clone()
                            : null;
            m.uv2 = source.uv2 != null && source.uv2.Length > 0
                            ? (Vector2[])source.uv2.Clone()
                            : null;
            m.uv3 = source.uv3 != null && source.uv3.Length > 0
                            ? (Vector2[])source.uv3.Clone()
                            : null;
            m.uv4 = source.uv4 != null && source.uv4.Length > 0
                            ? (Vector2[])source.uv4.Clone()
                            : null;

            // Deep copy triangles
            m.triangles = (int[])source.triangles.Clone();

            // Optional: copy bindposes and bone weights if used
            if (source.bindposes != null && source.bindposes.Length > 0)
                m.bindposes = (Matrix4x4[])source.bindposes.Clone();

            if (source.boneWeights != null && source.boneWeights.Length > 0)
                m.boneWeights = (BoneWeight[])source.boneWeights.Clone();

            m.RecalculateBounds();
            return m;
        }


        public static void FlipMesh(Mesh mesh)
        {
            if (!mesh) return;
            // Flip normals
            var normals = mesh.normals;
            for (int i = 0; i < normals.Length; i++)
                normals[i] = -normals[i];
            mesh.normals = normals;

            // Reverse triangle winding
            var triangles = mesh.triangles;
            for (int i = 0; i < triangles.Length; i += 3)
            {
                int temp = triangles[i];
                triangles[i] = triangles[i + 1];
                triangles[i + 1] = temp;
            }
            mesh.triangles = triangles;
            mesh.RecalculateNormals();
        }


        public static Mesh CloneMesh2(Mesh source)
        {
            Mesh m = new Mesh();
            m.indexFormat = source.indexFormat;

            m.vertices = source.vertices;
            m.normals = source.normals;
            m.tangents = source.tangents;
            m.uv = source.uv;
            m.uv2 = source.uv2;
            m.colors = source.colors;
            m.triangles = source.triangles;

            m.bounds = source.bounds;
            return m;
        }


    }
}