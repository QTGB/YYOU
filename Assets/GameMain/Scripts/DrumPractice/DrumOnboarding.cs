using System;
using UnityEngine;
using UnityEngine.UI;
using YYOU.Settings;
namespace YYOU.DrumPractice
{
    public sealed class DrumOnboarding:MonoBehaviour
    {
        public Text heading,body,nextLabel;public Button next,skip;int step;Action complete;
        readonly string[] titles={"跟随节奏，敲击同色","大音符，双手一起","连打，连续点蓝色"};
        readonly string[] descriptions={"小蓝或小绿到达判定圈时，点击任意一侧的同色按钮。\n键盘也可以使用设置中的四个键位。","带有两个白点的大音符，需要同时按下左右两侧同色按钮。\n同一根手指滑过去不能代替双手。","蓝色长条出现时连续点击蓝色按钮，可以左右交替。\n保持按住不会自动连打。"};
        void Awake(){next.onClick.AddListener(Next);skip.onClick.AddListener(Done);}
        public void Show(Action callback){complete=callback;step=0;gameObject.SetActive(true);Refresh();}
        void Refresh(){heading.text=titles[step];body.text=descriptions[step];nextLabel.text=step==2?"开始演奏":"下一步";}
        public void Next(){if(++step>=3)Done();else Refresh();}
        public void Done(){PlayerAudioSettings.MarkTutorial();gameObject.SetActive(false);var callback=complete;complete=null;callback?.Invoke();}
    }
}
