# BÁO CÁO KỸ THUẬT TRIỂN KHAI HỆ THỐNG NPC AI
## FINAL PROJECT REPORT - TECHNICAL IMPLEMENTATION

---

## 1. STATE MACHINE IMPLEMENTATION (Triển khai máy trạng thái)

### 1.1. NPC State Machine Architecture

**File:** `Assets/Scripts/NPCRoutineAI.cs`

#### Định nghĩa các State và Activity

```csharp
// Enum định nghĩa trạng thái NPC
public enum NPCState
{
    Idle,              // Đứng yên
    MovingToTarget,    // Di chuyển đến mục tiêu
    GatheringFlower,   // Đang hái hoa
    ReturningHome,     // Trở về nhà
    Resting,           // Đang nghỉ ngơi
    Socializing        // Giao tiếp xã hội
}

// Enum định nghĩa các hoạt động trong ngày
public enum NPCActivity
{
    Sleep,              // Ngủ (23:00 - 6:00)
    MorningRoutine,     // Dọn dẹp, ăn sáng (6:00 - 8:00)
    FlowerHunting,      // Đi hái hoa (8:00 - 12:00)
    MarketTrading,      // Bán hàng ở chợ (8:00 - 12:00)
    LunchBreak,         // Nghỉ trưa (12:00 - 13:00)
    ExploreVillage,     // Lang thang (13:00 - 17:00)
    EveningRoutine,     // Nấu ăn, trò chuyện (17:00 - 20:00)
    SocialTime,         // Gặp gỡ buổi tối (20:00 - 22:00)
    NightRoutine        // Chuẩn bị đi ngủ (22:00 - 23:00)
}
```

#### State Management Logic

```csharp
void UpdateCurrentActivity()
{
    // Lấy giờ hiện tại từ TimeManager hoặc dùng giá trị test
    float hour = useRealTimeManager && TimeManager.Instance != null
        ? TimeManager.Instance.GetCurrentHour()
        : currentGameTime;

    // Priority 1: Market trading for traders (8h-12h)
    if (isTrader && hour >= marketOpenHour && hour < marketCloseHour)
    {
        currentActivity = NPCActivity.MarketTrading;
    }
    // Priority 2: Flower hunting (14h-16h)
    else if (hour >= flowerHuntingStartHour && hour < flowerHuntingEndHour)
    {
        currentActivity = NPCActivity.FlowerHunting;
    }
    else
    {
        currentActivity = NPCActivity.FlowerHunting; // Default fallback
    }
}
```

**Đặc điểm kỹ thuật:**
- **Hierarchical State Machine**: Có 2 lớp trạng thái - Activity (macro) và State (micro)
- **Time-driven transitions**: Chuyển đổi trạng thái dựa trên thời gian game
- **Priority-based decision**: Ưu tiên các hoạt động theo thứ tự logic
- **Finite State Machine (FSM)**: Mỗi NPC chỉ ở một trạng thái tại một thời điểm

### 1.2. Enemy State Machine

**File:** `Assets/Scripts/Enemies/EnemyAI.cs`

```csharp
private enum State
{
    Roaming,    // Đi lang thang
    Attacking   // Tấn công
}

private void MovementStateControl()
{
    switch(state)
    {
        case State.Roaming:
            Roaming();
            break;
        case State.Attacking:
            Attacking();
            break;
    }
}

private void Roaming()
{
    // Di chuyển ngẫu nhiên
    timeRoaming += Time.deltaTime;
    enemyPathfinding.MoveTo(roamPosition);
    
    // Chuyển sang tấn công nếu player trong tầm
    if(Vector2.Distance(transform.position, PlayerController.Instance.transform.position) < attackRange)
    {
        state = State.Attacking;
    }
}

private void Attacking()
{
    // Quay về roaming nếu player ra khỏi tầm
    if (Vector2.Distance(transform.position, PlayerController.Instance.transform.position) > attackRange)
    {
        state = State.Roaming;
    }
    
    if(attackRange != 0 && canAttack)
    {
        canAttack = false;
        (enemyType as IEnemy).Attack();
        StartCoroutine(AttackCooldownRoutine());
    }
}
```

**Đặc điểm kỹ thuật:**
- **Simple FSM**: Chỉ 2 trạng thái chính (Roaming/Attacking)
- **Distance-based transitions**: Chuyển đổi dựa trên khoảng cách với player
- **Cooldown system**: Sử dụng Coroutine để quản lý thời gian hồi chiêu

---

## 2. A* PATHFINDING LOGIC (Thuật toán tìm đường A*)

**File:** `Assets/Scripts/NPCRoutineAI.cs` (Lines 1100-1170)

