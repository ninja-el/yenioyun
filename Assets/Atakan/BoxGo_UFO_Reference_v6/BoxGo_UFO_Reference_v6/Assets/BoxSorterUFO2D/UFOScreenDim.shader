Shader "BoxSorterUFO2D/ScreenDim" {
Properties { _Color("Dim",Color)=(0,0,0,0) }
SubShader { Tags { "Queue"="Transparent+5" "RenderType"="Transparent" } Pass {
Blend SrcAlpha OneMinusSrcAlpha
ZWrite Off
ZTest Always
Cull Off
CGPROGRAM
#pragma vertex vert
#pragma fragment frag
#include "UnityCG.cginc"
float4 _Color;
float4 vert(float4 p:POSITION):SV_POSITION{return UnityObjectToClipPos(p);}
fixed4 frag():SV_Target{return _Color;}
ENDCG
} } }
