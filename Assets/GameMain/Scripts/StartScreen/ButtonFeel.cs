using UnityEngine;
using UnityEngine.EventSystems;

namespace YYOU.StartScreen
{
    public sealed class ButtonFeel : MonoBehaviour, IPointerDownHandler, IPointerUpHandler,
        IPointerEnterHandler, IPointerExitHandler, ISelectHandler, IDeselectHandler
    {
        public RectTransform face;
        Vector2 origin;
        bool down, hover, selected;
        void Awake() { origin = face.anchoredPosition; }
        void Update()
        {
            face.anchoredPosition = Vector2.Lerp(face.anchoredPosition, origin + Vector2.down * (down ? 5 : 0), 1-Mathf.Exp(-24*Time.unscaledDeltaTime));
            transform.localScale = Vector3.Lerp(transform.localScale, Vector3.one * (down ? .985f : hover || selected ? 1.025f : 1), 1-Mathf.Exp(-18*Time.unscaledDeltaTime));
        }
        public void OnPointerDown(PointerEventData e) { down = true; }
        public void OnPointerUp(PointerEventData e) { down = false; }
        public void OnPointerEnter(PointerEventData e) { hover = true; }
        public void OnPointerExit(PointerEventData e) { hover = down = false; }
        public void OnSelect(BaseEventData e) { selected = true; }
        public void OnDeselect(BaseEventData e) { selected = down = false; }
        void OnDisable() { down = hover = selected = false; if (face) face.anchoredPosition = origin; transform.localScale = Vector3.one; }
    }
}
