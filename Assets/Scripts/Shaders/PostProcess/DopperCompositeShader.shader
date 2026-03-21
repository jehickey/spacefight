Shader "Post/DopplerCompositeShader"
{
    SubShader
    {
        Tags {  "RenderPipeline"="UniversalPipeline"  "RenderType"="Opaque" "Queue"="Overlay" }

        Pass
        {
            ZWrite Off
            Cull Off
            ZTest Always

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            TEXTURE2D(_BlitTexture);
            SAMPLER(sampler_BlitTexture);

            struct Varyings
            {
                float4 positionHCS : SV_POSITION;
                float2 uv : TEXCOORD0;
            };

            Varyings vert(uint vertexID : SV_VertexID)
            {
                Varyings OUT;
                OUT.positionHCS = GetFullScreenTriangleVertexPosition(vertexID);
                OUT.uv = GetFullScreenTriangleTexCoord(vertexID);
                return OUT;
            }

            float4 frag(Varyings IN) : SV_Target
            {
                return SAMPLE_TEXTURE2D(_BlitTexture, sampler_BlitTexture, IN.uv);
            }

            ENDHLSL
        }
    }
}