### 2.1. Node Structure

```csharp
class Node
{
    public Vector2Int pos;
    public float gCost, hCost;  // gCost: khoảng cách từ start, hCost: heuristic đến end
    public Node parent;
    public float fCost => gCost + hCost;  // Total cost

    public Node(Vector2Int position) => pos = position;
}
```

### 2.2. A* Algorithm Implementation

```csharp
List<Vector3> FindPath(Vector3 start, Vector3 end)
{
    Vector2Int startTile = new(Mathf.RoundToInt(start.x), Mathf.RoundToInt(start.y));
    Vector2Int endTile = new(Mathf.RoundToInt(end.x), Mathf.RoundToInt(end.y));

    List<Node> openList = new();      // Các node cần khám phá
    HashSet<Vector2Int> closedSet = new();  // Các node đã khám phá

    Node startNode = new(startTile);
    startNode.gCost = 0;
    startNode.hCost = Vector2Int.Distance(startTile, endTile);
    openList.Add(startNode);

    while (openList.Count > 0)
    {
        // Lấy node có fCost thấp nhất
        Node current = openList.OrderBy(n => n.fCost).First();

        // Nếu đến đích, xây dựng path
        if (current.pos == endTile)
            return ReconstructPath(current);

        openList.Remove(current);
        closedSet.Add(current.pos);

        // Kiểm tra các ô lân cận
        foreach (Vector2Int neighbor in GetNeighbors(current.pos))
        {
            if (closedSet.Contains(neighbor)) continue;
            if (!IsWalkable(neighbor)) continue;

            float tentativeG = current.gCost + Vector2Int.Distance(current.pos, neighbor);

            Node neighborNode = openList.FirstOrDefault(n => n.pos == neighbor);
            if (neighborNode == null)
            {
                neighborNode = new Node(neighbor);
                neighborNode.parent = current;
                neighborNode.gCost = tentativeG;
                neighborNode.hCost = Vector2Int.Distance(neighbor, endTile);
                openList.Add(neighborNode);
            }
            else if (tentativeG < neighborNode.gCost)
            {
                neighborNode.parent = current;
                neighborNode.gCost = tentativeG;
            }
        }
    }

    return new List<Vector3>(); // Không tìm thấy đường
}
```

### 2.3. Walkability Check

```csharp
bool IsWalkable(Vector2Int tile)
{
    // Kiểm tra trong map bounds
    if (tile.x < 0 || tile.x >= mapGenerator.width ||
        tile.y < 0 || tile.y >= mapGenerator.height)
        return false;

    // Kiểm tra obstacle/water bằng Physics2D
    Vector3 worldPos = new(tile.x + 0.5f, tile.y + 0.5f, 0);
    Collider2D hit = Physics2D.OverlapCircle(worldPos, 0.25f, 
        LayerMask.GetMask("Obstacle", "Water"));
    return hit == null;
}
```

### 2.4. Path Following

```csharp
public IEnumerator MoveToPosition(Vector3 targetPos, System.Action<bool> onComplete)
{
    // Tìm path bằng A*
    List<Vector3> path = FindPath(transform.position, targetPos);
    
    if (path == null || path.Count == 0)
    {
        onComplete?.Invoke(false);
        yield break;
    }

    int index = 0;
    float stopDistance = 0.15f;
    float finalStop = 0.45f;
    int mask = LayerMask.GetMask("Obstacle", "Water");

    while (index < path.Count)
    {
        Vector3 next = path[index];
        Vector2 dir = ((Vector2)next - rb.position);
        float dist = dir.magnitude;

        if (dist < stopDistance)
        {
            index++;
            continue;
        }

        dir.Normalize();

        // Obstacle avoidance
        if (Physics2D.Raycast(rb.position, dir, 0.25f, mask))
        {
            // Tính lại path nếu gặp obstacle mới
            path = FindPath(transform.position, targetPos);
            index = 0;
            yield return new WaitForSeconds(0.1f);
            continue;
        }

        // Di chuyển
        rb.MovePosition(rb.position + dir * moveSpeed * Time.fixedDeltaTime);

        // Kiểm tra đã đến đích chưa
        float distToTarget = Vector3.Distance(rb.position, targetPos);
        if (distToTarget < finalStop)
        {
            currentState = NPCState.Idle;
            onComplete?.Invoke(true);
            yield break;
        }

        yield return new WaitForFixedUpdate();
    }

    rb.velocity = Vector2.zero;
    currentState = NPCState.Idle;
    onComplete?.Invoke(true);
}
```

