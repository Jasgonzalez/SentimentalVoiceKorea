using System.Collections;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class DemoController_HF_Samples : MonoBehaviour
{
    [Header("Refs")]
    public TMP_InputField inputText;
    public TMP_Text outputText;
    public Button analyzeAndPlayBtn;
    public SampleSonifier sampleSonifier;

    [Header("Server")]
    public string baseUrl = "http://127.0.0.1:8000";

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
            if (outputText)
                outputText.text = "No sentences found.";
            return;
        }

        var scores = resp.results.Select(r => Mathf.Clamp(r.score, -1f, 1f)).ToList();
        var confs = resp.results.Select(r => Mathf.Clamp01(r.confidence)).ToList();

        // UI summary
        if (outputText)
        {
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

        // Play with your samples
        StartCoroutine(sampleSonifier.PlaySigned(scores, confs));
    }

    static string Truncate(string s, int n) =>
        (string.IsNullOrEmpty(s) || s.Length <= n) ? s : s[..(n - 1)] + "…";
}
