using UnityEngine;
using UnityEngine.EventSystems;
namespace YYOU.SongSelection
{
    public sealed class SongListItem:MonoBehaviour,IPointerClickHandler,IPointerDownHandler,IBeginDragHandler,IDragHandler,IEndDragHandler,IScrollHandler
    {
        SongSelectionView view;int index;float drag;bool dragged;
        public void Bind(SongSelectionView owner,int item){view=owner;index=item;}
        public void OnPointerClick(PointerEventData e){if(!dragged)view.SelectIndex(index);}
        public void OnBeginDrag(PointerEventData e){drag=0;dragged=true;}
        public void OnDrag(PointerEventData e){drag+=e.delta.y;}
        public void OnEndDrag(PointerEventData e){if(Mathf.Abs(drag)>30)view.Step(drag>0?1:-1);}
        public void OnScroll(PointerEventData e){view.Scroll(e.scrollDelta.y);}
        public void OnPointerDown(PointerEventData e){dragged=false;}
    }
}
