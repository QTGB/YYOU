using UnityEngine;

namespace YYOU.DrumPractice
{
    // Reposition the pads when the safe rectangle changes, keeping their 16:9 layout unchanged.
    [ExecuteAlways]
    public sealed class GameplayFrameLayout:MonoBehaviour
    {
        public DrumPracticeView view;
        void OnRectTransformDimensionsChange(){if(view){view.ApplyPadLayout();view.RefreshFeedbackLayout();}}
        void LateUpdate(){if(view)view.RefreshFeedbackLayout();}
    }
}
