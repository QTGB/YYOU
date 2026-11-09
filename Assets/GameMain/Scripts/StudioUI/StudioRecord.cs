using UnityEngine;
using YYOU.DrumPractice;
namespace YYOU.StudioUI
{
    public sealed class StudioRecord:MonoBehaviour
    {
        public DrumPracticeView view;
        void Update(){if(view && view.musicSource && view.musicSource.isPlaying)transform.Rotate(0,0,-8*Time.unscaledDeltaTime);}
    }
}
