using System.Collections.Generic;
using UnityEngine;
using System.Collections; 
using System.Linq;

public class NPCTrader : MonoBehaviour
{
    [Header("NPC Info")]
    [SerializeField] private int npcId = 1;
    [SerializeField] private string npcRole = "flower_merchant"; 
    
    [Header("Market Schedule")]
    [SerializeField] private bool useTimeBasedTrading = true;
    [SerializeField] private float marketOpenHour = 8f;  
    [SerializeField] private float marketCloseHour = 12f; 
    
    [Header("Market Location")]
    [SerializeField] private Transform marketStallLocation; 
    [SerializeField] private GameObject marketStallPrefab;  
    [SerializeField] private float marketProximity = 2f;    
    
    [Header("Visual Indicators")]
    [SerializeField] private GameObject shopOpenIndicator;  
    [SerializeField] private SpriteRenderer indicatorRenderer;
    [SerializeField] private Sprite shopOpenIcon;
    [SerializeField] private float indicatorOffset = 1.5f;
    
    [Header("Shop Inventory")]
    [SerializeField] private List<ShopItem> shopInventory = new List<ShopItem>();
    [SerializeField] private int maxInventorySlots = 12;
    
    // State tracking
    private GameObject instantiatedStall;
    private bool isAtMarket = false;
    private bool shopInventoryLoaded = false;
    
    // Components
    private NPCRoutineAI routineAI;
    private NPC npcComponent;

    void Start()
    {
        routineAI = GetComponent<NPCRoutineAI>();
        npcComponent = GetComponent<NPC>();
        
        // ✅ Auto-enable trader mode in NPCRoutineAI
        if (routineAI != null)
        {
            routineAI.isTrader = true;
            routineAI.marketOpenHour = marketOpenHour;
            routineAI.marketCloseHour = marketCloseHour;
            Debug.Log($"✅ Enabled trader mode for {name} (market hours: {marketOpenHour}-{marketCloseHour})");
        }
        
        // ⏰ Delay finding market to ensure MapGenerator has spawned Camp
        StartCoroutine(DelayedFindMarket());
        
        // Create shop indicator if not assigned
        if (shopOpenIndicator == null)
        {
            CreateShopIndicator();
        }
        
        // Hide indicator initially
        if (shopOpenIndicator != null)
            shopOpenIndicator.SetActive(false);
        
        // Load shop inventory from database
        StartCoroutine(LoadShopInventoryFromDatabase());
        
        Debug.Log($"🏪 NPCTrader initialized for {name} (role: {npcRole})");
    }
    
    IEnumerator DelayedFindMarket()
    {
        // Wait 2 seconds for MapGenerator to finish spawning Camp
        yield return new WaitForSeconds(2f);
        
        if (marketStallLocation == null)
        {
            Debug.Log($"🔍 Searching for Market location (after 2s delay)...");
            FindMarketLocation();
        }
    }
    
    /// <summary>
    /// Auto-find Market location in scene (spawned by MapGenerator in Camp)
    /// </summary>
    void FindMarketLocation()
    {
        // Method 1: Find by tag
        GameObject market = GameObject.FindWithTag("MarketStall");
        
        if (market == null)
        {
            // Method 2: Find Market inside Camp
            GameObject camp = GameObject.Find("Camp");
            if (camp != null)
            {
                // Search for Market child in Camp
                Transform marketTransform = camp.transform.Find("Market") ?? 
                                           camp.transform.Find("Market_0") ??
                                           camp.transform.Find("FlowerMarket_0");
                
                if (marketTransform != null)
                {
                    market = marketTransform.gameObject;
                    Debug.Log($"🏪 Found Market in Camp: {market.name}");
                }
            }
        }
        
        if (market == null)
        {
            // Method 3: Find by name pattern
            GameObject[] allObjects = GameObject.FindObjectsOfType<GameObject>();
            foreach (var obj in allObjects)
            {
                if (obj.name.Contains("Market") && !obj.name.Contains("Stall"))
                {
                    market = obj;
                    Debug.Log($"🏪 Found Market by name pattern: {market.name}");
                    break;
                }
            }
        }
        
        if (market != null)
        {
            marketStallLocation = market.transform;
            
            // Also set in NPCRoutineAI if exists
            if (routineAI != null)
            {
                routineAI.marketStallLocation = marketStallLocation;
            }
            
            Debug.Log($"✅ Auto-assigned market location: {market.name} at {marketStallLocation.position}");
        }
        else
        {
            Debug.LogWarning($"⚠️ Could not find Market location! Market stall prefab name should contain 'Market'");
        }
    }

