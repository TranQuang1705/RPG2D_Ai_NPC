using UnityEngine;

/// <summary>
/// Component này đánh dấu GameObject là bông hoa để NPC có thể nhận biết.
/// KHÔNG tự respawn — việc respawn do FlowerManager xử lý.
/// </summary>
public class FlowerMarking : MonoBehaviour
{
    [Header("Flower Settings")]
    public bool isGatherable = true;
    public string flowerType = "Default";

    [Header("Respawn Settings")]
    public bool canRespawn = true;
    public float respawnTime = 30f;

    [Header("Prefab Reference")]
    [Tooltip("Prefab gốc để FlowerManager dùng khi respawn.")]
    public GameObject prefabReference;

    private Vector3 originalPosition;
    private Vector3 originalScale;
    private Quaternion originalRotation;

    void Start()
    {
        if (gameObject.tag != "Flower")
            gameObject.tag = "Flower";

        originalPosition = transform.position;
        originalScale = transform.localScale;
        originalRotation = transform.rotation;

        // ✅ FIX: Gán prefabReference tự động với null check
        if (prefabReference == null)
        {
            // Try find appropriate prefab
            prefabReference = FindFlowerPrefab();
            if (prefabReference == null)
            {
                Debug.LogWarning($"⚠️ {gameObject.name}: No prefab reference found, using self");
                prefabReference = gameObject;
            }
            else
            {
                Debug.Log($"✅ {gameObject.name}: Found prefab reference: {prefabReference.name}");
            }
        }
    }

    GameObject FindFlowerPrefab()
    {
        // Try find prefab with similar name
        string searchName = gameObject.name.Replace("(Clone)", "").Trim();
        
        // Try common flower prefab names
        string[] possibleNames = { "Flower", "Flower1", "Flower (1)", "Flower_Prefab" };
        
        foreach (string name in possibleNames)
        {
            GameObject prefab = Resources.Load<GameObject>(name);
            if (prefab != null)
            {
                return prefab;
            }
        }
        
        // As fallback, search in scene for similar objects
        GameObject[] allObjects = FindObjectsOfType<GameObject>();
        foreach (GameObject obj in allObjects)
        {
            if (obj.name.Contains("Flower") && obj != gameObject && obj.name.Contains("(Clone)") == false)
            {
                return obj;
            }
        }
        
        return null;
    }

    void OnValidate()
    {
        if (gameObject.tag != "Flower")
        {
            Debug.LogWarning($"Object '{gameObject.name}' nên có tag 'Flower'!");
            gameObject.tag = "Flower";
        }
    }
}
