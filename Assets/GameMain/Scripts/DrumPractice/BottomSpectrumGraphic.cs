using System;
using UnityEngine;
using UnityEngine.UI;

namespace YYOU.DrumPractice
{
    // Music-only spectrum ribbon. Gameplay judgements never enter this signal path.
    [RequireComponent(typeof(CanvasRenderer))]
    public sealed class BottomSpectrumGraphic : MaskableGraphic
    {
        const int Columns = 160, BandCount = 12, FftSize = 2048, SampleSize = 1024;
        // The user's marked upper extent is about 330 UI units above the bottom baseline.
        // Keep signal normalization separate so resizing the display actually enlarges the motion.
        const float SampleInterval = 1f / 30, MaxHeight = 330, SignalRange = 75, Ln10 = 2.302585093f;
        public DrumPracticeView view;
        [Range(.25f, 2)] public float strength = 1;
        public Color sky = new Color(.22f, .65f, .81f, 1), mint = new Color(.30f, .76f, .65f, 1), lavender = new Color(.52f, .56f, .83f, 1);

        readonly float[] spectrumLeft = new float[FftSize], spectrumRight = new float[FftSize];
        readonly float[] samplesLeft = new float[SampleSize], samplesRight = new float[SampleSize];
        readonly float[] bands = new float[BandCount], smoothBands = new float[BandCount];
        readonly float[] targets = new float[Columns], levels = new float[Columns], echoes = new float[Columns];
        readonly float[] columnPower = new float[Columns], columnShape = new float[Columns], caps = new float[Columns];
        readonly int[] bandStart = new int[BandCount], bandEnd = new int[BandCount];
        DrumSession session;
        AudioClip lastClip;
        int outputRate;
        float sampleRemaining, referenceRms = .08f, slowRms = .08f, slowBass = .02f;
        float visibility, targetVisibility, targetEnergy, spectrumReference;

        public float PeakLevel { get; private set; }
        public float AudioEnergy { get; private set; }
        public float PeakHeight => PeakLevel * Mathf.Min(MaxHeight, Mathf.Max(1, rectTransform.rect.height - 46));

        protected override void OnEnable() { base.OnEnable(); raycastTarget = false; Bind(view ? view.Session : null); }
        protected override void OnDisable() { Bind(null); base.OnDisable(); }
        public void Bind(DrumSession next) { session = next; ResetSignal(); }

        void ResetSignal()
        {
            Array.Clear(bands, 0, bands.Length); Array.Clear(smoothBands, 0, smoothBands.Length);
            Array.Clear(targets, 0, targets.Length); Array.Clear(levels, 0, levels.Length); Array.Clear(echoes, 0, echoes.Length);
            Array.Clear(caps, 0, caps.Length); spectrumReference = 0;
            lastClip = null; sampleRemaining = visibility = targetVisibility = targetEnergy = PeakLevel = AudioEnergy = 0;
            referenceRms = slowRms = .08f; slowBass = .02f;
            SetVerticesDirty();
        }

        void LateUpdate()
        {
            if (view && session != view.Session) Bind(view.Session);
            var source = view ? view.musicSource : null;
            var clip = source ? source.clip : null;
            if (clip != lastClip) { ResetSignal(); lastClip = clip; }
            if (view && (view.Paused || view.Preparing)) return;

            float dt = Mathf.Clamp(Time.unscaledDeltaTime, 0, .1f);
            sampleRemaining -= dt;
            if (sampleRemaining <= 0)
            {
                sampleRemaining += SampleInterval;
                if (sampleRemaining < 0) sampleRemaining = 0;
                bool playing = session != null && !session.Finished && source && clip && source.isPlaying && !source.mute && source.volume > .0001f;
                if (playing) ReadMusic(source, clip);
                else { Array.Clear(targets, 0, targets.Length); targetVisibility = targetEnergy = 0; }
            }

            visibility = Follow(visibility, targetVisibility, dt, .05f, .28f);
            AudioEnergy = Follow(AudioEnergy, targetEnergy, dt, .05f, .28f);
            PeakLevel = 0;
            bool changed = false;
            for (int i = 0; i < Columns; i++)
            {
                float old = levels[i], oldEcho = echoes[i];
                levels[i] = Follow(old, targets[i], dt, .05f, .28f);
                // A subdued trailing curve, with no autonomous phase or artificial oscillation.
                echoes[i] = Follow(oldEcho, levels[i] * .38f, dt, .12f, .34f);
                caps[i] = Mathf.Max(levels[i], caps[i] - dt * .72f);
                if (levels[i] < .00005f && targets[i] == 0) levels[i] = 0;
                if (echoes[i] < .00005f && levels[i] == 0) echoes[i] = 0;
                changed |= levels[i] != old || echoes[i] != oldEcho;
                PeakLevel = Mathf.Max(PeakLevel, levels[i]);
            }
            if (changed || visibility > .0001f) SetVerticesDirty();
        }

