from flask import Flask, request, jsonify, send_from_directory, make_response
import os, certifi, ssl
os.environ["SSL_CERT_FILE"] = certifi.where()
os.environ["REQUESTS_CA_BUNDLE"] = certifi.where()
import asyncio, uuid, os, edge_tts, re, ssl, aiohttp
import requests, os, uuid, asyncio, threading, edge_tts, re
from gtts import gTTS
from flask_cors import CORS
from sentence_transformers import SentenceTransformer, util
from collections import deque
from flask import abort
import aiohttp, ssl
import pyttsx3
from sklearn.metrics.pairwise import cosine_similarity
from sklearn.decomposition import PCA
import numpy as np
import matplotlib
matplotlib.use("Agg")  

import matplotlib.pyplot as plt
import os
import time
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
    "farewell": ["goodbye", "bye", "see you", "take care", "farewell"],
    "ask_for_quest": [
        "do you need help", "do you need anything", "can I help you", 
        "any task for me", "do you have work for me", "need assistance",
        "what can I do for you", "any quests", "got any jobs"
    ],
    "quest_confirmation": [
        "yes I will help", "sure I'll help", "okay I accept", "yes let's do it",
        "I agree", "count me in", "yes", "sure", "okay", "alright"
    ],
    "quest_status": [
        "what is my quest", "show my quests", "what tasks do I have",
        "check my quest progress", "quest status", "how is my quest going",
        "what am I supposed to do"
    ],
    "complete_quest": [
        "I finished the quest", "quest done", "I completed the task",
        "here are the items", "I have what you need", "task completed",
        "I'm done with the quest", "turn in quest"
    ]
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
    "IMPORTANT: You MUST respond ONLY in English. Never use Vietnamese or any other language. "
    "All your responses must be in clear.\n"
    "Never include code blocks, JSON, or technical details. Speak like a person.\n"
    "IMPORTANT INTENT RULES:\n"
    "You must classify the user's intent into one of the following:\n"
    "greeting, ask_direction, ask_for_quest, quest_confirmation, trade, combat, farewell, other\n"

    "FAREWELL:\n"
    "Return 'farewell' ONLY IF the input clearly indicates ending the conversation,\n"
    "such as saying goodbye (e.g., 'bye', 'goodbye', 'see you'', 'farewell', 'take care').\n"

    "OTHER:\n"
    "Return 'other'' ONLY IF:\n"
    "- The input expresses emotion, thoughts, or observation\n"
    "- AND does NOT indicate greeting, farewell, request, or action\n"
    "- AND does NOT clearly imply conversation ending\n"
    "If unsure between 'farewell' and 'other'', choose 'other'.\n"
    "DIRECTION RULE:\n"
    "If the intent is ask_direction:\n"
    "- Do NOT describe landmarks, paths, trees, or locations.\n"
    "- Do NOT give step-by-step directions.\n"
    "- Respond briefly that the place is nearby.\n"
    "- Say that your firefly friends will guide the player.\n"
    "- You have an items that can call out your firefly friends to guide the player.(you can meantion this or not it up to you\n"
    "NOTE: Do NOT guide the player how to use the firefly item, just mention it exists and you have it so you can help the player Locate the Directions.\n"
    "If player asks for the items, explain that it's a small lantern with a small light inside that can call your firefly friends to light the way.\n"
    "This item is a speacial gift from your grandmother when you was young.\n"
    "DO NOT ask the player when you use it just use it directly to help the player find the direction.\n"
    "- Keep it natural and gentle.\n"
)

VOICE, RATE, PITCH = "en-US-JennyNeural", "-10%", "+4Hz"
SESSIONS = {}

MAX_TURNS = 20

