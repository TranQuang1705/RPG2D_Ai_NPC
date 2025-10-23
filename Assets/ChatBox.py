from flask import Flask, request, jsonify, send_from_directory, make_response
import os, certifi, ssl
os.environ["SSL_CERT_FILE"] = certifi.where()
os.environ["REQUESTS_CA_BUNDLE"] = certifi.where()
import asyncio, uuid, os, edge_tts, re, ssl, aiohttp
import requests, os, uuid, asyncio, threading, edge_tts, re
from flask_cors import CORS
from sentence_transformers import SentenceTransformer, util
from collections import deque
from flask import abort
import aiohttp, ssl
import pyttsx3

app = Flask(__name__)
CORS(app)

# ========== INTENT DETECTION ==========
EMB_MODEL = SentenceTransformer('all-MiniLM-L6-v2')
INTENT_EXAMPLES = {
    "greeting": ["hello", "hi", "hey there", "how are you"],
    "ask_direction": [
        "where is the village", "how do I get to the village", "show me the way",
        "which way to go", "guide me", "how to reach the town", "where is the town"
    ],
    "combat": ["attack", "fight", "kill the wolf", "start combat", "go fight", "battle"],
    "trade": ["open shop", "show me your wares", "buy items", "sell goods", "trade"],
    "farewell": ["goodbye", "bye", "see you", "take care", "farewell"]
}

INTENT_EMB = {k: EMB_MODEL.encode(v, convert_to_tensor=True) for k, v in INTENT_EXAMPLES.items()}
INTENT_THRESHOLD = 0.55

OLLAMA_URL = "http://127.0.0.1:1234/v1/chat/completions"
MODEL_NAME = "llama-3.2-3b-instruct"

system_prompt = (
    "You are Snow, a gentle young girl in the countryside. "
    "You are picking wildflowers in a sunny meadow, wearing a white dress. "
    "You are kind, soft-spoken, sometimes shy, but warm-hearted. "
    "Always reply as Snow, briefly and naturally.\n"
    "Never include code blocks, JSON, or technical details. Speak like a person.\n"
)

VOICE, RATE, PITCH = "en-US-JennyNeural", "-10%", "+4Hz"
SESSIONS = {}

MAX_TURNS = 20

def detect_intent_semantic(text: str):
    if not text:
        return "other", 0.0
    sent_emb = EMB_MODEL.encode(text, convert_to_tensor=True)
    best_intent, best_score = "other", -1.0
    for intent, ex_emb in INTENT_EMB.items():
        score = float(util.cos_sim(sent_emb, ex_emb).mean().item())
        if score > best_score:
            best_intent, best_score = intent, score
    return best_intent, best_score


def classify_intent_llama(text: str) -> str:
    payload = {
        "model": MODEL_NAME,
        "messages": [
            {"role": "system",
             "content": ("Classify the user's intent into one of: "
                         "greeting, ask_direction, combat, trade, farewell, other. "
                         "Return only the single label (lowercase).")},
            {"role": "user", "content": text}
        ]
    }
    try:
        r = requests.post(OLLAMA_URL, json=payload, timeout=15)
        j = r.json()
        intent = (j["choices"][0]["message"]["content"] or "").strip().lower().split()[0]
        return intent if intent in INTENT_EXAMPLES.keys() or intent == "other" else "other"
    except:
        return "other"


def detect_intent(text: str):
    intent, conf = detect_intent_semantic(text)
    if conf < INTENT_THRESHOLD:
        intent = classify_intent_llama(text)
    return intent


def get_history(session_id: str):
    q = SESSIONS.get(session_id)
    if q is None:
        q = deque(maxlen=MAX_TURNS)
        SESSIONS[session_id] = q
    return q


def normalize_rate(rate: str):  return rate if re.fullmatch(r"[+-]?\d+%", rate) else "0%"
def normalize_pitch(pitch: str): return pitch if re.fullmatch(r"[+-]?\d+Hz", pitch or "") else "+0Hz"


def clean_for_tts(text: str) -> str:
    if not text:
        return "..."
    text = re.sub(r"\*[^*]*\*", "", text)
    text = re.sub(r"\[[^\]]*\]", "", text)
    text = re.sub(r"(\*\*|__)(.*?)\1", r"\2", text)
    text = re.sub(r"(\*|_)(.*?)\1", r"\2", text)
    text = re.sub(r"\s+", " ", text).strip()
    return text or "..."


