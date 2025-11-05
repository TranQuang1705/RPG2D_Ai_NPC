using UnityEngine;
using System.Collections.Generic;
using System.Collections;
using UnityEngine.Networking;
using System;

// Dữ liệu item từ database
[System.Serializable]
public class DatabaseItem
{
    public int item_id;
    public string item_name;
    public string item_type;
    public string description;
    public string rarity;
    public float weight;
    public int value;
    public bool stackable;
    public int max_stack;
    public bool usable;
    public bool equipable;
    public string effect_type;
    public float effect_value;
    public string target_type;
    public string category;
    public string icon_path;
    public string model_path;
    public string created_at;
    public string updated_at;
}

// Dạng danh sách item từ database (JSON array)
[System.Serializable]
public class DatabaseItemList
{
    public List<DatabaseItem> items;
}

public class DatabaseItemManager : MonoBehaviour
{
    public static DatabaseItemManager Instance { get; private set; }
    
    [Header("Database Configuration")]
    private string apiUrl = "http://127.0.0.1:5002/items";
    
    [Header("Item Mapping")]
    [SerializeField] private List<ItemDatabaseMapping> itemMappings = new List<ItemDatabaseMapping>();
    
    // Cache database items
    private Dictionary<int, DatabaseItem> databaseItems = new Dictionary<int, DatabaseItem>();
    private Dictionary<string, DatabaseItem> itemMapByName = new Dictionary<string, DatabaseItem>();
    
    // Events
    public static event Action OnDatabaseItemsLoaded;
    
    void Awake()
    {
        if (Instance != null && Instance != this) 
        { 
            Destroy(gameObject); 
            return; 
        }
        Instance = this;
        
        // Don't destroy on load to keep database items across scenes
        DontDestroyOnLoad(gameObject);
    }
    
    void Start()
    {
        StartCoroutine(FetchDatabaseItems());
    }
    
    IEnumerator FetchDatabaseItems()
    {
        Debug.Log("🚀 DatabaseItemManager: Fetching items from database...");
        
        using (UnityWebRequest req = UnityWebRequest.Get(apiUrl))
        {
            yield return req.SendWebRequest();

            if (req.result == UnityWebRequest.Result.Success)
            {
                Debug.Log("✅ Database items loaded successfully");
                
                // Parse JSON array
                string jsonString = req.downloadHandler.text;
                DatabaseItemList itemList = JsonUtility.FromJson<DatabaseItemList>("{\"items\":" + jsonString + "}");
                
                // Clear previous data
                databaseItems.Clear();
                itemMapByName.Clear();
                
                // Cache items by ID and name
                foreach (var item in itemList.items)
                {
                    databaseItems[item.item_id] = item;
                    itemMapByName[item.item_name.ToLower()] = item;
                    
                    Debug.Log($"📦 Cached: {item.item_name} (ID: {item.item_id})");
                }
                
                Debug.Log($"✅ Loaded {databaseItems.Count} items from database");
                
                // Notify other systems that database is ready
                OnDatabaseItemsLoaded?.Invoke();
            }
            else
            {
                Debug.LogError("❌ Failed to load database items: " + req.error);
            }
        }
    }
    
    // Get database item by ID
    public DatabaseItem GetDatabaseItem(int itemId)
    {
        databaseItems.TryGetValue(itemId, out DatabaseItem item);
        return item;
    }
    
    // Get database item by name
    public DatabaseItem GetDatabaseItemByName(string itemName)
    {
        return itemMapByName.TryGetValue(itemName.ToLower(), out DatabaseItem item) ? item : null;
    }
    
    // Find ItemSO by database item ID
    public ItemSO FindItemSO(int databaseItemId)
    {
        foreach (var mapping in itemMappings)
        {
            if (mapping.databaseItemId == databaseItemId)
            {
                return mapping.itemSO;
            }
        }
        return null;
    }
    
    // Find ItemSO by database item name
    public ItemSO FindItemSOByName(string databaseItemName)
    {
        var dbItem = GetDatabaseItemByName(databaseItemName);
        if (dbItem != null)
        {
            return FindItemSO(dbItem.item_id);
        }
        return null;
    }
    
    // Create ItemSO from database item (for runtime creation)
    public ItemSO CreateItemSOFromDatabase(DatabaseItem dbItem)
    {
        // Create ScriptableObject instance
        var newItemSO = ScriptableObject.CreateInstance<ItemSO>();
        
        // Map database fields to ItemSO
        newItemSO.displayName = dbItem.item_name;
        newItemSO.description = dbItem.description;
        newItemSO.itemType = dbItem.item_type;
        newItemSO.rarity = dbItem.rarity;
        newItemSO.value = dbItem.value;
        newItemSO.weight = dbItem.weight;
        newItemSO.stackable = dbItem.stackable;
        newItemSO.maxStack = dbItem.max_stack;
        newItemSO.usable = dbItem.usable;
        newItemSO.equipable = dbItem.equipable;
        newItemSO.effectType = dbItem.effect_type;
        newItemSO.effectValue = dbItem.effect_value;
        newItemSO.databaseItemId = dbItem.item_id;
        
        // Try to load icon from Resources
        if (!string.IsNullOrEmpty(dbItem.icon_path))
        {
            string iconName = System.IO.Path.GetFileNameWithoutExtension(dbItem.icon_path);
            var icon = Resources.Load<Sprite>($"Icons/{iconName}");
            if (icon != null)
            {
                newItemSO.icon = icon;
                Debug.Log($"🖼️ Loaded icon for {dbItem.item_name}: Icons/{iconName}");
            }
            else
            {
                Debug.LogWarning($"⚠️ Could not find icon: Icons/{iconName}");
            }
        }
        
        // Try to load prefab from Resources
        if (!string.IsNullOrEmpty(dbItem.model_path))
        {
            string prefabName = System.IO.Path.GetFileNameWithoutExtension(dbItem.model_path);
            var prefab = Resources.Load<GameObject>($"Prefabs/{prefabName}");
            if (prefab != null)
            {
                newItemSO.prefab = prefab;
                Debug.Log($"🎁 Loaded prefab for {dbItem.item_name}: Prefabs/{prefabName}");
            }
            else
            {
                Debug.LogWarning($"⚠️ Could not find prefab: Prefabs/{prefabName}");
            }
        }
        
        return newItemSO;
    }
    
    // Check if database is loaded
    public bool IsDatabaseLoaded()
    {
        return databaseItems.Count > 0;
    }
    
    // Get all database items
    public Dictionary<int, DatabaseItem> GetAllDatabaseItems()
    {
        return databaseItems;
    }
    
    // Register item mapping (call this from MonoBehaviour or Unity Editor)
    public void RegisterItemMapping(int databaseItemId, ItemSO itemSO)
    {
        // Remove existing mapping for this database item if it exists
        itemMappings.RemoveAll(m => m.databaseItemId == databaseItemId);
        
        // Add new mapping
        itemMappings.Add(new ItemDatabaseMapping { databaseItemId = databaseItemId, itemSO = itemSO });
        
        Debug.Log($"🔗 Registered mapping: Database ID {databaseItemId} -> {itemSO.displayName}");
    }
    
    // Get all mappings
    public List<ItemDatabaseMapping> GetAllMappings()
    {
        return new List<ItemDatabaseMapping>(itemMappings);
    }
}

// Mapping class to connect database items with ScriptableObjects
[Serializable]
public class ItemDatabaseMapping
{
    public int databaseItemId;
    public ItemSO itemSO;
}