        void ReadMusic(AudioSource source, AudioClip clip)
        {
            int rate = AudioSettings.outputSampleRate;
            if (rate != outputRate)
            {
                outputRate = rate;
                float nyquist = Mathf.Max(1, rate * .5f), upper = Mathf.Min(10000, nyquist * .9f);
                for (int i = 0; i < BandCount; i++)
                {
                    float low = 45 * Mathf.Pow(upper / 45, i / (float)BandCount);
                    float high = 45 * Mathf.Pow(upper / 45, (i + 1f) / BandCount);
                    bandStart[i] = Mathf.Clamp(Mathf.FloorToInt(low / nyquist * FftSize), 1, FftSize - 2);
                    bandEnd[i] = Mathf.Clamp(Mathf.CeilToInt(high / nyquist * FftSize), bandStart[i] + 1, FftSize);
                }
            }
            source.GetSpectrumData(spectrumLeft, 0, FFTWindow.BlackmanHarris);
            source.GetOutputData(samplesLeft, 0);
            bool stereo = clip.channels > 1;
            if (stereo) { source.GetSpectrumData(spectrumRight, 1, FFTWindow.BlackmanHarris); source.GetOutputData(samplesRight, 1); }
            float square = 0;
            for (int i = 0; i < SampleSize; i++) square += samplesLeft[i] * samplesLeft[i] + (stereo ? samplesRight[i] * samplesRight[i] : 0);
            float rms = Mathf.Sqrt(square / (SampleSize * (stereo ? 2 : 1)));
            // Absolute gating precedes gain adaptation, so silence cannot be amplified.
            float gate = Mathf.SmoothStep(0, 1, Mathf.InverseLerp(.0006f, .003f, rms));
            if (gate == 0) { Array.Clear(targets, 0, targets.Length); targetVisibility = targetEnergy = 0; return; }

            float peak = .00001f, bass = 0;
            for (int b = 0; b < BandCount; b++)
            {
                float sum = 0;
                for (int j = bandStart[b]; j < bandEnd[b]; j++) sum += spectrumLeft[j] * spectrumLeft[j] + (stereo ? spectrumRight[j] * spectrumRight[j] : 0);
                bands[b] = Mathf.Sqrt(sum / (stereo ? 2 : 1));
                if (b < 4) bass += bands[b];
                bands[b] *= 1 + b * .07f;
                peak = Mathf.Max(peak, bands[b]);
            }

            float accent = Mathf.Clamp01(Mathf.Max(0, rms / Mathf.Max(.015f, slowRms) - 1) * 1.5f + Mathf.Max(0, bass / Mathf.Max(.008f, slowBass) - 1) * .42f);
            slowRms = Mathf.Lerp(slowRms, rms, 1 - Mathf.Exp(-SampleInterval / .24f));
            slowBass = Mathf.Lerp(slowBass, bass, 1 - Mathf.Exp(-SampleInterval / .24f));
            referenceRms = Mathf.Lerp(referenceRms, Mathf.Max(.025f, rms), 1 - Mathf.Exp(-SampleInterval / (rms > referenceRms ? 2.5f : 6f)));
            float loudness = Mathf.Clamp(rms / referenceRms, 0, 1.35f);
            targetEnergy = Mathf.Clamp01(loudness / 1.35f) * gate;
            targetVisibility = gate;
            for (int b = 0; b < BandCount; b++)
            {
                float value = 0, weight = 0;
                for (int k = -2; k <= 2; k++)
                {
                    float w = k == 0 ? 3 : Mathf.Abs(k) == 1 ? 2 : 1;
                    value += Mathf.Sqrt(bands[Mathf.Clamp(b + k, 0, BandCount - 1)] / peak) * w; weight += w;
                }
                smoothBands[b] = value / weight;
            }
            // Fine columns retain local spectral peaks; the 12 bands only provide the broad envelope.
            float nyquistHz = Mathf.Max(1, outputRate * .5f), topHz = Mathf.Min(10000, nyquistHz * .9f), finePeak = .00001f;
            for (int i = 0; i < Columns; i++)
            {
                float low = 55 * Mathf.Pow(topHz / 55, i / (float)Columns);
                float high = 55 * Mathf.Pow(topHz / 55, (i + 1f) / Columns);
                float bin = Mathf.Sqrt(low * high) / nyquistHz * FftSize;
                int a = Mathf.Clamp((int)bin, 1, FftSize - 2);
                float left = Mathf.Lerp(spectrumLeft[a], spectrumLeft[a + 1], bin - a);
                float right = stereo ? Mathf.Lerp(spectrumRight[a], spectrumRight[a + 1], bin - a) : left;
                float power = (left * left + right * right) * .5f;
                int lo = Mathf.Clamp(Mathf.FloorToInt(low / nyquistHz * FftSize), 1, FftSize - 2);
                int hi = Mathf.Clamp(Mathf.CeilToInt(high / nyquistHz * FftSize), lo + 1, FftSize);
                if (hi - lo > 2)
                {
                    power = 0;
                    for (int j = lo; j < hi; j++) power += (spectrumLeft[j] * spectrumLeft[j] + (stereo ? spectrumRight[j] * spectrumRight[j] : spectrumLeft[j] * spectrumLeft[j])) * .5f;
                    power /= hi - lo;
                }
                columnPower[i] = Mathf.Sqrt(power) * Mathf.Pow(Mathf.Sqrt(low * high) / 100, .24f);
                finePeak = Mathf.Max(finePeak, columnPower[i]);
            }
            spectrumReference = Mathf.Max(finePeak, spectrumReference * Mathf.Exp(-SampleInterval / .5f));
            for (int i = 0; i < Columns; i++) columnShape[i] = Mathf.Clamp01(1 + Mathf.Log10(Mathf.Max(.0000001f, columnPower[i]) / spectrumReference) * .56f);
            for (int i = 0; i < Columns; i++)
            {
                float t = i / (float)(Columns - 1), x = t * (BandCount - 1);
                int a = Mathf.Min((int)x, BandCount - 2);
                float u = x - a, y0 = smoothBands[Mathf.Max(0, a - 1)], y1 = smoothBands[a], y2 = smoothBands[a + 1], y3 = smoothBands[Mathf.Min(BandCount - 1, a + 2)];
                float profile = Mathf.Clamp01(.5f * ((2 * y1) + (-y0 + y2) * u + (2 * y0 - 5 * y1 + 4 * y2 - y3) * u * u + (-y0 + 3 * y1 - 3 * y2 + y3) * u * u * u));
                float fine = (columnShape[Mathf.Max(0, i - 1)] + 2 * columnShape[i] + columnShape[Mathf.Min(Columns - 1, i + 1)]) * .25f;
                float shape = Mathf.Lerp(fine, profile * .8f, .16f);
                float height = ((8 + 46 * loudness) * Mathf.Pow(shape, 1.4f) + 18 * accent * shape) * gate * strength;
                targets[i] = Mathf.Clamp01(height / SignalRange) * Edge(t);
            }
        }

