Shader "BoxGo/SoftParticle"
{
 Properties { _Shape("Shape: Dot 0 Star 1 Solid 2", Float)=0 }
 SubShader
 {
  Tags { "Queue"="Transparent" "RenderType"="Transparent" "IgnoreProjector"="True" }
  Pass
  {
   Blend SrcAlpha OneMinusSrcAlpha
   ZWrite Off
   Cull Off
   CGPROGRAM
   #pragma vertex vert
   #pragma fragment frag
   #include "UnityCG.cginc"
   float _Shape;
   struct appdata { float4 vertex:POSITION; float4 color:COLOR; float2 uv:TEXCOORD0; };
   struct v2f { float4 pos:SV_POSITION; float4 color:COLOR; float2 uv:TEXCOORD0; };
   v2f vert(appdata v) { v2f o; o.pos=UnityObjectToClipPos(v.vertex); o.color=v.color; o.uv=v.uv; return o; }
   fixed4 frag(v2f i):SV_Target
   {
    float2 p=abs(i.uv*2-1);
    float a=1;
    if(_Shape<0.5) a=pow(saturate(1-length(p)),1.6);
    else if(_Shape<1.5) {
     float d=pow(p.x,0.55)+pow(p.y,0.55);
     a=1-smoothstep(0.85,1.04,d);
    }
    return fixed4(i.color.rgb,i.color.a*a);
   }
   ENDCG
  }
 }
}
