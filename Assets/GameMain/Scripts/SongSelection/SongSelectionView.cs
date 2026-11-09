using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.Networking;
using YYOU.StartScreen;
using YYOU.DrumPractice;
using YYOU.Settings;
using YYOU.SongPacks;

namespace YYOU.SongSelection
{
    [Serializable] public class SongEntry
    {
        public string id,title,artist,duration,sourceUrl;
        public Texture2D cover;
        public AudioClip audio;
        [Min(0)] public float previewStart=12;
        public string audioPath;
        public int category;
        public int availableDifficulties;
    }
    [Serializable] public class SongLaunchEvent:UnityEvent<string,int>{}
    public sealed class SongSelectionView:MonoBehaviour
    {
        public SongEntry[] songs;
        public RectTransform[] rows,cards;
        public RoundedGraphic[] rowFaces;
        public CanvasGroup[] rowGroups;
        public Text[] rowScores;
        public RawImage[] cardImages;
        public Text title,artist,indexText,previewText,scoreText,notice,currencyText;
        public Button[] difficultyButtons,filterButtons;
        public Button backButton,startButton;
        public Button retryButton;
        public Text availabilityText;
        public Image progress;
        public AudioSource previewSource;
        public GameObject mainMenu;
        public SongLaunchEvent onSongStartRequested=new SongLaunchEvent();
        public UnityEvent onCurrencyRequested=new UnityEvent();
        public int currencyBalance;
        public GameObject practicePrefab,drumPracticePrefab;
        public GameObject songPackShopPrefab;
        SongPackShopView songPackShop;
        Text actionLabel;int openedFrame=-1;
        YYOU.DrumPractice.DrumPracticeView drumPractice;
        public int SelectedIndex {get;private set;}
        public int SelectedDifficulty {get;private set;}
        public int Filter {get;private set;}
        public int FilteredCount=>filtered.Count;
        public bool IsAnimating {get;private set;}
        public bool IsLoading {get;private set;}
        public int LastLaunchSong {get;private set;}=-1;
        public int LastLaunchDifficulty {get;private set;}=-1;
        readonly List<int> filtered=new List<int>();
        readonly int[] filterValues={0,1,2,16,4,8};
        Coroutine selectionRoutine,audioRoutine,previewFade;
        AudioSource previewA,previewB;
        float gainA,gainB;
        int audibleIndex=-1;
        Vector2[] difficultyPositions;
        UnityWebRequest audioRequest;
        float scrollGate,noticeUntil;
        bool muted,initialized,keepAudioForGame;
        void Awake()
        {
            actionLabel=startButton.GetComponentInChildren<Text>();
            previewSource.playOnAwake=false;previewSource.loop=true;previewSource.spatialBlend=0;
            previewA=previewSource;previewA.pitch=1;previewA.volume=0;
            previewB=new GameObject("PreviewCrossfade",typeof(AudioSource)).GetComponent<AudioSource>();previewB.transform.SetParent(transform,false);previewB.playOnAwake=false;previewB.loop=true;previewB.spatialBlend=0;previewB.pitch=1;previewB.volume=0;previewB.outputAudioMixerGroup=previewA.outputAudioMixerGroup;
            difficultyPositions=new Vector2[difficultyButtons.Length];
            for(int i=0;i<difficultyButtons.Length;i++){int n=i;difficultyPositions[i]=((RectTransform)difficultyButtons[i].transform).anchoredPosition;difficultyButtons[i].onClick.AddListener(()=>SelectDifficulty(n));}
            if(filterButtons!=null)for(int i=0;i<filterButtons.Length;i++){int n=i;filterButtons[i].onClick.AddListener(()=>SetFilter(filterValues[n]));}
            if(retryButton)retryButton.onClick.AddListener(RetryAudio);
            foreach(var song in songs)song.availableDifficulties=DrumChart.AvailableMask(song.id);
        }
        void OnEnable(){PlayerAudioSettings.Changed+=ApplyAudio;SongPackOwnership.Changed+=RefreshAcquisition;ApplyAudio();}
        void RefreshAcquisition(){if(filtered.Count>0){RefreshRows();RefreshAvailability();}}
        void ApplyAudio(){if(previewA){previewA.volume=PlayerAudioSettings.Music*gainA;previewA.mute=muted;}if(previewB){previewB.volume=PlayerAudioSettings.Music*gainB;previewB.mute=muted;}}
        float Gain(AudioSource source)=>source==previewA?gainA:gainB;
        void SetGain(AudioSource source,float value){if(source==previewA)gainA=value;else gainB=value;ApplyAudio();}
        void StopPreviewAudio()
        {
            if(previewFade!=null)StopCoroutine(previewFade);previewFade=null;
            if(previewA){previewA.Stop();previewA.clip=null;}if(previewB){previewB.Stop();previewB.clip=null;}
            gainA=gainB=0;audibleIndex=-1;ApplyAudio();
        }
        void ReleaseUnusedClips()
        {
            if(songs==null)return;for(int i=0;i<songs.Length;i++){var clip=songs[i].audio;if(i!=SelectedIndex && clip && (!previewA || previewA.clip!=clip) && (!previewB || previewB.clip!=clip))ReleaseClip(i);}
        }
        IEnumerator Crossfade(AudioSource incoming,AudioSource outgoing)
        {
            float oldGain=outgoing?Gain(outgoing):0;float duration=outgoing && outgoing.isPlaying ? .65f : .8f;
            for(float elapsed=0;elapsed<duration;elapsed+=Time.unscaledDeltaTime){float t=Mathf.SmoothStep(0,1,elapsed/duration);SetGain(incoming,t);if(outgoing)SetGain(outgoing,oldGain*(1-t));yield return null;}
            SetGain(incoming,1);if(outgoing){SetGain(outgoing,0);outgoing.Stop();outgoing.clip=null;}previewFade=null;ReleaseUnusedClips();
        }
        IEnumerator FadePreviewFailure()
        {
            float a=gainA,b=gainB;for(float elapsed=0;elapsed<.25f;elapsed+=Time.unscaledDeltaTime){float t=1-Mathf.SmoothStep(0,1,elapsed/.25f);gainA=a*t;gainB=b*t;ApplyAudio();yield return null;}
            if(previewA){previewA.Stop();previewA.clip=null;}if(previewB){previewB.Stop();previewB.clip=null;}gainA=gainB=0;audibleIndex=-1;previewFade=null;ReleaseUnusedClips();
        }
        void FadeFailedPreview(){if(previewFade!=null)StopCoroutine(previewFade);previewFade=StartCoroutine(FadePreviewFailure());}
        void Start(){if(!initialized)Open(null);}
        public void Open(GameObject fromMenu)
        {
            initialized=true;mainMenu=fromMenu;if(mainMenu)mainMenu.SetActive(false);gameObject.SetActive(true);
            openedFrame=Time.frameCount;
            RebuildFilter();SelectIndex(SelectedIndex,true);currencyText.text=currencyBalance.ToString("N0");
            EventSystem.current?.SetSelectedGameObject(startButton.gameObject);
        }
        void CancelLoad()
        {
            if(audioRoutine!=null){StopCoroutine(audioRoutine);audioRoutine=null;}
            if(audioRequest!=null){audioRequest.Abort();audioRequest.Dispose();audioRequest=null;}
            IsLoading=false;
        }
        void ReleaseClip(int index)
        {
            if(index<0 || index>=songs.Length)return;
            var item=songs[index];if(item.audio && !string.IsNullOrEmpty(item.audioPath)){Destroy(item.audio);item.audio=null;}
        }
        void OnDisable()
        {
            PlayerAudioSettings.Changed-=ApplyAudio;
            SongPackOwnership.Changed-=RefreshAcquisition;
            if(selectionRoutine!=null)StopCoroutine(selectionRoutine);selectionRoutine=null;IsAnimating=false;
            CancelLoad();StopPreviewAudio();
            if(!keepAudioForGame)ReleaseClip(SelectedIndex);keepAudioForGame=false;ReleaseUnusedClips();
        }
        public void Back(){gameObject.SetActive(false);if(mainMenu){mainMenu.SetActive(true);var view=mainMenu.GetComponent<StartScreenView>();if(view)EventSystem.current?.SetSelectedGameObject(view.startButton.gameObject);}}
        void Update()
        {
            if(Time.frameCount==openedFrame)return;
            if(Input.GetKeyDown(KeyCode.Escape)){Back();return;}
            if(Input.GetKeyDown(KeyCode.UpArrow))Step(-1);if(Input.GetKeyDown(KeyCode.DownArrow))Step(1);
            if(Input.GetKeyDown(KeyCode.LeftArrow))StepDifficulty(-1);
            if(Input.GetKeyDown(KeyCode.RightArrow))StepDifficulty(1);
            if(noticeUntil>0 && Time.unscaledTime>noticeUntil){notice.text="";noticeUntil=0;}
            if(!IsLoading && audibleIndex==SelectedIndex && previewSource.clip && previewSource.isPlaying){float time=previewSource.timeSamples/(float)previewSource.clip.frequency;progress.rectTransform.sizeDelta=new Vector2(160*time/previewSource.clip.length,3);previewText.text=(muted?"已静音  ":"试听中  ")+TimeLabel(time)+" / "+TimeLabel(previewSource.clip.length);}
        }
        static string TimeLabel(float seconds){int s=Mathf.Max(0,(int)seconds);return (s/60)+":"+(s%60).ToString("00");}
        void RebuildFilter()
        {
            filtered.Clear();for(int i=0;i<songs.Length;i++)if(Filter==0 || (songs[i].category&Filter)!=0)filtered.Add(i);
            if(filtered.Count>0 && !filtered.Contains(SelectedIndex))SelectedIndex=filtered[0];
            if(filterButtons!=null)for(int i=0;i<filterButtons.Length;i++)
            {
                var face=filterButtons[i].GetComponentInChildren<RoundedGraphic>();bool active=filterValues[i]==Filter;
                if(face){face.color=active?new Color(.73f,.72f,.94f):Color.white;face.bottomColor=active?new Color(.70f,.88f,.96f):new Color(.92f,.94f,.99f);}
            }
        }
        public void SetFilter(int value)
        {
            if(Array.IndexOf(filterValues,value)<0)return;
            int old=SelectedIndex;Filter=value;RebuildFilter();
            if(old!=SelectedIndex)CancelLoad();
            if(filtered.Count==0){foreach(var row in rows)row.gameObject.SetActive(false);RefreshDifficultyButtons(0);CancelLoad();StopPreviewAudio();ReleaseUnusedClips();title.text="暂无歌曲";artist.text="";indexText.text="00 / 00";startButton.interactable=false;return;}
            startButton.interactable=true;SelectIndex(SelectedIndex,true);
        }
        public void Step(int delta){if(filtered.Count==0)return;int slot=filtered.IndexOf(SelectedIndex);SelectIndex(filtered[Mathf.Clamp(slot+delta,0,filtered.Count-1)]);}
        public void Scroll(float delta){if(Mathf.Abs(delta)<.01f || Time.unscaledTime<scrollGate)return;scrollGate=Time.unscaledTime+.14f;Step(delta<0?1:-1);}
        public void SelectIndex(int index,bool instant=false)
        {
            if(songs==null || songs.Length==0 || filtered.Count==0 || !filtered.Contains(index))return;
            if(index==SelectedIndex && !instant)return;
            int previous=SelectedIndex,direction=filtered.IndexOf(index)>=filtered.IndexOf(previous)?1:-1;
            if(selectionRoutine!=null)StopCoroutine(selectionRoutine);CancelLoad();
            SelectedIndex=index;ReleaseUnusedClips();progress.rectTransform.sizeDelta=new Vector2(0,3);
            notice.text="";noticeUntil=0;var song=songs[index];title.text=song.title;artist.text=song.artist+"  ·  "+song.duration;if(retryButton)retryButton.gameObject.SetActive(false);
            indexText.text=(filtered.IndexOf(index)+1).ToString("00")+" / "+filtered.Count.ToString("00");
            // Keep a supported difficulty, otherwise select the easiest available chart.
            if(song.availableDifficulties!=0 && (song.availableDifficulties&(1<<SelectedDifficulty))==0)
                for(int d=0;d<5;d++)if((song.availableDifficulties&(1<<d))!=0){SelectedDifficulty=d;break;}
            RefreshRows();SelectDifficulty(SelectedDifficulty);
            selectionRoutine=StartCoroutine(AnimateCards(previous,direction,instant));audioRoutine=StartCoroutine(Preview(index));
        }
        void RefreshRows()
        {
            int selected=filtered.IndexOf(SelectedIndex),middle=rows.Length/2;
            for(int slot=0;slot<rows.Length;slot++)
            {
                int delta=slot-middle,position=selected+delta;var row=rows[slot];bool valid=position>=0 && position<filtered.Count;row.gameObject.SetActive(valid);if(!valid)continue;
                int index=filtered[position];var item=songs[index];row.GetComponent<SongListItem>().Bind(this,index);
                row.anchoredPosition=new Vector2(38-Mathf.Min(Mathf.Abs(delta),2)*42,-delta*150);
                row.localScale=Vector3.one*(delta==0?1:.93f);rowGroups[slot].alpha=delta==0?1:Mathf.Abs(delta)==1?.72f:.42f;
                rowFaces[slot].color=delta==0?new Color(.86f,.83f,.97f):new Color(1,1,1,.85f);
                rowFaces[slot].bottomColor=delta==0?new Color(.83f,.93f,.97f):new Color(.95f,.96f,1,.85f);
                var mark=row.Find("SelectedMark");if(mark)mark.gameObject.SetActive(delta==0);
                row.Find("Number").GetComponent<Text>().text=(position+1).ToString("00");
                row.Find("SongTitle").GetComponent<Text>().text=item.title;row.Find("Artist").GetComponent<Text>().text=item.artist;
                row.Find("Cover").GetComponent<RawImage>().texture=item.cover;
                rowScores[slot].text=SongPackCatalog.LockedPack(item.id)!=null?"获取":"最高分  "+Score(item.id,SelectedDifficulty);
            }
        }
        IEnumerator Preview(int index)
        {
            var song=songs[index];IsLoading=true;RefreshAvailability();previewText.text="准备试听…";yield return new WaitForSecondsRealtime(.25f);
            if(index!=SelectedIndex)yield break;
            if(!song.audio && !string.IsNullOrEmpty(song.audioPath))
            {
                IsLoading=true;string playbackPath=song.audioPath;for(int d=0;d<5;d++){var chart=DrumChart.Load(song.id,d);if(chart!=null && !string.IsNullOrEmpty(chart.audioPath)){playbackPath=chart.audioPath;break;}}playbackPath=MusicPlayback.Resolve(playbackPath);string path=Path.Combine(Application.streamingAssetsPath,playbackPath).Replace('\\','/');
                string url=path.Contains("://")?path:new Uri(path).AbsoluteUri;
                audioRequest=UnityWebRequestMultimedia.GetAudioClip(url,playbackPath.EndsWith(".ogg",StringComparison.OrdinalIgnoreCase)?AudioType.OGGVORBIS:AudioType.MPEG);var handler=(DownloadHandlerAudioClip)audioRequest.downloadHandler;handler.compressed=true;
                yield return audioRequest.SendWebRequest();
                if(index!=SelectedIndex)yield break;
                if(audioRequest.result!=UnityWebRequest.Result.Success){previewText.text="音频加载失败";Debug.LogWarning("Song load failed: "+audioRequest.error);audioRequest.Dispose();audioRequest=null;IsLoading=false;audioRoutine=null;if(retryButton)retryButton.gameObject.SetActive(true);RefreshAvailability();FadeFailedPreview();yield break;}
                song.audio=DownloadHandlerAudioClip.GetContent(audioRequest);audioRequest.Dispose();audioRequest=null;
            }
            if(!song.audio){IsLoading=false;RefreshAvailability();previewText.text="音频不可用";audioRoutine=null;if(retryButton)retryButton.gameObject.SetActive(true);FadeFailedPreview();yield break;}
            // Finish an in-flight blend before reusing its channel, avoiding a volume step during fast scrolling.
            while(previewFade!=null){if(index!=SelectedIndex)yield break;yield return null;}
            if(index!=SelectedIndex)yield break;
            var outgoing=previewSource;var incoming=outgoing==previewA?previewB:previewA;incoming.Stop();incoming.clip=song.audio;incoming.pitch=1;incoming.mute=muted;SetGain(incoming,0);
            incoming.timeSamples=Mathf.Clamp(Mathf.RoundToInt(Mathf.Min(song.previewStart,Mathf.Max(0,song.audio.length-1))*song.audio.frequency),0,song.audio.samples-1);
            previewSource=incoming;audibleIndex=index;incoming.Play();IsLoading=false;RefreshAvailability();previewFade=StartCoroutine(Crossfade(incoming,outgoing));audioRoutine=null;
        }
        IEnumerator AnimateCards(int previous,int direction,bool instant)
        {
            IsAnimating=!instant;int p=filtered.IndexOf(SelectedIndex);
            for(int i=0;i<3;i++){bool valid=p+i<filtered.Count;cards[i].gameObject.SetActive(valid);if(valid)cardImages[i].texture=songs[filtered[p+i]].cover;}
            cardImages[3].texture=songs[Mathf.Clamp(previous,0,songs.Length-1)].cover;cards[3].gameObject.SetActive(!instant);
            float elapsed=0,duration=instant?0:.34f;
            do{elapsed+=Time.unscaledDeltaTime;float t=duration==0?1:Mathf.Clamp01(elapsed/duration);float e=1-Mathf.Pow(1-t,3);
                for(int i=2;i>=0;i--){cards[i].anchoredPosition=new Vector2(i*17,-i*12)+new Vector2((1-e)*-direction*70,0);cards[i].localRotation=Quaternion.Euler(0,0,Mathf.Lerp(i==0?direction*13:i*7,i*7,e));cards[i].localScale=Vector3.one*(1-i*.035f);}
                cards[3].anchoredPosition=new Vector2(direction*e*390,e*35);cards[3].localRotation=Quaternion.Euler(0,0,-direction*e*18);cards[3].GetComponent<CanvasGroup>().alpha=1-e;
                if(t>=1)break;yield return null;}while(true);
            cards[3].gameObject.SetActive(false);IsAnimating=false;selectionRoutine=null;
        }
        public void SelectDifficulty(int index)
        {
            SelectedDifficulty=Mathf.Clamp(index,0,4);
            if(filtered.Count>0){int mask=songs[SelectedIndex].availableDifficulties;if(mask!=0 && (mask&(1<<SelectedDifficulty))==0)for(int d=0;d<difficultyButtons.Length;d++)if((mask&(1<<d))!=0){SelectedDifficulty=d;break;}}
            for(int i=0;i<difficultyButtons.Length;i++){var face=difficultyButtons[i].GetComponentInChildren<RoundedGraphic>(true);face.color=i==SelectedDifficulty?new Color(.75f,.72f,.92f):Color.white;face.bottomColor=i==SelectedDifficulty?new Color(.76f,.88f,.94f):new Color(.94f,.95f,.99f);}
            if(filtered.Count>0){scoreText.text="BEST  "+Score(songs[SelectedIndex].id,SelectedDifficulty);RefreshRows();}
            RefreshAvailability();
        }
        string Score(string id,int difficulty){var chart=DrumChart.Load(id,difficulty);var record=chart!=null?DrumRecords.Read(id,difficulty,chart.revision):null;return record!=null?record.score.ToString("N0"):"—";}
        void StepDifficulty(int direction)
        {
            if(filtered.Count==0 || SongPackCatalog.LockedPack(songs[SelectedIndex].id)!=null)return;
            int mask=songs[SelectedIndex].availableDifficulties;
            for(int d=SelectedDifficulty+direction;d>=0 && d<difficultyButtons.Length;d+=direction)
                if((mask&(1<<d))!=0){SelectDifficulty(d);return;}
        }
        void RefreshDifficultyButtons(int mask)
        {
            int count=0;for(int i=0;i<difficultyButtons.Length;i++)if((mask&(1<<i))!=0)count++;
            float center=(difficultyPositions[0].x+difficultyPositions[difficultyPositions.Length-1].x)*.5f;
            float spacing=difficultyPositions.Length>1?(difficultyPositions[difficultyPositions.Length-1].x-difficultyPositions[0].x)/(difficultyPositions.Length-1):0;
            int visible=0;
            for(int i=0;i<difficultyButtons.Length;i++)
            {
                var button=difficultyButtons[i];bool available=(mask&(1<<i))!=0;
                if(!available && EventSystem.current && EventSystem.current.currentSelectedGameObject==button.gameObject)EventSystem.current.SetSelectedGameObject(null);
                button.gameObject.SetActive(available);button.interactable=available;
                if(available){var position=difficultyPositions[i];position.x=center+(visible++-(count-1)*.5f)*spacing;((RectTransform)button.transform).anchoredPosition=position;}
            }
        }
        void RefreshAvailability()
        {
            if(songs==null || songs.Length==0 || filtered.Count==0){RefreshDifficultyButtons(0);startButton.interactable=false;return;}var song=songs[SelectedIndex];
            RefreshDifficultyButtons(song.availableDifficulties);
            var locked=SongPackCatalog.LockedPack(song.id);
            if(locked!=null){foreach(var button in difficultyButtons)button.interactable=false;startButton.interactable=true;if(actionLabel)actionLabel.text="获取";if(availabilityText)availabilityText.text=locked.title+"曲包  ·  "+locked.Price;return;}
            if(actionLabel)actionLabel.text="开始";
            bool chart=(song.availableDifficulties&(1<<SelectedDifficulty))!=0;startButton.interactable=chart && song.audio && !IsLoading;
            var selectedChart=chart?DrumChart.Load(song.id,SelectedDifficulty):null;
            if(availabilityText)availabilityText.text=!chart?"仅试听 · 谱面制作中":IsLoading?"正在加载歌曲…":!song.audio?"请重试加载音频":(selectedChart!=null && selectedChart.reviewOnly?"内部测试 · ":"整曲 · ")+new[]{"入门","简单","普通","困难","极限"}[SelectedDifficulty];
        }
        public void RetryAudio(){if(filtered.Count>0){StopPreviewAudio();CancelLoad();ReleaseClip(SelectedIndex);SelectIndex(SelectedIndex,true);}}
        public void TogglePreview(){muted=!muted;ApplyAudio();ShowNotice(muted?"试听已静音":"试听已开启");}
        public void Currency(){onCurrencyRequested.Invoke();ShowNotice("货币获取功能尚未开放");}
        public void Launch()
        {
            if(filtered.Count>0){var locked=SongPackCatalog.LockedPack(songs[SelectedIndex].id);if(locked!=null){if(songPackShopPrefab){if(!songPackShop)songPackShop=Instantiate(songPackShopPrefab).GetComponent<SongPackShopView>();songPackShop.Open(gameObject,locked.id,songs[SelectedIndex].id);}else ShowNotice("曲包商店暂不可用");return;}}
            if(filtered.Count==0)return;var selectedChart=DrumChart.Load(songs[SelectedIndex].id,SelectedDifficulty);if(selectedChart==null){ShowNotice("该歌曲当前仅支持试听");return;}if(IsLoading || !songs[SelectedIndex].audio){ShowNotice(IsLoading?"音频加载中，请稍候":"音频暂不可用");return;}if(Mathf.Abs(songs[SelectedIndex].audio.length-selectedChart.audioDuration)>.25f){ShowNotice("歌曲文件与谱面不匹配，请重新加载");return;}
            LastLaunchSong=SelectedIndex;LastLaunchDifficulty=SelectedDifficulty;StopPreviewAudio();onSongStartRequested.Invoke(songs[SelectedIndex].id,SelectedDifficulty);
            if(drumPracticePrefab){if(!drumPractice)drumPractice=Instantiate(drumPracticePrefab).GetComponent<YYOU.DrumPractice.DrumPracticeView>();keepAudioForGame=true;drumPractice.Begin(this,SelectedDifficulty);}
            else ShowNotice("玩法资源尚未接入");
        }
        public bool LaunchReview(SongEntry entry,DrumChart chart)
        {
            if(!drumPracticePrefab || entry==null || !entry.audio || chart==null || !chart.reviewOnly){ShowNotice("样段暂不可用");return false;}
            StopPreviewAudio();if(!drumPractice)drumPractice=Instantiate(drumPracticePrefab).GetComponent<DrumPracticeView>();keepAudioForGame=false;
            if(drumPractice.BeginReview(this,entry,chart))return true;ShowNotice("样段音频与谱面不匹配");RetryAudio();return false;
        }
        void ShowNotice(string value){notice.text=value;noticeUntil=Time.unscaledTime+3;}
        public void FocusSong(string id){SetFilter(0);for(int i=0;i<songs.Length;i++)if(songs[i].id==id){SelectIndex(i,true);return;}}
    }
}
