using UnityEngine;
using UnityEngine.UI;

namespace YYOU.RhythmPractice
{
    [RequireComponent(typeof(CanvasRenderer))]
    public sealed class PracticeHighway:MaskableGraphic
    {
        public RhythmPracticeView view;
        public float travelSeconds=2.7f;
        static readonly Color[] Palette={new Color(.37f,.93f,1),new Color(.65f,.62f,1),new Color(1,.86f,.43f),new Color(1,.5f,.79f),new Color(.47f,.95f,.86f)};
        Vector2 Point(float laneEdge,float t)
        {
            float perspective=t/(2-t);float width=Mathf.LerpUnclamped(190,1040,perspective);
            return new Vector2((laneEdge/PracticeSession.LaneCount-.5f)*width,Mathf.LerpUnclamped(345,-270,perspective));
        }
        protected override void OnPopulateMesh(VertexHelper vh)
        {
            vh.Clear();
            for(int lane=0;lane<PracticeSession.LaneCount;lane++)
            {
                Quad(vh,Point(lane,0),Point(lane+1,0),Point(lane+1,1),Point(lane,1),new Color(.06f,.08f,.20f,.80f),new Color(.03f,.06f,.14f,.95f));
                float pressed=view!=null && view.Pressed[lane] ? .2f : 0;
                if(pressed>0)Quad(vh,Point(lane,.6f),Point(lane+1,.6f),Point(lane+1,1),Point(lane,1),new Color(.3f,.8f,1,0),new Color(.3f,.8f,1,pressed));
            }
            for(int i=0;i<=PracticeSession.LaneCount;i++)Line(vh,Point(i,0),Point(i,1),1.5f,new Color(.42f,.55f,1,.55f));
            float clock=view!=null && view.Session!=null?view.Session.Time:0;
            for(int j=0;j<9;j++){float t=Mathf.Repeat(j/9f+clock*.13f,1);Line(vh,Point(0,t),Point(PracticeSession.LaneCount,t),1,new Color(.5f,.7f,1,.11f));}
            if(view!=null && view.Session!=null)
            {
                foreach(var n in view.Session.Notes)
                {
                    if(n.state==PracticeNoteState.Hit || n.state==PracticeNoteState.Miss || n.time-clock>travelSeconds)continue;
                    float head=n.state==PracticeNoteState.Holding?1:Mathf.Clamp(1-(n.time-clock)/travelSeconds,0,1.015f);
                    float tail=Mathf.Clamp01(1-(n.time+n.duration-clock)/travelSeconds);
                    Color c=Palette[n.lane];
                    if(n.source=="bass")c=new Color(1,.84f,.35f);
                    else if(n.source=="drums_low")c=new Color(.36f,.91f,1);
                    else if(n.source=="drums_high")c=new Color(1,.5f,.76f);
                    if(n.IsHold)
                    {
                        Color dim=c;dim.a=.20f;Color bright=c;bright.a=n.state==PracticeNoteState.Holding ? .7f : .45f;
                        Quad(vh,Point(n.lane+.12f,tail),Point(n.lane+.88f,tail),Point(n.lane+.88f,head),Point(n.lane+.12f,head),dim,bright);
                        Line(vh,Point(n.lane+.12f,tail),Point(n.lane+.12f,head),1.5f,c);
                        Line(vh,Point(n.lane+.88f,tail),Point(n.lane+.88f,head),1.5f,c);
                        Bar(vh,n.lane,tail,c,7);
                    }
                    Bar(vh,n.lane,head,c,n.IsHold?14:11);
                }
            }
            Line(vh,Point(0,1),Point(PracticeSession.LaneCount,1),14,new Color(.25f,.8f,1,.12f));
            Line(vh,Point(0,1),Point(PracticeSession.LaneCount,1),4,new Color(.55f,.98f,1));
            Line(vh,Point(0,1)+Vector2.down*5,Point(PracticeSession.LaneCount,1)+Vector2.down*5,1,new Color(.85f,.68f,1));
            if(view!=null)for(int lane=0;lane<PracticeSession.LaneCount;lane++)
            {
                float f=view.Flashes[lane];if(f<=0)continue;
                Vector2 center=Point(lane+.5f,1);Color c=Palette[lane];c.a=f;
                Line(vh,center-Vector2.right*86,center+Vector2.right*86,18*f,new Color(c.r,c.g,c.b,.45f*f));
                for(int j=0;j<8;j++){float a=j*Mathf.PI/4;Vector2 p=center+new Vector2(Mathf.Cos(a),Mathf.Sin(a))*(16+(1-f)*65);Box(vh,p,new Vector2(4,7)*f,c);}
            }
        }
        void Bar(VertexHelper vh,int lane,float t,Color c,float height)
        {
            var left=Point(lane+.1f,t);var right=Point(lane+.9f,t);float h=height*Mathf.Lerp(.4f,1,t);
            Box(vh,(left+right)/2,new Vector2(right.x-left.x+8,h+9),new Color(c.r,c.g,c.b,.18f));
            Box(vh,(left+right)/2,new Vector2(right.x-left.x,h),c);
            Line(vh,left+Vector2.up*h*.25f,right+Vector2.up*h*.25f,2,Color.white);
        }
        void Box(VertexHelper vh,Vector2 center,Vector2 size,Color c){Vector2 half=size/2;Quad(vh,center+new Vector2(-half.x,half.y),center+half,center+new Vector2(half.x,-half.y),center-half,c,c);}
        void Line(VertexHelper vh,Vector2 a,Vector2 b,float width,Color c){var d=(b-a).normalized;var n=new Vector2(-d.y,d.x)*width/2;Quad(vh,a+n,b+n,b-n,a-n,c,c);}
        void Quad(VertexHelper vh,Vector2 a,Vector2 b,Vector2 c,Vector2 d,Color top,Color bottom)
        {int n=vh.currentVertCount;vh.AddVert(a,top,Vector2.zero);vh.AddVert(b,top,Vector2.zero);vh.AddVert(c,bottom,Vector2.zero);vh.AddVert(d,bottom,Vector2.zero);vh.AddTriangle(n,n+1,n+2);vh.AddTriangle(n,n+2,n+3);}
    }
}


