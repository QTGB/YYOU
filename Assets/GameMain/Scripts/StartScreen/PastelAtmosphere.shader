Shader "YYOU/PastelAtmosphere" {
Properties{_MainTex("Texture",2D)="white"{}}
SubShader{Tags{"Queue"="Transparent" "RenderType"="Transparent"} Cull Off ZWrite Off ZTest [unity_GUIZTestMode]
Pass{CGPROGRAM
#pragma vertex vert
#pragma fragment frag
#include "UnityCG.cginc"
struct v2f{float4 vertex:SV_POSITION;float4 screen:TEXCOORD0;};
v2f vert(float4 vertex:POSITION){v2f o;o.vertex=UnityObjectToClipPos(vertex);o.screen=ComputeScreenPos(o.vertex);return o;}
float4 frag(v2f i):SV_Target{
float2 uv=i.screen.xy/i.screen.w;float2 p=(uv-.5)*float2(_ScreenParams.x/_ScreenParams.y,1);
float pink=exp(-dot(p-float2(-.65,-.28),p-float2(-.65,-.28))*8);
float blue=exp(-dot(p-float2(-.60,.38),p-float2(-.60,.38))*6);
float violet=exp(-dot(p-float2(.75,-.25),p-float2(.75,-.25))*8);
float3 c=float3(.98,.985,.985)-pink*float3(.01,.14,.06)-blue*float3(.18,.06,.015)-violet*float3(.075,.14,.015);
float ribbon=exp(-abs(p.y-.15*sin(p.x*3)-.20)*18);c=lerp(c,float3(.81,.93,.94),ribbon*.16);
float d=abs(frac((p.x+p.y*.55)*3)-.5);c+=smoothstep(.497,.5,d)*.025;
return float4(c,1);}
ENDCG}}
}
