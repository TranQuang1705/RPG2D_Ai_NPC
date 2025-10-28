using UnityEngine;
using System.Collections;
using System.Collections.Generic;

/// <summary>
/// Central manager cho việc quản lý bông hoa và NPCs.
/// Giúp NPCs có thể tìm bông hoa hiệu quả hơn.
/// </summary>
public class FlowerManager : MonoBehaviour
{
    [Header("Flower Detection")]
    public static Dictionary<string, List<GameObject>> FlowerZones = new Dictionary<string, List<GameObject>>();

    [Header("Flower Respawn Settings")]
    public bool enableAutoRespawn = true;
    public float defaultRespawnTime = 30f; // 30 giây
    public float flowerDetectionRadius = 3f;

    [Header("Debug")]
    public bool showFlowerDebug = true;
    public Color activeFlowerColor = Color.green;
    public Color harvestedFlowerColor = Color.red;

    // Singleton
    public static FlowerManager Instance;

    void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);

        Debug.Log("🌸 FlowerManager initialized");
        StartCoroutine(InitializeFlowerZonesCo());
    }

    // === FLOWER DETECTION AND TRACKING ===
    IEnumerator InitializeFlowerZonesCo()
    {
        yield return new WaitForSeconds(3f); // Chờ vài giây để NPCs khởi tạo

        FlowerZones.Clear();
        int total = 0;

        foreach (FlowerMarking flower in FindObjectsOfType<FlowerMarking>())
        {
            if (flower == null || flower.gameObject == null) continue;

            string type = string.IsNullOrEmpty(flower.flowerType) ? "Unknown" : flower.flowerType;
            if (!FlowerZones.ContainsKey(type))
                FlowerZones[type] = new List<GameObject>();

            if (!FlowerZones[type].Contains(flower.gameObject))
                FlowerZones[type].Add(flower.gameObject);

            // Auto tag
            if (flower.gameObject.tag != "Flower")
                flower.gameObject.tag = "Flower";

            total++;
        }

        Debug.Log($"✅ Flower zones initialized: {FlowerZones.Count} types, total {total} flowers.");
    }

    // === PUBLIC FINDING METHODS ===
    public static GameObject FindNearestFlower(Vector3 position, string specificType = null)
    {
        float minDist = float.MaxValue;
        GameObject nearest = null;

        foreach (var kvp in FlowerZones)
        {
            if (!string.IsNullOrEmpty(specificType) && kvp.Key != specificType)
                continue;

            foreach (GameObject flower in kvp.Value)
            {
                if (flower == null) continue;

                float dist = Vector3.Distance(flower.transform.position, position);
                if (dist < minDist)
                {
                    minDist = dist;
                    nearest = flower;
                }
            }
        }

        return nearest;
    }

    // === FLOWER SPAWNING ===
    public void AddFlower(GameObject flowerObj)
    {
        if (flowerObj == null) return;

        if (flowerObj.tag != "Flower")
            flowerObj.tag = "Flower";

        FlowerMarking fm = flowerObj.GetComponent<FlowerMarking>();
        if (fm != null)
        {
            fm.canRespawn = enableAutoRespawn;
            fm.respawnTime = defaultRespawnTime;
        }

        string type = fm != null ? fm.flowerType : "Unknown";
        if (!FlowerZones.ContainsKey(type))
            FlowerZones[type] = new List<GameObject>();

        if (!FlowerZones[type].Contains(flowerObj))
            FlowerZones[type].Add(flowerObj);
    }

    // === FLOWER REMOVAL & RESPAWN ===
    public void RemoveFlower(GameObject flowerObj)
    {
        if (flowerObj == null) return;

        FlowerMarking fm = flowerObj.GetComponent<FlowerMarking>();
        string type = fm != null ? fm.flowerType : "Unknown";

        // Lưu dữ liệu trước khi xóa
        Vector3 pos = flowerObj.transform.position;
        float delay = fm != null ? fm.respawnTime : defaultRespawnTime;
        bool canRespawn = fm != null ? fm.canRespawn : enableAutoRespawn;
        GameObject prefab = (fm != null && fm.prefabReference != null) ? fm.prefabReference : flowerObj;

        if (FlowerZones.ContainsKey(type))
            FlowerZones[type].Remove(flowerObj);

        Destroy(flowerObj);

        if (canRespawn)
            StartCoroutine(RespawnFlower(prefab, type, delay, pos));
    }

    IEnumerator RespawnFlower(GameObject prefab, string flowerType, float delay, Vector3 oldPos)
    {
        yield return new WaitForSeconds(delay);

        Vector3 respawnPos = oldPos + Random.insideUnitSphere * 2f;
        respawnPos.y = 0f;

        if (prefab == null)
        {
            Debug.LogWarning("⚠️ FlowerManager: prefab để respawn null.");
            yield break;
        }

        GameObject newFlower = Instantiate(prefab, respawnPos, Quaternion.identity);
        FlowerMarking fm = newFlower.GetComponent<FlowerMarking>();
        if (fm == null) fm = newFlower.AddComponent<FlowerMarking>();

        fm.flowerType = flowerType;
        fm.canRespawn = enableAutoRespawn;
        fm.respawnTime = defaultRespawnTime;
        fm.prefabReference = prefab;

        AddFlower(newFlower);
        Debug.Log($"🌱 Respawned flower '{flowerType}' at {respawnPos}");
    }

    // === VISUAL DEBUG ===
    void OnDrawGizmos()
    {
        if (!showFlowerDebug || FlowerZones == null) return;

        Gizmos.color = Color.green;

        foreach (var kvp in FlowerZones)
        {
            foreach (GameObject flower in kvp.Value)
            {
                if (flower == null) continue;
                Gizmos.DrawSphere(flower.transform.position + Vector3.up * 0.1f, 0.2f);
            }
        }
    }

    // === UTILITIES ===
    public List<GameObject> GetAllFlowerObjects()
    {
        List<GameObject> all = new List<GameObject>();
        foreach (var kvp in FlowerZones)
            all.AddRange(kvp.Value);
        return all;
    }
}
