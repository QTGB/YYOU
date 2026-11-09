using UnityEngine;
using UnityEngine.UI;
namespace YYOU.SongPacks
{
    // Original pack artwork, independent of all song covers. Text is separate native UI.
    [RequireComponent(typeof(CanvasRenderer))]
    public sealed class PackArtworkGraphic:MaskableGraphic
    {
        public int variant;public Color accent=new Color(.35f,.75f,.83f);
        Rect r;
        Vector2 P(float x,float y)=>new Vector2(r.xMin+x*r.width,r.yMin+y*r.height);
        Color A(Color c,float a){c.a=a*color.a;return c;}
        Color Mix(float amount)=>Color.Lerp(Color.white,accent,amount);
        protected override void OnPopulateMesh(VertexHelper vh)
        {
            vh.Clear();r=rectTransform.rect;if(r.width<=0 || r.height<=0)return;
            Quad(vh,P(0,0),P(0,1),P(1,1),P(1,0),Mix(.26f),Mix(.08f));
            Circle(vh,.83f,.86f,.34f,A(Color.white,.50f));
            if(variant==0)
            {
                for(int x=0;x<9;x++)for(int y=0;y<7;y++)Circle(vh,.12f+x*.095f,.32f+y*.08f,.0025f,A(accent,.28f));
                Ring(vh,.52f,.60f,.25f,.007f,A(accent,.32f));Ring(vh,.52f,.60f,.31f,.003f,A(accent,.25f));
                for(int i=0;i<16;i++){float x=.20f+i*.042f,h=.08f+.30f*Mathf.Pow(Mathf.Abs(Mathf.Sin(i*.47f)),2);Pill(vh,x,.60f,h,.020f,A(i%3==0?new Color(.55f,.55f,.83f):accent,.82f));}
                Star(vh,.78f,.78f,.075f,A(Color.white,.96f));
            }
            else if(variant==1)
            {
                Circle(vh,.60f,.61f,.28f,A(new Color(.38f,.43f,.70f),.74f));
                for(int i=0;i<9;i++)Ring(vh,.60f,.61f,.12f+i*.018f,.0025f,A(Color.white,.30f));
                Circle(vh,.60f,.61f,.085f,Mix(.12f));Circle(vh,.60f,.61f,.017f,A(accent,.85f));
                Quad(vh,P(.04f,.50f),P(.04f,.62f),P(.94f,.78f),P(.94f,.66f),A(Color.white,.80f),A(Color.white,.70f));
                for(int i=0;i<5;i++)Pill(vh,.21f+i*.065f,.43f,.12f+i*.025f,.022f,A(accent,.70f));
            }
            else if(variant==2)
            {
                Circle(vh,.72f,.73f,.14f,A(Color.white,.93f));Ring(vh,.72f,.73f,.19f,.003f,A(accent,.35f));
                Ribbon(vh,.54f,.10f,.07f,0,A(accent,.68f));
                Ribbon(vh,.49f,.13f,.055f,1.4f,A(new Color(.59f,.59f,.80f),.44f));
                Ribbon(vh,.43f,.09f,.05f,2.5f,A(Color.white,.84f));
                for(int i=0;i<5;i++)Pill(vh,.18f+i*.035f,.72f,.12f-i*.015f,.007f,A(accent,.50f));
            }
            else
            {
                Ring(vh,.54f,.61f,.29f,.006f,A(accent,.40f));
                Star(vh,.54f,.62f,.24f,A(Color.white,.95f));Star(vh,.54f,.62f,.13f,A(new Color(.59f,.61f,.85f),.60f));
                Star(vh,.82f,.80f,.055f,A(accent,.8f));Star(vh,.19f,.48f,.045f,A(accent,.6f));
                for(int i=0;i<7;i++){float x=.17f+i*.10f;Quad(vh,P(x,.83f),P(x+.04f,.89f),P(x+.08f,.87f),P(x+.04f,.81f),A(Color.white,.68f),A(Color.white,.68f));}
            }
            Quad(vh,P(0,0),P(0,.31f),P(1,.31f),P(1,0),A(Color.white,.30f),A(Color.white,0));
        }
        void Ribbon(VertexHelper vh,float centre,float amplitude,float width,float phase,Color c){for(int i=0;i<70;i++){float x=i/70f,n=(i+1)/70f,y=centre+Mathf.Sin(x*5+phase)*amplitude,ny=centre+Mathf.Sin(n*5+phase)*amplitude;Quad(vh,P(x,y),P(x,y+width),P(n,ny+width),P(n,ny),c,c);}}
        void Pill(VertexHelper vh,float x,float y,float h,float w,Color c){Quad(vh,P(x-w*.5f,y-h*.5f),P(x-w*.5f,y+h*.5f),P(x+w*.5f,y+h*.5f),P(x+w*.5f,y-h*.5f),c,c);Circle(vh,x,y-h*.5f,w*.5f,c);Circle(vh,x,y+h*.5f,w*.5f,c);}
        void Circle(VertexHelper vh,float x,float y,float radius,Color c){int n=vh.currentVertCount;vh.AddVert(P(x,y),c,Vector2.zero);for(int i=0;i<=64;i++){float a=i*Mathf.PI/32;vh.AddVert(P(x+Mathf.Cos(a)*radius,y+Mathf.Sin(a)*radius),c,Vector2.zero);if(i>0)vh.AddTriangle(n,n+i,n+i+1);}}
        void Ring(VertexHelper vh,float x,float y,float radius,float width,Color c){for(int i=0;i<80;i++){float a=i*Mathf.PI/40,b=(i+1)*Mathf.PI/40;Quad(vh,P(x+Mathf.Cos(a)*radius,y+Mathf.Sin(a)*radius),P(x+Mathf.Cos(a)*(radius-width),y+Mathf.Sin(a)*(radius-width)),P(x+Mathf.Cos(b)*(radius-width),y+Mathf.Sin(b)*(radius-width)),P(x+Mathf.Cos(b)*radius,y+Mathf.Sin(b)*radius),c,c);}}
        void Star(VertexHelper vh,float x,float y,float radius,Color c){int n=vh.currentVertCount;vh.AddVert(P(x,y),c,Vector2.zero);for(int i=0;i<=8;i++){float a=i*Mathf.PI/4,rr=i%2==0?radius:radius*.28f;vh.AddVert(P(x+Mathf.Cos(a)*rr,y+Mathf.Sin(a)*rr),c,Vector2.zero);if(i>0)vh.AddTriangle(n,n+i,n+i+1);}}
        static void Quad(VertexHelper vh,Vector2 a,Vector2 b,Vector2 c,Vector2 d,Color bottom,Color top){int n=vh.currentVertCount;vh.AddVert(a,bottom,Vector2.zero);vh.AddVert(b,top,Vector2.zero);vh.AddVert(c,top,Vector2.zero);vh.AddVert(d,bottom,Vector2.zero);vh.AddTriangle(n,n+1,n+2);vh.AddTriangle(n,n+2,n+3);}
    }
}
