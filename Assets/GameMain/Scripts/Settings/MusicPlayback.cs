using System;
using System.Collections.Generic;
using UnityEngine;

namespace YYOU.Settings
{
    // Playback copies are normalized offline. Mapping keeps original chart/source
    // provenance intact and makes selection, shop preview and gameplay share audio.
    public static class MusicPlayback
    {
        [Serializable] sealed class Catalog { public Entry[] tracks; }
        [Serializable] sealed class Entry { public string sourcePath,playbackPath; }
        static Dictionary<string,string> paths;

        // Music true peaks are prepared below -6 dBTP; another 6 dB of gameplay
        // attenuation keeps percussion in front at the intended settings.
        const float NormalizedGameplayMusicScale=.5f;

        public static float GameplayScale(string sourcePath)
        {
            return !string.IsNullOrEmpty(sourcePath) && Resolve(sourcePath)!=sourcePath?NormalizedGameplayMusicScale:1f;
        }

        public static string Resolve(string sourcePath)
        {
            if(string.IsNullOrEmpty(sourcePath))return sourcePath;
            if(paths==null)
            {
                paths=new Dictionary<string,string>(StringComparer.OrdinalIgnoreCase);
                var file=Resources.Load<TextAsset>("MusicNormalization");
                if(file)
                {
                    var catalog=JsonUtility.FromJson<Catalog>(file.text.TrimStart('\uFEFF'));
                    if(catalog!=null && catalog.tracks!=null)
                        foreach(var entry in catalog.tracks)
                            if(!string.IsNullOrEmpty(entry.sourcePath) && !string.IsNullOrEmpty(entry.playbackPath))
                                paths[entry.sourcePath.Replace('\\','/')]=entry.playbackPath;
                }
            }
            return paths.TryGetValue(sourcePath.Replace('\\','/'),out var result)?result:sourcePath;
        }
    }
}
