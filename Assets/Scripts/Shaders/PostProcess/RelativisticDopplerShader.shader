Shader "Post/RelativisticDopplerShader"
{
    Properties
    {
        _Strength("Strength", float)                = 1
        _MinHue("Min Hue", float)                   = 0
        _MaxHue("Max Hue", float)                   = 0.66
        _MaxAngle("Max Angle", float)               = 50
        _SaturationDelta("Saturation Delta", float) = .5
        _CameraForward("Camera Forward", Vector)    = (0,0,1,0)
        _Test("Test", float)                        = 0
    }

    SubShader
    {
        Tags { "RenderType"="Opaque" "RenderPipeline"="UniversalPipeline" }

        Pass
        {
            Name "DopplerShift"
            ZTest Always ZWrite Off Cull Off

            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            TEXTURE2D(_BlitTexture);
            SAMPLER(sampler_BlitTexture);

            float _Strength;
            float _MinHue;
            float _MaxHue;
            float _MaxAngle;
            float _SaturationDelta;
            float3 _CameraForward;
            float _Test;

            struct Varyings
            {
                float4 positionHCS : SV_POSITION;
                float2 uv : TEXCOORD0;
            };

            Varyings Vert(uint vertexID : SV_VertexID)
            {
                Varyings OUT;
                OUT.positionHCS = GetFullScreenTriangleVertexPosition(vertexID);
                OUT.uv = GetFullScreenTriangleTexCoord(vertexID);
                return OUT;
            }

            float3 RGBtoHSV(float3 c)
            {
                float4 K = float4(0.0, -1.0/3.0, 2.0/3.0, -1.0);
                float4 p = (c.g < c.b) ? float4(c.bg, K.wz) : float4(c.gb, K.xy);
                float4 q = (c.r < p.x) ? float4(p.xyw, c.r) : float4(c.r, p.yzx);

                float d = q.x - min(q.w, q.y);
                float e = 1e-10;

                float3 hsv;
                hsv.x = abs(q.z + (q.w - q.y) / (6.0 * d + e));  // Hue
                hsv.y = d / (q.x + e);                           // Saturation
                hsv.z = q.x;                                     // Value

                return hsv;
            }

            float3 HSVtoRGB(float3 hsv)
            {
                float4 K = float4(1.0, 2.0/3.0, 1.0/3.0, 3.0);
                float3 p = abs(frac(hsv.xxx + K.xyz) * 6.0 - K.www);
                return hsv.z * lerp(K.xxx, saturate(p - K.xxx), hsv.y);
            }

            float3 ApplyDoppler2(float3 color, float2 uv)
            {
                if (_Strength == 0) return color;
                // Reconstruct view direction from UV
                float2 ndc = uv * 2.0 - 1.0;
                float3 viewDir = normalize(float3(ndc.x, ndc.y, 1.0));

                //dot product - front = +1, back = -1
                float d = dot(viewDir, normalize(_CameraForward));
                
                //limit result to maximum effect angle (in radians)
                float minDot = cos(radians(_MaxAngle));
                d = clamp(d, minDot, 1);

                //set hue
                float t = (d-minDot) / (1.0 - minDot);
                t = saturate(t * _Strength);

                // Convert to HSV
                float3 hsv = RGBtoHSV(color);
                hsv.x  = lerp(_MinHue, _MaxHue, t);
                hsv.y = saturate((hsv.y + _SaturationDelta) * _Strength);

                // Convert back to RGB
                return HSVtoRGB(hsv);
            }

            float4 Frag(Varyings IN) : SV_Target
            {
                //return float4(IN.uv,0,1);
                float3 col = SAMPLE_TEXTURE2D(_BlitTexture, sampler_BlitTexture, IN.uv).rgb;
                col = ApplyDoppler2(col, IN.uv);
                if (_Test > 0) 
                {
                    return float4(1,0,1,1);   //field-of-magenta test
                } else {
                    return float4(col, 1.0);
                }
            }

            ENDHLSL
        }
    }
}