**Đặc điểm kỹ thuật:**
- **A* Algorithm**: Tìm đường ngắn nhất với heuristic Manhattan distance
- **Dynamic replanning**: Tự động tính lại path khi gặp obstacle
- **Grid-based navigation**: Sử dụng tile grid từ MapGenerator
- **Smooth movement**: Di chuyển liên tục bằng Rigidbody2D.MovePosition
- **Collision avoidance**: Raycast để phát hiện và tránh obstacle

---

## 3. ROUTINE UPDATE COROUTINE (Coroutine cập nhật routine)

**File:** `Assets/Scripts/NPCRoutineAI.cs`

### 3.1. Main Routine Loop

```csharp
IEnumerator SimpleFlowerHunting()
{
    Debug.Log($"🌸 {gameObject.name}: SimpleFlowerHunting started");

    while (true)
    {
        // Skip logic khi đang pause (đang nói chuyện với player)
        if (isPaused)
        {
            yield return new WaitForSeconds(0.5f);
            continue;
        }

        // PRIORITY: Nếu đang MarketTrading, không update activity
        if (currentActivity == NPCActivity.MarketTrading)
        {
            NPCTrader trader = GetComponent<NPCTrader>();
            if (trader != null && trader.IsMarketHours())
            {
                yield return new WaitForSeconds(1f);
                continue;
            }
            else
            {
                UpdateCurrentActivity();
            }
        }
        else
        {
            UpdateCurrentActivity();
        }

        // Market Trading Activity
        if (currentActivity == NPCActivity.MarketTrading)
        {
            yield return StartCoroutine(MarketTradingRoutine());
            yield return new WaitForSeconds(1f);
            continue;
        }

        bool isFlowerTime = IsFlowerHuntingTime();
        
        // Chỉ hái hoa khi: trong giờ hái hoa HOẶC có request từ người chơi
        if (!playerMadeRequest && !isFlowerTime)
        {
            currentState = NPCState.Idle;
            yield return StartCoroutine(IdleRoutine());
            yield return new WaitForSeconds(3f);
            continue;
        }

        // Flower gathering logic
        FlowerObject nearestFlower = FindNearestFlowerSimple();

        if (nearestFlower != null)
        {
            // Di chuyển đến hoa
            bool reachedFlowerMove = false;
            moveRoutine = StartCoroutine(MoveToPosition(nearestFlower.position, 
                r => reachedFlowerMove = r));
            yield return moveRoutine;
            
            float distance = Vector3.Distance(transform.position, nearestFlower.position);
            if (distance <= flowerDetectionRadius)
            {
                // Hái hoa
                yield return StartCoroutine(GatherFlower(nearestFlower));
                yield return new WaitForSeconds(2f);

                if (playerMadeRequest)
                {
                    playerMadeRequest = false;
                }
            }
        }
        else
        {
            if (playerMadeRequest)
            {
                playerMadeRequest = false;
                currentState = NPCState.Idle;
                yield return StartCoroutine(IdleRoutine());
            }
        }

        yield return new WaitForSeconds(1f);
    }
}
```

### 3.2. Market Trading Routine

```csharp
IEnumerator MarketTradingRoutine()
{
    NPCTrader trader = GetComponent<NPCTrader>();
    if (trader == null) yield break;

    // STEP 1: Di chuyển đến market
    Vector3 stallPos = marketStallLocation.position;
    
    bool reached = false;
    moveRoutine = StartCoroutine(MoveToPosition(stallPos, r => reached = r));
    yield return moveRoutine;
    
    if (!reached)
    {
        Debug.LogError($"Failed to reach Market Stall");
        yield break;
    }

    // STEP 2: Idle while trading
    currentState = NPCState.Idle;
    Rigidbody2D rb = GetComponent<Rigidbody2D>();
    if (rb) rb.velocity = Vector2.zero;

    while (currentActivity == NPCActivity.MarketTrading)
    {
        yield return new WaitForSeconds(1f);
    }
}
```

### 3.3. Flower Gathering Routine

```csharp
IEnumerator GatherFlower(FlowerObject flower)
{
    if (!flower.isAvailable) yield break;

    // Lock flower để tránh NPC khác hái cùng lúc
    flower.isAvailable = false;

    yield return StartCoroutine(GatheringTimer(flower));
}

IEnumerator GatheringTimer(FlowerObject flower)
{
    float timer = 0f;
    Vector3 lockedPos = transform.position;

    while (timer < gatheringTime)
    {
        // Lock vị trí để không bị rơi
        transform.position = lockedPos;

        // Kiểm tra khoảng cách
        float currentDistance = Vector3.Distance(transform.position, flower.position);
        if (currentDistance > flowerDetectionRadius * 2f)
        {
            Debug.LogWarning($"NPC rời xa hoa — có thể rơi xuống?");
            yield break;
        }

        // Đảm bảo Z = 0 (2D game)
        if (Mathf.Abs(transform.position.z) > 0.1f)
        {
            transform.position = new Vector3(transform.position.x, 
                                             transform.position.y, 0f);
        }

        timer += Time.deltaTime;
        yield return null;
    }

    // Hoàn thành hái hoa
    if (flower.gameObject != null)
    {
        if (FlowerManager.Instance != null)
        {
            StartCoroutine(DelayedRemoveFlower(flower.gameObject, 1.5f));
        }
        else
        {
            Destroy(flower.gameObject, 0.1f);
        }
    }
}
```