    void Update()
    {
        // Update indicator position
        if (shopOpenIndicator != null && shopOpenIndicator.activeSelf)
        {
            Vector3 indicatorPos = transform.position + Vector3.up * indicatorOffset;
            shopOpenIndicator.transform.position = indicatorPos;
        }
        
        // Check if NPC is at market location
        CheckMarketProximity();
    }

    void CreateShopIndicator()
    {
        shopOpenIndicator = new GameObject($"{name}_ShopIndicator");
        shopOpenIndicator.transform.SetParent(transform);
        
        indicatorRenderer = shopOpenIndicator.AddComponent<SpriteRenderer>();
        indicatorRenderer.sortingOrder = 100;
        
        // Load icon from Resources
        if (shopOpenIcon == null)
        {
            shopOpenIcon = Resources.Load<Sprite>("Icons/shop_icon");
        }
        
        if (shopOpenIcon != null)
        {
            indicatorRenderer.sprite = shopOpenIcon;
        }
        else
        {
            // Fallback: create simple visual indicator
            indicatorRenderer.color = Color.yellow;
        }
        
        shopOpenIndicator.SetActive(false);
        Debug.Log($"🎨 Shop indicator created for {name}");
    }

    void CheckMarketProximity()
    {
        if (marketStallLocation == null) return;
        
        float distance = Vector3.Distance(transform.position, marketStallLocation.position);
        bool wasAtMarket = isAtMarket;
        isAtMarket = distance <= marketProximity;
        
        // Trigger events when entering/leaving market
        if (isAtMarket && !wasAtMarket)
        {
            OnArrivedAtMarket();
        }
        else if (!isAtMarket && wasAtMarket)
        {
            OnLeftMarket();
        }
    }

    void OnArrivedAtMarket()
    {
        Debug.Log($"🏪 {name} arrived at market location");
        
        // Spawn market stall if defined
        if (marketStallPrefab != null && instantiatedStall == null)
        {
            instantiatedStall = Instantiate(marketStallPrefab, marketStallLocation.position, Quaternion.identity);
            instantiatedStall.name = $"{name}_MarketStall";
            Debug.Log($"🎪 Spawned market stall for {name}");
        }
        
        // Show shop indicator if during market hours
        if (IsMarketHours())
        {
            if (shopOpenIndicator != null)
                shopOpenIndicator.SetActive(true);
            
            Debug.Log($"🟢 {name}'s shop is now OPEN");
        }
    }

    void OnLeftMarket()
    {
        Debug.Log($"👋 {name} left market location");
        
        // Hide shop indicator
        if (shopOpenIndicator != null)
            shopOpenIndicator.SetActive(false);
        
        // Optionally destroy stall (or keep it for immersion)
        // if (instantiatedStall != null)
        // {
        //     Destroy(instantiatedStall);
        //     instantiatedStall = null;
        // }
    }

    /// <summary>
    /// Check if shop is currently open (market hours + at market location)
    /// </summary>
    public bool IsShopOpen()
    {
        if (!useTimeBasedTrading) return true; // Always open if time-based disabled
        
        return IsMarketHours() && isAtMarket;
    }

