using System;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.RenderGraphModule;
using UnityEngine.Rendering.Universal;

public class RelativisticDopplerFeature : ScriptableRendererFeature
{
    public static float DopplerStrength = 1;
    public static float DopplerMinHue = 0;
    public static float DopplerMaxHue = .66f;
    public static float DopplerMaxAngle = 50;
    public static float DopplerSaturationDelta = .5f;
    public static Vector3 DopplerCameraForward = new Vector3(0, 0, 1);
    public static float DopplerTest = 0;

    RelativisticDopplerFeature()
    {
        Debug.Log("RDF instantiated");
    }

    [System.Serializable]
    public class Settings
    {
        public Material dopplerMaterial;
        public Material compositeMaterial;
        public RenderPassEvent injectionPoint = RenderPassEvent.AfterRenderingPostProcessing;
    }

    public Settings settings = new Settings();

    // This handle is written by DopplerPass and read by CompositePass
    internal TextureHandle dopplerTempHandle;

    public void SetCameraForward(Vector3 forward)
    {
        settings.dopplerMaterial.SetVector("_CameraForward", forward);
    }

    public void SetDopplerStrength(float strength)
    {
        settings.dopplerMaterial.SetFloat("_ShiftStrength", strength);
    }



    class DopplerPass : ScriptableRenderPass
    {
        private readonly RelativisticDopplerFeature owner;
        private readonly Material material;

        private class PassData
        {
            public TextureHandle source;
            public TextureHandle destination;
            public Material material;
        }

        public DopplerPass(RelativisticDopplerFeature owner, Material mat, RenderPassEvent evt)
        {
            this.owner = owner;
            material = mat;
            renderPassEvent = evt;
        }

        public override void RecordRenderGraph(RenderGraph renderGraph, ContextContainer frameData)
        {
            if (material == null)
                return;

            var resources = frameData.Get<UniversalResourceData>();

            using (var builder = renderGraph.AddRasterRenderPass<PassData>(
                "Relativistic Doppler Pass", out var passData))
            {
                passData.material = material;

                // INPUT: camera color
                builder.UseTexture(resources.cameraColor);
                passData.source = resources.cameraColor;

                // OUTPUT: dopplerTemp (same descriptor as cameraColor)
                TextureHandle descHandle = resources.cameraColor;
                TextureDesc desc = descHandle.GetDescriptor(renderGraph);
                desc.name = "DopplerTemp";

                TextureHandle dopplerTemp = renderGraph.CreateTexture(desc);
                passData.destination = dopplerTemp;

                // Store handle on the feature so CompositePass can use it
                owner.dopplerTempHandle = dopplerTemp;

                // Write into dopplerTemp, not the camera target
                //builder.SetRenderAttachment(dopplerTemp, 0);
                builder.SetRenderAttachment(dopplerTemp, 0);

                /*
                //field of red for testing
                builder.SetRenderFunc((PassData data, RasterGraphContext ctx) =>
                {
                    ctx.cmd.ClearRenderTarget(false, true, new Color(1, 0, 0, 1));
                });
                */

                builder.SetRenderFunc((PassData data, RasterGraphContext ctx) =>
                {
                    Blitter.BlitTexture(
                        ctx.cmd,
                        data.source,
                        new Vector4(1f, 1f, 0f, 0f),
                        data.material,
                        0
                    );
                });

            }
        }

        [Obsolete]
        public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
        {
            // Empty when using RenderGraph
        }
    }

    class CompositePass : ScriptableRenderPass
    {
        private readonly RelativisticDopplerFeature owner;
        private readonly Material material;

        private class PassData
        {
            public TextureHandle source;
            public Material material;
        }

        public CompositePass(RelativisticDopplerFeature owner, Material mat, RenderPassEvent evt)
        {
            this.owner = owner;
            material = mat;
            renderPassEvent = evt;
        }

        public override void RecordRenderGraph(RenderGraph renderGraph, ContextContainer frameData)
        {
            if (material == null)
                return;

            // If the first pass did not run or did not create a handle, skip
            if (!owner.dopplerTempHandle.IsValid())
                return;

            var resources = frameData.Get<UniversalResourceData>();

            using (var builder = renderGraph.AddRasterRenderPass<PassData>(
                "Relativistic Doppler Composite", out var passData))
            {
                passData.material = material;

                // INPUT: dopplerTemp from the first pass
                TextureHandle dopplerTemp = owner.dopplerTempHandle;
                builder.UseTexture(dopplerTemp);
                passData.source = dopplerTemp;

                // OUTPUT: camera target (active color)
                builder.SetRenderAttachment(resources.cameraColor, 0);

                builder.SetRenderFunc((PassData data, RasterGraphContext ctx) =>
                {
                    Blitter.BlitTexture(
                        ctx.cmd,
                        data.source,
                        new Vector4(1f, 1f, 0f, 0f),
                        data.material,
                        0
                    );
                });
            }
        }

        [Obsolete]
        public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
        {
            // Empty when using RenderGraph
        }
    }

    private DopplerPass dopplerPass;
    private CompositePass compositePass;

    public override void Create()
    {
        dopplerPass = new DopplerPass(this, settings.dopplerMaterial, settings.injectionPoint);
        compositePass = new CompositePass(this, settings.compositeMaterial, settings.injectionPoint);
    }

    public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
    {
        if (settings.dopplerMaterial == null || settings.compositeMaterial == null)
            return;

        settings.dopplerMaterial.SetFloat("_Strength", DopplerStrength);
        settings.dopplerMaterial.SetFloat("_MinHue", DopplerMinHue);
        settings.dopplerMaterial.SetFloat("_MaxHue", DopplerMaxHue);
        settings.dopplerMaterial.SetFloat("_MaxAngle", DopplerMaxAngle);
        settings.dopplerMaterial.SetFloat("_SaturationDelta", DopplerSaturationDelta);
        settings.dopplerMaterial.SetVector("_CameraForward", DopplerCameraForward);
        settings.dopplerMaterial.SetFloat("_Test", DopplerTest);

        renderer.EnqueuePass(dopplerPass);
        renderer.EnqueuePass(compositePass);
    }
}