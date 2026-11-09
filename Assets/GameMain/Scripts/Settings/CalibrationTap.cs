using UnityEngine;
using UnityEngine.EventSystems;
namespace YYOU.Settings
{
    public sealed class CalibrationTap:MonoBehaviour,IPointerDownHandler
    {public RhythmCalibration calibration;public void OnPointerDown(PointerEventData e){if(e.button==PointerEventData.InputButton.Left)calibration.Tap();}}
}
