Shader "YYOU/StudioSurface"
{
 Properties {
  [PerRendererData] _MainTex("Texture",2D)="white"{}
  _Tint("Tint",Color)=(.3,.8,1,1)
  _Mode("Mode",Float)=0
  _Flash("Flash",Range(0,1))=0
  _StencilComp("Stencil Comparison",Float)=8
  _Stencil("Stencil ID",Float)=0
  _StencilOp("Stencil Operation",Float)=0
  _StencilWriteMask("Stencil Write Mask",Float)=255
  _StencilReadMask("Stencil Read Mask",Float)=255
  _ColorMask("Color Mask",Float)=15
 }
 SubShader {
  Tags {"Queue"="Transparent" "RenderType"="Transparent" "IgnoreProjector"="True" "CanUseSpriteAtlas"="True"}
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
   float4 _Tint;float _Mode,_Flash;
   v2f vert(appdata v){v2f o;o.vertex=UnityObjectToClipPos(v.vertex);o.color=v.color;o.uv=v.uv;return o;}
   float noise(float2 p){return frac(sin(dot(p,float2(127.1,311.7)))*43758.5453);}
   float ring(float r,float at,float width,float aa){return 1-smoothstep(width,width+aa,abs(r-at));}
   fixed4 frag(v2f i):SV_Target {
    float2 p=i.uv*2-1;float r=length(p);float aa=max(fwidth(r),.002);
    float3 c;float a=1;
    if(_Mode<.5){
     float2 q=p*float2(1.7,1);float light=exp(-length((q-float2(-.7,-.1))*float2(.8,1.4))*2);
     c=lerp(float3(.035,.045,.095),float3(.105,.155,.235),light*.75);
     c+=float3(.035,.08,.10)*exp(-dot(q-float2(-1.35,-.6),q-float2(-1.35,-.6))*1.6);
     c+=float3(.06,.035,.11)*exp(-dot(q-float2(1.3,-.7),q-float2(1.3,-.7))*2);
     float d=length((q-float2(.05,-.40))*float2(1,1.3));
     c+=float3(.09,.16,.21)*pow(saturate(.5+.5*cos(d*36)),24)*.035*smoothstep(.3,1,d)*(1-smoothstep(1.3,2.8,d));
     c+=(noise(i.uv*float2(2400,1400))-.5)*.006;
     c*=1-.23*pow(saturate(length(p)*.65),2);
    }else if(_Mode<1.5){
     a=1-smoothstep(.98,1,r);
     float light=saturate(.55+p.y*.38-p.x*.17);
     c=lerp(float3(.035,.055,.085),float3(.26,.34,.42),light);
     c=lerp(c,float3(.055,.075,.105),1-smoothstep(.87,.89,r));
     c+=ring(r,.94,.007,aa)*lerp(float3(.2,.3,.4),float3(.40,.50,.57),light);
     float face=1-smoothstep(.79,.81,r);
     float3 tint=_Tint.rgb;
     float3 surface=lerp(tint*.20,tint*.62,light);
     surface+=tint*pow(saturate(1-length(p-float2(-.26,.46))/.9),3)*.20;
     surface+=(noise(i.uv*400)-.5)*.018;
     c=lerp(c,surface,face);
     c+=tint*ring(r,.82,.009,aa)*.95;
     c+=tint*ring(r,.72,.002,aa)*.22;
     c+=tint*pow(saturate(1-r),2)*_Flash*.8;
     c+=ring(r,.805,.005,aa)*saturate(p.y)*.5;
     c=lerp(c,tint*.8,_Flash*.25);
    }else{
     a=1-smoothstep(.99,1,r);
     float ang=atan2(p.y,p.x);
     float shine=pow(abs(cos(ang-.65)),16)*.13;
     c=float3(.025,.032,.044)+shine*float3(.65,.78,1);
     float grooves=(.5+.5*sin(r*700))*.024*smoothstep(.32,.4,r);
     c+=grooves;c+=ring(r,.97,.004,aa)*.22;
     c=lerp(c,float3(.15,.23,.29),1-smoothstep(.27,.28,r));
     c+=ring(r,.24,.003,aa)*.16;
     c=lerp(c,float3(.015,.02,.035),1-smoothstep(.027,.035,r));
    }
    return fixed4(c,a)*i.color;
   }
   ENDCG
  }
 }
}
