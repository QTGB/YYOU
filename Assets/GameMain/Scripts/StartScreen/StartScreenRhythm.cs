using UnityEngine;

namespace YYOU.StartScreen
{
    public sealed class StartScreenRhythm : MonoBehaviour
    {
        [Range(40,180)] public float beatsPerMinute=108;
        [Range(0,1)] public float intensity=1;
        public RectTransform logo, disc, halo, orbit;
        public RoundedGraphic[] pulseRings;
        float clock;
        Vector2 logoPosition, discPosition;
        Quaternion logoRotation;
        Vector3 logoScale, discScale, haloScale, orbitScale;
        bool initialized;
        void OnEnable() { CaptureLayout(); }
        void CaptureLayout()
        {
            if(!logo || !disc || !halo || !orbit) return;
            logoPosition=logo.anchoredPosition; discPosition=disc.anchoredPosition;
            logoRotation=logo.localRotation;
            logoScale=logo.localScale; discScale=disc.localScale;
            haloScale=halo.localScale; orbitScale=orbit.localScale;
            initialized=true;
        }
        void Update() { clock+=Time.unscaledDeltaTime; ApplyTime(clock); }
        public void ApplyTime(float time)
        {
            if(!initialized) CaptureLayout();
            if(!initialized) return;
            float beats=time*beatsPerMinute/60;
            float beat=Mathf.Repeat(beats,1);
            // Smooth attack and longer decay: no discontinuity at the beat boundary.
            float pulse=Mathf.Pow(Mathf.Sin(Mathf.PI*beat),6);
            float sway=Mathf.Sin(beats*Mathf.PI*.5f);
            logo.localScale=logoScale*(1+intensity*.035f*pulse);
            logo.anchoredPosition=logoPosition+new Vector2(0,intensity*(5*sway+3*pulse));
            logo.localRotation=logoRotation*Quaternion.Euler(0,0,intensity*1.5f*sway);
            disc.localScale=discScale*(1+intensity*(.045f*pulse+.012f*sway));
            disc.anchoredPosition=discPosition+new Vector2(intensity*5*Mathf.Sin(beats*Mathf.PI*.25f),intensity*5*sway);
            halo.localScale=haloScale*(1+intensity*.018f*Mathf.Sin(beats*Mathf.PI));
            orbit.localScale=orbitScale*(1+intensity*.025f*pulse);
            for(int i=0;i<pulseRings.Length;i++)
            {
                float phase=Mathf.Repeat(beats/4+i*.5f,1);
                var ring=pulseRings[i];
                ring.rectTransform.localScale=Vector3.one*Mathf.Lerp(.78f,1.12f,phase);
                Color c=new Color(1,.80f,.23f,intensity*.27f*Mathf.Sin(phase*Mathf.PI));
                ring.color=c; ring.bottomColor=c; ring.SetVerticesDirty();
            }
        }
        void OnDisable()
        {
            clock=0;
            if(!initialized || !logo || !disc || !halo || !orbit) return;
            logo.localScale=logoScale; disc.localScale=discScale; halo.localScale=haloScale; orbit.localScale=orbitScale;
            logo.anchoredPosition=logoPosition; logo.localRotation=logoRotation;
            disc.anchoredPosition=discPosition;
            initialized=false;
        }
    }
}
