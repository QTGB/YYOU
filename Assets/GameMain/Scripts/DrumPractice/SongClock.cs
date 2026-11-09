using System;
using UnityEngine;
namespace YYOU.DrumPractice
{
    public sealed class SongClock
    {
        AudioSource source;AudioClip clip;double origin,resumeAt;float parked,duration;
        public bool Paused{get;private set;}
        public bool Preparing=>!Paused && (AudioSettings.dspTime<resumeAt || SongSeconds<0);
        public float SongSeconds=>At(AudioSettings.dspTime);
        public float PreparationRemaining=>AudioSettings.dspTime<resumeAt?(float)(resumeAt-AudioSettings.dspTime):Mathf.Max(0,-SongSeconds);
        public float At(double dsp)=>Paused || dsp<resumeAt?parked:Mathf.Clamp((float)(dsp-origin),-3,duration);
        public void Start(AudioSource audio,AudioClip music)
        {source=audio;clip=music;duration=clip?clip.length:42;parked=-3;Paused=false;resumeAt=AudioSettings.dspTime;origin=resumeAt+3;if(source){source.Stop();source.clip=clip;source.loop=false;if(clip){source.timeSamples=0;source.PlayScheduled(origin);}}}
        public void Pause(){if(Paused)return;parked=SongSeconds;Paused=true;if(source)source.Stop();}
        public void Resume()
        {if(!Paused)return;Paused=false;resumeAt=AudioSettings.dspTime+1;origin=resumeAt-parked;if(source && clip){source.Stop();source.timeSamples=Mathf.Clamp(Mathf.RoundToInt(Mathf.Max(0,parked)*clip.frequency),0,clip.samples-1);source.PlayScheduled(resumeAt+Math.Max(0,-parked));}}
        public void Stop(){parked=SongSeconds;if(source)source.Stop();Paused=true;}
    }
}
