using UnityEngine;
using System.Collections;
using System.Collections.Generic;

[System.Serializable]
public class ServerResponse
{
    public string action;
    public ResponseParams @params;
    public string intent;
    public string reply;
    public string audio_url;
}

[System.Serializable]
public class ResponseParams
{
    public string target;
    public string target_label;
    public string shop_id;
    public string name;
    public string location;
    public string item;
}

public class NavActionHandler : MonoBehaviour
{
    [Header("References")]
    public Transform player;              // Tham chiếu tới Player
    public MapGenerator mapGenerator;     // Sinh map & tìm đường
    public GameObject fireflyPrefab;      // Prefab hiệu ứng đom đóm
    private List<GameObject> activeFireflies = new(); // Lưu lại để clear khi cần

    /// <summary>
    /// Nhận phản hồi từ Flask server và xử lý hành động.
    /// </summary>
    public void HandleServerAction(ServerResponse resp)
    {
        if (resp == null)
        {
            Debug.LogWarning("⚠️ Server response is null.");
            return;
        }

        if (string.IsNullOrEmpty(resp.action))
        {
            Debug.LogWarning("⚠️ Missing resp.action!");
            return;
        }

        switch (resp.action)
        {
            case "NAVIGATE":
                if (resp.@params == null)
                {
                    Debug.LogWarning("⚠️ Missing params for NAVIGATE.");
                    return;
                }

                if (mapGenerator != null)
                {
                    StartCoroutine(SpawnFireflyTrail(resp.@params.target));
                }
                else
                {
                    Debug.LogWarning("⚠️ MapGenerator chưa được gán vào NavActionHandler!");
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

    /// <summary>
    /// Spawn hiệu ứng đom đóm theo đường dẫn A* từ player → camp.
    /// </summary>
    private IEnumerator SpawnFireflyTrail(string target)
    {
        if (fireflyPrefab == null)
        {
            Debug.LogWarning("⚠️ Chưa gán prefab đom đóm (Firefly Prefab)!");
            yield break;
        }

        // Xoá đom đóm cũ nếu có
        foreach (var f in activeFireflies)
            if (f != null) Destroy(f);
        activeFireflies.Clear();

        GameObject camp = GameObject.Find("Camp");
        if (camp == null)
        {
            yield break;
        }

        // Lấy grid từ mapGenerator
        var grid = mapGenerator.grid;
        if (grid == null)
        {
            yield break;
        }

        Vector3Int startCell = mapGenerator.groundTM.WorldToCell(player.position);
        Vector3Int endCell = mapGenerator.groundTM.WorldToCell(camp.transform.position);

        Vector2Int start = new(startCell.x, startCell.y);
        Vector2Int end = new(endCell.x, endCell.y);

        List<Vector2Int> path = AStarPathfinder.FindPath(grid.walkableGrid, start, end);
        if (path == null || path.Count == 0)
        {
            Debug.LogWarning("❌ Không tìm thấy đường dẫn!");
            yield break;
        }


        // Tạo hiệu ứng đom đóm chạy dọc theo path
        foreach (var point in path)
        {
            Vector3 worldPos = mapGenerator.groundTM.CellToWorld(new Vector3Int(point.x, point.y, 0)) + new Vector3(0.5f, 0.5f, 0);
            var fx = Instantiate(fireflyPrefab, worldPos, Quaternion.identity);
            activeFireflies.Add(fx);

            var fade = fx.GetComponent<FireflyFade>();
            if (fade != null) fade.player = player;

            yield return new WaitForSeconds(0.05f); // khoảng cách thời gian giữa các đốm
        }

        ShowHint($"🪶 Follow the lights to {target}");
        StartCoroutine(MonitorPlayerArrival(camp.transform.position)); 
    }

    /// <summary>
    /// Handle flower gathering request
    /// </summary>
    IEnumerator HandleFlowerGathering(ServerResponse resp)
    {
        // Find all NPCs - specifically Snow NPCs
        NPC[] allNPCs = FindObjectsOfType<NPC>();
        
        // Filter for Snow NPCs (girl NPCs)
        List<NPC> snowNPCs = new List<NPC>();
        foreach (NPC npc in allNPCs)
        {
            // Check if this NPC has "Snow" in its name or tag
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
        
        // Pick a random Snow NPC to respond
        int randomIndex = Random.Range(0, snowNPCs.Count);
        NPC chosenNPC = snowNPCs[randomIndex];
        
        // Get the NPC's routine AI component
        NPCRoutineAI routineAI = chosenNPC.GetComponent<NPCRoutineAI>();
        if (routineAI != null)
        {
            // Trigger flower gathering in the NPC's routine
            routineAI.PlayerMadeGatheringRequest();
            ShowHint($"🌸 {chosenNPC.name} has heard your request and will go gather flowers!");
        }
        else
        {
            ShowHint($"🌸 {chosenNPC.name} doesn't have routine AI component!");
        }
        
        yield break;
    }

    /// <summary>
    /// Hiển thị log hoặc HUD
    /// </summary>
    private void ShowHint(string message)
    {
        Debug.Log($"[HUD] {message}");
    }
    private IEnumerator MonitorPlayerArrival(Vector3 campPos)
    {
        while (true)
        {
            if (Vector3.Distance(player.position, campPos) < 2f)
            {
                ClearAllFireflies();
                yield break; 
            }
            yield return new WaitForSeconds(0.5f);
        }
    }

    private void ClearAllFireflies()
    {
        foreach (var f in activeFireflies)
            if (f != null) Destroy(f);
        activeFireflies.Clear();
    }

}
