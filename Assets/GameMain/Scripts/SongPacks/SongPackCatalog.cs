using System;
using UnityEngine;
using YYOU.SongSelection;
namespace YYOU.SongPacks
{
    [Serializable] public sealed class SongPack
    {
        public string id,title,subtitle;public int category,priceCents=1200;public string[] songIds;public Color accent;
        public string Price=>"¥"+(priceCents%100==0?(priceCents/100).ToString():(priceCents/100f).ToString("0.00"));
    }
    public sealed class SongPackCatalog:ScriptableObject
    {
        public SongPack[] packs;public SongEntry[] songs;
        static SongPackCatalog current;
        public static SongPackCatalog Current=>current?current:(current=Resources.Load<SongPackCatalog>("SongPacks/YYOUShop"));
        public SongPack FindPack(string id){if(packs!=null)foreach(var p in packs)if(p.id==id)return p;return null;}
        public SongPack PackForSong(string id){if(packs!=null)foreach(var p in packs)if(p.songIds!=null && Array.IndexOf(p.songIds,id)>=0)return p;return null;}
        public SongEntry FindSong(string id){if(songs!=null)foreach(var s in songs)if(s.id==id)return s;return null;}
        public static SongPack LockedPack(string songId){var p=Current?Current.PackForSong(songId):null;return p!=null && !SongPackOwnership.Owns(p.id)?p:null;}
    }
    public static class SongPackOwnership
    {
        static string prefix="YYOU.Shop.Owned.v1.";
        public static event Action Changed;
        public static bool Owns(string id)=>!string.IsNullOrEmpty(id) && PlayerPrefs.GetInt(prefix+id,0)==1;
        // Local prototype purchase. No payment SDK, network charge or wallet is involved.
        public static bool Purchase(string id)
        {
            if(!SongPackCatalog.Current || SongPackCatalog.Current.FindPack(id)==null || Owns(id))return false;
            PlayerPrefs.SetInt(prefix+id,1);PlayerPrefs.Save();Changed?.Invoke();return true;
        }
#if UNITY_EDITOR
        public static void UseTestScope(string scope){if(scope!=null && !scope.StartsWith("YYOU.Test.Shop."))throw new ArgumentException("Test scope required");prefix=scope??"YYOU.Shop.Owned.v1.";Changed?.Invoke();}
        public static void ClearTestScope(){if(!prefix.StartsWith("YYOU.Test.Shop."))throw new InvalidOperationException("Test scope required");if(SongPackCatalog.Current)foreach(var p in SongPackCatalog.Current.packs)PlayerPrefs.DeleteKey(prefix+p.id);PlayerPrefs.Save();Changed?.Invoke();}
#endif
    }
}