def detect_intent_semantic(text):
    user_embedding = EMB_MODEL.encode(text)

    best_intent = "other"
    best_conf = -1.0
    intent_embeddings = {}
    cosine_scores = {}
  

    for intent, examples in INTENT_EXAMPLES.items():
        emb = EMB_MODEL.encode(examples).mean(axis=0)
        intent_embeddings[intent] = emb
        sim = cosine_similarity([user_embedding], [emb])[0][0]
        cosine_scores[intent] = sim

        if sim > best_conf:
            best_conf = sim
            best_intent = intent
    print(f"[COSINE] intent={intent}, score={best_conf:.3f}")

    return best_intent, best_conf, user_embedding, intent_embeddings, cosine_scores



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


def detect_intent(text: str) -> str:
    intent_st, conf, user_embedding, intent_embeddings, cosine_scores = detect_intent_semantic(text)

    # 1️⃣ Vẽ diagram dựa trên SentenceTransformer
    draw_vector_similarity(
        user_text=text,
        final_intent=intent_st,
        user_vec=user_embedding,
        intent_vecs=list(intent_embeddings.values()),
        intent_labels=list(intent_embeddings.keys()),
        cosine_scores=cosine_scores
    )

    if conf >= INTENT_THRESHOLD:
        final_intent = intent_st
    else:
        final_intent = classify_intent_llama(text)
    return final_intent

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
    # Không cần clean_for_tts nữa vì đã clean ở tts_file()
    print(f"[TTS] 📡 Connecting to Microsoft TTS with voice: {VOICE}")
    
    try:
        # Không cần custom SSL context - để edge-tts tự xử lý
        communicator = edge_tts.Communicate(
            text,  # Text đã được clean rồi
            voice=VOICE,
            rate=normalize_rate(RATE),
            pitch=normalize_pitch(PITCH),
        )
        
        print(f"[TTS] ⏳ Downloading audio from Microsoft...")
        await communicator.save(out_path)
        print(f"[TTS] ✅ Audio saved successfully")
        
    except Exception as e:
        print(f"[TTS ASYNC ERROR] {type(e).__name__}: {e}")
        raise


def synth_to_file_blocking(text: str, out_path: str):
    asyncio.run(synth_to_file_async(text, out_path))



def tts_file_gtts(text: str, out_path: str):
    """Fallback TTS using Google TTS"""
    try:
        print(f"[TTS] 🔄 Using Google TTS (fallback)...")
        tts = gTTS(text=text, lang='en', slow=False)
        tts.save(out_path)
        
        if os.path.exists(out_path) and os.path.getsize(out_path) > 0:
            print(f"[TTS] ✅ Google TTS SUCCESS: {out_path} ({os.path.getsize(out_path)} bytes)")
            return True
        else:
            print(f"[TTS] ❌ Google TTS failed to create file")
            return False
    except Exception as e:
        print(f"[TTS] ❌ Google TTS ERROR: {e}")
        return False

def tts_file(text: str):
    os.makedirs("tmp", exist_ok=True)
    #Tạo thư mục tạm thời "tmp" nếu chưa tồn tại
    fname = f"tmp_{uuid.uuid4().hex}.mp3"  # ← MP3 cho cả Edge và gTTS
    #Tạo tên tệp duy nhất sử dụng UUID để tránh trùng lặp
    out_path = os.path.join("tmp", fname)
    
    # Clean text trước khi gửi
    cleaned_text = clean_for_tts(text)
    print(f"[TTS] 🔊 Original text: {text[:100]}")
    print(f"[TTS] 🧹 Cleaned text: {cleaned_text[:100]}")
    
    # TRY 1: Edge TTS (giọng tốt hơn nhưng hay bị lỗi)
    edge_success = False
    try:
        print(f"[TTS] 🎤 Trying Edge TTS first...")
        asyncio.run(synth_to_file_async(cleaned_text, out_path))
        
        # Kiểm tra file có tồn tại và có kích thước không
        if os.path.exists(out_path) and os.path.getsize(out_path) > 0:
            file_size = os.path.getsize(out_path)
            print(f"[TTS] ✅ Edge TTS SUCCESS: {out_path} ({file_size} bytes)")
            edge_success = True
        else:
            print(f"[TTS] ⚠️ Edge TTS created empty file")
    except Exception as e:
        print(f"[TTS] ⚠️ Edge TTS failed: {e}")
    
    # TRY 2: Google TTS (fallback nếu Edge fail)
    if not edge_success:
        print(f"[TTS] 🔄 Edge TTS failed, using Google TTS fallback...")
        tts_file_gtts(cleaned_text, out_path)
    
    return out_path, fname

