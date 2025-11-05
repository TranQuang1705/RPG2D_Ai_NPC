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
#Model "all-MiniLM-L6-v2" là loại Sentence Transformer đã được huấn luyện trước (pretrained) để hiểu ngữ nghĩa câu tiếng Anh.
#Ví dụ: “hello” và “hi there” → hai câu này nghĩa gần giống nhau, nên model tạo ra hai vector cũng gần nhau
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
MODEL_NAME = "Llama-3.2-3B-Instruct-GGUF"

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

#hàm này là bộ so khớp ngữ nghĩa giữa câu người dùng và các ví dụ intent, dùng cosine similarity để chấm điểm và chọn intent có độ giống ngữ nghĩa cao nhất.
def detect_intent_semantic(text: str):
    if not text:
        return "other", 0.0
    #nếu input rỗng → không thể suy ý định → trả ("other", 0.0)
    sent_emb = EMB_MODEL.encode(text, convert_to_tensor=True)
    #Mã hóa câu đầu vào thành vector nhúng (embedding vector) sử dụng mô hình Sentence Transformer đã tải trước đó.
    #convert_to_tensor=True giúp .encode() trả thẳng tensor thay vì mảng numpy
    best_intent, best_score = "other", -1.0
    #Khởi tạo kết quả tạm thời. -1.0 để đảm bảo bất kỳ điểm cosine hợp lệ nào (≥ -1) cũng sẽ lớn hơn giá trị khởi tạo này.
    for intent, ex_emb in INTENT_EMB.items():
        score = float(util.cos_sim(sent_emb, ex_emb).mean().item())
        if score > best_score:
            best_intent, best_score = intent, score
    return best_intent, best_score
#text = "can you show me the way to the town?"
#greeting: ví dụ kiểu “hello, hi…” → cosine thấp.
#ask_direction: ví dụ “where is the village / show me the way …” → cosine cao ở hầu hết ví dụ → điểm trung bình cao.
#Kết quả: ("ask_direction", ~0.82) chẳng hạn.
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
    #messages là danh sách hội thoại theo format chuẩn của API kiểu ChatGPT / OpenAI:
    #role: "system" → hướng dẫn cho AI về cách trả lời.
    #role: "user" → nội dung người dùng thật sự nói.
    try:
        r = requests.post(OLLAMA_URL, json=payload, timeout=15)
        j = r.json()
        #chuyển đổi phản hồi JSON từ Ollama thành dict Python
        intent = (j["choices"][0]["message"]["content"] or "").strip().lower().split()[0]
        #cắt chuỗi trả về, lấy từ đầu đến dấu cách đầu tiên, chuyển thành chữ thường
        return intent if intent in INTENT_EXAMPLES.keys() or intent == "other" else "other"
        #kiểm tra hợp lệ nhãn intent nếu không thì trả "other"
    except:
        return "other"


def detect_intent(text: str):
    intent, conf = detect_intent_semantic(text)
    #tính cosine similarity với intent và conf
    if conf < INTENT_THRESHOLD:
    #nếu điểm cosine thấp hơn ngưỡng (<0.55) → không chắc chắn → dùng mô hình Llama để phân loại intent
        intent = classify_intent_llama(text)
    #nếu điểm cosine cao hơn ngưỡng → dùng intent từ bộ so khớp ngữ nghĩa tự nhiên
    return intent
#Hàm này quyết định khi nào dùng cái nào — giống như “bộ điều phối” giữa 2 mô hình.

def get_history(session_id: str):
    #nhận vào 1 session_id (chuỗi định danh phiên trò chuyện)
    q = SESSIONS.get(session_id)
    #tạo session lưu trữ lịch sử hội thoại dạng hàng đợi (deque) với độ dài tối đa MAX_TURNS(20)
    if q is None:
        q = deque(maxlen=MAX_TURNS)
        SESSIONS[session_id] = q
    return q
#Lưu trữ lịch sử hội thoại cho từng phiên trò chuyện riêng biệt tránh NPC quên trò chuyện trước đó.

def normalize_rate(rate: str):  return rate if re.fullmatch(r"[+-]?\d+%", rate) else "0%"
def normalize_pitch(pitch: str): return pitch if re.fullmatch(r"[+-]?\d+Hz", pitch or "") else "+0Hz"
#Hai hàm này là “bộ lọc an toàn” cho đầu vào TTS, đảm bảo định dạng đúng.

def clean_for_tts(text: str) -> str:
    if not text:
        return "..."
    text = re.sub(r"\*[^*]*\*", "", text)
    text = re.sub(r"\[[^\]]*\]", "", text)
    text = re.sub(r"(\*\*|__)(.*?)\1", r"\2", text)
    text = re.sub(r"(\*|_)(.*?)\1", r"\2", text)
    text = re.sub(r"\s+", " ", text).strip()
    return text or "..."
#Làm sạch văn bản đầu vào

#Tạo tệp âm thanh TTS không đồng bộ sử dụng edge-tts và aiohttp để xử lý các yêu cầu HTTP một cách an toàn.
async def synth_to_file_async(text: str, out_path: str):
    sslcontext = ssl.create_default_context()
    sslcontext.check_hostname = False
    sslcontext.verify_mode = ssl.CERT_NONE

    async with aiohttp.ClientSession(connector=aiohttp.TCPConnector(ssl=sslcontext)) as session:
        #Hàm của thư viện edge-tts, dùng để gửi text đến máy chủ Microsoft Edge TTS
        communicator = edge_tts.Communicate(
            clean_for_tts(text),
            voice=VOICE,
            rate=normalize_rate(RATE),
            pitch=normalize_pitch(PITCH),
        )
        #chỉnh sửa tham số voice, rate, pitch theo cấu hình đã định nghĩa ở trên
        await communicator.save(out_path)
        #lưu tệp âm thanh TTS vào đường dẫn out_path


