#if UNITY_STANDALONE_WIN && !UNITY_EDITOR
using System;
using System.Collections;
using System.IO;
using UnityEngine;
using YYOU.StartScreen;
using YYOU.SongSelection;
using YYOU.DrumPractice;
using YYOU.Settings;

// Opt-in build diagnostic: normal launches never attach this component.
public sealed class StandaloneSmokeTest:MonoBehaviour
{
    string output;bool done;double deadline;
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void Init(){var args=Environment.GetCommandLineArgs();int i=Array.IndexOf(args,"--yyou-smoke");if(i<0)return;var go=new GameObject("Release smoke test");DontDestroyOnLoad(go);go.AddComponent<StandaloneSmokeTest>().output=i+1<args.Length?args[i+1]:Application.persistentDataPath;}
    IEnumerator Start()
    {
        deadline=Time.realtimeSinceStartupAsDouble+55;Application.runInBackground=true;
        Directory.CreateDirectory(output);yield return new WaitForSecondsRealtime(.5f);
        var start=FindObjectOfType<StartScreenView>();if(!start){Fail("No start UI");yield break;}
        start.OpenSettings();yield return null;if(!start.settingsPanel.activeSelf){Fail("Settings");yield break;}start.CloseSettings();start.RequestStart();
        var selection=FindObjectOfType<SongSelectionView>();selection.FocusSong("yyou_demo_00");selection.SelectDifficulty(2);
        while(selection.IsLoading||!selection.previewSource.isPlaying)yield return null;
        bool seen=PlayerAudioSettings.TutorialSeen;PlayerAudioSettings.MarkTutorial();selection.Launch();
        var game=FindObjectOfType<DrumPracticeView>();if(!game||game.Chart==null){Fail("Launch");yield break;}
        while(game.Preparing)yield return null;
        while(game.Clock.SongSeconds<9){if(game.Paused)game.Resume();yield return null;}
        var canvas=game.GetComponent<Canvas>();var camera=FindObjectOfType<Camera>();var rt=new RenderTexture(1600,900,24);
        camera.targetTexture=rt;canvas.renderMode=RenderMode.ScreenSpaceCamera;canvas.worldCamera=camera;canvas.planeDistance=5;Canvas.ForceUpdateCanvases();camera.Render();
        var old=RenderTexture.active;RenderTexture.active=rt;var tex=new Texture2D(1600,900,TextureFormat.RGB24,false);tex.ReadPixels(new Rect(0,0,1600,900),0,0);tex.Apply();File.WriteAllBytes(Path.Combine(output,"windows-gameplay.png"),tex.EncodeToPNG());RenderTexture.active=old;camera.targetTexture=null;canvas.renderMode=RenderMode.ScreenSpaceOverlay;Destroy(tex);Destroy(rt);
        game.Pause();float t=game.Clock.SongSeconds;yield return new WaitForSecondsRealtime(.2f);if(Mathf.Abs(t-game.Clock.SongSeconds)>.001f){Fail("Pause");yield break;}
        game.Resume();while(game.Preparing)yield return null;
        game.Back();if(!selection.gameObject.activeSelf){Fail("Return");yield break;}
        if(!seen){PlayerPrefs.DeleteKey("YYOU.Options.v1.tutorial");PlayerPrefs.Save();}
        File.WriteAllText(Path.Combine(output,"windows-smoke.txt"),"PASS: built Windows player; start/settings/preview/gameplay; pause/resume; return. This smoke intentionally does not provide input or save a completed score.\n");
        done=true;Debug.Log("WINDOWS_SMOKE_OK");Application.Quit(0);
    }
    void Update(){if(!done&&deadline>0&&Time.realtimeSinceStartupAsDouble>deadline)Fail("Timeout");}
    void Fail(string reason){done=true;File.WriteAllText(Path.Combine(output,"windows-smoke.txt"),"FAIL: "+reason);Debug.LogError(reason);Application.Quit(1);}
}
#endif
