using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(AudioSource))]
public class DataSonifier : MonoBehaviour
{
    [Header("Timing")]
    [Tooltip("Minimum time each sentence/event lasts in seconds.")]
    public float durationPerStep = 0.5f; // was 0.12

    [Tooltip("Silence between sentences/events in seconds.")]
    public float gapBetweenSteps = 0.15f; // was 0.02

    [Header("Clip Timing")]
    [Tooltip("How much of each clip length to wait before the next note. 1 = full length.")]
    [Range(0.1f, 2f)]
    public float clipDurationScale = 0.7f;

    [Header("Instrument Clips")]
    [Tooltip("Positive, joyful feeling (e.g., Gayageum).")]
    public AudioClip gayageumJoy;

    [Tooltip("Sad / reflective feeling (e.g., Daegeum).")]
    public AudioClip daegeumSad;

    [Tooltip("Alternative negative / heavy instrument (e.g., Ajaeng).")]
    public AudioClip ajaeng;

    [Tooltip("Neutral / grounding bell (e.g., Pyeongjong).")]
    public AudioClip pyeongjong;

    [Tooltip("Extra positive / bright instrument (e.g., Yangeum).")]
    public AudioClip yanggeum;

    [Header("Pitch & Dynamics")]
    [Tooltip("Base pitch for all clips.")]
    public float basePitch = 1.0f;

    [Tooltip("How much the sentiment can bend the pitch up/down.")]
    public float pitchRange = 0.2f; // was 0.4

    [Tooltip("Minimum volume for low confidence sentences.")]
    [Range(0f, 1f)]
    public float minVolume = 0.4f; // was 0.3

    [Tooltip("Maximum volume for high confidence sentences.")]
    [Range(0f, 1f)]
    public float maxVolume = 0.9f; // was 1.0

    [Header("Debug")]
    public bool logEvents = false;

    private AudioSource audioSource;

    void Awake()
    {
        audioSource = GetComponent<AudioSource>();
        audioSource.playOnAwake = false;
        audioSource.loop = false;
    }

    /// <summary>
    /// Main entry from DemoController_HF_Samples.
    /// scores  ∈ [-1, 1]    (negative .. positive)
    /// mags    ∈ [0, 1]     (confidence / strength)
    /// </summary>
    public IEnumerator PlaySigned(List<float> scores, List<float> magnitudes)
    {
        if (scores == null || scores.Count == 0)
            yield break;

        for (int i = 0; i < scores.Count; i++)
        {
            float score = Mathf.Clamp(scores[i], -1f, 1f);

            float mag = 1f;
            if (magnitudes != null && i < magnitudes.Count)
                mag = Mathf.Clamp01(magnitudes[i]);

            // Choose which instrument to play based on sentiment
            AudioClip clip = ChooseClip(score);

            if (clip != null && audioSource != null)
            {
                // Map confidence → volume
                float vol = Mathf.Lerp(minVolume, maxVolume, mag);

                // Map sentiment → pitch shift within pitchRange
                float t = Mathf.InverseLerp(-1f, 1f, score); // -1 → 0, +1 → 1
                float pitchOffset = Mathf.Lerp(-pitchRange, pitchRange, t);
                audioSource.pitch = basePitch + pitchOffset;

                if (logEvents)
                {
                    Debug.Log(
                        $"[DataSonifier] step={i} score={score:F2} mag={mag:F2} "
                            + $"clip={clip.name} vol={vol:F2} pitch={audioSource.pitch:F2}"
                    );
                }

                audioSource.PlayOneShot(clip, vol);
            }

            //  NEW: wait based on clip length so it feels like a song, not chaos
            float clipDur = clip != null ? clip.length * clipDurationScale : durationPerStep;
            float waitTime = Mathf.Max(durationPerStep, clipDur) + gapBetweenSteps;

            yield return new WaitForSeconds(waitTime);
        }
    }

    /// <summary>
    /// Pick an instrument based on sentiment score.
    /// </summary>
    private AudioClip ChooseClip(float score)
    {
        // Positive
        if (score > 0.25f)
        {
            // Prefer Gayageum / Yanggeum for joy
            return FirstNonNull(gayageumJoy, yanggeum, pyeongjong);
        }

        // Negative
        if (score < -0.25f)
        {
            // Prefer Daegeum / Ajaeng for sadness/heaviness
            return FirstNonNull(daegeumSad, ajaeng, pyeongjong);
        }

        // Neutral
        return FirstNonNull(pyeongjong, yanggeum, gayageumJoy, daegeumSad, ajaeng);
    }

    private AudioClip FirstNonNull(params AudioClip[] clips)
    {
        foreach (var c in clips)
        {
            if (c != null)
                return c;
        }
        return null;
    }
}
