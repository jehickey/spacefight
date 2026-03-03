Shader "Custom/Stars"
{
    Properties
    {
        _Color("Color", Color) = (1,1,1,1)
        _Brightness("Brightness", Float) = 1.0
        _Falloff("Falloff", Float) = 4.0
    }

    SubShader
    {
        Tags { "Queue"="Transparent" "RenderType"="Transparent" }
        Blend SrcAlpha OneMinusSrcAlpha
        ZWrite Off
        Cull Off

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_instancing

            #include "UnityCG.cginc"

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv     : TEXCOORD0;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct v2f
            {
                float4 pos : SV_POSITION;
                float2 uv  : TEXCOORD0;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            UNITY_INSTANCING_BUFFER_START(Props)
                UNITY_DEFINE_INSTANCED_PROP(float4, _Color)
                UNITY_DEFINE_INSTANCED_PROP(float, _Brightness)
            UNITY_INSTANCING_BUFFER_END(Props)

            float _Falloff;

            v2f vert(appdata v)
            {
                v2f o;
                UNITY_SETUP_INSTANCE_ID(v);
                UNITY_TRANSFER_INSTANCE_ID(v, o);

                o.pos = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                return o;
            }

            fixed4 frag(v2f i) : SV_Target
            {
                UNITY_SETUP_INSTANCE_ID(i);

                float2 uv = (i.uv - 0.5) * 2.0;
                float r2 = dot(uv, uv);

                float alpha = exp(-r2 * _Falloff);

                float4 col = UNITY_ACCESS_INSTANCED_PROP(Props, _Color);
                float brightness = UNITY_ACCESS_INSTANCED_PROP(Props, _Brightness);

                return float4(col.rgb * brightness * alpha, alpha);
            }
            ENDCG
        }
    }
}
