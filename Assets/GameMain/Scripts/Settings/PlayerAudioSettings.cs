using System;
using UnityEngine;
namespace YYOU.Settings
{
    public static class PlayerAudioSettings
    {
        static string prefix="YYOU.Options.v1.";
        public static event Action Changed;
        public static float Music=>PlayerPrefs.GetFloat(prefix+"music",.55f);
        public static float Hits=>PlayerPrefs.GetFloat(prefix+"hits",1f);
        public static float Miss=>PlayerPrefs.GetFloat(prefix+"miss",.55f);
        public static float OffsetMs=>PlayerPrefs.GetFloat(prefix+"offset",0);
        public static float TravelSeconds=>PlayerPrefs.GetFloat(prefix+"travel",3);
        public static bool TutorialSeen=>PlayerPrefs.GetInt(prefix+"tutorial",0)==1;
        public static void SetMusic(float v)=>Set("music",Mathf.Clamp01(v));
        public static void SetHits(float v)=>Set("hits",Mathf.Clamp01(v));
        public static void SetMiss(float v)=>Set("miss",Mathf.Clamp01(v));
        public static void SetOffset(float v)=>Set("offset",Mathf.Clamp(Mathf.Round(v/5)*5,-250,250));
        public static void SetTravel(float v)=>Set("travel",Mathf.Clamp(v,1.5f,4.5f));
        static void Set(string name,float v){PlayerPrefs.SetFloat(prefix+name,v);PlayerPrefs.Save();Changed?.Invoke();}
        public static void MarkTutorial(){PlayerPrefs.SetInt(prefix+"tutorial",1);PlayerPrefs.Save();}
#if UNITY_EDITOR
        public static void UseTestScope(string value){if(value!=null && !value.StartsWith("YYOU.Test."))throw new ArgumentException("Test scope required");prefix=value??"YYOU.Options.v1.";Changed?.Invoke();}
#endif
    }
}
