using UnityEngine;
using UnityEngine.UI;
namespace YYOU.SongPacks
{
    public enum PackGlyphKind{Play,Pause,Back,Next,Check}
    [RequireComponent(typeof(CanvasRenderer))]
    public sealed class PackGlyph:MaskableGraphic
    {
        public PackGlyphKind kind;
        public void Set(PackGlyphKind value){if(kind==value)return;kind=value;SetVerticesDirty();}
        protected override void OnPopulateMesh(VertexHelper vh)
        {
            vh.Clear();float s=Mathf.Min(rectTransform.rect.width,rectTransform.rect.height)/30f;
            if(kind==PackGlyphKind.Play){Triangle(vh,new Vector2(-6,-10)*s,new Vector2(-6,10)*s,new Vector2(10,0)*s);return;}
            if(kind==PackGlyphKind.Pause){Bar(vh,new Vector2(-5,0)*s,4*s,18*s,0);Bar(vh,new Vector2(5,0)*s,4*s,18*s,0);return;}
            if(kind==PackGlyphKind.Check){Bar(vh,new Vector2(-5,-2)*s,4*s,11*s,45);Bar(vh,new Vector2(4,1)*s,4*s,23*s,-40);return;}
            float d=kind==PackGlyphKind.Back?-1:1;Bar(vh,new Vector2(0,5)*s,3*s,15*s,45*d);Bar(vh,new Vector2(0,-5)*s,3*s,15*s,-45*d);
        }
        void Triangle(VertexHelper vh,Vector2 a,Vector2 b,Vector2 c){int n=vh.currentVertCount;vh.AddVert(a,color,Vector2.zero);vh.AddVert(b,color,Vector2.zero);vh.AddVert(c,color,Vector2.zero);vh.AddTriangle(n,n+1,n+2);}
        void Bar(VertexHelper vh,Vector2 p,float w,float h,float degrees){float a=degrees*Mathf.Deg2Rad;var x=new Vector2(Mathf.Cos(a),Mathf.Sin(a))*w*.5f;var y=new Vector2(-Mathf.Sin(a),Mathf.Cos(a))*h*.5f;int n=vh.currentVertCount;vh.AddVert(p-x-y,color,Vector2.zero);vh.AddVert(p-x+y,color,Vector2.zero);vh.AddVert(p+x+y,color,Vector2.zero);vh.AddVert(p+x-y,color,Vector2.zero);vh.AddTriangle(n,n+1,n+2);vh.AddTriangle(n,n+2,n+3);}
    }
}
