using System;
using UnityEngine;
namespace YYOU.DrumPractice
{
    [Serializable] public sealed class DrumRecord {public int score,maxCombo,perfect,good,miss,rollHits;public bool fullCombo;}
    public static class DrumRecords
    {
        static string prefix="YYOU.Drum.Record.v1.";
        static string Key(string id,int difficulty,int revision)=>prefix+id+"."+difficulty+"."+revision;
        public static DrumRecord Read(string id,int difficulty,int revision){string data=PlayerPrefs.GetString(Key(id,difficulty,revision),"");try{return data.Length==0?null:JsonUtility.FromJson<DrumRecord>(data);}catch{return null;}}
        public static void Save(DrumChart chart,DrumSession session)
        {if(chart==null || chart.reviewOnly || chart.version<2 || !session.Finished)return;var old=Read(chart.songId,chart.difficulty,chart.revision);if(old!=null && old.score>=session.Score)return;var record=new DrumRecord{score=session.Score,maxCombo=session.MaxCombo,perfect=session.Perfects,good=session.Goods,miss=session.Misses,rollHits=session.RollHits,fullCombo=session.Misses==0 && session.Completed==session.Notes.Count};PlayerPrefs.SetString(Key(chart.songId,chart.difficulty,chart.revision),JsonUtility.ToJson(record));PlayerPrefs.Save();}
#if UNITY_EDITOR
        public static void UseTestScope(string value){if(value!=null && !value.StartsWith("YYOU.Test."))throw new ArgumentException("Test scope required");prefix=value??"YYOU.Drum.Record.v1.";}
#endif
    }
}
