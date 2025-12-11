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
    public DataSonifier sampleSonifier;

    [Header("Story Mode")]
    public bool useMadinaStory = true;
    public TextAsset madinaStory; // assign MadinaStory.txt here in Inspector

    [Header("Behavior")]
    public bool autoRunOnStart = true;

    [Header("Server")]
    public string baseUrl = "http://127.0.0.1:8000";

    void Start()
    {
        if (analyzeAndPlayBtn != null)
            analyzeAndPlayBtn.onClick.AddListener(Run);

        if (autoRunOnStart)
            Run();
    }

    public void Run()
    {
        string text;

        // Story mode takes priority
        if (useMadinaStory && madinaStory != null)
        {
            text = madinaStory.text;
        }
        else
        {
            // fallback to user typing
            text =
                inputText != null && !string.IsNullOrWhiteSpace(inputText.text)
                    ? inputText.text
                    : "I love this. The wait was awful!";
        }

        // Send to HF server for sentiment analysis
        StartCoroutine(
            HFSentimentClient.Analyze(
                baseUrl,
                text,
                OnResults,
                err =>
                {
                    if (outputText != null)
                        outputText.text = $"Error: {err}";
                }
            )
        );
    }

    // HFResponse is whatever DTO your HFSentimentClient returns
    void OnResults(HFResponse resp)
    {
        if (resp == null || resp.results == null || resp.results.Count == 0)
        {
            if (outputText != null)
                outputText.text = "No results from sentiment server.";
            return;
        }

        var scores = resp.results.Select(r => Mathf.Clamp(r.score, -1f, 1f)).ToList();

        var confs = resp.results.Select(r => Mathf.Clamp01(r.confidence)).ToList();

        // Optional UI summary
        if (outputText != null)
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

        if (sampleSonifier != null && scores.Count > 0)
        {
            StartCoroutine(sampleSonifier.PlaySigned(scores, confs));
        }
    }

    static string Truncate(string s, int n) =>
        (string.IsNullOrEmpty(s) || s.Length <= n) ? s : s[..(n - 1)] + "…";
}