    /// <summary>
    /// Check if current time is within market hours
    /// </summary>
    public bool IsMarketHours()
    {
        if (!useTimeBasedTrading) return true;
        
        if (TimeManager.Instance == null) return false;
        
        float currentHour = TimeManager.Instance.GetCurrentHour();
        return currentHour >= marketOpenHour && currentHour < marketCloseHour;
    }

    /// <summary>
    /// Load shop inventory from database based on NPC ID
    /// </summary>
    IEnumerator LoadShopInventoryFromDatabase()
    {
        if (shopInventoryLoaded) yield break;
        
        Debug.Log($"📦 [NPCTrader] Loading shop inventory for NPC {npcId}...");
        
        // Check if DatabaseShopLoader exists
        if (DatabaseShopLoader.Instance == null)
        {
            Debug.LogWarning($"⚠️ [NPCTrader] DatabaseShopLoader not found! Creating fallback mock inventory.");
            GenerateMockInventoryByRole();
            shopInventoryLoaded = true;
            yield break;
        }
        
        // Load from database using DatabaseShopLoader
        yield return StartCoroutine(DatabaseShopLoader.Instance.LoadShopForTrader(this));
        
        // If no items loaded from database, use mock inventory
        if (shopInventory.Count == 0)
        {
            Debug.LogWarning($"⚠️ [NPCTrader] No items in database for NPC {npcId}, using mock inventory");
            GenerateMockInventoryByRole();
        }
        
        shopInventoryLoaded = true;
        Debug.Log($"✅ [NPCTrader] Loaded {shopInventory.Count} items for {name} (role: {npcRole})");
    }

    /// <summary>
    /// Generate mock shop inventory based on NPC role (temporary until database integration)
    /// </summary>
    void GenerateMockInventoryByRole()
    {
        shopInventory.Clear();
        
        switch (npcRole.ToLower())
        {
            case "flower_merchant":
                // Load flower items
                AddFlowerItems();
                break;
                
            case "hunter":
                // Load animal products, pelts, meat
                AddHunterItems();
                break;
                
            case "blacksmith":
                // Load weapons, tools, armor
                AddBlacksmithItems();
                break;
                
            case "alchemist":
                // Load potions, herbs, ingredients
                AddAlchemistItems();
                break;
                
            default:
                // General merchant - mixed items
                AddGeneralItems();
                break;
        }
    }

    void AddFlowerItems()
    {
        // Example: Load from Resources or database
        // For now, add mock items
        shopInventory.Add(new ShopItem
        {
            itemId = 2,
            itemName = "Daisy Flower",
            price = 5,
            stock = 20,
            coinType = "Obal"
        });
        
        shopInventory.Add(new ShopItem
        {
            itemId = 3,
            itemName = "Rose",
            price = 15,
            stock = 10,
            coinType = "Obal"
        });
        
        shopInventory.Add(new ShopItem
        {
            itemId = 4,
            itemName = "Tulip",
            price = 8,
            stock = 15,
            coinType = "Obal"
        });
    }

    void AddHunterItems()
    {
        shopInventory.Add(new ShopItem
        {
            itemId = 10,
            itemName = "Rabbit Pelt",
            price = 20,
            stock = 8,
            coinType = "Obal"
        });
        
        shopInventory.Add(new ShopItem
        {
            itemId = 11,
            itemName = "Deer Meat",
            price = 30,
            stock = 5,
            coinType = "Varos"
        });
    }

    void AddBlacksmithItems()
    {
        shopInventory.Add(new ShopItem
        {
            itemId = 20,
            itemName = "Iron Sword",
            price = 100,
            stock = 3,
            coinType = "Sylv"
        });
    }

    void AddAlchemistItems()
    {
        shopInventory.Add(new ShopItem
        {
            itemId = 30,
            itemName = "Health Potion",
            price = 25,
            stock = 10,
            coinType = "Obal"
        });
    }

