Shader "YYOU/PastelGlassPad"
{
 Properties {
  [PerRendererData] _MainTex("Texture",2D)="white"{}
  _Tint("Tint",Color)=(.3,.8,1,1)
  _Flash("Flash",Range(0,1))=0
  _StencilComp("Stencil Comparison",Float)=8
  _Stencil("Stencil ID",Float)=0
  _StencilOp("Stencil Operation",Float)=0
  _StencilWriteMask("Stencil Write Mask",Float)=255
  _StencilReadMask("Stencil Read Mask",Float)=255
  _ColorMask("Color Mask",Float)=15
 }
 SubShader {
  Tags {"Queue"="Transparent" "RenderType"="Transparent" "IgnoreProjector"="True"}
  Stencil {Ref [_Stencil] Comp [_StencilComp] Pass [_StencilOp] ReadMask [_StencilReadMask] WriteMask [_StencilWriteMask]}
  Cull Off Lighting Off ZWrite Off ZTest [unity_GUIZTestMode]
  Blend SrcAlpha OneMinusSrcAlpha ColorMask [_ColorMask]
  Pass {
   CGPROGRAM
   #pragma vertex vert
   #pragma fragment frag
   #pragma target 3.0
   #include "UnityCG.cginc"
   struct appdata {float4 vertex:POSITION;float4 color:COLOR;float2 uv:TEXCOORD0;};
   struct v2f {float4 vertex:SV_POSITION;float4 color:COLOR;float2 uv:TEXCOORD0;};
   float4 _Tint;float _Flash;
   v2f vert(appdata v){v2f o;o.vertex=UnityObjectToClipPos(v.vertex);o.color=v.color;o.uv=v.uv;return o;}
   float ring(float r,float at,float width,float aa){return 1-smoothstep(width,width+aa,abs(r-at));}
   fixed4 frag(v2f i):SV_Target {
    float2 p=i.uv*2-1;float r=length(p);float aa=max(fwidth(r),.002);
    float light=saturate(.58+p.y*.30-p.x*.16);
    float3 c=lerp(float3(.80,.88,.93),float3(.995,1,1),light);
    c=lerp(c,lerp(_Tint.rgb*.82,_Tint.rgb,light),1-smoothstep(.85,.87,r));
    float sheen=pow(saturate(1-length((p-float2(-.28,.48))*float2(.9,1.4))),2);
    c=lerp(c,float3(.98,1,1),sheen*.38*(1-smoothstep(.84,.86,r)));
    c+=ring(r,.89,.007,aa)*float3(.12,.15,.15);
    c=lerp(c,1,ring(r,.78,.002,aa)*.24);
    c=lerp(c,1,_Flash*.25*(1-smoothstep(.85,.87,r)));
    c=lerp(c,1,ring(r,.88,.006+_Flash*.015,aa)*_Flash);
    float a=1-smoothstep(.98,1,r);
    return fixed4(c,a)*i.color;
   }
   ENDCG
  }
 }
}
