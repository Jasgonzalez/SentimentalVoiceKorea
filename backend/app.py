# app.py
from fastapi import FastAPI
from pydantic import BaseModel
from typing import List, Dict, Any
import re
from transformers import pipeline

MODEL_ID = "cardiffnlp/twitter-roberta-base-sentiment-latest"

# use top_k=None (returns list of dicts with all labels/scores)
clf = pipeline("sentiment-analysis", model=MODEL_ID, top_k=None)  # <- change

app = FastAPI()

class AnalyzeRequest(BaseModel):
    text: str

def split_sentences(text: str) -> List[str]:
    parts = re.split(r'(?<=[\.\!\?])\s+', (text or "").strip())
    return [p.strip() for p in parts if p.strip()]

def signed_score(prob_list: List[Dict[str, Any]]) -> (float, float):
    # prob_list looks like: [{"label":"negative","score":...}, {"label":"neutral","score":...}, {"label":"positive","score":...}]
    d = {p["label"].lower(): p["score"] for p in prob_list}
    p_pos = d.get("positive", d.get("pos", 0.0))
    p_neg = d.get("negative", d.get("neg", 0.0))
    score = float(p_pos - p_neg)          # -1..1
    confidence = float(max(p_pos, p_neg)) # 0..1
    return score, confidence

@app.post("/analyze")
def analyze(req: AnalyzeRequest):
    sents = split_sentences(req.text)
    if not sents:
        return {"results": []}

    outs = clf(sents, batch_size=8, truncation=True)  # returns list[list[dict]]
    results = []
    for sent, probs in zip(sents, outs):
        score, conf = signed_score(probs)
        results.append({"sentence": sent, "score": score, "confidence": conf})
    return {"results": results}
