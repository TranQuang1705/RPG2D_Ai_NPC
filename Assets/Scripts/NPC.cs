// NPC.cs (debug-enhanced)
using UnityEngine;

public class NPC : MonoBehaviour
{
    [Header("UI")]
    [SerializeField] private GameObject dialoguePanel;

    [Header("Chat (optional)")]
    [SerializeField] private NpcChatSpeaker chatSpeaker; // gắn component này nếu muốn NPC nói

    [Header("Voice Recognition")]
    [SerializeField] private SpeechRecognitionTest speechRecognition;
    [SerializeField] private int recordSeconds = 5;

    [Header("Routine Settings")]
    [SerializeField] private bool useRoutineAI = true;

    private NPCRoutineAI routineAI;
    private NPCQuestGiver questGiver;
    private bool isPlayerNearby = false;
    private bool isDialogueActive = false;
    private bool isPlayerSpeaking = false;
    private bool isNpcSpeaking = false;
    
    // Quest dialogue state - remembers quest being offered
    private int pendingQuestId = -1;
    private string pendingQuestContext = null;

    private void Start()
    {
        if (dialoguePanel != null)
        {
            dialoguePanel.SetActive(false);
            Debug.Log($"🧩 {name}: dialoguePanel được gán và đang ẩn khi khởi động.");
        }
        else
        {
            Debug.LogWarning($"⚠️ {name}: dialoguePanel CHƯA được gán trong Inspector!");
        }

        if (useRoutineAI)
        {
            SetupRoutineAI();
        }

        // Setup quest giver
        questGiver = GetComponent<NPCQuestGiver>();
        if (questGiver == null)
        {
            Debug.Log($"⚠️ {name}: No NPCQuestGiver component found. Add one if this NPC gives quests.");
        }

        // 🟢 Kiểm tra Collider2D
        Collider2D[] cols = GetComponents<Collider2D>();
        foreach (var c in cols)
        {
            Debug.Log($"🔍 {name}: Phát hiện collider loại {c.GetType().Name}, IsTrigger={c.isTrigger}, Layer={LayerMask.LayerToName(gameObject.layer)}");
        }

        // 🟢 Kiểm tra Rigidbody2D
        Rigidbody2D rb = GetComponent<Rigidbody2D>();
        if (rb == null)
            Debug.LogError($"❌ {name}: Thiếu Rigidbody2D → OnTriggerEnter2D sẽ KHÔNG được gọi!");
        else
            Debug.Log($"✅ {name}: Rigidbody2D hợp lệ (BodyType={rb.bodyType}, Simulated={rb.simulated}, Layer={LayerMask.LayerToName(gameObject.layer)})");
    }

    void SetupRoutineAI()
    {
        routineAI = GetComponent<NPCRoutineAI>();
        if (routineAI == null)
        {
            routineAI = gameObject.AddComponent<NPCRoutineAI>();
            Debug.Log($"🧠 {name}: Đã thêm mới NPCRoutineAI component.");
        }

        routineAI.homeLocation = transform;
        routineAI.villageCenter = FindVillageCenter();
        routineAI.wanderRadius = 10f;

        if (NPCManager.Instance != null && routineAI.flowerPrefabs.Count == 0)
        {
            routineAI.flowerPrefabs = NPCManager.Instance.flowerPrefabs;
            Debug.Log($"🌸 {name}: Đã lấy danh sách flowerPrefabs từ NPCManager.");
        }

        // Thiết lập speech recognition nếu có
        if (speechRecognition == null)
        {
            speechRecognition = FindObjectOfType<SpeechRecognitionTest>();
            if (speechRecognition != null)
                Debug.Log($"🎤 {name}: Tìm thấy SpeechRecognitionTest.");
        }
    }

    Transform FindVillageCenter()
    {
        if (NPCManager.Instance != null && NPCManager.Instance.villageCenter != null)
        {
            Debug.Log($"🏘️ {name}: Lấy villageCenter từ NPCManager.");
            return NPCManager.Instance.villageCenter;
        }

        GameObject obj = GameObject.FindWithTag("VillageCenter");
        if (obj != null)
        {
            Debug.Log($"🏘️ {name}: Lấy villageCenter theo tag.");
            return obj.transform;
        }

        Debug.LogWarning($"⚠️ {name}: Không tìm thấy VillageCenter, tạo tạm tại vị trí hiện tại.");
        GameObject center = new GameObject("VillageCenter");
        center.transform.position = transform.position;
        return center.transform;
    }

