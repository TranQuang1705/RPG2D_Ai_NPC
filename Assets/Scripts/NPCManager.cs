using UnityEngine;
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// Manager để điều phối tất cả NPCs và tích hợp với chatbot
/// Không tự sinh hoa — chỉ sử dụng hoa có sẵn trong scene (prefab đặt sẵn)
/// </summary>
public class NPCManager : MonoBehaviour
{



    [Header("Flower Settings")]
    public List<GameObject> flowerPrefabs; // Prefab các loại hoa có thể hái
    public List<FlowerMarking> existingFlowers = new(); // Danh sách hoa có sẵn trong scene

    [Header("Village/Camp Settings")]
    public Transform villageCenter;
    public Transform campLocation;
    public Vector2 villageSize = new Vector2(20f, 20f);

    // Singleton
    public static NPCManager Instance;

    private List<NPCRoutineAI> activeNPCs = new List<NPCRoutineAI>();

    void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    void Start()
    {
        SpawnVillageArea();
        CacheExistingFlowers(); // ✅ Lấy hoa có sẵn thay vì spawn mới
        LinkExistingNPCs();
    }

    // === INITIALIZATION ===

    void SpawnVillageArea()
    {
        if (villageCenter == null)
        {
            GameObject village = new GameObject("VillageCenter");
            villageCenter = village.transform;
            village.transform.position = Vector3.zero;
        }

        if (campLocation == null)
        {
            GameObject camp = new GameObject("Camp");
            campLocation = camp.transform;
            camp.transform.position = new Vector3(10f, 0f, 0f);
        }
    }

    /// <summary>
    /// Tìm tất cả bông hoa trong scene có gắn FlowerMarking
    /// và thêm vào danh sách theo dõi
    /// </summary>
    void CacheExistingFlowers()
    {
        existingFlowers = FindObjectsOfType<FlowerMarking>().ToList();

        if (existingFlowers.Count == 0)
        {
            Debug.LogWarning("⚠️ Không tìm thấy bông hoa nào trong scene! Hãy kéo prefab hoa vào scene và gắn FlowerMarking.");
        }
        else
        {
            Debug.Log($"✅ Đã tìm thấy {existingFlowers.Count} bông hoa có sẵn trong scene.");
        }

        // Gán danh sách hoa này cho FlowerManager (nếu có)
        if (FlowerManager.Instance != null)
        {
            foreach (FlowerMarking flower in existingFlowers)
            {
                FlowerManager.Instance.AddFlower(flower.gameObject);
            }
        }
    }

    // === NPC SPAWNING ===

    void LinkExistingNPCs()
    {
        // Tìm tất cả NPC hiện có trong Scene
        NPCRoutineAI[] npcs = FindObjectsOfType<NPCRoutineAI>();

        if (npcs.Length == 0)
        {
            Debug.LogWarning("⚠️ Không tìm thấy NPC nào trong scene!");
            return;
        }

        foreach (var npc in npcs)
        {
            if (npc == null) continue;

            // Gán các giá trị cần thiết nếu chưa có
            if (npc.villageCenter == null) npc.villageCenter = villageCenter;
            if (npc.homeLocation == null) npc.homeLocation = campLocation;
            if (npc.flowerPrefabs == null || npc.flowerPrefabs.Count == 0)
                npc.flowerPrefabs = flowerPrefabs;

            // Đăng ký NPC vào danh sách quản lý
            activeNPCs.Add(npc);
        }

        Debug.Log($"✅ Đã liên kết {activeNPCs.Count} NPC có sẵn trong Scene.");
    }



    Vector3 GetRandomSpawnPosition()
    {
        Vector3 pos = villageCenter.position + new Vector3(
    Random.Range(-villageSize.x / 2, villageSize.x / 2),
    Random.Range(-villageSize.y / 2, villageSize.y / 2),
    0f
);
        return pos;
    }

    // === CHATBOT INTEGRATION ===

    public void HandleChatbotAction(string action, string intent, object parameters)
    {
        switch (intent.ToLower())
        {
            case "ask_direction":
                ShowDirectionToVillage();
                break;

            case "trade":
                InitiateTrade();
                break;

            case "help":
                HandleHelpRequest(action, parameters);
                break;
        }
    }

    void ShowDirectionToVillage()
    {
        Debug.Log("📍 Đang hiển thị đường đến làng...");
        CreatePathIndicator(villageCenter.position);
    }

    void InitiateTrade()
    {
        NPCRoutineAI nearestNPC = GetNearestNPC();
        if (nearestNPC == null) return;

        Transform player = GameObject.FindWithTag("Player")?.transform;
        if (player != null)
        {
            bool reachPlayer = false;
            nearestNPC.StartCoroutine(nearestNPC.MoveToPosition(
                player.position,
                r => reachPlayer = r
            ));

        }
    }

    void HandleHelpRequest(string action, object parameters)
    {
        if (action.ToLower().Contains("flower"))
        {
            Vector3 playerPos = GameObject.FindWithTag("Player")?.transform.position ?? Vector3.zero;
            Vector3 nearestFlowerPos = FindNearestFlowerPosition(playerPos);

            if (nearestFlowerPos != Vector3.zero)
            {
                NPCRoutineAI helper = GetNearestNPC();
                if (helper != null)
                {
                    Debug.Log("🌸 NPC đang dẫn bạn đến khu vực có hoa...");
                    bool reachFlower = false;
                    helper.StartCoroutine(helper.MoveToPosition(
                        nearestFlowerPos,
                        r => reachFlower = r
                    ));

                }
            }
        }
    }

    Vector3 FindNearestFlowerPosition(Vector3 fromPosition)
    {
        if (existingFlowers.Count == 0) return Vector3.zero;

        FlowerMarking nearest = existingFlowers
            .Where(f => f != null && f.isGatherable)
            .OrderBy(f => Vector3.Distance(fromPosition, f.transform.position))
            .FirstOrDefault();

        return nearest != null ? nearest.transform.position : Vector3.zero;
    }

    NPCRoutineAI GetNearestNPC()
    {
        Transform player = GameObject.FindWithTag("Player")?.transform;
        if (player == null) return null;

        return activeNPCs
            .Where(npc => npc != null)
            .OrderBy(npc => Vector3.Distance(npc.transform.position, player.position))
            .FirstOrDefault();
    }

    void CreatePathIndicator(Vector3 targetPosition)
    {
        Debug.DrawLine(Vector3.zero, targetPosition, Color.cyan, 10f);
    }

    // === UTILITIES ===

    public void RefreshFlowerList()
    {
        CacheExistingFlowers();
        foreach (NPCRoutineAI npc in activeNPCs)
        {
            npc.StartCoroutine(npc.ScanForFlowers());
        }
    }

    public List<NPCRoutineAI> GetAllNPCs()
    {
        return new List<NPCRoutineAI>(activeNPCs);
    }
}
