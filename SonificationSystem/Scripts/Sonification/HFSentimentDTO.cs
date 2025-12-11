using System;
using System.Collections.Generic;

[Serializable]
public class HFSingle
{
    public string sentence;
    public float score;
    public float confidence;
}

[Serializable]
public class HFResponse
{
    public List<HFSingle> results;
}

[Serializable]
public class HFRequest
{
    public string text;
}
