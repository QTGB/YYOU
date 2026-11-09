using UnityEngine.EventSystems;
using UnityEngine.UI;
namespace YYOU.SongPacks
{
    public sealed class PackCardButton:Button,IInitializePotentialDragHandler,IBeginDragHandler,IDragHandler,IEndDragHandler
    {
        public PackCarousel carousel;bool dragged;
        public override void OnPointerDown(PointerEventData e){dragged=false;base.OnPointerDown(e);}
        public override void OnPointerClick(PointerEventData e){if(!dragged)base.OnPointerClick(e);}
        public void OnInitializePotentialDrag(PointerEventData e){if(carousel)carousel.OnInitializePotentialDrag(e);}
        public void OnBeginDrag(PointerEventData e){dragged=true;if(carousel)carousel.OnBeginDrag(e);}
        public void OnDrag(PointerEventData e){if(carousel)carousel.OnDrag(e);}
        public void OnEndDrag(PointerEventData e){if(carousel)carousel.OnEndDrag(e);}
    }
}
