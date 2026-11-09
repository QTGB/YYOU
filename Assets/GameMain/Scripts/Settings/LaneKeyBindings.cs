using System;
using UnityEngine;
namespace YYOU.Settings
{
    public static class LaneKeyBindings
    {
        const string Prefix="YYOU.Input.4K.Lane.";
        public const int Count=4;
        static KeyCode[] keys;
        public static KeyCode[] Defaults()=>new[]{KeyCode.D,KeyCode.F,KeyCode.J,KeyCode.K};
        public static void Reload()
        {
            keys=Defaults();
            int[] oldSlots={0,1,3,4};
            for(int i=0;i<Count;i++)keys[i]=(KeyCode)PlayerPrefs.GetInt(Prefix+i,PlayerPrefs.GetInt("YYOU.Input.Lane."+oldSlots[i],(int)keys[i]));
            for(int i=0;i<Count;i++)
            {
                if(!Allowed(keys[i])){keys=Defaults();return;}
                for(int j=0;j<i;j++)if(keys[i]!=KeyCode.None && keys[i]==keys[j]){keys=Defaults();return;}
            }
        }
        public static KeyCode Get(int slot){if(keys==null)Reload();return keys[slot];}
        public static bool Allowed(KeyCode key)
        {return key!=KeyCode.None && key!=KeyCode.Escape && key<KeyCode.Mouse0 && Enum.IsDefined(typeof(KeyCode),key);}
        public static bool Apply(KeyCode[] values,int slot,KeyCode key,out string message)
        {
            message="";
            if(values==null || values.Length!=Count || slot<0 || slot>=Count || !Allowed(key)){message="此键不可用；Esc 用于暂停和取消。";return false;}
            int other=Array.IndexOf(values,key);
            if(key!=KeyCode.None && other>=0 && other!=slot)
            {
                values[other]=values[slot];message="与已占用的键位交换。";
            }
            values[slot]=key;return true;
        }
        public static bool Assign(int slot,KeyCode key,out string message)
        {if(keys==null)Reload();if(!Apply(keys,slot,key,out message))return false;Save();return true;}
        public static void Reset(){keys=Defaults();Save();}
        static void Save(){for(int i=0;i<Count;i++)PlayerPrefs.SetInt(Prefix+i,(int)keys[i]);PlayerPrefs.Save();}
        public static bool Held(int lane){return Input.GetKey(Get(lane));}
        public static string Name(KeyCode key)
        {
            if(key==KeyCode.None)return "无";
            if(key==KeyCode.Space)return "SPACE";
            if(key>=KeyCode.Alpha0 && key<=KeyCode.Alpha9)return ((int)key-(int)KeyCode.Alpha0).ToString();
            return key.ToString().ToUpperInvariant();
        }
        public static string LaneName(int lane){return Name(Get(lane));}
    }
}
