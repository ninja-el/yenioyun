Shader "MatchPack/VFX/UfoBillboard"
{
    Properties
    {
        _MainTex ("Base Color", 2D) = "white" {}
        _MaskTex ("Silhouette Mask", 2D) = "white" {}
        _Collected ("Lit Lamp Count", Range(0, 3)) = 0
        _LampDim ("Unlit Lamp Brightness", Range(0, 1)) = 0.16
    }

    SubShader
    {
        Tags
        {
            "Queue" = "Transparent+10"
            "IgnoreProjector" = "True"
            "RenderType" = "Transparent"
        }

        Blend SrcAlpha OneMinusSrcAlpha
        ZWrite Off
        Cull Off

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            // Lamba yerleri kaynak görselin piksel koordinatlarıdır; görsel değişirse bunlar da değişir.
            #define SOURCE_SIZE float2(1206, 1503)

            sampler2D _MainTex;
            float4 _MainTex_ST;
            sampler2D _MaskTex;
            float _Collected;
            float _LampDim;

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct v2f
            {
                float4 pos : SV_POSITION;
                float2 uv : TEXCOORD0;
            };

            v2f vert(appdata input)
            {
                v2f output;
                output.pos = UnityObjectToClipPos(input.vertex);
                output.uv = TRANSFORM_TEX(input.uv, _MainTex);
                return output;
            }

            float Lamp(float2 pixel, float2 center, float2 radius, float angle)
            {
                float2 offset = pixel - center;
                float s = sin(angle);
                float c = cos(angle);
                offset = float2(c * offset.x + s * offset.y, -s * offset.x + c * offset.y) / radius;
                return 1 - smoothstep(0.88, 1.04, length(offset));
            }

            fixed4 frag(v2f input) : SV_Target
            {
                fixed4 color = tex2D(_MainTex, input.uv);
                float2 pixel = float2(input.uv.x, 1 - input.uv.y) * SOURCE_SIZE;

                float dim = 0;
                if (_Collected < 0.5) { dim = max(dim, Lamp(pixel, float2(255, 951), float2(45, 55), -0.57)); }
                if (_Collected < 1.5) { dim = max(dim, Lamp(pixel, float2(598, 1021), float2(61, 49), 0)); }
                if (_Collected < 2.5) { dim = max(dim, Lamp(pixel, float2(936, 954), float2(45, 57), 0.57)); }

                color.rgb *= lerp(1, _LampDim, dim);
                color.a = tex2D(_MaskTex, input.uv).r;
                return color;
            }
            ENDCG
        }
    }
}
