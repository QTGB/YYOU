using UnityEngine;
using UnityEngine.UI;
namespace YYOU.DrumPractice
{
    [RequireComponent(typeof(CanvasRenderer))]
    public sealed class DrumRailGraphic:MaskableGraphic
    {
        public DrumPracticeView view;
        public float travelSeconds=3;
        public const float JudgeX=-555,SpawnX=725;
        static readonly Color Blue=new Color(.35f,.81f,.97f),Green=new Color(.31f,.92f,.65f);
        public float CurrentJudgeX=>rectTransform.rect.xMin+Mathf.Min(90,rectTransform.rect.width*.2f);
        public float CurrentSpawnX=>rectTransform.rect.xMax+20;
        public float NoteVisualScale=>Mathf.Clamp((CurrentSpawnX-CurrentJudgeX)/1380f,1,1.18f);
        public float SmallNoteRadius=>43*NoteVisualScale;
        protected override void OnPopulateMesh(VertexHelper vh)
        {
            vh.Clear();
            var bounds=rectTransform.rect;float width=bounds.width;float judge=CurrentJudgeX;float spawn=CurrentSpawnX;
            Box(vh,new Vector2((judge+spawn)*.5f,0),new Vector2(spawn-judge,1),new Color(.60f,.72f,.82f,.20f));
            Circle(vh,new Vector2(judge,-2),49,new Color(.46f,.65f,.76f,.10f));
            Circle(vh,new Vector2(judge,0),49,new Color(1,1,1,.62f));
            Ring(vh,new Vector2(judge,0),49,2,new Color(.55f,.76f,.86f,.40f));
            Ring(vh,new Vector2(judge,0),42,1,new Color(.65f,.81f,.89f,.42f));
            Ring(vh,new Vector2(judge,0),54,1,new Color(1,1,1,.75f));
            if(view==null||view.Session==null)return;
            if(view.JudgeFlash>0 && view.JudgeFailed)
            {
                var fx=view.JudgeFailed?new Color(1,.25f,.32f,view.JudgeFlash):new Color(.22f,.75f,1,view.JudgeFlash);
                Circle(vh,new Vector2(judge,0),53,new Color(fx.r,fx.g,fx.b,view.JudgeFlash*.22f));
                Ring(vh,new Vector2(judge,0),48+(1-view.JudgeFlash)*20,3,fx);
            }
            if(view.ImpactFlash>0)
            {
                float flash=view.ImpactFlash,punch=flash*flash*view.ImpactStrength,expand=1-flash*flash;var tint=view.ImpactTint;
                Circle(vh,new Vector2(judge,0),46-4*punch,new Color(1,1,1,Mathf.Clamp01(.75f*punch)));
                Ring(vh,new Vector2(judge,0),48+expand*17,1.5f+2.5f*flash,new Color(tint.r,tint.g,tint.b,flash*.85f*view.ImpactStrength));
                Ring(vh,new Vector2(judge,0),43-3*punch,1.5f,new Color(tint.r,tint.g,tint.b,flash*.55f));
            }
            int first=view.Session.FirstUnresolved,last=first;
            while(last<view.Session.Notes.Count && view.Session.Notes[last].time<=view.Session.Time+travelSeconds*1.15f)last++;
            // Draw future notes first: the next note to judge stays in front of an overlapping run.
            float scale=NoteVisualScale;
            for(int noteIndex=last-1;noteIndex>=first;noteIndex--)
            {
                var n=view.Session.Notes[noteIndex];
                if(n.state==DrumNoteState.Hit || n.state==DrumNoteState.Miss)continue;
                float x=judge+(n.time-view.Session.Time)/travelSeconds*(spawn-judge);
                if(n.state==DrumNoteState.Rolling)x=judge;
                if(x>spawn+80||x<bounds.xMin-80)continue;
                Color c=n.Color==0?Blue:Green;
                if(n.kind==DrumNoteKind.Roll)
                {
                    float end=judge+(n.time+n.duration-view.Session.Time)/travelSeconds*(spawn-judge);
                    end=Mathf.Clamp(end,judge,spawn+40);float start=Mathf.Max(x,judge-60);
                    Box(vh,new Vector2((start+end)/2,0),new Vector2(Mathf.Max(0,end-start),36),new Color(c.r,c.g,c.b,.55f));
                    Box(vh,new Vector2((start+end)/2,15),new Vector2(Mathf.Max(0,end-start),3),new Color(1,1,1,.7f));
                    Circle(vh,new Vector2(end,0),18,new Color(c.r,c.g,c.b,.65f));
                }
                float radius=(n.IsBig?54:43)*scale;
                Circle(vh,new Vector2(x,-3),radius+5*scale,new Color(.36f,.48f,.60f,.16f));
                Circle(vh,new Vector2(x,0),radius+3*scale,new Color(1,1,1,.90f));
                Sphere(vh,new Vector2(x,0),radius,c);
                Ring(vh,new Vector2(x,0),radius,1.4f*scale,new Color(.86f,.97f,1,.9f));
                Ring(vh,new Vector2(x,0),radius-5*scale,n.IsBig?2:1,new Color(.8f,1,1,.23f));
                if(n.IsBig){for(int i=0;i<2;i++)Circle(vh,new Vector2(x+(i==0?-12:12)*scale,0),5*scale,new Color(.93f,1,1,n.state==DrumNoteState.PairPending && n.firstSide==i?1:.9f));}
                else if(n.kind==DrumNoteKind.Roll){for(int i=-1;i<=1;i++)Circle(vh,new Vector2(x+i*10,0),2.5f,Color.white);}
                else Circle(vh,new Vector2(x,0),3,new Color(.92f,1,1,.8f));
            }
        }
        static void Sphere(VertexHelper vh,Vector2 center,float radius,Color tint)
        {
            int start=vh.currentVertCount;
            vh.AddVert(center+new Vector2(-radius*.15f,radius*.25f),Color.Lerp(tint,Color.white,.25f),Vector2.zero);
            for(int i=0;i<=48;i++){
                float a=i*Mathf.PI/24;float light=.90f+Mathf.Sin(a)*.08f-Mathf.Cos(a)*.02f;
                var c=new Color(tint.r*light,tint.g*light,tint.b*light,1);
                vh.AddVert(center+new Vector2(Mathf.Cos(a),Mathf.Sin(a))*radius,c,Vector2.zero);
                if(i>0)vh.AddTriangle(start,start+i,start+i+1);
            }
        }
        static void Circle(VertexHelper vh,Vector2 center,float radius,Color c)
        {int start=vh.currentVertCount;vh.AddVert(center,c,Vector2.zero);for(int i=0;i<=40;i++){float a=i*Mathf.PI/20;vh.AddVert(center+new Vector2(Mathf.Cos(a),Mathf.Sin(a))*radius,c,Vector2.zero);if(i>0)vh.AddTriangle(start,start+i,start+i+1);}}
        static void Ring(VertexHelper vh,Vector2 center,float radius,float width,Color c)
        {for(int i=0;i<64;i++){float a=i*Mathf.PI/32,b=(i+1)*Mathf.PI/32;var u=new Vector2(Mathf.Cos(a),Mathf.Sin(a));var v=new Vector2(Mathf.Cos(b),Mathf.Sin(b));Quad(vh,center+u*(radius-width),center+u*radius,center+v*radius,center+v*(radius-width),c);}}
        static void Box(VertexHelper vh,Vector2 p,Vector2 size,Color c){var s=size*.5f;Quad(vh,p+new Vector2(-s.x,s.y),p+s,p+new Vector2(s.x,-s.y),p-s,c);}
        static void Line(VertexHelper vh,Vector2 a,Vector2 b,float width,Color c){var n=(b-a).normalized;var p=new Vector2(-n.y,n.x)*width*.5f;Quad(vh,a+p,b+p,b-p,a-p,c);}
        static void Quad(VertexHelper vh,Vector2 a,Vector2 b,Vector2 c,Vector2 d,Color color){int n=vh.currentVertCount;vh.AddVert(a,color,Vector2.zero);vh.AddVert(b,color,Vector2.zero);vh.AddVert(c,color,Vector2.zero);vh.AddVert(d,color,Vector2.zero);vh.AddTriangle(n,n+1,n+2);vh.AddTriangle(n,n+2,n+3);}
    }
}
