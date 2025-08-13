
from fastapi import FastAPI
from sentence_transformers import SentenceTransformer
import uvicorn

app = FastAPI()
model = SentenceTransformer("all-MiniLM-L6-v2")

@app.post("/embed")
def embed(payload: dict):
    text = payload.get("text","")
    vec = model.encode(text).tolist()
    return vec

if __name__ == "__main__":
    uvicorn.run(app, host="0.0.0.0", port=8081)
