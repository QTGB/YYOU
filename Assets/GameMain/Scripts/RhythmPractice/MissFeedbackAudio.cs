using UnityEngine;
namespace YYOU.RhythmPractice
{
    [RequireComponent(typeof(AudioSource))]
    public sealed class MissFeedbackAudio:MonoBehaviour
    {
        public AudioSource Source {get;private set;}
        float nextAllowed;
        void Awake(){Source=GetComponent<AudioSource>();Source.playOnAwake=false;Source.loop=false;Source.spatialBlend=0;Source.volume=.55f;Source.clip=Resources.Load<AudioClip>("Audio/Miss");}
        public void Play(){if(!Source || !Source.clip || Time.unscaledTime<nextAllowed)return;nextAllowed=Time.unscaledTime+.06f;Source.PlayOneShot(Source.clip);}
        public void Stop(){if(Source)Source.Stop();}
        void OnDisable(){Stop();nextAllowed=0;}
    }
}
