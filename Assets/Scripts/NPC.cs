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
    private bool isPlayerNearby = false;
    private bool isDialogueActive = false;
    private bool isPlayerSpeaking = false;
    private bool isNpcSpeaking = false;

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

        if (!string.IsNullOrWhiteSpace(userText) && chatSpeaker != null)
        {
            isPlayerSpeaking = false;
            isNpcSpeaking = true;
            Debug.Log($"🔇 {name}: Người chơi nói xong, NPC bắt đầu trả lời.");

            // Thiết lập callback khi NPC nói xong
            chatSpeaker.OnSpeakEnd = OnNpcFinishedSpeaking;

            chatSpeaker.SpeakFromText(userText);
            HandleChatbotIntegration(userText);
        }
        else
        {
            Debug.LogWarning($"⚠️ {name}: Say() bị gọi nhưng chatSpeaker hoặc userText rỗng!");
        }
    }

    void HandleChatbotIntegration(string userText)
    {
        if (routineAI == null) return;

        if (userText.ToLower().Contains("where") && userText.ToLower().Contains("flower"))
        {
            Debug.Log($"🌼 {name}: Tôi biết một nơi có nhiều hoa đẹp!");
        }

        string activityInfo = routineAI.GetCurrentActivityName();
        float gameTime = routineAI.GetCurrentGameTime();
        Debug.Log($"🕒 {name}: Thông tin gửi chatbot → {activityInfo} (giờ {gameTime:F1})");
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

        // Dừng recording nếu đang ghi âm
        if (speechRecognition != null)
        {
            speechRecognition.StopRecording();
        }

        if (routineAI != null && useRoutineAI)
            routineAI.ResumeCurrentActivity();
    }


}
