using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using System.Linq;

public static class URPFeatures
{
    public static RelativisticDopplerFeature GetDopplerFeature(Renderer cam)
    {
        Debug.Log("Calling GetDoppler()");
        var urp = (UniversalRenderPipelineAsset)GraphicsSettings.currentRenderPipeline;

        // Unity 6: renderer data lives here
        var rendererData = urp.rendererDataList[0];
        if (rendererData) Debug.Log("Got RendererDataList");
        return rendererData.rendererFeatures
            .OfType<RelativisticDopplerFeature>()
            .FirstOrDefault();
    }
}
