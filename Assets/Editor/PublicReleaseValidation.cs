using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using YYOU.StartScreen;
using YYOU.Settings;
using YYOU.SongSelection;
using YYOU.SongPacks;
using YYOU.DrumPractice;

[InitializeOnLoad]
public static class PublicReleaseValidation
{
    static int stage,lastFrame=-1;static double next,deadline;static Camera camera;static StartScreenView start;static SongSelectionView selection;static SongPackShopView shop;static DrumPracticeView game;
    static string scope;static float[] release=new float[4];static bool paused,captured,restarted;static float pausedAt;static int errors;
    static string Out=>PublicReleaseBuild.Output;
    static PublicReleaseValidation(){if(SessionState.GetBool("YYOU.Public.Validation",false))EditorApplication.update+=Tick;}
    static void Check(bool value,string message){if(!value)throw new Exception(message);}

    [MenuItem("YYOU/Validate charts")]
    public static void Rules()
    {
        int count=0;
        var songs=AssetDatabase.LoadAssetAtPath<GameObject>("Assets/GameMain/Prefabs/UI/UISongSelection.prefab").GetComponent<SongSelectionView>().songs;
        Check(songs.Length==13,"Catalog size");
        foreach(var song in songs)for(int d=0;d<5;d++){
            var c=DrumChart.Load(song.id,d);Check(c!=null,"Missing chart");
            var session=new DrumSession(c);float end=-1;
            foreach(var n in session.Notes){
                Check(n.time>end && n.time+n.duration<session.Duration,"Overlap or out-of-range note");
                session.Advance(n.time-session.Time);session.SetPad(n.Color,true);
                if(n.IsBig)session.SetPad(n.Color+2,true);
                session.SetPad(n.Color,false);session.SetPad(n.Color+2,false);
                if(n.kind==DrumNoteKind.Roll){for(float t=n.time+.125f;t<n.time+n.duration;t+=.125f){session.Advance(t-session.Time);session.SetPad(0,true);session.SetPad(0,false);}session.Advance(n.time+n.duration-session.Time);}
                end=n.time+n.duration;
            }
            session.Advance(session.Duration-session.Time);
            Check(session.Misses==0&&session.Completed==session.Notes.Count,"Ideal replay failed: "+song.id+"/"+d);
            count++;
        }
        var packs=SongPackCatalog.Current.packs.SelectMany(p=>p.songIds).ToArray();
        Check(packs.Length==12&&packs.Distinct().Count()==12,"Duplicate pack tracks");
        Directory.CreateDirectory(Out);File.WriteAllText(Path.Combine(Out,"chart-validation.txt"),"PASS: "+count+" complete ideal replays; no overlap; 13 playable tracks; 12 unique pack tracks.\n");
        Debug.Log("PUBLIC_RULES_OK "+count);
    }
    public static void Run()
    {
        Rules();Directory.CreateDirectory(Out);EditorSceneManager.OpenScene("Assets/GameMain/Scenes/Start.unity");
        SessionState.SetBool("YYOU.Public.Validation",true);EditorApplication.isPlaying=true;
    }
    static void Capture(Canvas canvas,string name,int w=1600,int h=900)
    {
        var rt=new RenderTexture(w,h,24);camera.targetTexture=rt;canvas.renderMode=RenderMode.ScreenSpaceCamera;canvas.worldCamera=camera;canvas.planeDistance=5;
        Canvas.ForceUpdateCanvases();camera.Render();foreach(var f in canvas.GetComponentsInChildren<MobileSafeFrame>(true))f.RefreshLayout();if(game&&game.gameObject.activeSelf)game.RefreshFeedbackLayout();Canvas.ForceUpdateCanvases();camera.Render();
        var old=RenderTexture.active;RenderTexture.active=rt;var tex=new Texture2D(w,h,TextureFormat.RGB24,false);tex.ReadPixels(new Rect(0,0,w,h),0,0);tex.Apply();File.WriteAllBytes(Path.Combine(Out,name+".png"),tex.EncodeToPNG());RenderTexture.active=old;
        camera.targetTexture=null;canvas.renderMode=RenderMode.ScreenSpaceOverlay;canvas.worldCamera=null;UnityEngine.Object.Destroy(tex);UnityEngine.Object.Destroy(rt);
    }
    static void Clean()
    {
        if(game)game.gameObject.SetActive(false);if(selection)selection.gameObject.SetActive(false);if(shop)shop.gameObject.SetActive(false);
        if(scope!=null){SongPackOwnership.ClearTestScope();foreach(var k in new[]{"music","hits","miss","tutorial","offset","travel"})PlayerPrefs.DeleteKey(scope+k);PlayerPrefs.DeleteKey("YYOU.TestLayout.Public");for(int i=0;i<13;i++)for(int d=0;d<5;d++)PlayerPrefs.DeleteKey(scope+"yyou_demo_"+i.ToString("00")+"."+d+".1");PlayerPrefs.Save();}
        SongPackOwnership.UseTestScope(null);PlayerAudioSettings.UseTestScope(null);PadLayouts.UseTestKey(null);DrumRecords.UseTestScope(null);SessionState.SetBool("YYOU.Public.Validation",false);
    }
    static void Tick()
    {
        if(!EditorApplication.isPlaying||Time.frameCount==lastFrame||EditorApplication.timeSinceStartup<next)return;lastFrame=Time.frameCount;
        try{
            double now=EditorApplication.timeSinceStartup;
            if(stage==0){scope="YYOU.Test.Shop.Public."+Guid.NewGuid().ToString("N")+".";SongPackOwnership.UseTestScope(scope);PlayerAudioSettings.UseTestScope(scope);DrumRecords.UseTestScope(scope);PlayerAudioSettings.SetMusic(.55f);PlayerAudioSettings.MarkTutorial();PadLayouts.UseTestKey("YYOU.TestLayout.Public");camera=UnityEngine.Object.FindObjectOfType<Camera>();start=UnityEngine.Object.FindObjectOfType<StartScreenView>();stage=1;next=now+.4;return;}
            if(stage==1){Capture(start.GetComponent<Canvas>(),"start");start.OpenSettings();Capture(start.GetComponent<Canvas>(),"settings");start.CloseSettings();start.OpenSongPacks();shop=UnityEngine.Object.FindObjectOfType<SongPackShopView>();Capture(shop.GetComponent<Canvas>(),"shop");shop.ShowPack(0);Capture(shop.GetComponent<Canvas>(),"pack");shop.BuySelected();Check(SongPackOwnership.Owns("demo_pack_0"),"Mock purchase failed");shop.Close();start.RequestStart();selection=UnityEngine.Object.FindObjectOfType<SongSelectionView>();selection.FocusSong("yyou_demo_00");selection.SelectDifficulty(2);stage=2;deadline=now+25;return;}
            if(stage==2){if(selection.IsLoading||!selection.previewSource.isPlaying){Check(now<deadline,"Preview timeout");return;}Capture(selection.GetComponent<Canvas>(),"selection");selection.Launch();game=UnityEngine.Object.FindObjectOfType<DrumPracticeView>();Check(game&&game.Chart!=null,"Game launch failed");deadline=now+180;stage=3;return;}
            if(stage==3){
                Check(now<deadline,"Runtime validation timeout");
                if(game.Paused){Check(Mathf.Abs(game.Clock.SongSeconds-pausedAt)<.001f,"Pause clock drift");game.Resume();return;}
                if(game.Preparing)return;
                for(int pad=0;pad<4;pad++)if(release[pad]>0&&game.Session.Time>=release[pad]){game.SetPointer(900+pad,pad,false);release[pad]=0;}
                for(int i=game.Session.FirstUnresolved;i<game.Session.Notes.Count;i++){
                    var n=game.Session.Notes[i];if(n.time>game.Session.Time+.008f)break;
                    if(n.state==DrumNoteState.Waiting){game.SetPointer(900+n.Color,n.Color,true);release[n.Color]=game.Session.Time+.035f;if(n.IsBig){game.SetPointer(902+n.Color,2+n.Color,true);release[2+n.Color]=game.Session.Time+.035f;}}
                    else if(n.state==DrumNoteState.Rolling&&game.Session.Time-n.lastRoll>=.125f&&game.Session.Time<n.time+n.duration){game.SetPointer(900,0,false);game.SetPointer(900,0,true);release[0]=game.Session.Time+.035f;}
                }
                float t=game.Clock.SongSeconds;
                if(!restarted&&t>5){game.Restart();Array.Clear(release,0,4);Check(game.Session.Score==0&&game.Preparing,"Restart failed");restarted=true;return;}
                if(!paused&&t>10){game.Pause();pausedAt=game.Clock.SongSeconds;Capture(game.GetComponent<Canvas>(),"pause");paused=true;next=now+.4;return;}
                if(!captured&&t>20){game.Pause();game.pausePanel.SetActive(false);for(int pad=0;pad<4;pad++)game.SetPointer(900+pad,pad,false);Array.Clear(release,0,4);for(int layout=0;layout<3;layout++){PadLayouts.Set(layout);Capture(game.GetComponent<Canvas>(),"gameplay-"+layout);Capture(game.GetComponent<Canvas>(),"gameplay-wide-"+layout,2000,900);Capture(game.GetComponent<Canvas>(),"gameplay-tablet-"+layout,1200,900);}PadLayouts.Set(0);captured=true;game.Resume();return;}
                if(game.resultPanel.activeSelf){Check(game.Session.Misses==0,"Actual replay had "+game.Session.Misses+" MISS");Capture(game.GetComponent<Canvas>(),"results");var record=DrumRecords.Read("yyou_demo_00",2,1);Check(record!=null&&record.score>0,"Record not stored");game.Back();Check(selection.gameObject.activeSelf,"Return failed");File.WriteAllText(Path.Combine(Out,"runtime-validation.txt"),"PASS: actual full demo playback; zero MISS; pause/resume; restart; result; local score; return; preview; mock pack acquisition; screenshots at 16:9, 20:9, 4:3 in all three layouts.\nAndroid device testing: not performed.\n");Clean();Debug.Log("PUBLIC_RUNTIME_OK");EditorApplication.Exit(0);}
            }
        }catch(Exception e){Clean();Debug.LogException(e);EditorApplication.Exit(1);}
    }
}
