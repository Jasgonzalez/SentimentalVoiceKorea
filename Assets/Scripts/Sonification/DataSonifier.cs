using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(AudioSource))]
public class DataSonifier : MonoBehaviour
{
    [Header("Timing")]
    [Range(0.05f, 0.8f)]
    public float durationPerStep = 0.12f;

    [Range(0.00f, 0.2f)]
    public float gapBetweenSteps = 0.02f;

    [Header("Pitch Mapping (Hz)")]
    public float minFreq = 220f;
    public float maxFreq = 880f;

    [Header("Amplitude (Loudness)")]
    [Range(0f, 1f)]
    public float minAmp = 0.18f;

    [Range(0f, 1f)]
    public float maxAmp = 0.85f;
    public float ampGamma = 0.8f;

    [Header("Envelope (Attack/Decay)")]
    [Range(0f, 0.3f)]
    public float attackPortion = 0.01f;

    [Range(0f, 0.3f)]
    public float decayPortion = 0.02f;

    public enum WaveType
    {
        Sine,
        Square,
        Triangle,
        Saw,
    }

    public WaveType wave = WaveType.Sine;

    private AudioSource _src;
    private const int sampleRate = 48000;

    void Awake()
    {
        _src = GetComponent<AudioSource>();
    }

    /// <summary>
    /// Sonify normalized values [0,1].
    /// </summary>
    public IEnumerator Play01(List<float> values01, List<float> amps01 = null)
    {
        if (values01 == null || values01.Count == 0)
            yield break;

        for (int i = 0; i < values01.Count; i++)
        {
            float v = Mathf.Clamp01(values01[i]);
            float freq = Mathf.Lerp(minFreq, maxFreq, v);

            float a =
                amps01 != null && i < amps01.Count
                    ? Mathf.Clamp01(amps01[i])
                    : Mathf.Pow(v, ampGamma); // default loudness curve

            a = Mathf.Lerp(minAmp, maxAmp, a);

            var clip = MakeTone(freq, a, durationPerStep, wave);
            _src.PlayOneShot(clip);
            yield return new WaitForSeconds(durationPerStep + gapBetweenSteps);
            Destroy(clip);
        }
    }

    /// <summary>
    /// Sonify values in [-1, 1] (automatically normalized to [0,1]).
    /// </summary>
    public IEnumerator PlaySigned(List<float> valuesSigned, List<float> amps01 = null)
    {
        var vals01 = new List<float>(valuesSigned.Count);
        foreach (var x in valuesSigned)
            vals01.Add(0.5f * (x + 1f)); // -1..1 → 0..1
        yield return Play01(vals01, amps01);
    }

    /// <summary>
    /// Generate a procedural tone with optional waveform.
    /// </summary>
    private AudioClip MakeTone(float freq, float amp, float seconds, WaveType wt)
    {
        int n = Mathf.CeilToInt(sampleRate * Mathf.Max(0.01f, seconds));
        float[] buf = new float[n];

        int attack = Mathf.Max(1, Mathf.CeilToInt(attackPortion * n));
        int decay = Mathf.Max(1, Mathf.CeilToInt(decayPortion * n));

        for (int i = 0; i < n; i++)
        {
            float t = i / (float)sampleRate;

            // Envelope (fade in/out)
            float env = 1f;
            if (i < attack)
                env = i / (float)attack;
            else if (i > n - decay)
                env = (n - i) / (float)decay;

            // Generate waveform
            float sample = 0f;
            switch (wt)
            {
                case WaveType.Sine:
                    sample = Mathf.Sin(2f * Mathf.PI * freq * t);
                    break;
                case WaveType.Square:
                    sample = Mathf.Sign(Mathf.Sin(2f * Mathf.PI * freq * t));
                    break;
                case WaveType.Triangle:
                    sample = 2f * Mathf.Abs(2f * (t * freq - Mathf.Floor(t * freq + 0.5f))) - 1f;
                    break;
                case WaveType.Saw:
                    sample = 2f * (t * freq - Mathf.Floor(t * freq + 0.5f));
                    break;
            }

            buf[i] = amp * env * sample;
        }

        var clip = AudioClip.Create("tone", n, 1, sampleRate, false);
        clip.SetData(buf, 0);
        return clip;
    }
}
