using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.Rendering;

public class Trail : MonoBehaviour
{
    public int MaxParticles = 10;
    public float depositionRate = 2;        //per second
    public float ParticleSize = .005f;
    public float ParticleShrinkRate = .5f;  //per second
    public int ParticleDetail = 0;
    
    private float lastParticle;
    public Material material;
    public Mesh mesh;

    private List<Matrix4x4> index = new List<Matrix4x4>();

    void Start()
    {
        material = Simulation.I.TrailMaterial;
        mesh = Shapes.Icosphere.Generate(ParticleDetail);
    }

    void Update()
    {
        if (!mesh) mesh = Shapes.Icosphere.Generate(ParticleDetail);
        DropParticle();             //if timer goes off, leave a new bit of trail
        UpdateList();
        DrawParticles();
    }


    private void DropParticle()
    {
        if (depositionRate <= 0) return;                            //no deposition at all
        if (Time.time - lastParticle < 1 / depositionRate) return;  //not time yet
        Matrix4x4 matrix = Matrix4x4.TRS(transform.position, Quaternion.identity, Vector3.one * ParticleSize);
        index.Add(matrix);
        lastParticle = Time.time;
    }

    private void UpdateList()
    {
        while (index.Count > MaxParticles) index.RemoveAt(0);
        for (int i= 0; i < index.Count; i++)
        {
            Matrix4x4 m = index[i];
            float scale = m.lossyScale.x - ParticleShrinkRate * Time.deltaTime;
            if (scale > 0)
            {
                m.m00 = scale;
                m.m11 = scale;
                m.m22 = scale;
                index[i] = m;
            }
            else
            {
                index.RemoveAt(i);
                i--;
            }
        }
    }

    private void DrawParticles()
    {
        Graphics.DrawMeshInstanced(mesh, 0, material, index);
    }

}
