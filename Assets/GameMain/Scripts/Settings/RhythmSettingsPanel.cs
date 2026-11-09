using UnityEngine;
using UnityEngine.UI;
namespace YYOU.Settings
{
    public sealed class RhythmSettingsPanel:MonoBehaviour
    {
        public Slider music,hits,miss,offset,travel;public Text musicValue,hitsValue,missValue,offsetValue,travelValue;public Button calibrate;public RhythmCalibration calibration;
        void Awake(){music.onValueChanged.AddListener(PlayerAudioSettings.SetMusic);hits.onValueChanged.AddListener(PlayerAudioSettings.SetHits);miss.onValueChanged.AddListener(PlayerAudioSettings.SetMiss);offset.onValueChanged.AddListener(PlayerAudioSettings.SetOffset);travel.onValueChanged.AddListener(v=>PlayerAudioSettings.SetTravel(3/Mathf.Max(.1f,v)));calibrate.onClick.AddListener(()=>calibration.Open());}
        void OnEnable(){PlayerAudioSettings.Changed+=Refresh;Refresh();}void OnDisable(){PlayerAudioSettings.Changed-=Refresh;}
        void Refresh(){music.SetValueWithoutNotify(PlayerAudioSettings.Music);hits.SetValueWithoutNotify(PlayerAudioSettings.Hits);miss.SetValueWithoutNotify(PlayerAudioSettings.Miss);offset.SetValueWithoutNotify(PlayerAudioSettings.OffsetMs);travel.SetValueWithoutNotify(3/PlayerAudioSettings.TravelSeconds);musicValue.text=Mathf.RoundToInt(music.value*100)+"%";hitsValue.text=Mathf.RoundToInt(hits.value*100)+"%";missValue.text=Mathf.RoundToInt(miss.value*100)+"%";offsetValue.text=offset.value.ToString("+0;-0;0")+" ms";travelValue.text=travel.value.ToString("0.00")+"×";}
    }
}