def reduce_to_2d(vectors):
    pca = PCA(n_components=2)
    return pca.fit_transform(vectors)
def draw_vector_similarity(user_text, final_intent, user_vec, intent_vecs, intent_labels, cosine_scores):
    os.makedirs("diagrams", exist_ok=True)

    all_vecs = [user_vec] + intent_vecs
    reduced = reduce_to_2d(np.array(all_vecs))

    user_point = reduced[0]
    intent_points = reduced[1:]

    plt.figure(figsize=(9, 7))

    # User
    plt.scatter(user_point[0], user_point[1],
                c="red", s=120, label="User Input")

    for point, label in zip(intent_points, intent_labels):
        plt.scatter(point[0], point[1], c="blue")

        # vẽ đường nối
        plt.plot(
            [user_point[0], point[0]],
            [user_point[1], point[1]],
            linestyle="--",
            alpha=0.5
        )

        # ghi tên intent
        plt.text(point[0] + 0.01, point[1] + 0.01, label)

        # ghi cosine similarity ở giữa đường
        mid_x = (user_point[0] + point[0]) / 2
        mid_y = (user_point[1] + point[1]) / 2

        score = cosine_scores[label]
        plt.text(
            mid_x, mid_y,
            f"{score:.2f}",
            fontsize=9,
            color="darkgreen"
        )

    plt.title(
    "Semantic Similarity Visualization\n"
    f"User Input: \"{user_text}\"\n"
    f"Final Intent: {final_intent}",
    fontsize=12)
    plt.legend()
    plt.tight_layout()
    timestamp = int(time.time())
    plt.savefig(f"diagrams/intent_vector_similarity_cosine_{timestamp}.png")
    plt.close()

    print("[DIAGRAM] Saved with cosine scores")



@app.route("/audio/<name>")
#Tìm nạp và phục vụ tệp âm thanh TTS từ thư mục "tmp"

