using System;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.RenderGraphModule;
using UnityEngine.Rendering.Universal;

public class RelativisticDopplerFeature : ScriptableRendererFeature
{
    public static float DopplerStrength = 0;
    public static float DopplerMinHue = 0;
    public static float DopplerMaxHue = .66f;
    public static float DopplerMaxAngle = 50;
    public static float DopplerSaturationDelta = .5f;
    public static float DopplerBrightnessBoost = .25f;
    public static float DopplerBrightnessRange = .5f;
    public static Vector3 DopplerCameraForward = new Vector3(0, 0, 1);
    public static float DopplerTest = 0;

    [System.Serializable]
    public class Settings
    {
        public Material dopplerMaterial;
        public Material compositeMaterial;
        public RenderPassEvent injectionPoint = RenderPassEvent.AfterRenderingPostProcessing;
    }

    public Settings settings = new Settings();

    //handle used to transfer the texture between passes
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

                //camera color
                builder.UseTexture(resources.cameraColor);
                passData.source = resources.cameraColor;

                //dopplerTemp (same descriptor as cameraColor)
                TextureHandle descHandle = resources.cameraColor;
                TextureDesc desc = descHandle.GetDescriptor(renderGraph);
                desc.name = "DopplerTemp";

                TextureHandle dopplerTemp = renderGraph.CreateTexture(desc);
                passData.destination = dopplerTemp;

                //store handle on the feature so CompositePass can use it
                owner.dopplerTempHandle = dopplerTemp;

                //write into dopplerTemp (get errors if writing back to source)
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
            //is there any reason to keep this?
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
        settings.dopplerMaterial.SetFloat("_BrightnessBoost", DopplerBrightnessBoost);
        settings.dopplerMaterial.SetFloat("_BrightnessRange", DopplerBrightnessRange);
        settings.dopplerMaterial.SetVector("_CameraForward", DopplerCameraForward);
        settings.dopplerMaterial.SetFloat("_Test", DopplerTest);

        renderer.EnqueuePass(dopplerPass);
        renderer.EnqueuePass(compositePass);
    }
}