using UnityEngine;
using UnityEngine.EventSystems;
namespace YYOU.DrumPractice
{
    public sealed class DrumPadInput:MonoBehaviour,IPointerDownHandler,IPointerUpHandler,ICanvasRaycastFilter
    {
        public DrumPracticeView view;public int pad;
        public void OnPointerDown(PointerEventData e){if(e.button==PointerEventData.InputButton.Left)view.SetPointer(e.pointerId,pad,true);}
        public void OnPointerUp(PointerEventData e){view.SetPointer(e.pointerId,pad,false);}
        public bool IsRaycastLocationValid(Vector2 screenPoint,Camera camera){var r=(RectTransform)transform;RectTransformUtility.ScreenPointToLocalPointInRectangle(r,screenPoint,camera,out var p);return (p-r.rect.center).sqrMagnitude<=Mathf.Pow(Mathf.Min(r.rect.width,r.rect.height)*.5f,2);}
    }
}
