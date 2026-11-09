Shader "YYOU/Atmosphere"
{
Properties { _MainTex("Texture",2D)="white"{} }
SubShader {
Tags {"Queue"="Transparent" "RenderType"="Transparent" "IgnoreProjector"="True"}
Cull Off ZWrite Off ZTest [unity_GUIZTestMode] Blend SrcAlpha OneMinusSrcAlpha
Pass {
CGPROGRAM
#pragma vertex vert
#pragma fragment frag
#include "UnityCG.cginc"
struct appdata {float4 vertex:POSITION;};
struct v2f {float4 vertex:SV_POSITION; float4 screen:TEXCOORD0;};
v2f vert(appdata v){v2f o;o.vertex=UnityObjectToClipPos(v.vertex);o.screen=ComputeScreenPos(o.vertex);return o;}
float hash(float2 p){return frac(sin(dot(p,float2(127.1,311.7)))*43758.5453);}
float noise(float2 p){float2 i=floor(p),f=frac(p);f=f*f*(3-2*f);return lerp(lerp(hash(i),hash(i+float2(1,0)),f.x),lerp(hash(i+float2(0,1)),hash(i+1),f.x),f.y);}
float fog(float2 p){return noise(p)*.5+noise(p*2.03)*.25+noise(p*4.07)*.125+noise(p*8.13)*.0625;}
float4 frag(v2f i):SV_Target {
 float2 uv=i.screen.xy/i.screen.w;
 float2 p=(uv-.5)*float2(_ScreenParams.x/_ScreenParams.y,1);
 float n=fog(p*5+4);
 float warm=exp(-length((p-float2(.7,.25))*float2(1.5,2.5))*2.2);
 float cool=exp(-length((p-float2(-.65,-.15))*float2(1.7,2.8))*2);
 float3 c=float3(.026,.035,.065)+n*.035;
 c+=float3(.60,.26,.065)*warm*(.25+n*.7);
 c+=float3(.06,.21,.45)*cool*(.25+n*.8);
 float d=p.x-p.y*.7;
 float panels=step(.48,frac(d*.85+.2));
 c+=panels*float3(.025,.027,.040);
 float beam=exp(-abs(d-.73)*120)+.45*exp(-abs(d-.79)*190);
 c+=float3(.8,.5,.19)*beam*(.1+.5*uv.y);
 float blue=exp(-abs(d+.86)*130); c+=float3(.15,.36,.65)*blue*.35;
 float2 grid=frac(uv*_ScreenParams.xy/7)-.5;
 float dots=1-smoothstep(.08,.20,length(grid));
 c+=dots*.018*smoothstep(.4,1,abs(p.x));
 c+=(hash(uv*_ScreenParams.xy)-.5)*.013;
 c*=1-.42*smoothstep(.25,1.2,length(p));
 return float4(c,1);
}
ENDCG
}
}
}
