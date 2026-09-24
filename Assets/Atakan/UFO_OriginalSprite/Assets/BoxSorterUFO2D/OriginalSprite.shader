Shader "BoxSorterUFO2D/OriginalSprite" {
Properties {
 _MainTex("Original JPEG",2D)="white"{}
 _MaskTex("Silhouette mask",2D)="white"{}
 _Collected("Collected Objects",Range(0,3))=0
}
SubShader { Tags {"Queue"="Transparent+10" "RenderType"="Transparent"} Pass {
Blend SrcAlpha OneMinusSrcAlpha
ZWrite Off
Cull Off
CGPROGRAM
#pragma vertex vert
#pragma fragment frag
#include "UnityCG.cginc"
sampler2D _MainTex;sampler2D _MaskTex;float _Collected;
struct a {float4 vertex:POSITION;float2 uv:TEXCOORD0;};
struct v {float4 pos:SV_POSITION;float2 uv:TEXCOORD0;};
v vert(a i){v o;o.pos=UnityObjectToClipPos(i.vertex);o.uv=i.uv;return o;}
float lens(float2 p,float2 center,float2 radius,float angle){float2 q=p-center;float sn=sin(angle),cs=cos(angle);q=float2(cs*q.x+sn*q.y,-sn*q.x+cs*q.y)/radius;return 1-smoothstep(.88,1.04,length(q));}
fixed4 frag(v i):SV_Target {
 float4 c=tex2D(_MainTex,i.uv);
 // Source pixels remain unchanged. A separate silhouette mask hides only the background.
 float alpha=tex2D(_MaskTex,i.uv).r;
 float2 pixel=float2(i.uv.x*1206,(1-i.uv.y)*1503);
 float dim=0;
 if(_Collected<.5)dim=max(dim,lens(pixel,float2(255,951),float2(45,55),-.57));
 if(_Collected<1.5)dim=max(dim,lens(pixel,float2(598,1021),float2(61,49),0));
 if(_Collected<2.5)dim=max(dim,lens(pixel,float2(936,954),float2(45,57),.57));
 c.rgb*=lerp(1,.16,dim);c.a=alpha;return c;
}
ENDCG
} } }