def synth_to_file_blocking(text: str, out_path: str):
    asyncio.run(synth_to_file_async(text, out_path))



def tts_file(text: str):
    os.makedirs("tmp", exist_ok=True)
    #Tạo thư mục tạm thời "tmp" nếu chưa tồn tại
    fname = f"tmp_{uuid.uuid4().hex}.mp3"
    #Tạo tên tệp duy nhất sử dụng UUID để tránh trùng lặp
    out_path = os.path.join("tmp", fname)
    try:
        asyncio.run(synth_to_file_async(text, out_path))
        #Gọi hàm async để thực sự chuyển text → file MP3.
        print(f"[TTS] ✅ Saved MP3: {out_path}")
    except Exception as e:
        print(f"[TTS ERROR] {e}")
    return out_path, fname



@app.route("/audio/<name>")
#Tìm nạp và phục vụ tệp âm thanh TTS từ thư mục "tmp"

def serve_audio(name):
    path = os.path.join("tmp", name)
    #Ghép chuỗi
    if not os.path.exists(path):
        return abort(404, description=f"Audio file {name} not found")
    resp = make_response(send_from_directory("tmp", name, mimetype="audio/mpeg"))
    #Gửi tệp âm thanh với kiểu MIME audio/mpeg
    resp.headers["Cache-Control"] = "no-store, no-cache, must-revalidate, max-age=0"
    resp.headers["Pragma"] = "no-cache"
    #Chặn trình duyệt lưu cache tệp âm thanh
    return resp



@app.route("/chat", methods=["POST"])
def chat():
    data = request.get_json(silent=True) or {}
    user_input = (data.get("text") or "").strip()
    session_id = (data.get("session_id") or "default").strip() or "default"
    #Lấy dữ liệu JSON từ yêu cầu POST, trích xuất văn bản người dùng và session_id
    if not user_input:
        return jsonify({"reply": "I didn’t hear anything...", "audio_url": None, "intent": "other"}), 200
    #Nếu văn bản người dùng rỗng, trả về phản hồi mặc định
    history = get_history(session_id)
    #Lấy lịch sử hội thoại cho session hiện tại
    intent = detect_intent(user_input)
    #Phát hiện intent từ văn bản người dùng
    low_text = user_input.lower()
    if any(k in low_text for k in ["village", "town", "where"]):
        intent = "ask_direction"
    elif any(k in low_text for k in ["attack", "fight", "wolf", "combat"]):
        intent = "combat"
    elif any(k in low_text for k in ["shop", "buy", "sell"]):
        intent = "trade"
    elif any(k in low_text for k in ["bye", "goodbye"]):
        intent = "farewell"
    elif any(k in low_text for k in ["flower", "pick", "gather", "bloom", "petal"]):
        intent = "gather_flower"
    #Cải thiện phát hiện intent dựa trên từ khóa cụ thể trong văn bản người dùng
    history.append({"role": "user", "content": f"[intent={intent}] {user_input}"})
    #Lưu lịch sử hội thoại với định dạng đặc biệt để bao gồm intent
    messages = [{"role": "system", "content": system_prompt}] + list(history)
    payload = {"model": MODEL_NAME, "messages": messages}
    # Tạo payload cho yêu cầu API Ollama với lịch sử hội thoại
    try:
        print(f"[DEBUG] Sending to LM Studio: {OLLAMA_URL}")
        print(f"[DEBUG] Payload: {payload}")
        resp = requests.post(OLLAMA_URL, json=payload, timeout=60)
        #Gửi yêu cầu POST đến API LM Studio với payload đã tạo
        print(f"[DEBUG] LM Studio response status: {resp.status_code}")
        j = resp.json()

        # ✅ FIX: LM Studio có thể trả về nội dung ở "choices[0].message.content" hoặc "choices[0].text"
        reply = (
            j.get("choices", [{}])[0]
             .get("message", {})
             .get("content")
            or j.get("choices", [{}])[0].get("text", "")
        )
        reply = (reply or "").strip()
        #Phân tích phản hồi JSON từ LM Studio để lấy nội dung trả lời

        if not reply:
            reply = "(no reply from model)"
    except Exception as e:
        reply = f"LM Studio not reachable: {e}"

    history.append({"role": "assistant", "content": reply or ""})
    #Luu phản hồi của NPC vào lịch sử hội thoại

    try:
        _, audio_name = tts_file(reply)
        audio_url = request.url_root.rstrip("/") + f"/audio/{audio_name}"
    #Tạo tệp âm thanh TTS cho phản hồi và tạo URL để truy cập tệp đó
    #Ghép URL dựa trên URL gốc của yêu cầu hiện tại
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
    elif intent == "gather_flower":
        action = "GATHER_FLOWER"
        params = {"target": "flower_field", "target_label": "Wildflowers"}
    else:
        action = "NONE"
    #Cầu nối intent đã phát hiện với các hành động trò chơi cụ thể và tham số liên quan
    return jsonify({
        "reply": reply,
        "audio_url": audio_url,
        "intent": intent,
        "action": action,
        "params": params
    }), 200
    #Trả về phản hồi JSON bao gồm văn bản trả lời, URL âm thanh, intent, hành động và tham số



@app.route("/reset", methods=["POST"])
def reset():
    data = request.get_json(silent=True) or {}
    session_id = (data.get("session_id") or "default").strip()
    if session_id in SESSIONS:
        del SESSIONS[session_id]
    return jsonify({"ok": True})
#Xóa lịch sử hội thoại cho một session cụ thể khi nhận được yêu cầu reset.

if __name__ == "__main__":
    app.run(host="0.0.0.0", port=5000, debug=True)
