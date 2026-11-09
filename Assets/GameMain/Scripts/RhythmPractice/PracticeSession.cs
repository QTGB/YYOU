using System;
using System.Collections.Generic;

namespace YYOU.RhythmPractice
{
    public enum PracticeNoteState { Waiting, Holding, Hit, Miss }
    [Serializable] public sealed class PracticeNote
    {
        public int lane;
        public float time,duration;
        public string source;
        public PracticeNoteState state;
        public bool perfect;
        public bool IsHold=>duration>0;
    }
    // Judgement state stays in memory; fixed chart assets remain read-only.
    public sealed class PracticeSession
    {
        public const int LaneCount=4;
        public const float PerfectWindow=.08f,GoodWindow=.18f,ReleaseGrace=.10f;
        public readonly List<PracticeNote> Notes=new List<PracticeNote>();
        public float Time {get;private set;}
        public float Duration {get;private set;}=60;
        public int Score {get;private set;}
        public int Combo {get;private set;}
        public int MaxCombo {get;private set;}
        public int Perfects {get;private set;}
        public int Goods {get;private set;}
        public int Misses {get;private set;}
        public bool Finished=>Time>=Duration;
        public event Action<int,string> Judged;
        readonly bool[] down=new bool[LaneCount];
        public PracticeSession(PracticeChart chart,float duration)
        {
            Duration=Math.Max(6,duration);
            foreach(var note in chart.notes)Notes.Add(new PracticeNote{lane=note.lane,time=note.time+3,duration=note.duration,source=note.source});
        }
        public PracticeSession(int seed,int difficulty=0,float duration=60)
        {
            Duration=Math.Max(6,duration);
            var random=new Random(seed);var busy=new float[LaneCount];float t=3;
            float gap=.85f-Math.Min(4,Math.Max(0,difficulty))*.10f;
            while(t<Duration-3)
            {
                int start=random.Next(LaneCount),lane=-1;
                for(int j=0;j<LaneCount;j++){int candidate=(start+j)%LaneCount;if(busy[candidate]<t){lane=candidate;break;}}
                if(lane>=0){float hold=random.NextDouble()<.33?(.8f+(float)random.NextDouble()*1.1f):0;
                    Notes.Add(new PracticeNote{lane=lane,time=t,duration=hold});busy[lane]=t+hold+.45f;}
                t+=gap*(.8f+(float)random.NextDouble()*.4f);
            }
        }
        public void Advance(float seconds)
        {
            if(Finished || seconds<0)return;
            Time=Math.Min(Duration,Time+seconds);
            foreach(var n in Notes)
            {
                if(n.state==PracticeNoteState.Waiting && Time>n.time+GoodWindow)Miss(n);
                else if(n.state==PracticeNoteState.Holding && Time>=n.time+n.duration)Complete(n);
            }
        }
        public void SetLane(int lane,bool pressed)
        {
            if(lane<0 || lane>=LaneCount || Finished || down[lane]==pressed)return;
            down[lane]=pressed;
            if(!pressed){foreach(var n in Notes)if(n.lane==lane && n.state==PracticeNoteState.Holding){if(Time>=n.time+n.duration-ReleaseGrace)Complete(n);else Miss(n);}return;}
            PracticeNote nearest=null;float distance=float.MaxValue;
            foreach(var n in Notes)if(n.lane==lane && n.state==PracticeNoteState.Waiting){float d=Math.Abs(Time-n.time);if(d<=GoodWindow && d<distance){nearest=n;distance=d;}}
            if(nearest==null)return;
            nearest.perfect=distance<=PerfectWindow;
            if(nearest.IsHold){nearest.state=PracticeNoteState.Holding;Judged?.Invoke(lane,"HOLD");}
            else Complete(nearest);
        }
        void Complete(PracticeNote n)
        {
            if(n.state==PracticeNoteState.Hit || n.state==PracticeNoteState.Miss)return;
            n.state=PracticeNoteState.Hit;Combo++;MaxCombo=Math.Max(MaxCombo,Combo);
            if(n.perfect){Perfects++;Score+=n.IsHold?1500:1000;}else{Goods++;Score+=n.IsHold?900:600;}
            Judged?.Invoke(n.lane,n.perfect?"PERFECT":"GOOD");
        }
        void Miss(PracticeNote n){n.state=PracticeNoteState.Miss;Misses++;Combo=0;Judged?.Invoke(n.lane,"MISS");}
    }
}

