using UnityEngine;
namespace YYOU.StartScreen
{
    public sealed class AnimeMenuMotion:MonoBehaviour
    {
        public RectTransform artwork, logo;
        Vector2 artOrigin,logoOrigin;
        void Awake(){artOrigin=artwork.anchoredPosition;logoOrigin=logo.anchoredPosition;}
        void Update(){artwork.anchoredPosition=artOrigin+new Vector2(0,Mathf.Sin(Time.unscaledTime*.55f)*3);logo.anchoredPosition=logoOrigin+new Vector2(0,Mathf.Sin(Time.unscaledTime*.7f)*2);}
    }
}