        // Response times specify 90% settling, independent of render frame rate.
        static float Follow(float current, float target, float dt, float attack, float release)
        { return Mathf.Lerp(current, target, 1 - Mathf.Exp(-Ln10 * dt / (target > current ? attack : release))); }

        protected override void OnPopulateMesh(VertexHelper vh)
        {
            vh.Clear(); var r = rectTransform.rect; if (r.width <= 0 || r.height <= 0) return;
            float baseline = r.yMin + 32, height = Mathf.Min(MaxHeight, Mathf.Max(1, r.height - 46)), step = r.width / (Columns - 1);
            for (int i = 0; i < Columns - 1; i++) { float t = i / (float)(Columns - 1); var c = Palette(t); Gradient(vh, r.xMin + i * step, baseline - .5f, step + 1, 1, Alpha(c, .13f * Edge(t)), Alpha(c, .13f * Edge(t))); }
            if (visibility < .002f || PeakLevel < .002f) return;
            Vector2 previousSecond = Vector2.zero;
            float energy = .65f + .35f * AudioEnergy, barWidth = Mathf.Min(3, step * .38f);
            for (int i = 0; i < Columns; i++)
            {
                float t = i / (float)(Columns - 1), alpha = Edge(t) * visibility, h = levels[i] * height, x = r.xMin + i * step;
                var c = Palette(t); var tip2 = new Vector2(x, baseline + echoes[i] * height);
                if (i > 0) Line(vh, previousSecond, tip2, 1.1f, Alpha(lavender, .20f * alpha * energy));
                previousSecond = tip2;
                if (h < 1) continue;
                float shine = alpha * (.45f + .55f * Mathf.Sqrt(levels[i]));
                // Glass-like stems, brighter crests and a short reflection: no heavy filled panel.
                Gradient(vh, x - barWidth * 2, baseline, barWidth * 4, h + 1, Alpha(c, .015f * shine), Alpha(c, .085f * shine));
                Gradient(vh, x - barWidth * .5f, baseline, barWidth, h, Alpha(c, .20f * shine), Alpha(c, .87f * shine));
                Gradient(vh, x - .5f, baseline + h * .30f, 1, h * .70f, Alpha(Color.white, 0), Alpha(Color.white, .47f * shine));
                Gradient(vh, x - barWidth * .5f, baseline + h - 1, barWidth, 1.5f, Alpha(c, .95f * shine), Alpha(c, .95f * shine));
                float reflection = Mathf.Min(22, h * .27f);
                Gradient(vh, x - barWidth * .5f, baseline - reflection, barWidth, reflection, Alpha(c, 0), Alpha(c, .15f * shine));
                if (i % 3 == 0 && caps[i] - levels[i] > .04f)
                {
                    Gradient(vh, x - 1.1f, baseline + caps[i] * height, 2.2f, 1.5f, Alpha(c, .42f * alpha), Alpha(c, .42f * alpha));
                }
            }
        }
        static float Edge(float t) { return Mathf.SmoothStep(0, 1, Mathf.Min(t, 1 - t) * 9); }
        Color Palette(float t) { return t < .52f ? Color.Lerp(lavender, sky, t / .52f) : Color.Lerp(sky, mint, (t - .52f) / .48f); }
        Color Alpha(Color c, float alpha) { c.a = alpha * color.a; return c; }
        static void Gradient(VertexHelper vh, float x, float y, float w, float h, Color bottom, Color top)
        { int n = vh.currentVertCount; vh.AddVert(new Vector2(x, y), bottom, Vector2.zero); vh.AddVert(new Vector2(x, y + h), top, Vector2.zero); vh.AddVert(new Vector2(x + w, y + h), top, Vector2.zero); vh.AddVert(new Vector2(x + w, y), bottom, Vector2.zero); vh.AddTriangle(n, n + 1, n + 2); vh.AddTriangle(n, n + 2, n + 3); }
        static void Line(VertexHelper vh, Vector2 a, Vector2 b, float width, Color c)
        { var d = (b - a).normalized; var p = new Vector2(-d.y, d.x) * width * .5f; int n = vh.currentVertCount; vh.AddVert(a - p, c, Vector2.zero); vh.AddVert(a + p, c, Vector2.zero); vh.AddVert(b + p, c, Vector2.zero); vh.AddVert(b - p, c, Vector2.zero); vh.AddTriangle(n, n + 1, n + 2); vh.AddTriangle(n, n + 2, n + 3); }
    }
}
