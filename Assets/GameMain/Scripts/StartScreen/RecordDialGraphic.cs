using UnityEngine;
using UnityEngine.UI;

namespace YYOU.StartScreen
{
    public sealed class RecordDialGraphic : MaskableGraphic
    {
        protected override void OnPopulateMesh(VertexHelper vh)
        {
            vh.Clear();
            float r=rectTransform.rect.width*.5f;
            Arc(vh,r*.93f,1.5f,0,360,new Color(.42f,.45f,.57f,.18f));
            Arc(vh,r*.83f,1,0,360,new Color(.42f,.45f,.57f,.20f));
            Arc(vh,r*.76f,2,0,360,new Color(.8f,.54f,.22f,.22f));
            for(int i=0;i<120;i++)
            {
                float a=i*3;
                Arc(vh,r*.89f,i%5==0?14:5,a,a+(i%5==0?.35f:.20f),new Color(.61f,.64f,.73f,i%5==0?.34f:.16f));
            }
            for(int i=0;i<64;i++) Arc(vh,r*.965f,3,i*5.625f,i*5.625f+.30f,new Color(.94f,.68f,.34f,.36f));
            GlowArc(vh,r*.80f,22,78,new Color(1,.58f,.17f,.8f));
            GlowArc(vh,r*.80f,202,251,new Color(.25f,.58f,1,.6f));
            GlowArc(vh,r*.94f,290,328,new Color(.67f,.36f,.97f,.5f));
            for(int i=0;i<9;i++) Arc(vh,r*(.44f+i*.026f),.7f,0,360,new Color(.4f,.43f,.5f,.10f));
        }
        void GlowArc(VertexHelper vh,float r,float from,float to,Color c)
        {
            for(int i=5;i>=1;i--)
            {
                Color glow=c; glow.a=.018f*(6-i); Arc(vh,r,i*5,from,to,glow,true);
            }
            Arc(vh,r,1.8f,from,to,c,true);
        }
        static void Arc(VertexHelper vh,float r,float width,float from,float to,Color c,bool fadeEnds=false)
        {
            int steps=Mathf.Max(1,Mathf.CeilToInt((to-from)/2));
            for(int i=0;i<steps;i++)
            {
                float a=Mathf.Lerp(from,to,(float)i/steps)*Mathf.Deg2Rad;
                float b=Mathf.Lerp(from,to,(float)(i+1)/steps)*Mathf.Deg2Rad;
                var x=new Vector2(Mathf.Cos(a),Mathf.Sin(a)); var y=new Vector2(Mathf.Cos(b),Mathf.Sin(b));
                int n=vh.currentVertCount;
                Color ca=c,cb=c;
                if(fadeEnds){ca.a*=Mathf.SmoothStep(0,1,Mathf.Min(i,steps-i)/(steps*.22f)); cb.a*=Mathf.SmoothStep(0,1,Mathf.Min(i+1,steps-i-1)/(steps*.22f));}
                vh.AddVert(x*(r-width*.5f),ca,Vector2.zero); vh.AddVert(x*(r+width*.5f),ca,Vector2.zero);
                vh.AddVert(y*(r+width*.5f),cb,Vector2.zero); vh.AddVert(y*(r-width*.5f),cb,Vector2.zero);
                vh.AddTriangle(n,n+1,n+2); vh.AddTriangle(n,n+2,n+3);
            }
        }
    }
}
