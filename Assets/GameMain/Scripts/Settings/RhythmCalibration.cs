using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
namespace YYOU.Settings
{
    public sealed class RhythmCalibration:MonoBehaviour
    {
        public AudioSource metronome;public Text instruction,count,result;public Button restart,apply,close;public float SuggestedOffset{get;private set;}
        readonly List<float> errors=new List<float>(24);readonly HashSet<int> tapped=new HashSet<int>();AudioClip clicks;double start;bool running;
        void Awake(){restart.onClick.AddListener(Begin);apply.onClick.AddListener(Apply);close.onClick.AddListener(()=>gameObject.SetActive(false));CreateClicks();}
        void CreateClicks(){const int sr=44100;var samples=new float[sr*15];for(int beat=0;beat<28;beat++)for(int i=0;i<sr*.035f;i++){float t=i/(float)sr;samples[(int)((.5f+beat*.5f)*sr)+i]=Mathf.Sin(2*Mathf.PI*1000*t)*Mathf.Exp(-t*110)*Mathf.Min(1,t/.002f)*.6f;}clicks=AudioClip.Create("Calibration clicks",samples.Length,1,sr,false);clicks.SetData(samples,0);}
        public void Open(){gameObject.SetActive(true);Begin();}
        public void Begin(){errors.Clear();tapped.Clear();SuggestedOffset=0;result.text="";apply.interactable=false;instruction.text="跟着声音点击下方区域或按空格\n前 4 拍用于准备，随后记录 20 次点击";count.text="准备";metronome.Stop();metronome.clip=clicks;metronome.volume=.65f;start=AudioSettings.dspTime+.5;metronome.PlayScheduled(start);running=true;}
        public void Tap(){TapAt(AudioSettings.dspTime);}
        public void TapAt(double stamp){if(!running)return;double elapsed=stamp-start-.5;int beat=(int)Math.Round(elapsed/.5);if(beat<4 || beat>=28 || tapped.Contains(beat))return;float error=(float)(elapsed-beat*.5)*1000;if(Mathf.Abs(error)>250)return;tapped.Add(beat);errors.Add(error);count.text=errors.Count+" / 20";if(errors.Count>=20)Complete();}
        void Complete(){running=false;metronome.Stop();errors.Sort();SuggestedOffset=Mathf.Clamp(Mathf.Round(((errors[9]+errors[10])*.5f)/5)*5,-250,250);result.text="建议偏移  "+SuggestedOffset.ToString("+0;-0;0")+" ms\n正值补偿偏晚输入";instruction.text="可以应用结果，也可以重新校准";apply.interactable=true;}
        public void Apply(){PlayerAudioSettings.SetOffset(SuggestedOffset);gameObject.SetActive(false);}
        void Update(){if(!running)return;bool pressed=Input.GetKeyDown(KeyCode.Space);for(int i=0;i<4;i++)pressed|=Input.GetKeyDown(LaneKeyBindings.Get(i));if(pressed)Tap();if(AudioSettings.dspTime-start>15){running=false;metronome.Stop();instruction.text="有效点击不足 20 次，请重新校准";}}
        void OnDisable(){running=false;if(metronome)metronome.Stop();}void OnDestroy(){if(clicks)Destroy(clicks);}
    }
}
