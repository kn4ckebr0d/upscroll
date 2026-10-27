using System.Collections.Generic;
using UnityEngine;
#if UNITY_IOS && !UNITY_EDITOR
using System.Runtime.InteropServices;
#endif

/// Tiny chiptune synth: square, triangle and noise voices generated live, no sound files.
/// Same recipes as the Swift prototype.
public class Synth : MonoBehaviour
{
    public static Synth Instance { get; private set; }

    enum Wave { Square, Triangle, Noise }

    struct Voice
    {
        public Wave Wave;
        public float Duty, F0, F1, Duration, Volume, Delay, Sustain;
        public float T, Phase, Noise;
    }

    readonly List<Voice> pending = new();
    readonly object padlock = new();
    readonly Voice[] voices = new Voice[96];   // audio thread only
    int voiceCount;
    float sampleRate = 48000;
    uint rng = 0x12345678;
    bool enabledCache = true;
    float lastNope = -1;

    public bool Enabled
    {
        get => enabledCache;
        set
        {
            enabledCache = value;
            PlayerPrefs.SetInt("sound", value ? 1 : 0);
        }
    }

    public static Synth Create() => new GameObject("Synth").AddComponent<Synth>();

#if UNITY_IOS && !UNITY_EDITOR
    [DllImport("__Internal")] static extern void ScrollyAudioPlayEvenWhenSilent();
#endif

    void Awake()
    {
        Instance = this;
        enabledCache = PlayerPrefs.GetInt("sound", 1) == 1;

        // Small buffers so taps and blips feel instant.
        var config = AudioSettings.GetConfiguration();
        config.dspBufferSize = 256;
        AudioSettings.Reset(config);
        sampleRate = AudioSettings.outputSampleRate;
#if UNITY_IOS && !UNITY_EDITOR
        ScrollyAudioPlayEvenWhenSilent();
#endif

        // A silent looping clip keeps this source playing, so OnAudioFilterRead keeps being called.
        var source = gameObject.AddComponent<AudioSource>();
        source.clip = AudioClip.Create("silence", 1024, 1, (int)sampleRate, false);
        source.loop = true;
        source.Play();
    }

    void Play(Voice voice)
    {
        if (!enabledCache) return;
        lock (padlock) pending.Add(voice);
    }

    // MARK: - Rendering (audio thread)

    void OnAudioFilterRead(float[] data, int channels)
    {
        lock (padlock)
        {
            foreach (var v in pending)
                if (voiceCount < voices.Length) voices[voiceCount++] = v;
            pending.Clear();
        }

        float dt = 1f / sampleRate;
        int frames = data.Length / channels;
        for (int f = 0; f < frames; f++)
        {
            float mix = 0;
            for (int i = 0; i < voiceCount; i++) mix += Next(ref voices[i], dt);
            mix = Mathf.Clamp(mix * 0.45f, -1f, 1f);
            for (int c = 0; c < channels; c++) data[f * channels + c] = mix;
        }

        int kept = 0;
        for (int i = 0; i < voiceCount; i++)
            if (voices[i].T < voices[i].Delay + voices[i].Duration) voices[kept++] = voices[i];
        voiceCount = kept;
    }

    float Next(ref Voice v, float dt)
    {
        float local = v.T - v.Delay;
        v.T += dt;
        if (local < 0 || local >= v.Duration) return 0;
        float p = local / v.Duration;
        float freq = v.F0 * Mathf.Pow(v.F1 / v.F0, p);   // exponential slide F0 → F1
        v.Phase += freq * dt;
        if (v.Phase >= 1)
        {
            v.Phase -= 1;
            if (v.Wave == Wave.Noise) v.Noise = NextNoise();
        }
        float s = v.Wave switch
        {
            Wave.Square => v.Phase < v.Duty ? 1 : -1,
            Wave.Triangle => 4 * Mathf.Abs(v.Phase - 0.5f) - 1,
            _ => v.Noise,
        };
        const float attack = 0.004f;
        float env;
        if (local < attack) env = local / attack;
        else if (p < v.Sustain) env = 1;
        else
        {
            float r = (1 - p) / Mathf.Max(0.0001f, 1 - v.Sustain);
            env = r * r;
        }
        return s * env * v.Volume;
    }

