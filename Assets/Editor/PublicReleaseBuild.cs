using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.UI;
using YYOU.SongSelection;
using YYOU.SongPacks;
using YYOU.DrumPractice;

public static class PublicReleaseBuild
{
    [Serializable] sealed class InputCatalog { public SongEntry[] songs; }
    const string Selection="Assets/GameMain/Prefabs/UI/UISongSelection.prefab";
    const string StartScene="Assets/GameMain/Scenes/Start.unity";
    public static string Output
    {
        get { var args=Environment.GetCommandLineArgs();int i=Array.IndexOf(args,"-yyouOutput");return Path.GetFullPath(i>=0&&i+1<args.Length?args[i+1]:"Builds"); }
    }

    [MenuItem("YYOU/Refresh demo catalog")]
    public static void Prepare()
    {
        AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
        var data=JsonUtility.FromJson<InputCatalog>(File.ReadAllText("Tools/demo-catalog.json"));
        if(data.songs==null || data.songs.Length!=13)throw new Exception("Expected the 13 bundled synth examples");
        var cover=AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/GameMain/AppIcons/YYOU.png");
        foreach(var song in data.songs){song.cover=cover;song.audio=null;song.sourceUrl="";}
        var root=PrefabUtility.LoadPrefabContents(Selection);
        try{root.GetComponent<SongSelectionView>().songs=data.songs;PrefabUtility.SaveAsPrefabAsset(root,Selection);}
        finally{PrefabUtility.UnloadPrefabContents(root);}
        var catalog=AssetDatabase.LoadAssetAtPath<SongPackCatalog>("Assets/GameMain/Resources/SongPacks/YYOUShop.asset");
        catalog.songs=data.songs.Skip(1).ToArray();
        for(int i=0;i<catalog.packs.Length;i++){
            catalog.packs[i].id="demo_pack_"+i;
            catalog.packs[i].songIds=data.songs.Skip(1+i*3).Take(3).Select(s=>s.id).ToArray();
            catalog.packs[i].subtitle=new[]{"SYNTH VOICES · DEMO","POP SYNTH · DEMO","MELODY SYNTH · DEMO","ANIME SYNTH · DEMO"}[i];
        }
        EditorUtility.SetDirty(catalog);
        // Noto CJK has taller line metrics than the internal prototype font.
        // Preserve the original point sizes and positions without silently clipping titles.
        foreach(var path in Directory.GetFiles("Assets/GameMain/Prefabs/UI","*.prefab")){
            var ui=PrefabUtility.LoadPrefabContents(path);
            try{foreach(var label in ui.GetComponentsInChildren<Text>(true))label.verticalOverflow=VerticalWrapMode.Overflow;PrefabUtility.SaveAsPrefabAsset(ui,path);}
            finally{PrefabUtility.UnloadPrefabContents(ui);}
        }
        EditorBuildSettings.scenes=new[]{new EditorBuildSettingsScene(StartScene,true)};
        PlayerSettings.productName="YYOU Open Source";PlayerSettings.companyName="QTGB";
        PlayerSettings.bundleVersion="0.3.0";
        PlayerSettings.SetApplicationIdentifier(BuildTargetGroup.Standalone,"io.github.qtgb.yyou");
        PlayerSettings.SetApplicationIdentifier(BuildTargetGroup.Android,"io.github.qtgb.yyou");
        PlayerSettings.defaultScreenWidth=1600;PlayerSettings.defaultScreenHeight=900;
        PlayerSettings.fullScreenMode=FullScreenMode.FullScreenWindow;
        PlayerSettings.resizableWindow=true;PlayerSettings.runInBackground=false;
        PlayerSettings.defaultInterfaceOrientation=UIOrientation.AutoRotation;
        PlayerSettings.allowedAutorotateToLandscapeLeft=true;PlayerSettings.allowedAutorotateToLandscapeRight=true;
        PlayerSettings.allowedAutorotateToPortrait=false;PlayerSettings.allowedAutorotateToPortraitUpsideDown=false;
        AssetDatabase.SaveAssets();
        Debug.Log("PUBLIC_CONTENT_READY: 13 tracks, 65 charts, 4 mock packs");
    }

    [MenuItem("YYOU/Build Windows x64")]
    public static void Windows()
    {
        PlayerSettings.SetScriptingBackend(BuildTargetGroup.Standalone,ScriptingImplementation.Mono2x);
        Build(BuildTarget.StandaloneWindows64,Path.Combine(Output,"Windows/YYOU.exe"));
    }

    [MenuItem("YYOU/Build Android APK")]
    public static void Android()
    {
        PlayerSettings.SetScriptingBackend(BuildTargetGroup.Android,ScriptingImplementation.IL2CPP);
        PlayerSettings.Android.bundleVersionCode=4;
        PlayerSettings.Android.targetArchitectures=AndroidArchitecture.ARM64|AndroidArchitecture.ARMv7;
        PlayerSettings.Android.minSdkVersion=AndroidSdkVersions.AndroidApiLevel23;
        PlayerSettings.Android.targetSdkVersion=AndroidSdkVersions.AndroidApiLevel34;
        PlayerSettings.Android.useCustomKeystore=false;
        PlayerSettings.SetUseDefaultGraphicsAPIs(BuildTarget.Android,false);
        PlayerSettings.SetGraphicsAPIs(BuildTarget.Android,new[]{GraphicsDeviceType.OpenGLES3});
        PlayerSettings.Android.startInFullscreen=true;
        EditorUserBuildSettings.buildAppBundle=false;
        Build(BuildTarget.Android,Path.Combine(Output,"YYOU-0.3.0-Android.apk"));
    }

    static void Build(BuildTarget target,string path)
    {
        if(!AssetDatabase.LoadAssetAtPath<GameObject>(Selection).GetComponent<SongSelectionView>().songs[0].id.StartsWith("yyou_demo_"))throw new Exception("Run YYOU/Refresh demo catalog first");
        Directory.CreateDirectory(Path.GetDirectoryName(path));
        EditorUserBuildSettings.development=false;
        var result=BuildPipeline.BuildPlayer(new BuildPlayerOptions{scenes=new[]{StartScene},locationPathName=path,target=target,options=BuildOptions.CompressWithLz4HC});
        var s=result.summary;Directory.CreateDirectory(Output);
        File.WriteAllText(Path.Combine(Output,target+"-build.txt"),$"Result: {s.result}\nErrors: {s.totalErrors}\nWarnings: {s.totalWarnings}\nBytes: {s.totalSize}\nTime: {s.totalTime}\nVersion: 0.3.0\nContent: original synth demo only\n");
        if(s.result!=BuildResult.Succeeded)throw new Exception("Build failed: "+s.result);
        Debug.Log("PUBLIC_BUILD_OK "+path);
    }
}
