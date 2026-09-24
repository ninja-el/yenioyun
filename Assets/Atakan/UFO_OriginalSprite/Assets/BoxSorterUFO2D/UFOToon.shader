Shader "BoxSorterUFO2D/Toon" {
Properties { _Color("Color",Color)=(1,1,1,1) _Emission("Emission",Float)=0 }
SubShader { Tags { "RenderType"="Opaque" } Pass {
CGPROGRAM
#pragma vertex vert
#pragma fragment frag
#include "UnityCG.cginc"
struct a {float4 vertex:POSITION;float3 normal:NORMAL;};
struct v {float4 pos:SV_POSITION;float3 n:TEXCOORD0;float3 world:TEXCOORD1;};
float4 _Color;float _Emission;
v vert(a i){v o;o.pos=UnityObjectToClipPos(i.vertex);o.n=UnityObjectToWorldNormal(i.normal);o.world=mul(unity_ObjectToWorld,i.vertex).xyz;return o;}
fixed4 frag(v i):SV_Target {float3 n=normalize(i.n);float3 l=normalize(float3(-.5,.8,.65));float3 eye=normalize(_WorldSpaceCameraPos-i.world);float d=saturate(dot(n,l));float spec=pow(saturate(dot(n,normalize(l+eye))),55);float rim=pow(1-saturate(dot(n,eye)),3);return float4(_Color.rgb*(.36+.64*d+_Emission)+spec*.7+rim*.1,1);}
ENDCG
} } }
