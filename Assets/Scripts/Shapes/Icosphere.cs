using System.Collections.Generic;
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


        private static JobHandle ScheduleIcosaJob(int subdivisions)
        {
            //set up buffers for native (since it can't be done in the job)
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

            //job creation
            var job = new IcosaJob
            {
                subdivisions = subdivisions,

                //working containers
                vertices = verts,
                faces = faces,
                midpointCache = midpointCache,

                //final output
                outVerts = native.vertices,
                outTris = native.triangles,
                outNormals = native.normals,
                outUVs = native.uvs
            };
            
            JobHandle handle = job.Schedule();      //schedule the job
            jobHandles[subdivisions] = handle;      //store the handle
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
            //range checking
            subdivisions = Mathf.Clamp(subdivisions, 0, MaxSubdivisions);
            //check cache and return a clone from it if present
            if (cacheData.TryGetValue(subdivisions, out MeshData cached)) return cached;

            //not cached.  See if there's a job in progress.
            if (!jobHandles.ContainsKey(subdivisions))          //no job in progress
            {
                //launch job
                jobHandles[subdivisions] = ScheduleIcosaJob(subdivisions);
                //return empty data for now
                return new MeshData();                          //or we could just let it bleed through
            }

            if (jobHandles[subdivisions].IsCompleted)           //job completed
            {
                //should verify nativedata is present
                jobHandles[subdivisions].Complete();
                MeshData meshData = nativeData[subdivisions].toMeshData();
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


    }
}