**Đặc điểm kỹ thuật:**
- **Hierarchical Coroutines**: Routine chính gọi các sub-routine
- **State preservation**: Lưu trạng thái khi pause/resume
- **Time-based execution**: Sử dụng WaitForSeconds để timing
- **Non-blocking**: Không block main thread
- **Error handling**: Kiểm tra và xử lý các trường hợp edge case

---

## 4. AI BACKEND PIPELINE (Pipeline AI Backend)

**File:** `Assets/ChatBox.py` (Flask Server)

### 4.1. Architecture Overview

```
[Unity Client] → [Flask Server] → [LLM (LM Studio/Ollama)] → [TTS (Edge-TTS)] → [Unity Client]
      ↑                   ↓
      └─────── Audio URL ─────┘
```

### 4.2. Intent Detection System

#### Semantic Similarity Matching

```python
# Model: Sentence Transformer
EMB_MODEL = SentenceTransformer('all-MiniLM-L6-v2')

INTENT_EXAMPLES = {
    "greeting": ["hello", "hi", "hey there", "how are you"],
    "ask_direction": ["where is the village", "how do I get to", "show me the way"],
    "combat": ["attack", "fight", "kill the wolf", "start combat"],
    "trade": ["open shop", "show me your wares", "buy items"],
    "ask_for_quest": ["do you need help", "can I help you", "any task for me"],
    "quest_confirmation": ["yes I will help", "sure I'll help", "okay I accept"],
    "quest_status": ["what is my quest", "show my quests", "quest status"],
    "complete_quest": ["I finished the quest", "quest done", "task completed"]
}

# Encode tất cả intent examples
INTENT_EMB = {k: EMB_MODEL.encode(v, convert_to_tensor=True) 
              for k, v in INTENT_EXAMPLES.items()}

def detect_intent_semantic(text: str):
    if not text:
        return "other", 0.0
        
    # Encode câu input
    sent_emb = EMB_MODEL.encode(text, convert_to_tensor=True)
    
    best_intent, best_score = "other", -1.0
    
    # So sánh với tất cả intent examples
    for intent, ex_emb in INTENT_EMB.items():
        # Tính cosine similarity
        score = float(util.cos_sim(sent_emb, ex_emb).mean().item())
        if score > best_score:
            best_intent, best_score = intent, score
            
    return best_intent, best_score
```

#### LLM Fallback Classification

```python
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
```

#### Hybrid Intent Detection

```python
INTENT_THRESHOLD = 0.55

def detect_intent(text: str):
    # BƯỚC 1: Thử semantic matching
    intent, conf = detect_intent_semantic(text)
    
    # BƯỚC 2: Nếu confidence thấp, dùng LLM
    if conf < INTENT_THRESHOLD:
        intent = classify_intent_llama(text)
    
    return intent
```

### 4.3. LLM Integration (LM Studio)

