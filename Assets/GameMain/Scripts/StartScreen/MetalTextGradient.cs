using UnityEngine;
using UnityEngine.UI;

namespace YYOU.StartScreen
{
    public sealed class MetalTextGradient : BaseMeshEffect
    {
        public override void ModifyMesh(VertexHelper vh)
        {
            if(!IsActive() || vh.currentVertCount==0) return;
            var vertex=new UIVertex(); float min=float.MaxValue,max=float.MinValue;
            for(int i=0;i<vh.currentVertCount;i++) { vh.PopulateUIVertex(ref vertex,i); min=Mathf.Min(min,vertex.position.y); max=Mathf.Max(max,vertex.position.y); }
            for(int i=0;i<vh.currentVertCount;i++)
            {
                vh.PopulateUIVertex(ref vertex,i);
                float t=Mathf.InverseLerp(min,max,vertex.position.y);
                Color c=t<.5f?Color.Lerp(new Color(.49f,.55f,.65f),new Color(.86f,.88f,.92f),t*2):Color.Lerp(new Color(.72f,.75f,.80f),new Color(1,.97f,.87f),(t-.5f)*2);
                vertex.color=(Color)vertex.color*c; vh.SetUIVertex(vertex,i);
            }
        }
    }
}
