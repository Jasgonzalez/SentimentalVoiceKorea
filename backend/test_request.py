import requests, json
j = {"text": "I love the vibe. The wait was awful. Overall okay."}
r = requests.post("http://127.0.0.1:8000/analyze", json=j, timeout=60)
print(json.dumps(r.json(), indent=2))
