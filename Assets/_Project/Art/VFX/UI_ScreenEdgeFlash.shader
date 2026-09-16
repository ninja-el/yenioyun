Shader "MatchPack/UI/ScreenEdgeFlash"
{
    Properties
    {
        [PerRendererData] _MainTex ("Sprite Texture", 2D) = "white" {}
        _Color ("Tint", Color) = (1, 1, 1, 1)
        _FlashColor ("Flash Color", Color) = (0.62, 0.02, 0.02, 1)
        _Intensity ("Intensity", Range(0, 1)) = 0
        _Thickness ("Edge Thickness", Range(0.01, 0.5)) = 0.16
        _EdgePower ("Edge Power", Range(0.5, 8)) = 3
        _Aspect ("Width / Height", Float) = 0.5625

        _StencilComp ("Stencil Comparison", Float) = 8
        _Stencil ("Stencil ID", Float) = 0
        _StencilOp ("Stencil Operation", Float) = 0
        _StencilWriteMask ("Stencil Write Mask", Float) = 255
        _StencilReadMask ("Stencil Read Mask", Float) = 255
        _ColorMask ("Color Mask", Float) = 15
    }

    SubShader
    {
        Tags
        {
            "Queue" = "Transparent"
            "IgnoreProjector" = "True"
            "RenderType" = "Transparent"
            "PreviewType" = "Plane"
            "CanUseSpriteAtlas" = "True"
        }

        Stencil
        {
            Ref [_Stencil]
            Comp [_StencilComp]
            Pass [_StencilOp]
            ReadMask [_StencilReadMask]
            WriteMask [_StencilWriteMask]
        }

        Cull Off
        Lighting Off
        ZWrite Off
        ZTest [unity_GUIZTestMode]
        Blend SrcAlpha OneMinusSrcAlpha
        ColorMask [_ColorMask]

        Pass
        {
            Name "Default"
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma target 2.0

            #include "UnityCG.cginc"

            struct appdata_t
            {
                float4 vertex : POSITION;
                float4 color : COLOR;
                float2 texcoord : TEXCOORD0;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct v2f
            {
                float4 vertex : SV_POSITION;
                fixed4 color : COLOR;
                float2 texcoord : TEXCOORD0;
                UNITY_VERTEX_OUTPUT_STEREO
            };

            fixed4 _Color;
            fixed4 _FlashColor;
            float _Intensity;
            float _Thickness;
            float _EdgePower;
            float _Aspect;

            v2f vert (appdata_t v)
            {
                v2f o;
                UNITY_SETUP_INSTANCE_ID(v);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.texcoord = v.texcoord;
                o.color = v.color * _Color;
                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                // Merkezden kenara uzaklık, her eksende ayrı: 0 = merkez, 1 = kenar.
                float2 distance = abs(i.texcoord - 0.5) * 2.0;

                // Bant kalınlığı ekran genişliğinin oranı olarak verilir; dikey kalınlık en-boy
                // oranıyla küçültülür ki dört kenarda da piksel kalınlığı eşit olsun.
                float2 band = float2(_Thickness, _Thickness * _Aspect);
                band = max(band, 0.0001);

                float2 edges = saturate((distance - (1.0 - band)) / band);
                float edge = max(edges.x, edges.y);
                float border = pow(edge, _EdgePower);

                fixed4 color = _FlashColor;
                color.a = border * _Intensity * _FlashColor.a * i.color.a;
                color.rgb *= i.color.rgb;

                return color;
            }
            ENDCG
        }
    }
}
