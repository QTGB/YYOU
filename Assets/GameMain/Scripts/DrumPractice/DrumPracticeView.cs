using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using YYOU.Settings;
using YYOU.StartScreen;
using YYOU.SongSelection;
using YYOU.RhythmPractice;

namespace YYOU.DrumPractice
{
    public sealed class DrumPracticeView:MonoBehaviour
    {
        public DrumRailGraphic rail;
        public BottomSpectrumGraphic performanceWave;
        public RoundedGraphic[] pads;
        public Text[] keyLabels;
        public Text score,combo,timer,songLabel,feedback,rollLabel,countdown,resultText;
        public Text trackTitle,trackArtist,feedbackHint;
        public Font feedbackDisplayFont,feedbackBodyFont;
        public RawImage songBackdrop;
        public AspectRatioFitter songBackdropAspect;
        public RectTransform musicProgress,musicProgressTrack;
        public GameObject pausePanel,resultPanel,homeMenu;
        public AudioSource musicSource,hitSource;
        public AudioSource wrongSource;
        public AudioClip blueSound,greenSound,bigSound,rollSound,wrongSound;
        public MissFeedbackAudio missSound;
        public SongSelectionView selection;
        public DrumOnboarding onboarding;
        public DrumSession Session {get;private set;}
        public DrumChart Chart {get;private set;}
        public bool Paused {get;private set;}
        public bool Preparing=>Session!=null && clock.Preparing;
        public SongClock Clock=>clock;
        public float JudgeFlash {get;private set;}
        public bool JudgeFailed {get;private set;}
        public float ImpactFlash {get;private set;}
        public float ImpactStrength {get;private set;}
        public Color ImpactTint {get;private set;}
        public readonly float[] PadFlash=new float[4];
        readonly Dictionary<int,int> touches=new Dictionary<int,int>();
        float musicMixScale=1f;
        AudioClip music;bool initialized,resumeGate,resultCommitted;float feedbackTime;
        readonly SongClock clock=new SongClock();
        readonly bool[] keyboardHeld=new bool[4],blockedKeys=new bool[4];
        readonly HashSet<int> blockedPointers=new HashSet<int>();
        int shownScore=-1,shownCombo=-1,shownSeconds=-1,shownCountdown=-1;
        void OnEnable(){PadLayouts.Changed+=ApplyPadLayout;PlayerAudioSettings.Changed+=ApplyAudioSettings;ApplyPadLayout();ApplyAudioSettings();}
        void Start(){if(!initialized)Begin(null,0);}
        public void ApplyPadLayout()
        {
            if(pads==null)return;
            for(int i=0;i<pads.Length && i<4;i++)
            {
                if(!pads[i])continue;var position=AdaptedPadPosition(i);pads[i].rectTransform.anchoredPosition=position;
                var shadow=pads[i].transform.parent.Find("PadShadow"+i) as RectTransform;if(shadow)shadow.anchoredPosition=position+Vector2.down*6;
            }
        }
        public Vector2 AdaptedPadPosition(int index)
        {
            var position=PadLayouts.Position(PadLayouts.Selected,index);
            if(pads==null || index<0 || index>=pads.Length || !pads[index])return position;
            var parent=pads[index].transform.parent as RectTransform;if(!parent)return position;
            var safe=parent.GetComponent<MobileSafeFrame>();if(!safe || !safe.expandToSafeArea)return position;
            var size=parent.rect.size;return position+new Vector2(Mathf.Sign(position.x)*Mathf.Max(0,size.x-1600)*.5f,-Mathf.Max(0,size.y-900)*.5f);
        }
        public void RefreshFeedbackLayout()
        {
            if(!feedback || !rail)return;
            // All transient status text shares the space above judgement, outside the rail mask.
            var world=rail.rectTransform.TransformPoint(new Vector3(rail.CurrentJudgeX,rail.rectTransform.rect.yMax+8,0));
            PositionStatus(feedback,world);PositionStatus(countdown,world);PositionStatus(rollLabel,world);
        }
        static void PositionStatus(Text label,Vector3 world)
        {
            if(!label)return;var rect=label.rectTransform;var position=rect.parent.InverseTransformPoint(world);
            if((rect.localPosition-position).sqrMagnitude>.001f)rect.localPosition=position;
        }
        void ShowPreparation(bool preparing)
        {
            countdown.enabled=preparing;feedback.enabled=!preparing;if(feedbackHint)feedbackHint.enabled=!preparing;
            // Per-hit roll counts already use OnFeedback; the old central helper is redundant.
            if(rollLabel){rollLabel.text="";rollLabel.enabled=false;}
        }
        public void Begin(SongSelectionView from,int difficulty)
        {
            var entry=from?from.songs[from.SelectedIndex]:null;
            var chart=entry!=null?DrumChart.Load(entry.id,difficulty):null;
            BeginEntry(from,entry,chart);
        }
        public bool BeginReview(SongSelectionView from,SongEntry entry,DrumChart chart)
        {if(entry==null || chart==null || !chart.reviewOnly)return false;return BeginEntry(from,entry,chart);}
        bool BeginEntry(SongSelectionView from,SongEntry entry,DrumChart chart)
        {
            initialized=true;
            if(entry!=null && (chart==null || !entry.audio)){Debug.LogWarning("Selected song/difficulty is not playable");gameObject.SetActive(false);return false;}
            if(chart!=null && Mathf.Abs(entry.audio.length-chart.audioDuration)>.25f){Debug.LogWarning("Chart/audio duration mismatch");gameObject.SetActive(false);return false;}
            initialized=true;selection=from;music=entry!=null?entry.audio:null;Chart=chart;musicMixScale=MusicPlayback.GameplayScale(chart!=null && !string.IsNullOrEmpty(chart.audioPath)?chart.audioPath:entry!=null?entry.audioPath:null);
            songLabel.text=Chart!=null?Chart.label:"操作练习";
            if(trackTitle)trackTitle.text=entry!=null?entry.title:"自由练习";if(trackArtist)trackArtist.text=entry!=null?entry.artist:"";
            if(from)from.gameObject.SetActive(false);gameObject.SetActive(true);
            if(onboarding && !PlayerAudioSettings.TutorialSeen && Chart!=null){pausePanel.SetActive(false);resultPanel.SetActive(false);countdown.enabled=false;int length=Mathf.CeilToInt(music.length);timer.text=(length/60).ToString("00")+":"+(length%60).ToString("00");onboarding.Show(Restart);}else Restart();
            return true;
        }
        public void Restart()
        {
            if(Session!=null){Session.Feedback-=OnFeedback;Session.Performed-=OnPerformance;}
            hitSource.Stop();missSound.Stop();if(wrongSource)wrongSource.Stop();
            Session=Chart!=null?new DrumSession(Chart):new DrumSession(45);Session.Feedback+=OnFeedback;Session.Performed+=OnPerformance;
            clock.Start(musicSource,music);Paused=false;resumeGate=false;resultCommitted=false;feedbackTime=JudgeFlash=ImpactFlash=0;
            GateInput();Array.Clear(PadFlash,0,4);shownScore=shownCombo=shownSeconds=shownCountdown=-1;
            if(performanceWave)performanceWave.Bind(Session);if(onboarding)onboarding.gameObject.SetActive(false);
            pausePanel.SetActive(false);resultPanel.SetActive(false);feedback.text="";if(feedbackHint)feedbackHint.text="";rollLabel.text="";
            for(int i=0;i<4;i++)keyLabels[i].text=LaneKeyBindings.Name(LaneKeyBindings.Get(i));
            ApplyAudioSettings();ClearFocus();Refresh();rail.SetVerticesDirty();
        }
        public void ApplyAudioSettings()
        {
            if(musicSource)musicSource.volume=PlayerAudioSettings.Music*musicMixScale;if(hitSource)hitSource.volume=PlayerAudioSettings.Hits;if(wrongSource)wrongSource.volume=PlayerAudioSettings.Miss;
            if(missSound){var a=missSound.GetComponent<AudioSource>();if(a)a.volume=PlayerAudioSettings.Miss;}
            if(rail)rail.travelSeconds=PlayerAudioSettings.TravelSeconds;
        }
        void ClearFocus(){EventSystem.current?.SetSelectedGameObject(null);}
        void GateInput(){foreach(var id in touches.Keys)blockedPointers.Add(id);touches.Clear();Array.Clear(keyboardHeld,0,4);for(int i=0;i<4;i++)blockedKeys[i]=Input.GetKey(LaneKeyBindings.Get(i));Session?.ClearInput();}
        void AdvanceClock(double stamp)
        {if(Session==null || Paused || Session.Finished)return;float raw=clock.At(stamp);if(Preparing && raw>=0)return;float target=raw>=(music?music.length:42)?Session.Duration:Mathf.Min(Session.Duration-.001f,raw+3-PlayerAudioSettings.OffsetMs*.001f);Session.Advance(Mathf.Max(0,target-Session.Time));}
        public void SetPointer(int id,int pad,bool pressed){SetPointerAt(id,pad,pressed,AudioSettings.dspTime);}
        public void SetPointerAt(int id,int pad,bool pressed,double timestamp)
        {
            if(pad<0 || pad>=4)return;
            if(!pressed){blockedPointers.Remove(id);if(touches.TryGetValue(id,out var held)){touches.Remove(id);SyncPad(held,timestamp);}return;}
            if(Paused || Preparing || Session==null || Session.Finished){blockedPointers.Add(id);return;}
            if(blockedPointers.Contains(id))return;
            if(touches.TryGetValue(id,out var old)){if(old==pad)return;touches.Remove(id);SyncPad(old,timestamp);}
            touches[id]=pad;SyncPad(pad,timestamp);
        }
        void SyncPad(int pad,double timestamp)
        {
            if(Session==null || Paused || Preparing || Session.Finished)return;AdvanceClock(timestamp);
            bool down=keyboardHeld[pad] || touches.ContainsValue(pad);
            if(down && !Session.Held[pad])PadFlash[pad]=1;
            var sound=Session.SetPad(pad,down);
            var clip=sound==DrumStrikeSound.Blue?blueSound:sound==DrumStrikeSound.Green?greenSound:sound==DrumStrikeSound.Big?bigSound:sound==DrumStrikeSound.Roll?rollSound:null;
            if(clip)hitSource.PlayOneShot(clip);
        }
        void PollKeyboard()
        {
            for(int i=0;i<4;i++){
                var key=LaneKeyBindings.Get(i);bool held=Input.GetKey(key);
                if(blockedKeys[i]){if(!held)blockedKeys[i]=false;continue;}
                if(Input.GetKeyUp(key)){keyboardHeld[i]=false;SyncPad(i,AudioSettings.dspTime);}
                if(!Input.GetKeyDown(key))continue;
                if(Paused || Preparing || Session.Finished){blockedKeys[i]=true;continue;}
                keyboardHeld[i]=true;SyncPad(i,AudioSettings.dspTime);
            }
        }
        void Update()
        {
            if(Session==null)return;float dt=Time.unscaledDeltaTime;
            if(Input.GetKeyDown(KeyCode.Escape) && !Session.Finished){if(Paused)Resume();else Pause();}
            PollKeyboard();for(int i=0;i<4;i++){PadFlash[i]=Mathf.Max(0,PadFlash[i]-dt*4);pads[i].rectTransform.localScale=Vector3.one*(1-.055f*PadFlash[i]);}
            if(Session.Finished){if(!resultCommitted)Finish();return;}if(Paused)return;
            if(Preparing){AdvanceClock(AudioSettings.dspTime);Refresh();rail.SetVerticesDirty();return;}
            if(resumeGate){Session.ClearInput(true);resumeGate=false;}
            ImpactFlash=Mathf.Max(0,ImpactFlash-dt/.18f);JudgeFlash=Mathf.Max(0,JudgeFlash-dt*2.2f);
            AdvanceClock(AudioSettings.dspTime);
            feedbackTime=Mathf.Max(0,feedbackTime-dt);var c=feedback.color;c.a=Mathf.Clamp01(feedbackTime/.24f);feedback.color=c;
            if(feedbackHint)feedbackHint.color=new Color(c.r,c.g,c.b,c.a*.8f);
            feedback.rectTransform.localScale=Vector3.one*(1+.10f*Mathf.Clamp01((feedbackTime-.65f)/.15f));
            Refresh();rail.SetVerticesDirty();if(Session.Finished)Finish();
        }
        void Refresh()
        {
            if(Session.Score!=shownScore){shownScore=Session.Score;score.text=shownScore.ToString("D7");}if(Session.Combo!=shownCombo){shownCombo=Session.Combo;combo.text=shownCombo.ToString();}
            int remaining=Mathf.CeilToInt(Mathf.Max(0,(music?music.length:42)-Mathf.Max(0,clock.SongSeconds)));
            if(remaining!=shownSeconds){shownSeconds=remaining;timer.text=(remaining/60).ToString("00")+":"+(remaining%60).ToString("00");}
            if(musicProgress && musicProgressTrack){float progress=Mathf.Clamp01(Mathf.Max(0,clock.SongSeconds)/(music?music.length:42));musicProgress.sizeDelta=new Vector2(musicProgressTrack.rect.width*progress,musicProgress.sizeDelta.y);}
            int preparing=Preparing?(resumeGate?-2:Mathf.CeilToInt(clock.PreparationRemaining)):0;
            if(preparing!=shownCountdown){shownCountdown=preparing;countdown.text=preparing==-2?"准备继续":preparing>0?preparing.ToString():"";if(feedbackDisplayFont && feedbackBodyFont)countdown.font=preparing==-2?feedbackBodyFont:feedbackDisplayFont;countdown.resizeTextMaxSize=preparing==-2?22:32;}
            ShowPreparation(Preparing);
        }
        void OnPerformance(DrumPerformanceCue cue)
        {
            if(cue.Kind==DrumPerformanceKind.Miss){ImpactFlash=0;return;}
            ImpactFlash=1;ImpactStrength=(cue.Kind==DrumPerformanceKind.Perfect?1:cue.Kind==DrumPerformanceKind.Good ? .68f : .4f)*(cue.Big?1.18f:1);
            ImpactTint=cue.Color==0?new Color(.28f,.76f,.95f):new Color(.27f,.85f,.63f);
        }
        void OnFeedback(string text,int color)
        {
            bool wrong=text=="WRONG";JudgeFailed=wrong || text=="MISS";feedback.text=wrong?"MISS":text.Replace("双手 ","");feedbackTime=.8f;JudgeFlash=1;
            if(feedbackHint)feedbackHint.text=wrong?"错色":"";feedback.rectTransform.localScale=Vector3.one*1.10f;RefreshFeedbackLayout();
            bool grade=wrong || text.Contains("PERFECT") || text.Contains("GOOD") || text=="MISS";
            if(feedbackDisplayFont && feedbackBodyFont){feedback.font=grade?feedbackDisplayFont:feedbackBodyFont;feedback.fontStyle=grade?FontStyle.Italic:FontStyle.Normal;}
            feedback.resizeTextMaxSize=grade?32:20;feedback.color=JudgeFailed?new Color(.86f,.29f,.40f):text.Contains("PERFECT")?new Color(.78f,.53f,.25f):text.Contains("GOOD")?new Color(.15f,.62f,.47f):new Color(.12f,.58f,.78f);
            if(feedbackHint){var tint=feedback.color;feedbackHint.color=new Color(tint.r,tint.g,tint.b,.8f);}
            if(wrong && wrongSource && wrongSound)wrongSource.PlayOneShot(wrongSound);else if(JudgeFailed)missSound.Play();
        }
        public void Pause(){if(Session==null || Session.Finished || Paused)return;AdvanceClock(AudioSettings.dspTime);Paused=true;clock.Pause();hitSource.Stop();if(wrongSource)wrongSource.Stop();missSound.Stop();GateInput();pausePanel.SetActive(true);ClearFocus();}
        public void Resume(){if(!Paused)return;pausePanel.SetActive(false);GateInput();Paused=false;resumeGate=true;clock.Resume();ClearFocus();Refresh();}
        void OnApplicationFocus(bool focus){if(!focus && initialized && isActiveAndEnabled)Pause();}
        void OnApplicationPause(bool pause){if(pause && initialized && isActiveAndEnabled)Pause();}
        void Finish(){resultCommitted=true;clock.Stop();hitSource.Stop();GateInput();Refresh();DrumRecords.Save(Chart,Session);resultPanel.SetActive(true);bool fc=Session.Misses==0 && Session.Completed==Session.Notes.Count;resultText.text=(Chart!=null && Chart.reviewOnly?"测试谱面 · 不计正式成绩\n":"")+(fc?"FULL COMBO\n":"")+"得分  "+Session.Score.ToString("N0")+"    最大连击  "+Session.MaxCombo+"\nPERFECT  "+Session.Perfects+"    GOOD  "+Session.Goods+"    MISS  "+Session.Misses+"\n连打完成  "+Session.RollCompleted+"    连打次数  "+Session.RollHits;ClearFocus();}
        public void Back(){gameObject.SetActive(false);if(selection)selection.Open(selection.mainMenu);else if(homeMenu)homeMenu.SetActive(true);}
        void OnDisable(){PadLayouts.Changed-=ApplyPadLayout;PlayerAudioSettings.Changed-=ApplyAudioSettings;clock.Stop();if(hitSource)hitSource.Stop();if(wrongSource)wrongSource.Stop();if(missSound)missSound.Stop();touches.Clear();blockedPointers.Clear();Session?.ClearInput();}
    }
}
