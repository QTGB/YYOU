using System;
using UnityEngine;
namespace YYOU.DrumPractice
{
    [Serializable] public sealed class DrumChartEntry {public DrumNoteKind kind;public float time,duration;public string source;}
    [Serializable] public sealed class DrumChart
    {
        public int version,revision,difficulty;public string songId,title,status,label,audioPath;public float audioDuration,travelSeconds,bpm,gridOffset;public DrumChartEntry[] notes;
        public bool reviewOnly;public float sourceExcerptStart;
        public static DrumChart Load(string songId){var file=Resources.Load<TextAsset>("DrumCharts/"+songId);return file?JsonUtility.FromJson<DrumChart>(file.text.TrimStart('\uFEFF')):null;}
        static readonly System.Collections.Generic.Dictionary<string,DrumChart> cache=new System.Collections.Generic.Dictionary<string,DrumChart>();
        public static DrumChart Load(string songId,int difficulty)
        {
            string key=songId+"/"+difficulty;if(cache.TryGetValue(key,out var result))return result;
            var file=Resources.Load<TextAsset>("DrumCharts/"+key);if(!file){cache[key]=null;return null;}
            try{result=JsonUtility.FromJson<DrumChart>(file.text.TrimStart('\uFEFF'));if(result.version<2 || result.songId!=songId || result.difficulty!=difficulty || result.notes==null || result.notes.Length==0)result=null;}
            catch(Exception e){Debug.LogWarning("Invalid drum chart "+key+": "+e.Message);result=null;}cache[key]=result;return result;
        }
        public static int AvailableMask(string id){int mask=0;for(int d=0;d<5;d++)if(Load(id,d)!=null)mask|=1<<d;return mask;}
        public static void ClearCache(){cache.Clear();}
    }
}
