using UnityEngine;
using UnityEngine.EventSystems;
namespace YYOU.SongSelection
{
    public sealed class SongListGesture:MonoBehaviour,IScrollHandler,IBeginDragHandler,IDragHandler,IEndDragHandler
    {
        public SongSelectionView view;float drag;
        public void OnScroll(PointerEventData e){view.Scroll(e.scrollDelta.y);}
        public void OnBeginDrag(PointerEventData e){drag=0;}
        public void OnDrag(PointerEventData e){drag+=e.delta.y;}
        public void OnEndDrag(PointerEventData e){if(Mathf.Abs(drag)>30)view.Step(drag>0?1:-1);}
    }
}