    // ================== TRIGGER ==================

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Player")) TriggerDialogueEnter();
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (other.CompareTag("Player")) TriggerDialogueExit();
    }


    // ================== CHAT ==================

    public void Say(string userText)
    {
        Debug.Log($"🗣️ {name}: Say() được gọi với input: \"{userText}\"");

        if (!string.IsNullOrWhiteSpace(userText))
        {
            isPlayerSpeaking = false;
            isNpcSpeaking = true;
            Debug.Log($"🔇 {name}: Người chơi nói xong, NPC bắt đầu xử lý.");

            // Prepare context for chatbot
            string questContext = GetQuestContextForChatbot(userText);
            string npcContext = GetCurrentActivityInfo();
            
            Debug.Log($"🔍 {name}: Quest context status - HasContext: {!string.IsNullOrEmpty(questContext)}, PendingQuestId: {pendingQuestId}");
            if (!string.IsNullOrEmpty(questContext))
            {
                Debug.Log($"📤 {name}: Sending quest context to chatbot");
            }

            // Send message to chatbot if available
            if (ChatbotClient.Instance != null)
            {
                ChatbotClient.Instance.SendMessage(userText, this, questContext, npcContext);
            }
            // Fallback to direct processing
            else if (chatSpeaker != null)
            {
                // Thiết lập callback khi NPC nói xong
                chatSpeaker.OnSpeakEnd = OnNpcFinishedSpeaking;
                chatSpeaker.SpeakFromText(userText, questContext, npcContext);
                HandleChatbotIntegration(userText);
            }
            else
            {
                Debug.LogWarning($"⚠️ {name}: No chatbot or chatSpeaker available!");
                HandleChatbotIntegration(userText);
                OnNpcFinishedSpeaking();
            }
        }
        else
        {
            Debug.LogWarning($"⚠️ {name}: Say() bị gọi nhưng userText rỗng!");
        }
    }

    string GetQuestContextForChatbot(string userText)
    {
        if (questGiver == null)
            return null;

        // If we already have a pending quest context, return it
        // This allows player to say "yes" without repeating quest keywords
        if (!string.IsNullOrEmpty(pendingQuestContext))
        {
            Debug.Log($"📜 {name}: Using pending quest context for quest ID {pendingQuestId}");
            return pendingQuestContext;
        }

        string lowerText = userText.ToLower();

        // Check if player is asking about quests
        if (lowerText.Contains("need") || lowerText.Contains("help") || 
            lowerText.Contains("quest") || lowerText.Contains("task") ||
            lowerText.Contains("job") || lowerText.Contains("anything"))
        {
            var availableQuests = QuestManager.Instance?.GetQuestsForNPC(questGiver.GetNPCId());
            
            if (availableQuests != null && availableQuests.Count > 0)
            {
                var quest = availableQuests[0];
                var objectives = QuestManager.Instance?.GetQuestObjectives(quest.quest_id);
                
                // Build quest context string
                string context = $"QUEST_AVAILABLE: {quest.quest_name}\n";
                context += $"Description: {quest.description}\n";
                context += $"Difficulty: {quest.difficulty}\n";
                
                if (objectives != null && objectives.Count > 0)
                {
                    context += "Objectives:\n";
                    foreach (var obj in objectives)
                    {
                        context += $"- {obj.objective_type}: {obj.description} ({obj.quantity}x {obj.target_name})\n";
                    }
                }
                
                context += $"Rewards: {quest.reward_gold} gold";
                if (quest.reward_exp > 0)
                    context += $", {quest.reward_exp} exp";
                if (quest.reward_item_id > 0)
                    context += $", item reward";
                
                // Store pending quest state
                pendingQuestId = quest.quest_id;
                pendingQuestContext = context;
                
                Debug.Log($"📜 {name}: Quest context prepared and stored for quest ID {pendingQuestId}:\n{context}");
                return context;
            }
            // Check for completable quests
            else if (questGiver.HasCompletableQuests())
            {
                return "QUEST_COMPLETABLE: Player has completed quest objectives and can turn in the quest.";
            }
        }

        return null;
    }
    
    void ClearPendingQuest()
    {
        Debug.Log($"🗑️ {name}: Clearing pending quest state");
        pendingQuestId = -1;
        pendingQuestContext = null;
    }

    // Called by ChatbotClient when NPC needs to speak a response
    public void SpeakResponse(string responseText)
    {
        if (chatSpeaker != null)
        {
            chatSpeaker.OnSpeakEnd = OnNpcFinishedSpeaking;
            chatSpeaker.SpeakFromText(responseText);
        }
        else
        {
            Debug.Log($"💬 {name}: {responseText}");
            OnNpcFinishedSpeaking();
        }
    }

    void HandleChatbotIntegration(string userText)
    {
        if (string.IsNullOrEmpty(userText)) return;

        string lowerText = userText.ToLower();

        // ❌ REMOVED FALLBACK LOGIC - Now handled by chatbot with QUEST_DIALOGUE and ACCEPT_QUEST_CONFIRM
        // The chatbot will:
        // 1. Detect "need help" → send quest_context → chatbot explains quest → action: QUEST_DIALOGUE
        // 2. Detect "yes/sure" with quest_context → action: ACCEPT_QUEST_CONFIRM → accept quest
        // This gives natural dialogue before accepting quest!

        // Flower direction
        if (lowerText.Contains("where") && lowerText.Contains("flower"))
        {
            Debug.Log($"🌼 {name}: Tôi biết một nơi có nhiều hoa đẹp!");
        }

        // Send activity info to chatbot
        if (routineAI != null)
        {
            string activityInfo = routineAI.GetCurrentActivityName();
            float gameTime = routineAI.GetCurrentGameTime();
            Debug.Log($"🕒 {name}: Thông tin gửi chatbot → {activityInfo} (giờ {gameTime:F1})");
        }
    }

    // Handler for chatbot action responses
    public void HandleChatbotAction(string action, System.Collections.Generic.Dictionary<string, object> parameters)
    {
        Debug.Log($"🎮 {name}: Received action '{action}' from chatbot");
        Debug.Log($"🔍 {name}: Action comparison - received: '{action}', length: {action.Length}");
        Debug.Log($"🔍 {name}: routineAI null? {routineAI == null}, useRoutineAI: {useRoutineAI}");

        switch (action)
        {
            case "QUEST_DIALOGUE":
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
                if (routineAI == null)
                {
                    Debug.Log($"⚠️ {name}: routineAI is null, trying to get component...");
                    routineAI = GetComponent<NPCRoutineAI>();
                }
                
                if (routineAI != null)
                {
                    Debug.Log($"🌸 {name}: Starting flower gathering activity from chatbot request");
                    routineAI.PlayerMadeGatheringRequest();
                    Debug.Log($"🌸 {name}: Called PlayerMadeGatheringRequest()");
                }
                else
                {
                    Debug.LogWarning($"⚠️ {name}: Cannot start flower gathering - no NPCRoutineAI component");
                }
                break;

            default:
                Debug.Log($"ℹ️ {name}: No special action matched for '{action}'");
                break;
        }
        
        Debug.Log($"🏁 {name}: HandleChatbotAction finished");
    }

    // Callback khi NPC nói xong
    void OnNpcFinishedSpeaking()
    {
        isNpcSpeaking = false;
        isPlayerSpeaking = false; // 🔧 thêm dòng này
        Debug.Log($"🎤 {name}: NPC nói xong, mở mic cho người chơi tiếp tục.");

        // Mở lại mic sau 0.5 giây để người chơi tiếp tục nói
        Invoke(nameof(StartListeningForPlayer), 0.5f);
    }


    // Bắt đầu lắng nghe người chơi
    void StartListeningForPlayer()
    {
        if (isDialogueActive && !isPlayerSpeaking && !isNpcSpeaking)
        {
            if (speechRecognition != null)
            {
                Debug.Log($"🎙️ {name}: Bắt đầu lắng nghe người chơi...");
                isPlayerSpeaking = true;

                // Thiết lập callback khi nhận được voice input
                speechRecognition.OnSpeechResult = OnPlayerSpeechReceived;
                speechRecognition.StartRecording();
            }
            else
            {
                Debug.LogWarning($"⚠️ {name}: SpeechRecognition không có sẵn!");
            }
        }
    }

    // Xử lý khi nhận được speech từ người chơi
    void OnPlayerSpeechReceived(string recognizedText)
    {
        if (!string.IsNullOrWhiteSpace(recognizedText))
        {
            Debug.Log($"💬 {name}: Người chơi nói: \"{recognizedText}\"");
            Say(recognizedText);
        }
        else
        {
            Debug.Log($"🔇 {name}: Không nhận được speech, thử lại sau 1 giây.");
            Invoke(nameof(StartListeningForPlayer), 1f);
        }
    }

    public string GetCurrentActivityInfo()
    {
        if (routineAI != null)
            return $"Hiện tại tôi đang {routineAI.GetCurrentActivityName()}. Giờ là {Mathf.Floor(routineAI.GetCurrentGameTime())}.";
        return "Tôi đang làm công việc của mình.";
    }

    public bool IsAvailableForInteraction()
    {
        bool available = isPlayerNearby && !IsBusy();
        Debug.Log($"🔎 {name}: IsAvailableForInteraction() → {available}");
        return available;
    }

    bool IsBusy()
    {
        if (routineAI == null) return false;
        return routineAI.currentState == NPCState.GatheringFlower ||
               (!useRoutineAI && routineAI.currentState == NPCState.Socializing);
    }

    public void OnFlowerGathered(GameObject flower)
    {
        Debug.Log($"🌸 {name}: OnFlowerGathered() gọi với {flower.name}");
        Animator anim = GetComponent<Animator>();
        if (anim != null) anim.SetTrigger("Happy");
    }
    private Transform player;

    private void Update()
    {
        if (player == null)
        {
            GameObject p = GameObject.FindWithTag("Player");
            if (p != null) player = p.transform;
            return;
        }

        float distance = Vector3.Distance(transform.position, player.position);
        bool wasNearby = isPlayerNearby;

        if (distance < 1.5f && !isPlayerNearby)
        {
            // Khi người chơi bước vào vùng hội thoại
            isPlayerNearby = true;
            TriggerDialogueEnter();
        }
        else if (distance >= 1.8f && isPlayerNearby)
        {
            // Khi người chơi rời vùng hội thoại
            isPlayerNearby = false;
            TriggerDialogueExit();
        }
    }
    public void TriggerDialogueEnter()
    {
        Debug.Log($"✅ {name}: Player đến gần, bắt đầu hội thoại.");
        if (dialoguePanel != null) dialoguePanel.SetActive(true);

        isDialogueActive = true;
        isPlayerSpeaking = false;
        isNpcSpeaking = false;

        if (routineAI != null && useRoutineAI)
            routineAI.PauseCurrentActivity();

        if (routineAI != null)
        {
            routineAI.currentState = NPCState.Idle;
            Animator anim = routineAI.GetComponent<Animator>();
            if (anim)
            {
                anim.SetBool("Walking", false);
                anim.SetBool("Idle", true);
            }
        }

        // NPC chào hỏi trước rồi mới mở mic cho người chơi
        if (chatSpeaker != null)
        {
            Debug.Log($"🎙️ {name}: Mở mic cho người chơi bắt đầu nói.");
            StartListeningForPlayer();
        }
    }

    public void TriggerDialogueExit()
    {
        Debug.Log($"👋 {name}: Player rời xa, đóng hội thoại.");
        if (dialoguePanel != null) dialoguePanel.SetActive(false);

        // Dừng dialogue và thu hồi mic
        isDialogueActive = false;
        isPlayerSpeaking = false;
        isNpcSpeaking = false;
        
        // Clear pending quest when dialogue ends
        ClearPendingQuest();

        // Dừng recording nếu đang ghi âm
        if (speechRecognition != null)
        {
            speechRecognition.StopRecording();
        }

        // Chỉ resume activity nếu NPC có player request hoặc đang trong routine bình thường
        if (routineAI != null && useRoutineAI)
        {
            bool hasRequest = routineAI.HasPlayerRequest();
            Debug.Log($"🔍 {name}: Player left dialogue. PlayerRequest={hasRequest}");
            
            // Nếu có player request hái hoa, NPC sẽ tiếp tục
            // Nếu không, NPC sẽ quay về routine bình thường theo thời gian
            if (hasRequest)
            {
                Debug.Log($"🌸 {name}: Resuming flower gathering from player request");
            }
            routineAI.ResumeCurrentActivity();
        }
    }


}
