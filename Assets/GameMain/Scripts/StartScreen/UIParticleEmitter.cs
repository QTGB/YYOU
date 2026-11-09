using UnityEngine;
using UnityEngine.UI;

namespace YYOU.StartScreen
{
    // The ParticleSystem owns emission, lifetime, velocity, size and color.
    // Canvas draws its live particles so they work with Screen Space Overlay UI.
    [RequireComponent(typeof(CanvasRenderer), typeof(ParticleSystem))]
    public sealed class UIParticleEmitter : MaskableGraphic
    {
        ParticleSystem source;
        readonly ParticleSystem.Particle[] particles = new ParticleSystem.Particle[64];
        protected override void Awake()
        {
            base.Awake();
            source=GetComponent<ParticleSystem>();
            raycastTarget=false;
        }
        void LateUpdate() { SetVerticesDirty(); }
        protected override void OnPopulateMesh(VertexHelper vh)
        {
            vh.Clear();
            if(!source) source=GetComponent<ParticleSystem>();
            int count=source.GetParticles(particles);
            for(int i=0;i<count;i++)
            {
                var p=particles[i];
                float s=p.GetCurrentSize(source);
                Color32 c=p.GetCurrentColor(source);
                float a=p.rotation*Mathf.Deg2Rad;
                var right=new Vector2(Mathf.Cos(a),Mathf.Sin(a));
                var up=new Vector2(-right.y,right.x);
                Quad(vh,p.position,right,up,s*.5f,s*.17f,c);
                Quad(vh,p.position,right,up,s*.17f,s*.5f,c);
            }
        }
        static void Quad(VertexHelper vh,Vector2 center,Vector2 x,Vector2 y,float w,float h,Color32 c)
        {
            int n=vh.currentVertCount;
            vh.AddVert(center-x*w-y*h,c,Vector2.zero);
            vh.AddVert(center-x*w+y*h,c,Vector2.zero);
            vh.AddVert(center+x*w+y*h,c,Vector2.zero);
            vh.AddVert(center+x*w-y*h,c,Vector2.zero);
            vh.AddTriangle(n,n+1,n+2); vh.AddTriangle(n,n+2,n+3);
        }
    }
}
