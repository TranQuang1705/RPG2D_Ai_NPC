using UnityEngine;

/// <summary>
/// Script dùng để test hệ thống time-based routine cho NPC
/// Cho phép người chơi dễ dàng thay đổi thời gian trong game để test
/// </summary>
public class TimeBasedRoutineTester : MonoBehaviour
{
    [Header("Test Time Settings")]
    public float testHour = 14f; // Mặc định 4:00 PM (trong giờ hái hoa)
    public bool autoChangeTime = false;
    public float changeInterval = 30f; // Thay đổi thời gian sau 30 giây

    private float timer = 0f;
    private NPCRoutineAI[] npcs;
    private NPCRoutineAI npcRoutineHelper;

    void Start()
    {
        // Tìm tất cả NPCs trong scene
        npcs = FindObjectsOfType<NPCRoutineAI>();
        
        // Set thời gian ban đầu cho testing
        SetTestTime(testHour);
    }

    void Update()
    {
        if (autoChangeTime && Time.time - timer > changeInterval)
        {
            // Tạo thời gian ngẫu nhiên để test
            float randomHour = Random.Range(6f, 24f);
            SetTestTime(randomHour);
            timer = Time.time;
            Debug.Log($"🕐 Test: Changed time to {Mathf.Floor(randomHour)}:00");
        }

        // Debug input để thay đổi thời gian thủ công
        if (Input.GetKeyDown(KeyCode.T))
        {
            // Chuyển qua các thời điểm test quan trọng
            if (testHour < 7f) testHour = 7f;           // Sáng (đang xới đất)
            else if (testHour < 9f) testHour = 10f;     // Sáng (dọn dẹp)
            else if (testHour < 12f) testHour = 13f;    // Trưa (nghỉ trưa)
            else if (testHour < 14f) testHour = 14.5f;  // Chiều (hái hoa - 14:30)
            else if (testHour < 17f) testHour = 19f;    // Tối (không làm việc)
            else if (testHour < 22f) testHour = 23f;    // Đêm (không làm việc)
            else testHour = 6f;                         // Reset về sáng sớm
            
            SetTestTime(testHour);
            Debug.Log($"⏰ Manual time change to {Mathf.Floor(testHour)}:00");
        }

        // Toggle TimeManager usage với phím M
        if (Input.GetKeyDown(KeyCode.M))
        {
            bool useManager = npcs.Length > 0 && npcs[0].useRealTimeManager;
            foreach (var npc in npcs)
            {
                npc.UseTimeManager(!useManager);
            }
            Debug.Log($"🔄 TimeManager usage set to {!useManager}");
        }
    }

    void SetTestTime(float hour)
    {
        foreach (var npc in npcs)
        {
            npc.SetCustomTime(hour);
            Debug.Log($"🤖 {npc.name}: Time set to {hour:F1}:00 - Flower hunting: {npc.IsFlowerHuntingTime()}");
        }
    }

    void OnGUI()
    {
        GUILayout.BeginArea(new Rect(10, 140, 300, 200));
        GUILayout.Label("=== Time-Based Routine Test ===");
        GUILayout.Label($"Current Test Time: {Mathf.Floor(testHour)}:00");
        
        if (npcs.Length > 0)
        {
            GUILayout.Label($"Flower Hunting Time: {npcs[0].IsFlowerHuntingTime()}");
            GUILayout.Label($"Using TimeManager: {npcs[0].useRealTimeManager}");
            GUILayout.Label($"Real Time: {TimeManager.Instance?.GetCurrentTimeString()}");
            
            // Hiển thị thông tin flower hunting
            npcRoutineHelper = npcs[0];
            if (npcRoutineHelper != null)
            {
                using (new GUILayout.VerticalScope("box"))
                {
                    GUILayout.Label("=== Schedule Info ===");
                    GUILayout.Label($"Flower Hunting: {npcRoutineHelper.flowerHuntingStartHour}:00-{npcRoutineHelper.flowerHuntingEndHour}:00");
                    GUILayout.Label($"Flower Hunting Time: {npcRoutineHelper.IsFlowerHuntingTime()}");
                }
            }
        }
        
        GUILayout.Space(10);
        GUILayout.Label("Press T: Change test time");
        GUILayout.Label("Press M: Toggle TimeManager");
        GUILayout.Label("Flower Hunting: 14:00-16:00");
        
        if (GUILayout.Button(autoChangeTime ? "Stop Auto Time" : "Start Auto Time"))
        {
            autoChangeTime = !autoChangeTime;
            timer = Time.time;
        }
        
        GUILayout.EndArea();
    }
    
    // Helper method để đọc private field (chỉ cho testing)
    T GetPrivateField<T>(object obj, string fieldName)
    {
        var field = obj.GetType().GetField(fieldName, System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        if (field != null)
            return (T)field.GetValue(obj);
        return default(T);
    }
}
