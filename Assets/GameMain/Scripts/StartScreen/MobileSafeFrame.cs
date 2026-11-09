using UnityEngine;
namespace YYOU.StartScreen
{
    [ExecuteAlways,RequireComponent(typeof(RectTransform))]
    public sealed class MobileSafeFrame:MonoBehaviour
    {
        public static Rect? PreviewSafeArea;
        public bool expandToSafeArea;
        RectTransform frame;Rect lastSafe;Vector2 lastSize;int lastWidth,lastHeight;
        void OnEnable(){frame=GetComponent<RectTransform>();if(Application.isPlaying && Application.isMobilePlatform){Application.targetFrameRate=60;Screen.sleepTimeout=SleepTimeout.NeverSleep;}Fit();}
        void LateUpdate(){Fit();}
        public void RefreshLayout(){lastWidth=0;Fit();}
        void Fit()
        {
            if(!frame)frame=GetComponent<RectTransform>();var parent=frame.parent as RectTransform;if(!parent)return;
            Rect safe=Screen.safeArea;int w=Mathf.Max(1,Screen.width),h=Mathf.Max(1,Screen.height);var size=parent.rect.size;
            if(PreviewSafeArea.HasValue){var test=PreviewSafeArea.Value;safe=new Rect(test.x*w,test.y*h,test.width*w,test.height*h);}
            if(lastSafe==safe && lastSize==size && lastWidth==w && lastHeight==h)return;
            lastSafe=safe;lastSize=size;lastWidth=w;lastHeight=h;
            var area=new Vector2(size.x*safe.width/w,size.y*safe.height/h);float scale=Mathf.Min(area.x/1600,area.y/900);
            frame.anchorMin=frame.anchorMax=frame.pivot=new Vector2(.5f,.5f);frame.sizeDelta=expandToSafeArea?area/Mathf.Max(.01f,scale):new Vector2(1600,900);
            frame.anchoredPosition=new Vector2((safe.center.x/w-.5f)*size.x,(safe.center.y/h-.5f)*size.y);frame.localScale=Vector3.one*Mathf.Max(.01f,scale);
        }
    }
}
