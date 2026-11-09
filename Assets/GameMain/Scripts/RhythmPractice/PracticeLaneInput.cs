using UnityEngine;
using UnityEngine.EventSystems;
namespace YYOU.RhythmPractice
{
    public sealed class PracticeLaneInput:MonoBehaviour,IPointerDownHandler,IPointerUpHandler
    {
        public RhythmPracticeView view;public int lane;
        public void OnPointerDown(PointerEventData e){if(e.button==PointerEventData.InputButton.Left)view.SetPointer(e.pointerId,lane,true);}
        public void OnPointerUp(PointerEventData e){view.SetPointer(e.pointerId,lane,false);}
    }
}
