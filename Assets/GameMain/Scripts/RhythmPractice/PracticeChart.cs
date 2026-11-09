using System;
using UnityEngine;
namespace YYOU.RhythmPractice
{
    [Serializable] public sealed class ChartNote
    {
        public int lane;
        public float time,duration;
        public string source;
    }
    [Serializable] public sealed class PracticeChart
    {
        public int version,difficulty;
        public string songId,label;
        public float bpm,gridOffset,audioDuration,travelSeconds;
        public ChartNote[] notes;
        public static PracticeChart Load(string songId,int difficulty)
        {
            var asset=Resources.Load<TextAsset>("Charts/"+songId+"/"+difficulty);
            if(!asset)return null;
            var chart=JsonUtility.FromJson<PracticeChart>(asset.text.TrimStart('\uFEFF'));
            if(chart==null || chart.notes==null || chart.notes.Length==0 || chart.songId!=songId || chart.difficulty!=difficulty)throw new InvalidOperationException("Invalid chart: "+songId+"/"+difficulty);
            return chart;
        }
    }
}
