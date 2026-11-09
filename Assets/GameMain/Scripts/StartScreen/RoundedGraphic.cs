using UnityEngine;
using UnityEngine.UI;

namespace YYOU.StartScreen
{
    // Vector UI: no texture dependency; remains sharp at any Canvas scale.
    [AddComponentMenu("YYOU/UI/Rounded Graphic")]
    public sealed class RoundedGraphic : MaskableGraphic
    {
        [Min(0)] public float radius = 30;
        public Color bottomColor = Color.white;
        public bool cutCorners;
        [Range(0, 30)] public float ringWidth;

        protected override void OnPopulateMesh(VertexHelper vh)
        {
            vh.Clear();
            Rect r = rectTransform.rect;
            if(cutCorners)
            {
                float cut=Mathf.Min(16,Mathf.Min(r.width,r.height)*.25f);
                Vector2[] points={new Vector2(r.xMin+cut,r.yMin),new Vector2(r.xMin,r.yMin+cut),new Vector2(r.xMin,r.yMax-cut),new Vector2(r.xMin+cut,r.yMax),new Vector2(r.xMax-cut,r.yMax),new Vector2(r.xMax,r.yMax-cut),new Vector2(r.xMax,r.yMin+cut),new Vector2(r.xMax-cut,r.yMin)};
                for(int i=0;i<8;i++)
                {
                    Add(vh,points[i],r);
                    Vector2 p=points[i];
                    p.x=Mathf.Lerp(p.x,r.center.x,ringWidth/(r.width*.5f));
                    p.y=Mathf.Lerp(p.y,r.center.y,ringWidth/(r.height*.5f));
                    Add(vh,ringWidth>0?p:r.center,r);
                }
                for(int i=0;i<8;i++){int a=i*2,b=((i+1)%8)*2; vh.AddTriangle(a,b,a+1); if(ringWidth>0)vh.AddTriangle(b,b+1,a+1);}
                return;
            }
            float rad = Mathf.Min(radius, Mathf.Min(r.width, r.height) / 2);
            const int steps = 24;
            const int count = 4 * (steps + 1);
            for (int i = 0; i < count; i++)
            {
                int corner = i / (steps + 1);
                float angle = (corner * 90f + i % (steps + 1) * 90f / steps) * Mathf.Deg2Rad;
                Vector2 center = new Vector2(corner == 0 || corner == 3 ? r.xMax-rad : r.xMin+rad,
                    corner < 2 ? r.yMax-rad : r.yMin+rad);
                Vector2 dir = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle));
                Vector2 outer = center + dir * rad;
                Add(vh, outer, r);
                Add(vh, ringWidth > 0 ? center + dir * Mathf.Max(0, rad-ringWidth) : r.center, r);
            }
            for (int i = 0; i < count; i++)
            {
                int a = i*2, b = ((i+1)%count)*2;
                vh.AddTriangle(a, b, a+1);
                if (ringWidth > 0) vh.AddTriangle(b, b+1, a+1);
            }
        }

        void Add(VertexHelper vh, Vector2 p, Rect r)
        {
            var v = UIVertex.simpleVert;
            v.position = p;
            v.color = Color.Lerp(bottomColor, color, Mathf.InverseLerp(r.yMin,r.yMax,p.y));
            vh.AddVert(v);
        }
    }
}
