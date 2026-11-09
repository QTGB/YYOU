using UnityEngine;
using UnityEngine.UI;
namespace YYOU.Settings
{
    [RequireComponent(typeof(CanvasRenderer))]
    public sealed class PadLayoutPreviewGraphic:MaskableGraphic
    {
        [Range(0,2)]public int layout;
        protected override void OnPopulateMesh(VertexHelper vh)
        {
            vh.Clear();var r=rectTransform.rect;float lo=1000,hi=-1000;
            for(int i=0;i<4;i++){float y=PadLayouts.Position(layout,i).y;lo=Mathf.Min(lo,y);hi=Mathf.Max(hi,y);}
            for(int i=0;i<4;i++){
                var p=PadLayouts.Position(layout,i);p=new Vector2(r.center.x+p.x/740*r.width*.5f,r.center.y+(p.y-(lo+hi)*.5f)/260*r.height);
                var tint=i%2==0?new Color(.31f,.71f,.84f):new Color(.35f,.76f,.64f);
                float radius=Mathf.Min(8,r.height*.14f);
                Circle(vh,p+Vector2.down*1.5f,radius+1,new Color(.35f,.53f,.69f,.13f));
                Circle(vh,p,radius+1,Color.white);Circle(vh,p,radius,tint);
                Circle(vh,p+new Vector2(-radius*.25f,radius*.3f),radius*.18f,new Color(1,1,1,.7f));
            }
        }
        static void Circle(VertexHelper vh,Vector2 p,float radius,Color c){int n=vh.currentVertCount;vh.AddVert(p,c,Vector2.zero);for(int i=0;i<=32;i++){float a=i*Mathf.PI/16;vh.AddVert(p+new Vector2(Mathf.Cos(a),Mathf.Sin(a))*radius,c,Vector2.zero);if(i>0)vh.AddTriangle(n,n+i,n+i+1);}}
    }
}
