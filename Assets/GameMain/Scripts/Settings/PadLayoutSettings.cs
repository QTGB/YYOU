using System;
using UnityEngine;
using UnityEngine.UI;
using YYOU.StartScreen;

namespace YYOU.Settings
{
    public static class PadLayouts
    {
        const string Key="YYOU.Drum.PadLayout.v1";
#if UNITY_EDITOR
        static string testKey;
        public static void UseTestKey(string key){if(key!=null && !key.StartsWith("YYOU.TestLayout."))throw new ArgumentException("Test key required");testKey=key;Changed?.Invoke();}
        static string StorageKey=>testKey??Key;
#else
        static string StorageKey=>Key;
#endif
        public static event Action Changed;
        public static int Selected=>Mathf.Clamp(PlayerPrefs.GetInt(StorageKey,0),0,2);
        public static void Set(int index){index=Mathf.Clamp(index,0,2);if(index==Selected)return;PlayerPrefs.SetInt(StorageKey,index);PlayerPrefs.Save();Changed?.Invoke();}
        public static Vector2 Position(int layout,int pad)
        {
            Vector2[][] positions={
                new[]{new Vector2(-505,-105),new Vector2(-610,-295),new Vector2(505,-105),new Vector2(610,-295)},
                new[]{new Vector2(-665,-260),new Vector2(-480,-335),new Vector2(665,-260),new Vector2(480,-335)},
                new[]{new Vector2(-670,-335),new Vector2(-460,-335),new Vector2(670,-335),new Vector2(460,-335)}
            };
            return positions[Mathf.Clamp(layout,0,2)][Mathf.Clamp(pad,0,3)];
        }
    }
    public sealed class PadLayoutSettings:MonoBehaviour
    {
        public Button[] choices;
        public bool darkTheme;
        void Awake(){for(int i=0;i<choices.Length;i++){int selected=i;choices[i].onClick.AddListener(()=>PadLayouts.Set(selected));}}
        void OnEnable(){PadLayouts.Changed+=Refresh;Refresh();}
        void OnDisable(){PadLayouts.Changed-=Refresh;}
        public void Refresh()
        {
            if(choices==null)return;
            for(int i=0;i<choices.Length;i++)
            {
                var face=choices[i].GetComponentInChildren<RoundedGraphic>();if(!face)continue;bool active=i==PadLayouts.Selected;
                var marker=choices[i].transform.Find("Selected");if(marker)marker.gameObject.SetActive(active);
                if(darkTheme){face.color=active?new Color(.22f,.45f,.55f):new Color(.20f,.27f,.38f);face.bottomColor=active?new Color(.12f,.29f,.38f):new Color(.13f,.19f,.30f);continue;}
                face.color=active?new Color(.75f,.72f,.92f):Color.white;
                face.bottomColor=active?new Color(.72f,.88f,.96f):new Color(.92f,.94f,.99f);
            }
        }
    }
}
