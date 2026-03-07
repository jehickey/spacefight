Shader "Custom/AtmosphereGlow"
{
    Properties
    {
        [MainColor] _BaseColor("_Color", Color) = (1,1, 1, 1)
        _LightDirection ("_LightDirection", Vector) = (0,0,1,0)
    }

    SubShader
    {
        Tags { "RenderType"="Transparent" "Queue"="Transparent" "RenderPipeline"="UniversalPipeline"}
        Blend SrcAlpha OneMinusSrcAlpha
        //Blend One One
        
        Pass
        {
            HLSLPROGRAM

            #pragma vertex vert
            #pragma fragment frag

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS : NORMAL;
            };

            struct Varyings
            {
                float4 positionHCS : SV_POSITION;
                float3 normalWS : TEXCOORD0;
                float3 positionWS : TEXCOORD1;
            };


            CBUFFER_START(UnityPerMaterial)
                half4 _BaseColor;
                float4 _LightDirection;
            CBUFFER_END

            Varyings vert(Attributes IN)
            {
                Varyings OUT;
                OUT.positionHCS = TransformObjectToHClip(IN.positionOS.xyz);
                OUT.positionWS = TransformObjectToWorld(IN.positionOS.xyz);
                OUT.normalWS = TransformObjectToWorldNormal(IN.normalOS);
                return OUT;
            }

            half4 frag(Varyings IN) : SV_Target
            {
                half4 OUT;
                float3 viewDir = normalize(_WorldSpaceCameraPos - IN.positionWS);
                float3 normal = normalize(IN.normalWS);
                float ndotv = dot(normal, viewDir);
                float fresnelBase = 1.0 - ndotv;
                float fresnel = pow(fresnelBase, 3.0);

                float3 lightDir = normalize(_LightDirection);
                float ndotl = dot(normal, lightDir);
                //remap so night side isn't fully dark
                float lightFactor = saturate(ndotl * 0.5 + 0.5);
                float finalGlow = fresnel * lightFactor;

                return _BaseColor * finalGlow;
            }

            ENDHLSL
        }
    }
}
