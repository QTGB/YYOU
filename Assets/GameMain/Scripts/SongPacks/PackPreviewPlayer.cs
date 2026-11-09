using System;
using System.Collections;
using System.IO;
using UnityEngine;
using UnityEngine.Networking;
using YYOU.Settings;
using YYOU.SongSelection;
namespace YYOU.SongPacks
{
    public sealed class PackPreviewPlayer:MonoBehaviour
    {
        public AudioSource first,second;
        public SongEntry ActiveSong{get;private set;}
        public string RequestedId{get;private set;}
        public bool IsLoading{get;private set;}
        public bool Failed{get;private set;}
        public bool IsPlaying=>active && active.isPlaying;
        public AudioSource ActiveSource=>active;
        public event Action Changed;
        AudioSource active;UnityWebRequest request;Coroutine loading,fade;AudioClip pending;
        float gainA,gainB;bool paused;
        void OnEnable(){PlayerAudioSettings.Changed+=ApplyVolume;ApplyVolume();}
        void OnDisable(){PlayerAudioSettings.Changed-=ApplyVolume;StopAll();}
        void ApplyVolume(){if(first)first.volume=gainA*PlayerAudioSettings.Music;if(second)second.volume=gainB*PlayerAudioSettings.Music;}
        void Release(AudioSource source){if(!source)return;source.Stop();var clip=source.clip;source.clip=null;if(clip)Destroy(clip);}
        void CancelLoading(){if(loading!=null)StopCoroutine(loading);loading=null;if(request!=null){request.Abort();request.Dispose();request=null;}if(pending){Destroy(pending);pending=null;}IsLoading=false;}
        public void Play(SongEntry song)
        {
            if(song==null)return;
            if(IsLoading && RequestedId==song.id)return;
            if(!IsLoading && ActiveSong!=null && ActiveSong.id==song.id && active && active.clip){Toggle();return;}
            CancelLoading();RequestedId=song.id;Failed=false;IsLoading=true;Changed?.Invoke();loading=StartCoroutine(Load(song));
        }
        IEnumerator Load(SongEntry song)
        {
            yield return new WaitForSecondsRealtime(.12f);
            string playbackPath=MusicPlayback.Resolve(song.audioPath);string path=Path.Combine(Application.streamingAssetsPath,playbackPath).Replace('\\','/');
            request=UnityWebRequestMultimedia.GetAudioClip(path.Contains("://")?path:new Uri(path).AbsoluteUri,playbackPath.EndsWith(".ogg",StringComparison.OrdinalIgnoreCase)?AudioType.OGGVORBIS:AudioType.MPEG);
            ((DownloadHandlerAudioClip)request.downloadHandler).compressed=true;yield return request.SendWebRequest();
            if(request.result!=UnityWebRequest.Result.Success){request.Dispose();request=null;IsLoading=false;Failed=true;loading=null;Changed?.Invoke();yield break;}
            pending=DownloadHandlerAudioClip.GetContent(request);request.Dispose();request=null;
            while(fade!=null)yield return null;
            var outgoing=active;var incoming=active==first?second:first;Release(incoming);incoming.clip=pending;pending=null;
            incoming.timeSamples=Mathf.Clamp(Mathf.RoundToInt(Mathf.Min(song.previewStart,Mathf.Max(0,incoming.clip.length-1))*incoming.clip.frequency),0,incoming.clip.samples-1);
            incoming.pitch=1;incoming.loop=true;incoming.spatialBlend=0;
            if(incoming==first)gainA=0;else gainB=0;ApplyVolume();incoming.Play();active=incoming;ActiveSong=song;paused=false;IsLoading=false;loading=null;Changed?.Invoke();
            fade=StartCoroutine(Blend(incoming,outgoing));
        }
        IEnumerator Blend(AudioSource incoming,AudioSource outgoing)
        {
            float old=outgoing?(outgoing==first?gainA:gainB):0;float duration=outgoing && outgoing.isPlaying ? .65f : .8f;
            for(float time=0;time<duration;time+=Time.unscaledDeltaTime){float t=Mathf.SmoothStep(0,1,time/duration);if(incoming==first){gainA=t;gainB=old*(1-t);}else{gainB=t;gainA=old*(1-t);}ApplyVolume();yield return null;}
            if(incoming==first){gainA=1;gainB=0;}else{gainB=1;gainA=0;}Release(outgoing);ApplyVolume();fade=null;
        }
        public void Toggle()
        {
            if(IsLoading)return;if(Failed && RequestedId!=null){Play(SongPackCatalog.Current.FindSong(RequestedId));return;}
            if(!active || !active.clip)return;
            if(fade!=null){StopCoroutine(fade);fade=null;Release(active==first?second:first);gainA=active==first?1:0;gainB=active==second?1:0;ApplyVolume();}
            paused=!paused;if(paused)active.Pause();else active.UnPause();Changed?.Invoke();
        }
        public void StopAll()
        {
            CancelLoading();if(fade!=null)StopCoroutine(fade);fade=null;
            Release(first);Release(second);active=null;ActiveSong=null;RequestedId=null;Failed=false;gainA=gainB=0;paused=false;ApplyVolume();Changed?.Invoke();
        }
    }
}
