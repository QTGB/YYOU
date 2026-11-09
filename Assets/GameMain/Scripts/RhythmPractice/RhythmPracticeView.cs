using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using YYOU.SongSelection;
using YYOU.StartScreen;

namespace YYOU.RhythmPractice
{
    public sealed class RhythmPracticeView:MonoBehaviour
    {
        public PracticeHighway highway;
        public Text scoreLabel,comboLabel,maxComboLabel,timeLabel,judgementLabel,countdownLabel,modeLabel;
        public Image timeProgress;
        public GameObject pausePanel,resultPanel;
        public Text resultLabel;
        public RoundedGraphic[] keys;
        public PracticeSession Session {get;private set;}
        public bool Paused {get;private set;}
        public int Difficulty {get;private set;}
        public SongSelectionView selection;
        public AudioSource musicSource;
        public MissFeedbackAudio missAudio;
        public AudioClip MusicClip {get;private set;}
        string songTitle;
        public PracticeChart Chart {get;private set;}
        public AudioClip reviewClip;
        public TextAsset reviewChart;
        public GameObject reviewReturnMenu;
        bool musicStarted,resumeMusic;
        public readonly bool[] Pressed=new bool[PracticeSession.LaneCount];
        public readonly float[] Flashes=new float[PracticeSession.LaneCount];
        readonly Dictionary<int,int> pointers=new Dictionary<int,int>();
        float resumeCountdown,judgeFade;
        bool initialized;
        void Start(){if(!initialized)Begin(null,0);}
        public void Begin(SongSelectionView from,int difficulty)
        {
            initialized=true;selection=from;Difficulty=Mathf.Clamp(difficulty,0,4);
            MusicClip=selection?selection.songs[selection.SelectedIndex].audio:reviewClip;
            songTitle=selection?selection.songs[selection.SelectedIndex].title:reviewClip?"Lofi 声部样谱 24–50s":"随机练习";
            Chart=MusicClip && selection?PracticeChart.Load(selection.songs[selection.SelectedIndex].id,Difficulty):reviewChart?JsonUtility.FromJson<PracticeChart>(reviewChart.text.TrimStart('\uFEFF')):null;
            if(!selection && Chart!=null)Difficulty=Chart.difficulty;
            if(!musicSource){musicSource=gameObject.AddComponent<AudioSource>();musicSource.playOnAwake=false;musicSource.spatialBlend=0;musicSource.volume=.8f;}
            if(selection)selection.gameObject.SetActive(false);
            gameObject.SetActive(true);Restart();
        }
        public void Restart()
        {
            musicSource.Stop();musicSource.clip=MusicClip;musicSource.loop=false;musicStarted=resumeMusic=false;
            Session=Chart!=null?new PracticeSession(Chart,MusicClip.length+3):new PracticeSession(Environment.TickCount,Difficulty,MusicClip?MusicClip.length+3:60);Session.Judged+=OnJudge;
            highway.travelSeconds=Chart!=null?Chart.travelSeconds:new[]{3.6f,3.3f,3f,2.7f,2.4f}[Difficulty];
            Paused=false;resumeCountdown=0;pointers.Clear();Array.Clear(Pressed,0,Pressed.Length);Array.Clear(Flashes,0,Flashes.Length);
            pausePanel.SetActive(false);resultPanel.SetActive(false);judgementLabel.text="";judgeFade=0;
            ResetKeyVisuals();ClearFocus();
            if(missAudio)missAudio.Stop();
            for(int i=0;i<PracticeSession.LaneCount;i++){var label=keys[i].transform.Find("KeyLabel").GetComponent<Text>();label.text=YYOU.Settings.LaneKeyBindings.LaneName(i);label.resizeTextForBestFit=true;label.resizeTextMinSize=12;label.resizeTextMaxSize=32;}
            modeLabel.text=(MusicClip?songTitle:"随机练习")+"\n"+new[]{"入门","简单","普通","困难","极限"}[Difficulty]+(Chart!=null?" · 固定谱面 · "+Mathf.RoundToInt(Chart.bpm)+" BPM":" · 随机音符")+(MusicClip?"":" · 无音频");
            Refresh();
        }
        public void SetPointer(int id,int lane,bool pressed)
        {
            if(lane<0 || lane>=PracticeSession.LaneCount)return;
            if(pressed){if(!Paused && !Session.Finished)pointers[id]=lane;}
            else pointers.Remove(id);
        }
        bool Keyboard(int lane)
        {
            return YYOU.Settings.LaneKeyBindings.Held(lane);
        }
        void Update()
        {
            if(Session==null)return;
            if(Input.GetKeyDown(KeyCode.Escape) && !Session.Finished){if(Paused)Resume();else Pause();}
            float dt=Time.unscaledDeltaTime;
            for(int i=0;i<PracticeSession.LaneCount;i++)Flashes[i]=Mathf.Max(0,Flashes[i]-dt*2.8f);
            if(Session.Finished){if(!resultPanel.activeSelf){Refresh();Finish();}highway.SetVerticesDirty();return;}
            if(Paused){highway.SetVerticesDirty();return;}
            if(resumeCountdown>0){resumeCountdown-=dt;countdownLabel.text="准备继续";highway.SetVerticesDirty();return;}
            if(resumeMusic){musicSource.UnPause();resumeMusic=false;}
            if(MusicClip && musicStarted)
            {
                float audioTime=musicSource.timeSamples/(float)MusicClip.frequency;
                if(musicSource.isPlaying)Session.Advance(Mathf.Max(0,3+audioTime-Session.Time));
                else Session.Advance(Session.Duration-Session.Time);
            }
            else
            {
                Session.Advance(dt);
                if(MusicClip && Session.Time>=3){musicSource.time=Mathf.Max(0,Session.Time-3);musicSource.Play();musicStarted=true;}
            }
            for(int i=0;i<PracticeSession.LaneCount;i++)
            {
                bool held=Keyboard(i)||pointers.ContainsValue(i);
                Session.SetLane(i,held);Pressed[i]=held;
                keys[i].color=held?new Color(.7f,.97f,1):new Color(.16f,.23f,.41f);
                keys[i].bottomColor=held?new Color(.45f,.58f,.95f):new Color(.08f,.12f,.25f);
            }
            judgeFade=Mathf.Max(0,judgeFade-dt);judgementLabel.color=new Color(judgementLabel.color.r,judgementLabel.color.g,judgementLabel.color.b,Mathf.Clamp01(judgeFade*2));
            Refresh();highway.SetVerticesDirty();
            if(Session.Finished)Finish();
        }
        void Refresh()
        {
            scoreLabel.text=Session.Score.ToString("D7");comboLabel.text=Session.Combo.ToString();maxComboLabel.text="MAX COMBO  "+Session.MaxCombo;
            timeLabel.text=Mathf.CeilToInt(Mathf.Max(0,Session.Duration-Session.Time)).ToString()+"s";timeProgress.rectTransform.sizeDelta=new Vector2(420*Session.Time/Session.Duration,6);
            countdownLabel.text=Session.Time<2.6f?Mathf.CeilToInt(3-Session.Time).ToString():"";
        }
        void OnJudge(int lane,string judgement)
        {
            if(judgement=="MISS" && missAudio)missAudio.Play();
            Flashes[lane]=judgement=="MISS" ? .15f : 1;judgeFade=.7f;judgementLabel.text=judgement;
            judgementLabel.color=judgement=="MISS"?new Color(1,.49f,.61f):judgement=="GOOD"?new Color(1,.87f,.5f):new Color(.55f,1,1);
        }
        void ClearFocus(){if(EventSystem.current)EventSystem.current.SetSelectedGameObject(null);}
        void ResetKeyVisuals()
        {
            Array.Clear(Pressed,0,Pressed.Length);
            foreach(var key in keys){if(!key)continue;key.color=new Color(.16f,.23f,.41f);key.bottomColor=new Color(.08f,.12f,.25f);}
        }
        public void Pause(){if(Session==null || Session.Finished)return;Paused=true;if(musicStarted)musicSource.Pause();resumeMusic=false;pointers.Clear();ResetKeyVisuals();ClearFocus();pausePanel.SetActive(true);}
        public void Resume(){pausePanel.SetActive(false);Paused=false;resumeCountdown=1;resumeMusic=musicStarted;ClearFocus();}
        void OnApplicationFocus(bool focus){if(!focus && initialized && isActiveAndEnabled)Pause();}
        void Finish()
        {
            if(musicSource)musicSource.Stop();
            pointers.Clear();ResetKeyVisuals();ClearFocus();resultPanel.SetActive(true);countdownLabel.text="";
            resultLabel.text="得分  "+Session.Score.ToString("N0")+"\n最大连击  "+Session.MaxCombo+"\n\nPERFECT  "+Session.Perfects+"    GOOD  "+Session.Goods+"    MISS  "+Session.Misses;
        }
        public void Back()
        {
            pointers.Clear();gameObject.SetActive(false);
            if(selection)selection.Open(selection.mainMenu);
            else if(reviewReturnMenu)reviewReturnMenu.SetActive(true);
        }
        void OnDisable(){if(musicSource)musicSource.Stop();if(missAudio)missAudio.Stop();pointers.Clear();ResetKeyVisuals();}
    }
}


