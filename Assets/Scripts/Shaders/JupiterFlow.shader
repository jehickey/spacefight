Shader "Custom/JupiterFlow"
{
    Properties
    {
        _MainTex ("Surface Texture", 2D) = "white" {}
        _BandTex ("Band Control Texture", 2D) = "white" {}

        _NoiseScale ("Noise Scale", Float) = 2.0
        _NoiseStrength ("Noise Strength", Float) = 0.05
        _NoiseSpeed ("Noise Speed", Float) = 1.0

        _PolarFalloffInner ("Polar Inner", Float) = 0.2
        _PolarFalloffOuter ("Polar Outer", Float) = 0.4

        _Color ("Tint Color", Color) = (1,1,1,1)

        _FresnelColor ("Fresnel Color", Color) = (1,1,1,1)
        _FresnelPower ("Fresnel Power", Float) = 3.0
        _FresnelIntensity ("Fresnel Intensity", Float) = 1.0
    }

    SubShader
    {
        Tags { "RenderType"="Opaque" "Queue"="Geometry" }
        LOD 200

        // ============================================================
        // PASS 1: LIT SURFACE WITH CURL NOISE + BAND CONTROL
        // ============================================================
        Pass
        {
            Tags { "LightMode"="UniversalForward" }

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS
            #pragma multi_compile _ _ADDITIONAL_LIGHTS
            #pragma multi_compile _ _SHADOWS_SOFT

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS   : NORMAL;
                float2 uv         : TEXCOORD0;
            };

            struct Varyings
            {
                float4 positionHCS : SV_POSITION;
                float3 positionWS  : TEXCOORD0;
                float3 normalWS    : TEXCOORD1;
                float2 uv          : TEXCOORD2;
            };

            sampler2D _MainTex;
            float4 _MainTex_ST;

            sampler2D _BandTex;
            float4 _BandTex_ST;

            float _NoiseScale;
            float _NoiseStrength;
            float _NoiseSpeed;

            float _PolarFalloffInner;
            float _PolarFalloffOuter;

            float4 _Color;

            // -----------------------------
            // Simple hash + value noise
            // -----------------------------
            float hash(float2 p)
            {
                return frac(sin(dot(p, float2(127.1, 311.7))) * 43758.5453);
            }

            float noise(float2 p)
            {
                float2 i = floor(p);
                float2 f = frac(p);

                float a = hash(i);
                float b = hash(i + float2(1,0));
                float c = hash(i + float2(0,1));
                float d = hash(i + float2(1,1));

                float2 u = f * f * (3.0 - 2.0 * f);

                return lerp(lerp(a, b, u.x), lerp(c, d, u.x), u.y);
            }

            // -----------------------------
            // 2D curl noise from scalar noise
            // -----------------------------
            float2 curlNoise(float2 p)
            {
                float eps = 0.1;

                float n1 = noise(p + float2(0, eps));
                float n2 = noise(p - float2(0, eps));
                float a = (n1 - n2) * 0.5;

                float n3 = noise(p + float2(eps, 0));
                float n4 = noise(p - float2(eps, 0));
                float b = (n3 - n4) * 0.5;

                return float2(a, -b);
            }

            Varyings vert (Attributes IN)
            {
                Varyings OUT;
                OUT.positionHCS = TransformObjectToHClip(IN.positionOS);
                OUT.positionWS  = TransformObjectToWorld(IN.positionOS.xyz);
                OUT.normalWS    = TransformObjectToWorldNormal(IN.normalOS);
                OUT.uv          = TRANSFORM_TEX(IN.uv, _MainTex);
                return OUT;
            }

            half4 frag (Varyings IN) : SV_Target
            {
                // Base UV
                float2 uv = IN.uv;

                // Band control texture (same UV space)
                float2 bandUV = TRANSFORM_TEX(IN.uv, _BandTex);
                float4 band   = tex2D(_BandTex, bandUV);

                // Alpha encodes both direction and strength:
                //  alpha < 0.5  -> one direction
                //  alpha > 0.5  -> opposite direction
                //  strength = distance from 0.5
                float a = band.a;
                float dir = (a < 0.5) ? -1.0 : 1.0;
                float bandStrength = abs(a - 0.5) * 2.0; // 0..1

                // Polar falloff: less activity near poles (uv.y ~ 0 or 1)
                float latitude = uv.y;
                float southFall = smoothstep(_PolarFalloffInner, _PolarFalloffOuter, latitude);
                float northFall = 1.0 - smoothstep(1.0 - _PolarFalloffOuter, 1.0 - _PolarFalloffInner, latitude);
                float polarFalloff = southFall * northFall; // strong in mid-latitudes

                // Time for animated noise
                float t = _Time.y * _NoiseSpeed;

                // Noise space: scale horizontally, slightly compress vertically if desired
                float2 p = float2(uv.x * _NoiseScale, uv.y * _NoiseScale) + t;

                // Curl field
                float2 curl = curlNoise(p);

                // Final distortion strength
                float finalStrength = _NoiseStrength * bandStrength * polarFalloff;

                // Apply direction and strength
                float2 uvDistorted = uv + curl * finalStrength * dir;

                // Sample surface texture
                float4 albedo = tex2D(_MainTex, uvDistorted) * _Color;

                // Simple URP main light
                float3 normalWS = normalize(IN.normalWS);
                float3 viewDirWS = normalize(_WorldSpaceCameraPos - IN.positionWS);

                Light mainLight = GetMainLight();
                float3 lightDirWS = normalize(mainLight.direction);
                float NdotL = saturate(dot(normalWS, -lightDirWS));

                float3 diffuse = albedo.rgb * mainLight.color * NdotL;

                // Simple ambient from spherical harmonics
                float3 ambient = SampleSH(normalWS) * albedo.rgb;

                float3 color = diffuse + ambient;

                return float4(color, 1.0);
            }

            ENDHLSL
        }

        // ============================================================
        // PASS 2: FRESNEL ATMOSPHERE GLOW (ADDITIVE)
        // ============================================================
        /*
        Pass
        {
            Tags { "LightMode"="UniversalForward" }
            Blend One One
            ZWrite Off

            HLSLPROGRAM
            #pragma vertex vertGlow
            #pragma fragment fragGlow

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS   : NORMAL;
                float2 uv         : TEXCOORD0;
            };

            struct Varyings
            {
                float4 positionHCS : SV_POSITION;
                float3 positionWS  : TEXCOORD0;
                float3 normalWS    : TEXCOORD1;
                float2 uv          : TEXCOORD2;
            };

            float4 _FresnelColor;
            float  _FresnelPower;
            float  _FresnelIntensity;

            Varyings vertGlow (Attributes IN)
            {
                Varyings OUT;
                OUT.positionHCS = TransformObjectToHClip(IN.positionOS);
                OUT.positionWS  = TransformObjectToWorld(IN.positionOS.xyz);
                OUT.normalWS    = TransformObjectToWorldNormal(IN.normalOS);
                OUT.uv          = IN.uv;
                return OUT;
            }

            half4 fragGlow (Varyings IN) : SV_Target
            {
                float3 normalWS = normalize(IN.normalWS);
                float3 viewDirWS = normalize(_WorldSpaceCameraPos - IN.positionWS);

                // Fresnel term: stronger at grazing angles
                float fresnel = pow(1.0 - saturate(dot(normalWS, viewDirWS)), _FresnelPower);

                float3 color = _FresnelColor.rgb * fresnel * _FresnelIntensity;

                return float4(color, 1.0);
            }

            ENDHLSL
        }
        */
    }
}