```python
OLLAMA_URL = "http://127.0.0.1:1234/v1/chat/completions"
MODEL_NAME = "Llama-3.2-3B-Instruct-GGUF"

system_prompt = (
    "You are Snow, a gentle young girl in the countryside. "
    "You are picking wildflowers in a sunny meadow, wearing a white dress. "
    "You are kind, soft-spoken, sometimes shy, but warm-hearted. "
    "Always reply as Snow, briefly and naturally.\n"
    "IMPORTANT: You MUST respond ONLY in English."
)

def get_history(session_id: str):
    q = SESSIONS.get(session_id)
    if q is None:
        q = deque(maxlen=MAX_TURNS)  # Giới hạn 20 turns
        SESSIONS[session_id] = q
    return q

@app.route("/chat", methods=["POST"])
def chat():
    data = request.get_json(silent=True) or {}
    user_input = (data.get("text") or "").strip()
    session_id = (data.get("session_id") or "default").strip()
    quest_context = (data.get("quest_context") or "").strip()
    npc_context = (data.get("npc_context") or "").strip()
    
    if not user_input:
        return jsonify({"reply": "I didn't hear anything...", 
                       "audio_url": None, "intent": "other"}), 200
    
    # Lấy lịch sử hội thoại
    history = get_history(session_id)
    
    # Phát hiện intent
    intent = detect_intent(user_input)
    
    # Build contextual prompt
    contextual_prompt = system_prompt
    if quest_context:
        contextual_prompt += f"\n\n[QUEST INFO]\n{quest_context}\n"
    if npc_context:
        contextual_prompt += f"\n\n[YOUR CURRENT STATUS]\n{npc_context}"
    
    # Tạo payload cho LLM
    messages = [{"role": "system", "content": contextual_prompt}] + list(history)
    payload = {"model": MODEL_NAME, "messages": messages}
    
    # Gọi LLM API
    resp = requests.post(OLLAMA_URL, json=payload, timeout=60)
    j = resp.json()
    reply = j.get("choices", [{}])[0].get("message", {}).get("content", "")
    
    # Lưu lịch sử
    history.append({"role": "user", "content": f"[intent={intent}] {user_input}"})
    history.append({"role": "assistant", "content": reply or ""})
    
    # Tạo audio TTS
    _, audio_name = tts_file(reply)
    audio_url = request.url_root.rstrip("/") + f"/audio/{audio_name}"
    
    # Map intent → game action
    action = None
    params = {}
    
    if intent == "ask_direction":
        action = "NAVIGATE"
        params = {"target": "village", "target_label": "Village"}
    elif intent == "gather_flower":
        action = "GATHER_FLOWER"
        params = {"target": "flower_field", "target_label": "Wildflowers"}
    elif intent == "quest_confirmation":
        action = "ACCEPT_QUEST_CONFIRM"
        params = {"trigger": "player_confirmed"}
    
    return jsonify({
        "reply": reply,
        "audio_url": audio_url,
        "intent": intent,
        "action": action,
        "params": params
    }), 200
```

### 4.4. Text-to-Speech (TTS) System

```python
import edge_tts
import aiohttp
import ssl

VOICE = "en-US-JennyNeural"
RATE = "-10%"
PITCH = "+4Hz"

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

def tts_file(text: str):
    os.makedirs("tmp", exist_ok=True)
    fname = f"tmp_{uuid.uuid4().hex}.mp3"
    out_path = os.path.join("tmp", fname)
    
    try:
        asyncio.run(synth_to_file_async(text, out_path))
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
    return resp
```

**Đặc điểm kỹ thuật:**
- **Multi-stage Intent Detection**: Semantic + LLM fallback
- **Session Management**: Lưu lịch sử hội thoại theo session ID
- **Context-aware LLM**: Inject quest và NPC context vào prompt
- **Async TTS**: Microsoft Edge TTS với async/await pattern
- **Action Mapping**: Chuyển đổi intent thành game actions

---

## 5. NPC OVERRIDE LOGIC (Logic ghi đè NPC)

**File:** `Assets/Scripts/NPCRoutineAI.cs`

### 5.1. Pause/Resume System

```csharp
private bool isPaused = false;
private NPCActivity pausedActivity;
private IEnumerator pausedCoroutine;

public void PauseCurrentActivity()
{
    if (isPaused) return;
    isPaused = true;

    // Không StopAllCoroutines — chỉ tạm dừng di chuyển
    Rigidbody2D rb = GetComponent<Rigidbody2D>();
    if (rb != null)
    {
        rb.velocity = Vector2.zero;
        rb.angularVelocity = 0f;
    }

    currentState = NPCState.Idle;

    if (animator)
    {
        animator.SetBool("Walking", false);
        animator.SetBool("Idle", true);
    }
}

public void ResumeCurrentActivity()
{
    if (!isPaused) return;
    isPaused = false;

    Debug.Log($"▶️ {gameObject.name}: Resuming activity");
    
    // SimpleFlowerHunting vẫn đang chạy, chỉ cần unpause
    // Coroutine sẽ tự tiếp tục từ vị trí đã pause
}
```

### 5.2. Player Request System

```csharp
private bool playerMadeRequest = false;

/// <summary>
/// Called by NavActionHandler when player requests flower gathering
/// </summary>
public void PlayerMadeGatheringRequest()
{
    Debug.Log($"🌸 {gameObject.name}: Player requested flower gathering!");
    playerMadeRequest = true;

    // Reset request after completion
    if (stopResetCoroutine != null)
    {
        StopCoroutine(stopResetCoroutine);
    }
    stopResetCoroutine = StartCoroutine(ResetPlayerRequest());
}

private Coroutine stopResetCoroutine;

IEnumerator ResetPlayerRequest()
{
    yield return new WaitForSeconds(30f);
    playerMadeRequest = false;
    Debug.Log($"🌸 {gameObject.name}: Reset player request (timeout)");
}

public bool HasPlayerRequest()
{
    return playerMadeRequest;
}

public void ForceResetPlayerRequest()
{
    playerMadeRequest = false;
    if (stopResetCoroutine != null)
    {
        StopCoroutine(stopResetCoroutine);
        stopResetCoroutine = null;
    }
}
```