    float NextNoise()
    {
        rng ^= rng << 13;
        rng ^= rng >> 17;
        rng ^= rng << 5;
        return rng / (float)uint.MaxValue * 2 - 1;
    }

    // MARK: - Sound effects

    static readonly float[] Pentatonic = { 0, 2, 4, 7, 9 };

    public static float Note(int step, float baseFrequency = 261.63f)
    {
        int octave = step / 5;
        float semitones = octave * 12 + Pentatonic[step % 5];
        return baseFrequency * Mathf.Pow(2, semitones / 12f);
    }

    static Voice Square(float f0, float f1, float duration, float volume, float duty = 0.5f, float delay = 0, float sustain = 0) =>
        new Voice { Wave = Wave.Square, Duty = duty, F0 = f0, F1 = f1, Duration = duration, Volume = volume, Delay = delay, Sustain = sustain };

    static Voice Triangle(float f0, float f1, float duration, float volume, float delay = 0, float sustain = 0) =>
        new Voice { Wave = Wave.Triangle, F0 = f0, F1 = f1, Duration = duration, Volume = volume, Delay = delay, Sustain = sustain };

    static Voice NoiseVoice(float f0, float f1, float duration, float volume, float delay = 0, float sustain = 0) =>
        new Voice { Wave = Wave.Noise, F0 = f0, F1 = f1, Duration = duration, Volume = volume, Delay = delay, Sustain = sustain };

    /// One step of the rising climb tone. Higher notes get softer so the top isn't shrill.
    public void ClimbBlip(int step, int maxStep)
    {
        float f = Note(step);
        float height = step / (float)Mathf.Max(1, maxStep);
        Play(Square(f, f * 1.02f, 0.09f, 0.2f - 0.08f * height, 0.25f + 0.25f * height));
    }

    public void SnapBack(int fromStep)
    {
        float f = Note(Mathf.Clamp(fromStep, 4, 12));
        Play(Square(f, 90, 0.32f, 0.2f));
    }

    public void ZoneFanfare()
    {
        int[] steps = { 5, 7, 9, 10 };
        for (int i = 0; i < steps.Length; i++)
        {
            float f = Note(steps[i]);
            Play(Square(f, f, 0.12f, 0.2f, 0.25f, i * 0.09f));
        }
        float top = Note(12);
        Play(Square(top, top, 0.45f, 0.2f, 0.25f, 0.36f));
        Play(Triangle(Note(0), Note(0), 0.8f, 0.45f, 0.36f));
    }

    public void Hit()
    {
        Play(NoiseVoice(6000, 700, 0.3f, 0.35f));
        Play(Square(700, 60, 0.9f, 0.2f, 0.5f, 0.05f));
    }

    public void FallWhistle(float duration, float delay)
    {
        Play(Square(1400, 160, duration, 0.1f, 0.5f, delay, 0.85f));
        Play(NoiseVoice(2500, 700, duration, 0.07f, delay, 0.85f));
    }

    public void Thud()
    {
        Play(NoiseVoice(600, 60, 0.3f, 0.5f));
        Play(Triangle(120, 40, 0.35f, 0.8f));
    }

    public void Coin()
    {
        Play(Square(988, 988, 0.06f, 0.13f));
        Play(Square(1319, 1319, 0.22f, 0.13f, 0.5f, 0.06f));
    }

    public void Swoosh() => Play(NoiseVoice(4000, 1500, 0.12f, 0.12f));

    public void StaminaWarning() => Play(Triangle(220, 196, 0.12f, 0.5f));

    public void Slip()
    {
        Play(NoiseVoice(3000, 1200, 0.15f, 0.15f));
        Play(Square(900, 300, 0.15f, 0.15f, 0.125f));
    }

