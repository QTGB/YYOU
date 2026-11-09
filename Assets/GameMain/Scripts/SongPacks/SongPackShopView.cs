using System;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using YYOU.StartScreen;
using YYOU.SongSelection;
namespace YYOU.SongPacks
{
    [Serializable] public sealed class PackCardWidgets { public PackCardButton button;public Text price;public RoundedGraphic stripe; }
    [Serializable] public sealed class PackTrackWidgets { public Button button;public RawImage cover;public Text title,artist,duration,status;public PackGlyph glyph;public RoundedGraphic ring; }
    public sealed class SongPackShopView:MonoBehaviour
    {
        public SongPackCatalog catalog;
        public GameObject listPage,detailPage;
        public PackCarousel carousel;
        public Button back,previous,next,buy,goToSelection,previewToggle;
        public PackCardWidgets[] cards;
        public PackTrackWidgets[] tracks;
        public PackArtworkGraphic detailArtwork;
        public Text detailArtworkTitle,detailArtworkTag;
        public Text heading,subheading,ownedCount,detailTitle,detailInfo,buyLabel,toast,previewTitle,previewArtist,previewTime;
        public RawImage previewCover;
        public RectTransform previewProgress,previewTrack;
        public PackGlyph previewGlyph;
        public PackPreviewPlayer player;
        public GameObject Origin{get;private set;}
        public SongPack SelectedPack{get;private set;}
        public string SelectedSongId{get;private set;}
        public bool InDetails=>detailPage && detailPage.activeSelf;
        bool deepLink,scrollTween;float scrollTarget,toastUntil;int openedFrame,focused,lastSecond=-1;
        void Awake()
        {
            back.onClick.AddListener(Back);previous.onClick.AddListener(()=>ScrollToPack(focused-1));next.onClick.AddListener(()=>ScrollToPack(focused+1));
            buy.onClick.AddListener(BuySelected);goToSelection.onClick.AddListener(GoToSelection);previewToggle.onClick.AddListener(()=>player.Toggle());
            for(int i=0;i<cards.Length;i++){int index=i;cards[i].button.onClick.AddListener(()=>ShowPack(index));}
            for(int i=0;i<tracks.Length;i++){int index=i;tracks[i].button.onClick.AddListener(()=>PreviewSong(index));}
        }
        void OnEnable(){SongPackOwnership.Changed+=RefreshOwnership;player.Changed+=RefreshPreview;RefreshOwnership();RefreshPreview();}
        void OnDisable(){SongPackOwnership.Changed-=RefreshOwnership;player.Changed-=RefreshPreview;player.StopAll();scrollTween=false;}
        public void Open(GameObject from,string packId=null,string songId=null)
        {
            Origin=from;if(from)from.SetActive(false);gameObject.SetActive(true);openedFrame=Time.frameCount;toast.text="";toastUntil=0;
            deepLink=!string.IsNullOrEmpty(packId);SelectedSongId=songId;ShowList(false);
            if(deepLink){int index=Array.FindIndex(catalog.packs,p=>p.id==packId);if(index>=0)ShowPack(index,songId);}
            RefreshOwnership();RefreshPreview();EventSystem.current?.SetSelectedGameObject(back.gameObject);
        }
        public void ShowList(bool stopAudio=true)
        {
            if(stopAudio)player.StopAll();listPage.SetActive(true);detailPage.SetActive(false);SelectedPack=null;heading.text="曲包商店";subheading.text="4 个曲包  ·  12 首歌曲";lastSecond=-1;
        }
        public void ShowPack(int index){ShowPack(index,null);}
        void ShowPack(int index,string focusSong)
        {
            if(index<0 || index>=catalog.packs.Length)return;player.StopAll();focused=index;SelectedPack=catalog.packs[index];
            SelectedSongId=Array.IndexOf(SelectedPack.songIds,focusSong)>=0?focusSong:SelectedPack.songIds[0];
            listPage.SetActive(false);detailPage.SetActive(true);heading.text="曲包详情";subheading.text="曲包商店  /  "+SelectedPack.title;detailTitle.text=SelectedPack.title;
            detailArtwork.variant=index;detailArtwork.accent=SelectedPack.accent;detailArtwork.SetVerticesDirty();detailArtworkTitle.text=SelectedPack.title;detailArtworkTag.text=SelectedPack.subtitle;
            int total=0;
            for(int i=0;i<3;i++)
            {
                var song=catalog.FindSong(SelectedPack.songIds[i]);tracks[i].cover.texture=song.cover;
                tracks[i].title.text=song.title;tracks[i].artist.text=song.artist;tracks[i].duration.text=song.duration;
                var d=song.duration.Split(':');if(d.Length==2 && int.TryParse(d[0],out var m) && int.TryParse(d[1],out var s))total+=m*60+s;
            }
            detailInfo.text="3 首歌曲  ·  "+(total/60)+":"+((total%60).ToString("00"));
            RefreshOwnership();RefreshPreview();
        }
        public void PreviewSong(int index)
        {
            if(SelectedPack==null || index<0 || index>=3)return;SelectedSongId=SelectedPack.songIds[index];player.Play(catalog.FindSong(SelectedSongId));RefreshPreview();
        }
        public void BuySelected()
        {
            if(SelectedPack==null)return;
            if(SongPackOwnership.Purchase(SelectedPack.id)){toast.text="已获取 · 演示购买，无需付款";toastUntil=Time.unscaledTime+2;}
            RefreshOwnership();
        }
        void RefreshOwnership()
        {
            if(!catalog || cards==null)return;int owned=0;
            for(int i=0;i<cards.Length;i++){bool yes=SongPackOwnership.Owns(catalog.packs[i].id);if(yes)owned++;cards[i].price.text=yes?"已拥有":catalog.packs[i].Price;cards[i].price.color=yes?new Color(.27f,.60f,.53f):new Color(.22f,.34f,.46f);}
            if(ownedCount)ownedCount.text="已拥有  "+owned+" / "+cards.Length;
            if(SelectedPack!=null){bool yes=SongPackOwnership.Owns(SelectedPack.id);buy.interactable=!yes;buyLabel.text=yes?"已拥有":"模拟购买  ·  "+SelectedPack.Price;goToSelection.gameObject.SetActive(yes);}
        }
        void RefreshPreview()
        {
            lastSecond=-1;
            if(SelectedPack!=null)for(int i=0;i<3;i++)
            {
                string id=SelectedPack.songIds[i];bool playing=player.ActiveSong!=null && player.ActiveSong.id==id && player.IsPlaying;
                bool loading=player.IsLoading && player.RequestedId==id,failed=player.Failed && player.RequestedId==id;
                tracks[i].status.text=loading?"正在加载…":failed?"点击重试":playing?"正在试听":"点击试听";
                tracks[i].glyph.Set(playing?PackGlyphKind.Pause:PackGlyphKind.Play);
                tracks[i].ring.color=id==SelectedSongId?SelectedPack.accent:new Color(.83f,.89f,.94f,.6f);tracks[i].ring.bottomColor=tracks[i].ring.color;
                tracks[i].ring.SetVerticesDirty();
            }
            var song=player.ActiveSong;
            previewTitle.text=song!=null?song.title:"选择一首歌曲试听";previewArtist.text=song!=null?song.artist:"在曲包详情中点击歌曲封面";
            previewCover.enabled=song!=null;if(song!=null)previewCover.texture=song.cover;
            previewToggle.interactable=song!=null && !player.IsLoading;previewGlyph.Set(player.IsPlaying?PackGlyphKind.Pause:PackGlyphKind.Play);
            if(player.Failed){toast.text="试听加载失败，点击歌曲重试";toastUntil=Time.unscaledTime+3;}
        }
        public void CancelScroll(){scrollTween=false;}
        public void ScrollToPack(int index)
        {
            focused=Mathf.Clamp(index,0,catalog.packs.Length-1);
            float max=Mathf.Max(0,carousel.content.rect.width-carousel.viewport.rect.width);
            scrollTarget=-Mathf.Clamp(focused*432,0,max);carousel.StopMovement();scrollTween=true;
        }
        void Update()
        {
            if(Time.frameCount!=openedFrame){if(Input.GetKeyDown(KeyCode.Escape)){Back();return;}if(!InDetails){if(Input.GetKeyDown(KeyCode.LeftArrow))ScrollToPack(focused-1);if(Input.GetKeyDown(KeyCode.RightArrow))ScrollToPack(focused+1);}}
            if(scrollTween && listPage.activeSelf){var p=carousel.content.anchoredPosition;p.x=Mathf.Lerp(p.x,scrollTarget,1-Mathf.Exp(-18*Time.unscaledDeltaTime));if(Mathf.Abs(p.x-scrollTarget)<.2f){p.x=scrollTarget;scrollTween=false;}carousel.content.anchoredPosition=p;}
            if(listPage.activeSelf){float max=Mathf.Max(0,carousel.content.rect.width-carousel.viewport.rect.width);previous.interactable=max>1 && carousel.content.anchoredPosition.x<-.5f;next.interactable=max>1 && carousel.content.anchoredPosition.x> -max+.5f;}
            if(toastUntil>0 && Time.unscaledTime>=toastUntil){toast.text="";toastUntil=0;}
            var source=player.ActiveSource;float position=source && source.clip?source.timeSamples/(float)source.clip.frequency:0,duration=source && source.clip?source.clip.length:0;
            previewProgress.sizeDelta=new Vector2(previewTrack.rect.width*(duration>0?position/duration:0),3);
            int second=(int)position;if(second!=lastSecond){lastSecond=second;previewTime.text=duration>0?TimeLabel(position)+" / "+TimeLabel(duration):"—:—";}
        }
        static string TimeLabel(float t){int seconds=Mathf.Max(0,(int)t);return seconds/60+":"+(seconds%60).ToString("00");}
        public void Back(){if(InDetails && !deepLink){ShowList();return;}Close();}
        public void Close()
        {
            var origin=Origin;gameObject.SetActive(false);
            if(origin){var selection=origin.GetComponent<SongSelectionView>();if(selection)selection.Open(selection.mainMenu);else origin.SetActive(true);}
        }
        public void GoToSelection()
        {
            if(SelectedPack==null || !SongPackOwnership.Owns(SelectedPack.id))return;
            string id=SelectedSongId??SelectedPack.songIds[0];var origin=Origin;gameObject.SetActive(false);
            if(!origin)return;var selection=origin.GetComponent<SongSelectionView>();
            if(selection){selection.Open(selection.mainMenu);selection.FocusSong(id);return;}
            var start=origin.GetComponent<StartScreenView>();origin.SetActive(true);if(start)start.OpenSong(id);
        }
    }
}
