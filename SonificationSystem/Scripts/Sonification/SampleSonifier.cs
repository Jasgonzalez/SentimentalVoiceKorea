using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(AudioSource))]
public class SampleSonifier : MonoBehaviour
{
    [Header("Clips (assign in Inspector)")]
    public List<AudioClip> positiveClips; // e.g., Gayageum (joy), Yangeum
    public List<AudioClip> neutralClips;  // e.g., Pyeongjong
    public List<AudioClip> negativeClips; // e.g., Daegeum (sad), Ajaeng

    [Header("Playback")]
    [Tooltip("Short pause between notes (seconds).")]
    [Range(0f, 0.5f)] public float gapBetweenSteps = 0.06f;

    [Tooltip("Map -1..1 sentiment to pitch semitones in [-semiRange, +semiRange].")]
    [Range(0f, 12f)] public float pitchSemiRange = 6f;

    [Tooltip("Minimum and maximum volume for a note (0..1).")]
    [Range(0f, 1f)] public float minVol = 0.15f;
    [Range(0f, 1f)] public float maxVol = 0.95f;

    [Tooltip("Blend: loudness = 0.6*|score| + 0.4*confidence")]
    public bool useMagConfBlend = true;

    AudioSource _src;

    void Awake()
    {
        _src = GetComponent<AudioSource>();
        _src.playOnAwake = false;
        _src.spatialBlend = 0f; // 2D
    }

    /// <summary> Plays a sequence using signed scores [-1,1] and optional confidences [0,1]. </summary>
    public IEnumerator PlaySigned(List<float> scores, List<float> confidences = null)
    {
        if (scores == null || scores.Count == 0) yield break;

        for (int i = 0; i < scores.Count; i++)
        {
            float score = Mathf.Clamp(scores[i], -1f, 1f);
            float conf  = (confidences != null && i < confidences.Count) ? Mathf.Clamp01(confidences[i]) : 0.5f;

            // choose clip by polarity
            AudioClip clip = ChooseClip(score);
            if (!clip) continue;

            // volume mapping
            float mag = Mathf.Abs(score);
            float loud01 = useMagConfBlend ? Mathf.Clamp01(0.6f * mag + 0.4f * conf) : mag;
            float volume = Mathf.Lerp(minVol, maxVol, loud01);

            // gentle pitch shift in semitones
            float semis = Mathf.Lerp(-pitchSemiRange, pitchSemiRange, 0.5f * (score + 1f));
            float pitch = Mathf.Pow(2f, semis / 12f);
            float prevPitch = _src.pitch;
            _src.pitch = pitch;

            _src.PlayOneShot(clip, volume);

            // wait: clip length scaled by pitch, then a small gap
            float wait = (clip.length / Mathf.Max(0.001f, pitch)) + gapBetweenSteps;
            yield return new WaitForSeconds(wait);

            _src.pitch = prevPitch; // restore
        }
    }

    AudioClip ChooseClip(float score)
    {
        if (score >  0.15f && positiveClips != null && positiveClips.Count > 0)
            return positiveClips[Random.Range(0, positiveClips.Count)];
        if (score < -0.15f && negativeClips != null && negativeClips.Count > 0)
            return negativeClips[Random.Range(0, negativeClips.Count)];
        if (neutralClips != null && neutralClips.Count > 0)
            return neutralClips[Random.Range(0, neutralClips.Count)];
        // fallback if a bucket is empty
        if (positiveClips != null && positiveClips.Count > 0) return positiveClips[0];
        if (negativeClips != null && negativeClips.Count > 0) return negativeClips[0];
        return null;
    }
}

