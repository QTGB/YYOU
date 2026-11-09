using System;
using System.Collections.Generic;

namespace YYOU.DrumPractice
{
    public enum DrumNoteKind { Blue, Green, BigBlue, BigGreen, Roll }
    public enum DrumNoteState { Waiting, PairPending, Rolling, Hit, Miss }
    public enum DrumPerformanceKind { Perfect,Good,RollHit,Miss }
    // One sound decision per physical press. Holds/releases never retrigger a sound.
    public enum DrumStrikeSound { None,Blue,Green,Big,Roll }
    public readonly struct DrumPerformanceCue
    {
        public readonly DrumPerformanceKind Kind;
        public readonly int Color,Combo;
        public readonly bool Big;
        public DrumPerformanceCue(DrumPerformanceKind kind,int color,int combo,bool big){Kind=kind;Color=color;Combo=combo;Big=big;}
    }
    public sealed class DrumNote
    {
        public DrumNoteKind kind;
        public DrumNoteState state;
        public float time,duration,firstPress,lastRoll;
        public int firstSide=-1,rollHits;
        public bool perfect;
        public bool IsBig=>kind==DrumNoteKind.BigBlue || kind==DrumNoteKind.BigGreen;
        public int Color=>kind==DrumNoteKind.Green || kind==DrumNoteKind.BigGreen?1:0;
    }
    public sealed class DrumSession
    {
        public const float PerfectWindow=.08f,HitWindow=.18f,PairWindow=.08f,RollMaxGap=.30f,RollMinGap=.055f;
        public readonly List<DrumNote> Notes=new List<DrumNote>();
        public readonly bool[] Held=new bool[4];
        public float Time {get;private set;}
        public float Duration {get;private set;}
        public int Score {get;private set;}
        public int Combo {get;private set;}
        public int MaxCombo {get;private set;}
        public int Misses {get;private set;}
        public int Completed {get;private set;}
        public int RollHits {get;private set;}
        public int Perfects {get;private set;}
        public int Goods {get;private set;}
        public int RollCompleted {get;private set;}
        int first;
        public int FirstUnresolved {get{RepairCursor();return first;}}
        void RepairCursor(){if(first>0 && (first>Notes.Count || (Notes[first-1].state!=DrumNoteState.Hit && Notes[first-1].state!=DrumNoteState.Miss)))first=0;while(first<Notes.Count && (Notes[first].state==DrumNoteState.Hit || Notes[first].state==DrumNoteState.Miss))first++;}
        public bool Finished=>Time>=Duration;
        public event Action<string,int> Feedback;
        public event Action<DrumPerformanceCue> Performed;
        public DrumSession(DrumChart chart)
        {
            Duration=chart.audioDuration+3;
            foreach(var n in chart.notes)Notes.Add(new DrumNote{kind=n.kind,time=n.time+3,duration=n.duration});
        }
        public DrumSession(float duration=45)
        {
            Duration=Math.Max(12,duration);
            // Deliberately authored input tutorial, not a converted or music-aligned chart.
            var kinds=new[]{DrumNoteKind.Blue,DrumNoteKind.Green,DrumNoteKind.Blue,DrumNoteKind.Green,DrumNoteKind.BigBlue,DrumNoteKind.BigGreen,DrumNoteKind.Blue,DrumNoteKind.Roll};
            float t=4.4f;int i=0;
            while(t<Duration-4){var kind=kinds[i%kinds.Length];float hold=kind==DrumNoteKind.Roll?2.1f:0;Notes.Add(new DrumNote{kind=kind,time=t,duration=hold});t+=hold>0?hold+1.1f:(i%8>=4?1.35f:.9f);i++;}
        }
        public void Advance(float delta)
        {
            if(delta<0 || Finished)return;Time=Math.Min(Duration,Time+delta);
            RepairCursor();for(int i=first;i<Notes.Count;i++)
            {
                var n=Notes[i];if(n.time>Time+HitWindow)break;
                if((n.state==DrumNoteState.Waiting || n.state==DrumNoteState.PairPending) && Time>n.time+HitWindow){Miss(n);continue;}
                if(n.state!=DrumNoteState.Rolling)continue;
                float until=Math.Min(Time,n.time+n.duration);
                if(until-Math.Max(n.time,n.lastRoll)>RollMaxGap+.00001f)Miss(n);
                else if(Time>=n.time+n.duration)Complete(n,"连打完成",500);
            }
        }
        public DrumStrikeSound SetPad(int pad,bool pressed)
        {
            if(pad<0 || pad>=4 || Finished || Held[pad]==pressed)return DrumStrikeSound.None;
            Held[pad]=pressed;if(!pressed)return DrumStrikeSound.None;
            int color=pad%2,side=pad/2;
            var single=color==0?DrumStrikeSound.Blue:DrumStrikeSound.Green;
            RepairCursor();for(int i=first;i<Notes.Count;i++)
            {
                var roll=Notes[i];if(roll.time>Time+HitWindow)break;
                if(roll.state!=DrumNoteState.Rolling || Time>roll.time+roll.duration)continue;
                if(color==0 && Time-roll.lastRoll>=RollMinGap){roll.lastRoll=Time;roll.rollHits++;RollHits++;Score+=100;Feedback?.Invoke("连打 "+roll.rollHits,0);Emit(DrumPerformanceKind.RollHit,roll);}
                // Rapid, distinct taps remain audible even when score debounce rejects a duplicate.
                return color==0?DrumStrikeSound.Roll:single;
            }
            DrumNote nearest=null;float distance=float.MaxValue;
            for(int i=first;i<Notes.Count;i++)
            {
                var n=Notes[i];if(n.time>Time+HitWindow)break;
                if(n.state!=DrumNoteState.Waiting && n.state!=DrumNoteState.PairPending)continue;
                // Dense runs are played in order; a late tap must not jump to a closer future note.
                float d=Math.Abs(Time-n.time);if(d<=HitWindow){nearest=n;distance=d;break;}
            }
            if(nearest==null)return single;
            if(nearest.Color!=color){Miss(nearest,true);return single;}
            if(nearest.kind==DrumNoteKind.Roll)
            {
                nearest.state=DrumNoteState.Rolling;nearest.lastRoll=Time;nearest.rollHits=1;RollHits++;Score+=100;Feedback?.Invoke("蓝色连打！",0);Emit(DrumPerformanceKind.RollHit,nearest);return DrumStrikeSound.Roll;
            }
            if(nearest.IsBig)
            {
                if(nearest.state==DrumNoteState.PairPending && nearest.firstSide!=side && Time-nearest.firstPress<=PairWindow && Held[nearest.firstSide*2+color])
                {
                    nearest.perfect=Math.Abs(nearest.firstPress-nearest.time)<=PerfectWindow && distance<=PerfectWindow;
                    Complete(nearest,nearest.perfect?"双手 PERFECT":"双手 GOOD",nearest.perfect?1800:1100);return DrumStrikeSound.Big;
                }
                nearest.state=DrumNoteState.PairPending;nearest.firstSide=side;nearest.firstPress=Time;Feedback?.Invoke("再按同色另一侧",color);return DrumStrikeSound.None;
            }
            nearest.perfect=distance<=PerfectWindow;
            Complete(nearest,nearest.perfect?"PERFECT":"GOOD",nearest.perfect?1000:600);
            return single;
        }
        public void ClearInput(bool resumeRoll=false)
        {
            Array.Clear(Held,0,Held.Length);
            foreach(var n in Notes){if(n.state==DrumNoteState.PairPending){n.state=DrumNoteState.Waiting;n.firstSide=-1;}if(resumeRoll && n.state==DrumNoteState.Rolling)n.lastRoll=Time;}
        }
        void Complete(DrumNote n,string text,int points){n.state=DrumNoteState.Hit;Completed++;if(n.kind==DrumNoteKind.Roll)RollCompleted++;else if(n.perfect)Perfects++;else Goods++;Combo++;MaxCombo=Math.Max(MaxCombo,Combo);Score+=points;Feedback?.Invoke(text,n.Color);Emit(n.kind==DrumNoteKind.Roll?DrumPerformanceKind.RollHit:n.perfect?DrumPerformanceKind.Perfect:DrumPerformanceKind.Good,n);}
        void Miss(DrumNote n,bool wrongColor=false){n.state=DrumNoteState.Miss;Misses++;Combo=0;Feedback?.Invoke(wrongColor?"WRONG":"MISS",-1);Emit(DrumPerformanceKind.Miss,n);}
        void Emit(DrumPerformanceKind kind,DrumNote note){Performed?.Invoke(new DrumPerformanceCue(kind,note.Color,Combo,note.IsBig));}
    }
}
