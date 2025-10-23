using System;
using System.Collections;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;

public static class HFSentimentClient
{
    public static IEnumerator Analyze(
        string baseUrl,
        string text,
        Action<HFResponse> onDone,
        Action<string> onError = null
    )
    {
        var req = new HFRequest { text = text ?? "" };
        var json = JsonUtility.ToJson(req);
        using var uwr = new UnityWebRequest($"{baseUrl.TrimEnd('/')}/analyze", "POST");
        uwr.uploadHandler = new UploadHandlerRaw(Encoding.UTF8.GetBytes(json));
        uwr.downloadHandler = new DownloadHandlerBuffer();
        uwr.SetRequestHeader("Content-Type", "application/json");
        yield return uwr.SendWebRequest();

        if (uwr.result != UnityWebRequest.Result.Success)
        {
            onError?.Invoke(uwr.error);
            yield break;
        }
        var resp = JsonUtility.FromJson<HFResponse>(uwr.downloadHandler.text);
        onDone?.Invoke(resp);
    }
}