def serve_audio(name):
    path = os.path.join("tmp", name)
    #Ghép chuỗi
    if not os.path.exists(path):
        return abort(404, description=f"Audio file {name} not found")
    
    # Tự động detect MIME type dựa vào extension
    mimetype = "audio/wav" if name.endswith(".wav") else "audio/mpeg"
    resp = make_response(send_from_directory("tmp", name, mimetype=mimetype))
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
    quest_context = (data.get("quest_context") or "").strip()
    npc_context = (data.get("npc_context") or "").strip()
    quest_state = data.get("quest_state", "NONE")
    #Lấy dữ liệu JSON từ yêu cầu POST, trích xuất văn bản người dùng, session_id và contexts
    
    print(f"[DEBUG] Input: '{user_input}' | Has quest_context: {bool(quest_context)}")
    if quest_context:
        print(f"[DEBUG] Quest context (first 80 chars): {quest_context[:80]}...")
    
    if not user_input:
        return jsonify({"reply": "I didn’t hear anything...", "audio_url": None, "intent": "other"}), 200
    #Nếu văn bản người dùng rỗng, trả về phản hồi mặc định
    history = get_history(session_id)
    #Lấy lịch sử hội thoại cho session hiện tại
    intent = detect_intent(user_input)
    #Phát hiện intent từ văn bản người dùng
    low_text = user_input.lower()
    
    # PRIORITY 1: Check for quest confirmation if quest_context exists
    # This must be checked FIRST to prevent other keywords from overriding
    confirmation_keywords = ["yes", "sure", "okay", "ok", "i'll help", "i will help", "accept", "agree", "let's do it", "let me help", "count me in", "sounds good", "alright"]
    if quest_state == "WAITING_CONFIRM" and any(k in low_text for k in confirmation_keywords):
        intent = "quest_confirmation"
        print(f"[QUEST] Detected quest confirmation with keyword in: {low_text}")
    # PRIORITY 2: Check for quest completion
    elif any(k in low_text for k in ["finished", "completed", "complete", "done", "turn in", "here are", "i have", "i'm done", "task done", "quest done"]):
        intent = "complete_quest"
        print(f"[QUEST] Detected complete_quest with keyword in: {low_text}")
    # PRIORITY 3: Check for asking about quests
    elif any(k in low_text for k in ["need help", "need anything", "can i help", "any task", "any quest", "any job"]):
        intent = "ask_for_quest"
    # PRIORITY 4: Check for quest status
    elif any(k in low_text for k in ["my quest", "quest status", "quest progress", "what task", "check quest"]):
        intent = "quest_status"
    # PRIORITY 5: Other specific intents
    elif (
        "where" in low_text
        and any(p in low_text for p in ["village", "town"])
        and any(p in low_text for p in ["i", "me", "we"])
    ):
        intent = "ask_direction"
    elif any(k in low_text for k in ["attack", "fight", "wolf", "combat"]):
        intent = "combat"
    elif any(k in low_text for k in ["shop", "buy", "sell"]):
        intent = "trade"
    elif any(k in low_text for k in ["bye", "goodbye"]):
        intent = "farewell"
    elif any(k in low_text for k in ["flower", "pick", "gather", "bloom", "petal"]):
        intent = "gather_flower"    
    #Cải thiện phát hiện intent dựa trên từ khóa cụ thể - với ưu tiên quest confirmation
    history.append({"role": "user", "content": f"[intent={intent}] {user_input}"})
    #Lưu lịch sử hội thoại với định dạng đặc biệt để bao gồm intent
    
    # Build contextual system prompt
    contextual_prompt = system_prompt
    if quest_context:
        contextual_prompt += f"\n\n[QUEST INFO]\n{quest_context}\n"
        contextual_prompt += "When player asks about quests or help, naturally explain this quest in your own words IN ENGLISH. "
        contextual_prompt += "Make it sound like you really need help, not like you're reading from a quest log. "
        contextual_prompt += "After explaining, ask if they would be willing to help you. "
        contextual_prompt += "Remember: Respond ONLY in English, never in Vietnamese or other languages."
        contextual_prompt += ("\nIf the user says yes, okay, or agrees but there is no quest being offered, " "respond politely with confusion and ask what they mean. ")
    if npc_context:
        contextual_prompt += f"\n\n[YOUR CURRENT STATUS]\n{npc_context}"
    
    messages = [{"role": "system", "content": contextual_prompt}] + list(history)
    payload = {"model": MODEL_NAME, "messages": messages}
    # Tạo payload cho yêu cầu API Ollama với lịch sử hội thoại và context
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
    elif intent == "quest_confirmation":
        action = "ACCEPT_QUEST_CONFIRM"
        params = {
            "trigger": "player_confirmed",
            "next_quest_state": "NONE"
        }
    elif intent == "ask_for_quest":
        # Player asking if NPC needs help - explain quest but don't accept yet
        action = "QUEST_DIALOGUE"
        params = {
        "trigger": "player_ask_help",
        "next_quest_state": "WAITING_CONFIRM"
    }
    elif intent == "quest_status":
        action = "SHOW_QUEST_STATUS"
        params = {"open_quest_panel": True}
    elif intent == "complete_quest":
        action = "COMPLETE_QUEST"
        params = {"trigger": "turn_in"}
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