async def synth_to_file_async(text: str, out_path: str):
    sslcontext = ssl.create_default_context()
    sslcontext.check_hostname = False
    sslcontext.verify_mode = ssl.CERT_NONE

    async with aiohttp.ClientSession(connector=aiohttp.TCPConnector(ssl=sslcontext)) as session:
        communicator = edge_tts.Communicate(
            clean_for_tts(text),
            voice=VOICE,
            rate=normalize_rate(RATE),
            pitch=normalize_pitch(PITCH),
        )
        await communicator.save(out_path)


def synth_to_file_blocking(text: str, out_path: str):
    asyncio.run(synth_to_file_async(text, out_path))



def tts_file(text: str):
    os.makedirs("tmp", exist_ok=True)
    fname = f"tmp_{uuid.uuid4().hex}.mp3"
    out_path = os.path.join("tmp", fname)
    try:
        asyncio.run(synth_to_file_async(text, out_path))
        print(f"[TTS] ✅ Saved MP3: {out_path}")
    except Exception as e:
        print(f"[TTS ERROR] {e}")
    return out_path, fname



@app.route("/audio/<name>")
def serve_audio(name):
    path = os.path.join("tmp", name)
    if not os.path.exists(path):
        return abort(404, description=f"Audio file {name} not found")
    resp = make_response(send_from_directory("tmp", name, mimetype="audio/mpeg"))
    resp.headers["Cache-Control"] = "no-store, no-cache, must-revalidate, max-age=0"
    resp.headers["Pragma"] = "no-cache"
    return resp



@app.route("/chat", methods=["POST"])
def chat():
    data = request.get_json(silent=True) or {}
    user_input = (data.get("text") or "").strip()
    session_id = (data.get("session_id") or "default").strip() or "default"

    if not user_input:
        return jsonify({"reply": "I didn’t hear anything...", "audio_url": None, "intent": "other"}), 200

    history = get_history(session_id)
    intent = detect_intent(user_input)
    low_text = user_input.lower()
    if any(k in low_text for k in ["village", "town", "where", "đường", "làng"]):
        intent = "ask_direction"
    elif any(k in low_text for k in ["attack", "fight", "wolf", "combat", "đánh"]):
        intent = "combat"
    elif any(k in low_text for k in ["shop", "buy", "sell", "mua", "bán"]):
        intent = "trade"
    elif any(k in low_text for k in ["bye", "goodbye", "tạm biệt"]):
        intent = "farewell"
    history.append({"role": "user", "content": f"[intent={intent}] {user_input}"})

    messages = [{"role": "system", "content": system_prompt}] + list(history)
    payload = {"model": MODEL_NAME, "messages": messages}

    try:
        print(f"[DEBUG] Sending to Ollama: {OLLAMA_URL}")
        print(f"[DEBUG] Payload: {payload}")
        resp = requests.post(OLLAMA_URL, json=payload, timeout=60)
        print(f"[DEBUG] Ollama response status: {resp.status_code}")
        j = resp.json()
        reply = (j["choices"][0]["message"]["content"] or "").strip()
    except Exception as e:
        reply = f"Ollama not reachable: {e}"

    history.append({"role": "assistant", "content": reply or ""})

    try:
        _, audio_name = tts_file(reply)
        audio_url = request.url_root.rstrip("/") + f"/audio/{audio_name}"
    except Exception as e:
        audio_url = None

    # ===== NEW: Map intent → game action =====
    action = None
    params = {}

    if intent == "ask_direction":
        action = "NAVIGATE"
        params = {"target": "village", "target_label": "Village"}
    elif intent == "combat":
        action = "START_COMBAT"
    elif intent == "trade":
        action = "OPEN_SHOP"
        params = {"shop_id": "default_shop"}
    elif intent == "farewell":
        action = "ANIM"
        params = {"name": "wave"}
    else:
        action = "NONE"

    return jsonify({
        "reply": reply,
        "audio_url": audio_url,
        "intent": intent,
        "action": action,
        "params": params
    }), 200


@app.route("/reset", methods=["POST"])
def reset():
    data = request.get_json(silent=True) or {}
    session_id = (data.get("session_id") or "default").strip()
    if session_id in SESSIONS:
        del SESSIONS[session_id]
    return jsonify({"ok": True})


if __name__ == "__main__":
    app.run(host="0.0.0.0", port=5000, debug=True)
