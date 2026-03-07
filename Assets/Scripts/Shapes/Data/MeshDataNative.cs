using Unity.Collections;
using Unity.Mathematics;
using UnityEngine;

namespace Shapes
{
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


        public MeshData toMeshData()
        {
            MeshData data = new MeshData();
            int vCount = vertices.Length;
            int tCount = triangles.Length;
            int uvCount = uvs.Length;
            int nCount = normals.Length;
            data.vertices = new Vector3[vCount];
            data.triangles = new int[tCount];
            data.uvs = new Vector2[uvCount];
            data.normals = new Vector3[nCount];

            for (int i = 0; i < vCount; i++)
                data.vertices[i] = new Vector3(vertices[i].x, vertices[i].y, vertices[i].z);
            for (int i = 0; i < tCount; i++)
                data.triangles[i] = triangles[i];
            for (int i = 0; i < uvCount; i++)
                data.uvs[i] = new Vector2(uvs[i].x, uvs[i].y);
            for (int i = 0; i < nCount; i++)
                data.normals[i] = new Vector3(normals[i].x, normals[i].y, normals[i].z);
            return data;
        }
    }

}