    void AddGeneralItems()
    {
        shopInventory.Add(new ShopItem
        {
            itemId = 1,
            itemName = "Apple",
            price = 8,
            stock = 20,
            coinType = "Obal"
        });
    }

    /// <summary>
    /// Open shop UI (called from player interaction or dialogue)
    /// </summary>
    public void OpenShop()
    {
        if (!IsShopOpen())
        {
            string message = !IsMarketHours() 
                ? $"I'm not selling right now. Come back between {marketOpenHour}:00 - {marketCloseHour}:00."
                : "I need to be at my market stall to trade.";
            
            Debug.Log($"💬 {name}: {message}");
            
            // TODO: Show notification to player
            return;
        }
        
        Debug.Log($"🏪 Opening {name}'s shop with {shopInventory.Count} items");
        
        // TODO: Open TradePanel UI
        // TradePanel.Instance?.Open(this);
    }

    /// <summary>
    /// Called by dialogue system when player says "trade" or "buy"
    /// </summary>
    public void OnPlayerRequestTrade()
    {
        OpenShop();
    }

    /// <summary>
    /// Buy an item from this shop
    /// </summary>
    public bool BuyItem(int itemId, int quantity)
    {
        ShopItem item = shopInventory.FirstOrDefault(i => i.itemId == itemId);
        
        if (item == null)
        {
            Debug.LogWarning($"⚠️ Item {itemId} not found in {name}'s shop");
            return false;
        }
        
        if (item.stock < quantity)
        {
            Debug.LogWarning($"⚠️ Not enough stock for {item.itemName} (want: {quantity}, have: {item.stock})");
            return false;
        }
        
        int totalCost = item.price * quantity;
        
        // TODO: Check player currency
        // if (!PlayerHasEnoughCurrency(item.coinType, totalCost))
        // {
        //     Debug.LogWarning($"⚠️ Player doesn't have enough {item.coinType}");
        //     return false;
        // }
        
        // Deduct stock
        item.stock -= quantity;
        
        // TODO: Add item to player inventory
        // TODO: Deduct player currency
        
        Debug.Log($"✅ Player bought {quantity}x {item.itemName} for {totalCost} {item.coinType}");
        return true;
    }

    /// <summary>
    /// Sell an item to this shop
    /// </summary>
    public bool SellItem(int itemId, int quantity)
    {
        // TODO: Implement sell logic
        // Check if shop buys this type of item
        // Calculate sell price (usually 50% of buy price)
        // Add currency to player
        
        Debug.Log($"💰 Sell feature not yet implemented");
        return false;
    }

    // Public getters
    public int GetNPCId() => npcId;
    public string GetNPCRole() => npcRole;
    public Transform GetMarketLocation() => marketStallLocation;
    public List<ShopItem> GetShopInventory() => shopInventory;
    public bool IsAtMarket() => isAtMarket;

    // Public setters
    public void SetNPCId(int id) => npcId = id;
    public void SetNPCRole(string role)
    {
        npcRole = role;
        shopInventoryLoaded = false; // Reset flag
        StartCoroutine(LoadShopInventoryFromDatabase()); // Reload inventory for new role
    }
    public void SetMarketLocation(Transform location) => marketStallLocation = location;

    void OnDrawGizmosSelected()
    {
        // Draw market location and proximity radius
        if (marketStallLocation != null)
        {
            Gizmos.color = Color.green;
            Gizmos.DrawWireSphere(marketStallLocation.position, marketProximity);
            
            Gizmos.color = Color.yellow;
            Gizmos.DrawLine(transform.position, marketStallLocation.position);
        }
    }
}

/// <summary>
/// Represents a single item in shop inventory
/// </summary>
[System.Serializable]
public class ShopItem
{
    public int itemId;
    public string itemName;
    public int price;
    public string coinType = "Obal"; // Obal, Varos, Sylv, Feron, Astryl, Aurum
    public int stock;
    public bool isUnlimited = false;
    
    [Header("Display Info")]
    public Sprite icon;
    public string description;
}
