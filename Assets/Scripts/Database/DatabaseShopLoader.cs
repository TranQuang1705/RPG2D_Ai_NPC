using UnityEngine;
using UnityEngine.Networking;
using System.Collections;
using System.Collections.Generic;
using System;

/// <summary>
/// Database model for npc_shop_inventory table
/// </summary>
[System.Serializable]
public class DatabaseShopItem
{
    public int shop_inventory_id;
    public int npc_id;
    public int item_id;
    public string item_name;
    public string item_type;
    public string description;
    public string rarity;
    public string icon_path;
    public string model_path;
    public int stock;
    public int price;
    public string coin_type;
    public float discount_percent;
    public bool is_available;
    public int currentStock;
}

[System.Serializable]
public class DatabaseShopItemList
{
    public List<DatabaseShopItem> shop_items;
}

/// <summary>
/// Loads NPC shop inventory from database
/// Pattern similar to DatabaseCoinLoader
/// </summary>
public class DatabaseShopLoader : MonoBehaviour
{
    public static DatabaseShopLoader Instance { get; private set; }

    [Header("API Configuration")]
    [SerializeField] private string apiUrl = "http://127.0.0.1:5002";

    [Header("Auto Load")]
    [SerializeField] private bool loadOnStart = false; // Load manually when needed

    private Dictionary<int, List<DatabaseShopItem>> shopCache = new Dictionary<int, List<DatabaseShopItem>>();

    public static event Action<int> OnShopInventoryLoaded; // event(npcId)
    [Header("Reset Settings")]
    [SerializeField] private int defaultStockOnGameStart = 20;
    [SerializeField] private bool resetStockOnGameStart = true;
    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    void Start()
    {
        Debug.Log($"🏪 [DatabaseShopLoader] Initialized");

        if (resetStockOnGameStart)
        {
            StartCoroutine(ResetAllShopStockOnGameStart());
        }

    }

    /// <summary>
    /// Fetch shop inventory for specific NPC from database
    /// </summary>
    public IEnumerator FetchShopInventory(int npcId)
    {
        Debug.Log($"🏪 [DatabaseShopLoader] Fetching shop inventory for NPC {npcId}...");

        string url = $"{apiUrl}/npc_shop_inventory?npc_id={npcId}";
        using (UnityWebRequest req = UnityWebRequest.Get(url))
        {
            yield return req.SendWebRequest();

            if (req.result == UnityWebRequest.Result.Success)
            {
                string json = req.downloadHandler.text;
                Debug.Log($"✅ [DatabaseShopLoader] Shop data: {json}");

                try
                {
                    // Handle empty array
                    if (json == "[]" || string.IsNullOrEmpty(json))
                    {
                        Debug.LogWarning($"⚠️ [DatabaseShopLoader] NPC {npcId} has no shop inventory in database");
                        shopCache[npcId] = new List<DatabaseShopItem>();
                        OnShopInventoryLoaded?.Invoke(npcId);
                        yield break;
                    }

                    DatabaseShopItemList shopList = JsonUtility.FromJson<DatabaseShopItemList>("{\"shop_items\":" + json + "}");

                    // Cache shop inventory
                    shopCache[npcId] = shopList.shop_items;

                    Debug.Log($"✅ [DatabaseShopLoader] Loaded {shopList.shop_items.Count} items for NPC {npcId}");

                    // Log items
                    foreach (var item in shopList.shop_items)
                    {
                        Debug.Log($"  📦(afafaf) {item.item_name} - {item.price} {item.coin_type} (stock: {item.stock})");
                    }

                    OnShopInventoryLoaded?.Invoke(npcId);
                }
                catch (Exception e)
                {
                    Debug.LogError($"❌ [DatabaseShopLoader] Failed to parse shop inventory: {e.Message}");
                }
            }
            else
            {
                Debug.LogError($"❌ [DatabaseShopLoader] Failed to fetch shop inventory: {req.error}");
            }
        }
    }

