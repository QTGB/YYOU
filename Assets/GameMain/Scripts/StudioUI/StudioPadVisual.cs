using UnityEngine;
using UnityEngine.UI;
using YYOU.DrumPractice;
namespace YYOU.StudioUI
{
    [RequireComponent(typeof(RawImage))]
    public sealed class StudioPadVisual:MonoBehaviour
    {
        public DrumPracticeView view;
        public int pad;
        Material instance;
        void Awake(){var image=GetComponent<RawImage>();instance=new Material(image.material);image.material=instance;}
        void LateUpdate(){if(instance && view)instance.SetFloat("_Flash",view.PadFlash[pad]);}
        void OnDestroy(){if(instance)Destroy(instance);}
    }
}