### 5.3. Market Trading Override

```csharp
private bool physicsLockedForMarket = false;

public void DisablePhysicsForMarket()
{
    physicsLockedForMarket = true;
    if (moveRoutine != null)
    {
        StopCoroutine(moveRoutine);
        moveRoutine = null;
    }

    Rigidbody2D rb = GetComponent<Rigidbody2D>();
    if (rb != null)
    {
        rb.velocity = Vector2.zero;
        rb.simulated = false;  // Tắt hoàn toàn physics
    }
}

public void EnablePhysicsAfterMarket()
{
    physicsLockedForMarket = false;
    Rigidbody2D rb = GetComponent<Rigidbody2D>();
    if (rb != null)
    {
        rb.simulated = true;
    }
}
```

**Đặc điểm kỹ thuật:**
- **Non-destructive pause**: Không stop coroutine, chỉ pause logic
- **Request-based override**: Player có thể yêu cầu NPC thực hiện hành động
- **Physics locking**: Khóa physics khi NPC ở trạng thái đặc biệt (market)
- **State preservation**: Lưu và khôi phục trạng thái khi pause/resume
- **Timeout mechanism**: Tự động reset request sau một khoảng thời gian

---

## 6. DIALOGUE → ACTION SYSTEM (Hệ thống chuyển đổi Dialogue thành Action)

### 6.1. Architecture Overview

```
[Player Voice] → [Whisper STT] → [Flask Server] → [Intent Detection] → [LLM Response]
                                         ↓
                                  [Action Mapping]
                                         ↓
                                  [Unity Handler]
                                         ↓
                        ┌────────────────┴────────────────┐
                        ↓                                 ↓
              [NavActionHandler]                   [NPC Component]
              (Global Actions)                  (NPC-specific Actions)
```

### 6.2. Unity Client - NpcChatSpeaker.cs

```csharp
private IEnumerator CoAskServer(string userText, string questContext = null, string npcContext = null)
{
    // Tạo JSON payload
    string payload = "{\"text\":\"" + EscapeJson(userText) + 
                     "\",\"session_id\":\"" + EscapeJson(sessionId) + "\"";
    
    if (!string.IsNullOrEmpty(questContext))
    {
        payload += ",\"quest_context\":\"" + EscapeJson(questContext) + "\"";
    }
    
    if (!string.IsNullOrEmpty(npcContext))
    {
        payload += ",\"npc_context\":\"" + EscapeJson(npcContext) + "\"";
    }
    
    payload += "}";
    
    using (UnityWebRequest req = new UnityWebRequest(chatUrl, "POST"))
    {
        byte[] bodyRaw = System.Text.Encoding.UTF8.GetBytes(payload);
        req.uploadHandler = new UploadHandlerRaw(bodyRaw);
        req.downloadHandler = new DownloadHandlerBuffer();
        req.SetRequestHeader("Content-Type", "application/json");

        yield return req.SendWebRequest();

        if (req.result != UnityWebRequest.Result.Success)
        {
            OnSpeakEnd?.Invoke();
            yield break;
        }

        string json = req.downloadHandler.text;
        var br = JsonUtility.FromJson<BotReply>(json);
        
        // Phân loại action: NPC-specific vs Global
        bool isNpcAction = IsNpcSpecificAction(br.action);
        
        if (isNpcAction && npcComponent != null)
        {
            // Actions dành cho NPC cụ thể
            npcComponent.HandleChatbotAction(br.action, parameters);
        }
        else
        {
            // Global actions → NavActionHandler
            if (navHandler != null)
            {
                navHandler.HandleServerAction(new ServerResponse
                {
                    action = br.action,
                    intent = br.intent,
                    reply = br.reply,
                    @params = br.@params
                });
            }
        }
        
        // Phát âm thanh TTS
        if (!string.IsNullOrEmpty(br.audio_url))
        {
            currentAudioCo = StartCoroutine(CoDownloadAndPlay(br.audio_url, thisId, replyText));
        }
    }
}

private bool IsNpcSpecificAction(string action)
{
    switch (action)
    {
        // NPC-specific actions
        case "GATHER_FLOWER":
        case "ASK_FOR_QUEST":
        case "QUEST_DIALOGUE":
        case "ACCEPT_QUEST_CONFIRM":
        case "COMPLETE_QUEST":
        case "SHOW_QUEST_STATUS":
        case "ANIM":
            return true;
        
        // Global actions
        case "NAVIGATE":
        case "START_COMBAT":
        case "OPEN_SHOP":
        case "NONE":
        default:
            return false;
    }
}
```