    /// <summary>
    /// Get cached shop inventory for NPC
    /// </summary>
    public List<DatabaseShopItem> GetShopInventory(int npcId)
    {
        if (shopCache.TryGetValue(npcId, out List<DatabaseShopItem> items))
        {
            return items;
        }
        return new List<DatabaseShopItem>();
    }
    private IEnumerator ResetAllShopStockOnGameStart()
    {
        Debug.Log("🔄 [ShopReset] Resetting all shop stock on game start...");

        // ⚠️ TẠM THỜI: bạn cần list npcId
        // Cách đơn giản nhất: reset theo cache sau khi fetch
        // Ví dụ: giả sử npcId từ 1 → 10
        for (int npcId = 1; npcId <= 10; npcId++)
        {
            yield return StartCoroutine(FetchShopInventory(npcId));

            if (!shopCache.ContainsKey(npcId))
                continue;

            foreach (var item in shopCache[npcId])
            {
                // Skip unlimited stock
                if (item.stock == -1)
                    continue;

                if (item.stock != defaultStockOnGameStart)
                {
                    item.stock = defaultStockOnGameStart;

                    yield return StartCoroutine(
                        UpdateItemStock(
                            npcId,
                            item.item_id,
                            defaultStockOnGameStart
                        )
                    );

                    Debug.Log($"🔁 Reset stock: NPC {npcId} | Item {item.item_id} → {defaultStockOnGameStart}");
                }
            }
        }

        Debug.Log("✅ [ShopReset] All shop stock reset completed");
    }

    public ShopItem ConvertToShopItem(DatabaseShopItem dbItem)
    {
        ShopItem shopItem = new ShopItem
        {
            itemId = dbItem.item_id,
            itemName = dbItem.item_name,
            price = dbItem.price,
            coinType = dbItem.coin_type,
            stock = dbItem.stock,
            isUnlimited = dbItem.stock == -1,
            description = dbItem.description
        };

        // Load icon from Resources
        if (!string.IsNullOrEmpty(dbItem.icon_path))
        {
            string iconPath = dbItem.icon_path.Replace(".png", "").Replace(".jpg", "");
            Sprite icon = Resources.Load<Sprite>(iconPath);

            if (icon != null)
            {
                shopItem.icon = icon;
            }
            else
            {
                Debug.LogWarning($"⚠️ Could not find icon: {iconPath}");
            }
        }

        return shopItem;
    }

    /// <summary>
    /// Load shop inventory and apply to NPCTrader component
    /// </summary>
    public IEnumerator LoadShopForTrader(NPCTrader trader)
    {
        if (trader == null)
        {
            Debug.LogError("❌ [DatabaseShopLoader] NPCTrader is null!");
            yield break;
        }

        int npcId = trader.GetNPCId();
        Debug.Log($"🏪 [DatabaseShopLoader] Loading shop for trader NPC {npcId}...");

        // Fetch from database
        yield return StartCoroutine(FetchShopInventory(npcId));

        // Get cached inventory
        List<DatabaseShopItem> dbItems = GetShopInventory(npcId);

        if (dbItems.Count == 0)
        {
            Debug.LogWarning($"⚠️ [DatabaseShopLoader] No items found for NPC {npcId}");
            yield break;
        }

        // Convert to ShopItem and apply to trader
        List<ShopItem> shopInventory = new List<ShopItem>();
        foreach (var dbItem in dbItems)
        {
            if (!dbItem.is_available) continue; // Skip unavailable items

            ShopItem shopItem = ConvertToShopItem(dbItem);
            shopInventory.Add(shopItem);
        }

        // Update trader's inventory
        trader.GetShopInventory().Clear();
        trader.GetShopInventory().AddRange(shopInventory);

        Debug.Log($"✅ [DatabaseShopLoader] Applied {shopInventory.Count} items to trader NPC {npcId}");
    }

    /// <summary>
    /// Update stock in database after purchase
    /// </summary>
    public IEnumerator UpdateItemStock(int npcId, int itemId, int newStock)
    {
        string url = $"{apiUrl}/npc_shop_inventory/update_stock";

        WWWForm form = new WWWForm();
        form.AddField("npc_id", npcId);
        form.AddField("item_id", itemId);
        form.AddField("stock", newStock);

        using (UnityWebRequest req = UnityWebRequest.Post(url, form))
        {
            req.SetRequestHeader("Content-Type", "application/x-www-form-urlencoded");
            yield return req.SendWebRequest();

            if (req.result == UnityWebRequest.Result.Success)
            {
                Debug.Log($"✅ Updated stock for item {itemId} in NPC {npcId}'s shop: {newStock}");
            }
            else
            {
                Debug.LogError($"❌ Failed to update stock: {req.error}");
            }
        }
    }

    /// <summary>
    /// Clear shop cache for NPC (force reload)
    /// </summary>
    public void ClearShopCache(int npcId)
    {
        if (shopCache.ContainsKey(npcId))
        {
            shopCache.Remove(npcId);
            Debug.Log($"🗑️ Cleared shop cache for NPC {npcId}");
        }
    }

    /// <summary>
    /// Refresh shop inventory for NPC (reload from database)
    /// </summary>
    public void RefreshShop(int npcId)
    {
        ClearShopCache(npcId);
        StartCoroutine(FetchShopInventory(npcId));
    }
}
