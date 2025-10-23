using System.Collections;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class DemoController_HF : MonoBehaviour
{
    [Header("Refs")]
    public TMP_InputField inputText;
    public TMP_Text outputText;
    public Button analyzeAndPlayBtn;
    public DataSonifier sonifier;

    [Header("Server")]
    public string baseUrl = "http://127.0.0.1:8000";

    [Header("Timbre by Polarity")]
    public bool useWaveByPolarity = true;
    public DataSonifier.WaveType positiveWave = DataSonifier.WaveType.Saw;
    public DataSonifier.WaveType neutralWave = DataSonifier.WaveType.Sine;
    public DataSonifier.WaveType negativeWave = DataSonifier.WaveType.Triangle;

    void Start()
    {
        analyzeAndPlayBtn.onClick.AddListener(Run);
    }

    public void Run()
    {
        var text = !string.IsNullOrWhiteSpace(inputText.text)
            ? inputText.text
            : "I love this. The wait was awful! Overall fine.";
        StartCoroutine(
            HFSentimentClient.Analyze(
                baseUrl,
                text,
                OnResults,
                err =>
                {
                    if (outputText)
                        outputText.text = $"Error: {err}";
                }
            )
        );
    }

    void OnResults(HFResponse resp)
    {
        if (resp?.results == null || resp.results.Count == 0)
        {
            outputText.text = "No sentences found.";
            return;
        }

        var scores = resp.results.Select(r => Mathf.Clamp(r.score, -1f, 1f)).ToList();
        var amps = new List<float>(scores.Count);
        for (int i = 0; i < scores.Count; i++)
        {
            float mag = Mathf.Clamp01(Mathf.Abs(scores[i]));
            float conf = Mathf.Clamp01(resp.results[i].confidence);
            amps.Add(Mathf.Clamp01(0.6f * mag + 0.4f * conf));
        }

        if (useWaveByPolarity)
            StartCoroutine(PlayWithWavePerSentence(scores, amps));
        else
            StartCoroutine(sonifier.PlaySigned(scores, amps));

        var lines = new List<string> { $"Sentences: {resp.results.Count}" };
        for (int i = 0; i < resp.results.Count; i++)
        {
            var r = resp.results[i];
            string label =
                r.score > 0.15f ? "Positive"
                : r.score < -0.15f ? "Negative"
                : "Neutral";
            lines.Add(
                $"[{i + 1}] {label}  score={r.score:F2}  conf={r.confidence:F2}  \"{Truncate(r.sentence, 80)}\""
            );
        }
        outputText.text = string.Join("\n", lines);
    }

    IEnumerator PlayWithWavePerSentence(List<float> scores, List<float> amps01)
    {
        for (int i = 0; i < scores.Count; i++)
        {
            sonifier.wave =
                scores[i] > 0.15f ? positiveWave
                : scores[i] < -0.15f ? negativeWave
                : neutralWave;
            yield return sonifier.PlaySigned(
                new List<float> { scores[i] },
                new List<float> { amps01[i] }
            );
        }
    }

    static string Truncate(string s, int n) =>
        (string.IsNullOrEmpty(s) || s.Length <= n) ? s : s[..(n - 1)] + "…";
}