### 6.3. Global Action Handler - NavActionHandler.cs

```csharp
public void HandleServerAction(ServerResponse resp)
{
    if (resp == null || string.IsNullOrEmpty(resp.action))
    {
        Debug.LogWarning("⚠️ Invalid server response");
        return;
    }

    switch (resp.action)
    {
        case "NAVIGATE":
            if (mapGenerator != null)
            {
                StartCoroutine(SpawnFireflyTrail(resp.@params.target));
            }
            break;

        case "START_COMBAT":
            ShowHint("⚔️ Starting combat...");
            break;

        case "OPEN_SHOP":
            ShowHint($"🛒 Opening shop: {resp.@params?.shop_id}");
            break;

        case "ANIM":
            ShowHint($"🎬 Playing animation: {resp.@params?.name}");
            break;
            
        case "GATHER_FLOWER":
            StartCoroutine(HandleFlowerGathering(resp));
            break;

        default:
            ShowHint($"🤔 Unknown action: {resp.action}");
            break;
    }
}

IEnumerator HandleFlowerGathering(ServerResponse resp)
{
    // Find Snow NPCs
    NPC[] allNPCs = FindObjectsOfType<NPC>();
    List<NPC> snowNPCs = new List<NPC>();
    
    foreach (NPC npc in allNPCs)
    {
        if (npc.name.ToLower().Contains("snow") || 
            (npc.gameObject.tag.ToLower().Contains("snow")))
        {
            snowNPCs.Add(npc);
        }
    }
    
    if (snowNPCs.Count == 0)
    {
        ShowHint("🌸 Could not find any Snow NPCs to gather flowers!");
        yield break;
    }
    
    // Pick a random Snow NPC
    int randomIndex = Random.Range(0, snowNPCs.Count);
    NPC chosenNPC = snowNPCs[randomIndex];
    
    // Trigger flower gathering
    NPCRoutineAI routineAI = chosenNPC.GetComponent<NPCRoutineAI>();
    if (routineAI != null)
    {
        routineAI.PlayerMadeGatheringRequest();
        ShowHint($"🌸 {chosenNPC.name} has heard your request!");
    }
    
    yield break;
}
```

### 6.4. NPC-Specific Action Handler - NPC.cs

```csharp
public void HandleChatbotAction(string action, Dictionary<string, object> parameters)
{
    switch (action)
    {
        case "QUEST_DIALOGUE":
            // NPC explains quest naturally
            break;

        case "ACCEPT_QUEST_CONFIRM":
            if (questGiver != null)
            {
                questGiver.OnPlayerAskForQuest();
                ClearPendingQuest(); 
            }
            break;

        case "ASK_FOR_QUEST":
            if (questGiver != null)
            {
                questGiver.OnPlayerAskForQuest();
            }
            break;

        case "COMPLETE_QUEST":
            if (questGiver != null)
            {
                questGiver.OnPlayerInteract();
            }
            break;

        case "SHOW_QUEST_STATUS":
            var questPanel = GameObject.FindObjectOfType<QuestPanel>();
            if (questPanel != null)
            {
                questPanel.OpenQuestDetail();
            }
            break;

        case "GATHER_FLOWER":
            if (routineAI != null)
            {
                routineAI.PlayerMadeGatheringRequest();
            }
            break;

        case "OPEN_SHOP":
        case "TRADE":
            var trader = GetComponent<NPCTrader>();
            if (trader != null)
            {
                trader.OnPlayerRequestTrade();
            }
            break;

        default:
            Debug.Log($"No special action matched for '{action}'");
            break;
    }
}
```

### 6.5. Firefly Visual Trail System

