using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
namespace YYOU.Settings
{
    public sealed class KeyBindingSettings:MonoBehaviour
    {
        public Button[] bindingButtons;
        public Text hint;
        int waiting=-1;
        public bool IsCapturing=>waiting>=0;
        public int ConsumedFrame {get;private set;}=-1;
        void Awake(){for(int i=0;i<bindingButtons.Length;i++){int slot=i;bindingButtons[i].onClick.AddListener(()=>BeginCapture(slot));}}
        void OnEnable(){waiting=-1;Refresh();if(hint)hint.text="点击键位后按新键；重复键位自动交换";}
        void OnDisable(){waiting=-1;}
        public void BeginCapture(int slot){waiting=slot;Refresh();hint.text="按下新按键；Esc 取消";EventSystem.current?.SetSelectedGameObject(null);}
        void OnGUI(){if(waiting<0)return;var e=Event.current;if(e.type!=EventType.KeyDown || e.keyCode==KeyCode.None)return;Submit(e.keyCode);e.Use();}
        public void Submit(KeyCode key)
        {
            if(waiting<0)return;
            ConsumedFrame=Time.frameCount;
            if(key==KeyCode.Escape){waiting=-1;hint.text="已取消改键";Refresh();return;}
            if(LaneKeyBindings.Assign(waiting,key,out var message)){waiting=-1;Refresh();hint.text=string.IsNullOrEmpty(message)?"键位已保存":message;}
            else hint.text=message;
        }
        public void ResetDefaults(){waiting=-1;LaneKeyBindings.Reset();Refresh();hint.text="已恢复 D / F / J / K";}
        void Refresh(){if(bindingButtons==null)return;for(int i=0;i<bindingButtons.Length;i++){var t=bindingButtons[i].GetComponentInChildren<Text>();t.text=i==waiting?"按键…":LaneKeyBindings.Name(LaneKeyBindings.Get(i));var marker=bindingButtons[i].transform.Find("CaptureHighlight");if(marker)marker.gameObject.SetActive(i==waiting);}}
    }
}
