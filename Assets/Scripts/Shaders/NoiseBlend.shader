Shader "Custom/NoiseBlend"
{
    Properties
    {
        _MainTex ("Base", 2D) = "white" {}
        _NoiseTex ("Noise", 2D) = "white" {}
        _Strength ("Strength", Range(0,1)) = .5
    }

    SubShader
    {
        Tags { "RenderType"="Opaque" }
        Pass
        {
            ZWrite Off
            Cull Off
            ZTest LEqual

            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            sampler2D _MainTex;
            sampler2D _NoiseTex;
            float _Strength;

            struct appdata { float4 vertex : POSITION; float2 uv : TEXCOORD0; };
            struct v2f { float4 pos : SV_POSITION; float2 uv : TEXCOORD0; };

            v2f vert (appdata v)
            {
                v2f o;
                o.pos = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                return o;
            }

            float4 frag (v2f i) : SV_Target
            {
                float4 baseCol = tex2D(_MainTex, i.uv);
                float4 noiseCol = tex2D(_NoiseTex, i.uv);

                //multiply blend
                //float4 result = lerp(baseCol, baseCol * noiseCol, _Strength);
                //additive blend
                float4 result = baseCol + noiseCol*_Strength;
                //screen
                //float4 result = 1 - (1 - baseCol) * (1 - noiseCol);

                return result;
            }
            ENDCG
        }
    }
}