```csharp
private IEnumerator SpawnFireflyTrail(string target)
{
    if (fireflyPrefab == null)
    {
        yield break;
    }

    // Xoá đom đóm cũ
    foreach (var f in activeFireflies)
        if (f != null) Destroy(f);
    activeFireflies.Clear();

    GameObject camp = GameObject.Find("Camp");
    if (camp == null) yield break;

    // Lấy grid từ mapGenerator
    var grid = mapGenerator.grid;
    
    Vector3Int startCell = mapGenerator.groundTM.WorldToCell(player.position);
    Vector3Int endCell = mapGenerator.groundTM.WorldToCell(camp.transform.position);

    Vector2Int start = new(startCell.x, startCell.y);
    Vector2Int end = new(endCell.x, endCell.y);

    // Tìm path bằng A*
    List<Vector2Int> path = AStarPathfinder.FindPath(grid.walkableGrid, start, end);
    
    if (path == null || path.Count == 0)
    {
        Debug.LogWarning("❌ Không tìm thấy đường dẫn!");
        yield break;
    }

    // Tạo hiệu ứng đom đóm chạy dọc theo path
    foreach (var point in path)
    {
        Vector3 worldPos = mapGenerator.groundTM.CellToWorld(
            new Vector3Int(point.x, point.y, 0)) + new Vector3(0.5f, 0.5f, 0);
        
        var fx = Instantiate(fireflyPrefab, worldPos, Quaternion.identity);
        activeFireflies.Add(fx);

        var fade = fx.GetComponent<FireflyFade>();
        if (fade != null) fade.player = player;

        yield return new WaitForSeconds(0.05f);
    }

    ShowHint($"🪶 Follow the lights to {target}");
    StartCoroutine(MonitorPlayerArrival(camp.transform.position)); 
}
```

**Đặc điểm kỹ thuật:**
- **Intent-to-Action Mapping**: Chuyển đổi tự động từ intent sang game action
- **Hierarchical Action System**: Global actions vs NPC-specific actions
- **Visual Feedback**: Firefly trail system cho navigation
- **Context-aware Actions**: Actions có thể thay đổi dựa trên context (quest, NPC state)
- **Async Audio Playback**: TTS audio được download và play async
- **Session Management**: Lưu lịch sử hội thoại để LLM có context

---

## 7. KẾT LUẬN VÀ ĐÁNH GIÁ

### 7.1. Technical Achievements

1. **Complex State Management**: Hệ thống FSM hierarchical cho NPC với nhiều trạng thái
2. **Advanced Pathfinding**: A* algorithm với dynamic replanning
3. **AI Integration**: LLM + Intent Detection + TTS pipeline hoàn chỉnh
4. **Async Programming**: Coroutines và async/await pattern
5. **Context-aware Dialogue**: Quest và NPC state được inject vào conversation

### 7.2. Performance Considerations

- **A* Optimization**: Sử dụng HashSet cho closed list (O(1) lookup)
- **Intent Caching**: Encode intent examples một lần khi khởi động
- **Session Management**: Limit lịch sử hội thoại (20 turns) để tiết kiệm memory
- **Coroutine Pooling**: Không tạo coroutine mới khi có thể reuse
- **Physics Optimization**: Disable physics khi không cần thiết (market trading)

### 7.3. Extensibility

- **Pluggable State Machine**: Dễ dàng thêm NPCActivity mới
- **Action System**: Có thể thêm actions mới trong action mapping
- **Intent Detection**: Thêm intent mới bằng cách update INTENT_EXAMPLES
- **Multi-NPC Support**: Session-based dialogue cho nhiều NPC

### 7.4. Future Improvements

1. **Behavior Trees**: Thay FSM bằng Behavior Tree cho logic phức tạp hơn
2. **NavMesh**: Sử dụng Unity NavMesh thay vì A* custom
3. **Voice Recognition**: Thêm Whisper API cho speech-to-text
4. **Emotion System**: Facial animation dựa trên emotion markers
5. **Multi-language Support**: Hỗ trợ nhiều ngôn ngữ cho dialogue

---

## 8. REFERENCES

### Code Files Referenced:
- `Assets/Scripts/NPCRoutineAI.cs` - Main NPC AI system
- `Assets/Scripts/Enemies/EnemyAI.cs` - Enemy state machine
- `Assets/Scripts/Enemies/EnemyPathfinding.cs` - Enemy movement
- `Assets/NavActionHandler.cs` - Global action handler
- `Assets/NpcChatSpeaker.cs` - Unity-Flask integration
- `Assets/Scripts/NPC.cs` - NPC component
- `Assets/ChatBox.py` - Flask AI backend server
- `Assets/Web_Item/python_sever/database.py` - Database API

### Technologies Used:
- **Unity 2022**: Game engine
- **C# (.NET)**: Game logic programming
- **Python 3.13**: AI backend
- **Flask**: Web framework
- **LM Studio/Ollama**: LLM hosting
- **Llama 3.2 3B**: Language model
- **Sentence Transformers**: Intent detection
- **Edge-TTS**: Text-to-speech
- **MySQL**: Database
- **A* Algorithm**: Pathfinding

---

**Report Generated**: 2025-11-30  
**Project**: OneMonth_AlexTheWanderer  
**Version**: 1.0
