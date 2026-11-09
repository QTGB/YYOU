using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
namespace YYOU.SongPacks
{
    public sealed class PackCarousel:ScrollRect
    {
        public SongPackShopView shop;
        public override void OnBeginDrag(PointerEventData e){if(shop)shop.CancelScroll();base.OnBeginDrag(e);}
        public override void OnScroll(PointerEventData e){if(shop)shop.CancelScroll();var original=e.scrollDelta;e.scrollDelta=new Vector2(Mathf.Abs(original.x)>.01f?original.x:original.y,0);base.OnScroll(e);e.scrollDelta=original;}
    }
}
