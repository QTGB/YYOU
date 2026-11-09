using UnityEngine;
using UnityEngine.UI;

namespace YYOU.StartScreen
{
    public sealed class UtilityIconGraphic : MaskableGraphic
    {
        public bool exitIcon;
        protected override void OnPopulateMesh(VertexHelper vh)
        {
            vh.Clear();
            if(exitIcon)
            {
                Bar(vh,new Vector2(-9,0),3,22,0);
                Bar(vh,new Vector2(-4,10),12,3,0); Bar(vh,new Vector2(-4,-10),12,3,0);
                Bar(vh,new Vector2(4,0),18,3,0);
                Bar(vh,new Vector2(10,3),10,3,-45); Bar(vh,new Vector2(10,-3),10,3,45);
                return;
            }
            for(int i=0;i<40;i++)
            {
                float a=i*Mathf.PI*2/40,b=(i+1)*Mathf.PI*2/40;
                int n=vh.currentVertCount;
                vh.AddVert(new Vector2(Mathf.Cos(a),Mathf.Sin(a))*6,color,Vector2.zero);
                vh.AddVert(new Vector2(Mathf.Cos(a),Mathf.Sin(a))*10,color,Vector2.zero);
                vh.AddVert(new Vector2(Mathf.Cos(b),Mathf.Sin(b))*10,color,Vector2.zero);
                vh.AddVert(new Vector2(Mathf.Cos(b),Mathf.Sin(b))*6,color,Vector2.zero);
                vh.AddTriangle(n,n+1,n+2); vh.AddTriangle(n,n+2,n+3);
            }
            for(int i=0;i<8;i++)
            {
                float a=i*45*Mathf.Deg2Rad;
                Bar(vh,new Vector2(Mathf.Cos(a),Mathf.Sin(a))*11,6,5,i*45);
            }
        }
        void Bar(VertexHelper vh,Vector2 center,float width,float height,float degrees)
        {
            float a=degrees*Mathf.Deg2Rad;
            Vector2 x=new Vector2(Mathf.Cos(a),Mathf.Sin(a))*width*.5f;
            Vector2 y=new Vector2(-Mathf.Sin(a),Mathf.Cos(a))*height*.5f;
            int n=vh.currentVertCount;
            vh.AddVert(center-x-y,color,Vector2.zero); vh.AddVert(center-x+y,color,Vector2.zero);
            vh.AddVert(center+x+y,color,Vector2.zero); vh.AddVert(center+x-y,color,Vector2.zero);
            vh.AddTriangle(n,n+1,n+2); vh.AddTriangle(n,n+2,n+3);
        }
    }
}