    public void NewBest()
    {
        int[] steps = { 10, 11, 12, 13, 15 };
        for (int i = 0; i < steps.Length; i++)
        {
            float f = Note(steps[i]);
            Play(Triangle(f, f, 0.1f, 0.45f, i * 0.06f));
        }
    }

    public void CountdownBeep(bool final)
    {
        float f = final ? 1046.5f : 523.25f;
        Play(Square(f, f, final ? 0.45f : 0.15f, 0.18f, 0.5f, 0, 0.6f));
    }

    public void Liftoff(float duration)
    {
        Play(NoiseVoice(250, 1800, duration, 0.32f, 0, 0.7f));
        Play(Square(70, 320, duration, 0.1f, 0.125f, 0, 0.7f));
    }

    public void Purchase()
    {
        int[] steps = { 7, 9, 12, 14 };
        for (int i = 0; i < steps.Length; i++)
        {
            float f = Note(steps[i]);
            Play(Triangle(f, f, 0.1f, 0.45f, i * 0.06f));
        }
        Coin();
    }

    /// One soft downward "bwomp". Ignores rapid repeats so mashing a button can't stack it up.
    public void Nope()
    {
        if (Time.unscaledTime - lastNope < 0.3f) return;
        lastNope = Time.unscaledTime;
        Play(Triangle(330, 196, 0.22f, 0.5f, 0, 0.3f));
    }

    public void Click() => Play(Triangle(880, 1320, 0.05f, 0.4f));

    // MARK: - The daily gift

    /// A punchy tap on the gift: a thump, a crack, and a quick run of notes that climbs with each tap.
    public void GiftTap(int tap, int tier)
    {
        Play(Triangle(200, 45, 0.16f, 0.9f));
        Play(NoiseVoice(6000, 900, 0.08f, 0.25f));
        for (int i = 0; i < 3; i++)
        {
            float f = Note(4 + tap * 2 + tier + i * 2);
            Play(Square(f, f * 1.03f, 0.08f, 0.12f, 0.25f, 0.035f + i * 0.045f));
        }
    }

    /// The gift upgrades: a sparkling glissando up and a bright chord.
    public void GiftUpgrade(int tier)
    {
        Play(Triangle(200, 45, 0.16f, 0.9f));
        Play(NoiseVoice(9000, 3000, 0.25f, 0.12f));
        for (int i = 0; i < 7; i++)
        {
            float f = Note(6 + tier + i);
            Play(Square(f, f, 0.07f, 0.1f, 0.25f, i * 0.035f));
        }
        foreach (int step in new[] { 10, 12, 14 })
        {
            float f = Note(step + tier);
            Play(Triangle(f, f, 0.5f, 0.35f, 0.26f, 0.4f));
        }
    }

    /// One beat of the suspense drumroll: a snare tick over a low pulse. Intensity 0..1 makes it brighter
    /// and louder; a better gift tier raises the pulse.
    public void SuspenseTick(float intensity, int tier)
    {
        Play(NoiseVoice(2500 + 3500 * intensity, 1200, 0.05f, 0.06f + 0.12f * intensity));
        float low = 65 * Mathf.Pow(2, (tier * 2 + intensity * 7) / 12f);
        Play(Triangle(low * 1.5f, low, 0.12f, 0.3f + 0.35f * intensity));
    }

    /// The rising whine while the gift shakes, just before it opens.
    public void GiftRiser(float duration)
    {
        Play(Square(220, 1760, duration, 0.07f, 0.5f, 0, 0.9f));
        Play(Square(330, 2640, duration, 0.04f, 0.25f, 0, 0.9f));
        Play(NoiseVoice(600, 7000, duration, 0.1f, 0, 0.9f));
    }

    /// The lid pops off: a big burst and a boom.
    public void GiftOpen()
    {
        Play(NoiseVoice(8000, 300, 0.5f, 0.4f));
        Play(Triangle(110, 35, 0.5f, 0.9f));
        Play(Square(1800, 600, 0.15f, 0.1f, 0.5f));
    }
}
