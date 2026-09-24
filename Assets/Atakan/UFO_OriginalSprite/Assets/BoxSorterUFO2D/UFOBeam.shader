Shader "BoxSorterUFO2D/Beam" {
Properties { _Color("Color",Color)=(.08,.75,1,.5) }
SubShader { Tags { "Queue"="Transparent" "RenderType"="Transparent" } Pass {
Blend SrcAlpha One
ZWrite Off
Cull Off
CGPROGRAM
#pragma vertex vert
#pragma fragment frag
#include "UnityCG.cginc"
struct a {float4 vertex:POSITION;float4 color:COLOR;};
struct v {float4 pos:SV_POSITION;float4 color:COLOR;};float4 _Color;
v vert(a i){v o;o.pos=UnityObjectToClipPos(i.vertex);o.color=i.color*_Color;return o;}
fixed4 frag(v i):SV_Target{return i.color;}
ENDCG
} } }
