using UnityEngine;

namespace YYOU.StartScreen
{
    [ExecuteAlways]
    [RequireComponent(typeof(RectTransform))]
    public sealed class FullCanvasBackground : MonoBehaviour
    {
        bool fitting;
        void OnEnable() { Fit(); }
        void OnRectTransformDimensionsChange() { Fit(); }
        void Fit()
        {
            if(fitting)return;
            fitting=true;
            try{
            var r=(RectTransform)transform;
            if(r.anchorMin!=Vector2.zero)r.anchorMin=Vector2.zero;
            if(r.anchorMax!=Vector2.one)r.anchorMax=Vector2.one;
            if(r.anchoredPosition!=Vector2.zero)r.anchoredPosition=Vector2.zero;
            if(r.sizeDelta!=Vector2.zero)r.sizeDelta=Vector2.zero;
            if(r.localScale!=Vector3.one)r.localScale=Vector3.one;
            if(r.localRotation!=Quaternion.identity)r.localRotation=Quaternion.identity;
            }finally{fitting=false;}
        }
    }
}
