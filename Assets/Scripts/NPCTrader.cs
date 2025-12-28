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
    [SerializeField] private MarketTrigger marketTrigger;
    [SerializeField] private MarketSellTrigger marketSellTrigger;
    private bool hasLeftMarket = false;
    void Start()
    {
        routineAI = GetComponent<NPCRoutineAI>();
        npcComponent = GetComponent<NPC>();

        if (routineAI != null)
        {
            routineAI.isTrader = true;
            routineAI.marketOpenHour = marketOpenHour;
            routineAI.marketCloseHour = marketCloseHour;
        }
        StartCoroutine(DelayedFindMarket());

        if (shopOpenIndicator == null)
        {
            CreateShopIndicator();
        }

        if (shopOpenIndicator != null)
            shopOpenIndicator.SetActive(false);

        StartCoroutine(LoadShopInventoryFromDatabase());

        if (marketTrigger == null)
            marketTrigger = FindObjectOfType<MarketTrigger>(true);

        if (marketSellTrigger == null)
            marketSellTrigger = FindObjectOfType<MarketSellTrigger>(true);

    }

    IEnumerator DelayedFindMarket()
    {
        yield return new WaitForSeconds(2f);

        if (marketStallLocation == null)
        {
            FindMarketLocation();
        }
    }


    void FindMarketLocation()
    {
        GameObject market = GameObject.FindWithTag("MarketStall");

        if (market == null)
        {
            GameObject camp = GameObject.Find("Camp");
            if (camp != null)
            {
                Transform marketTransform = camp.transform.Find("Market") ??
                                           camp.transform.Find("Market_0") ??
                                           camp.transform.Find("FlowerMarket_0");

                if (marketTransform != null)
                {
                    market = marketTransform.gameObject;
                }
            }
        }

        if (market == null)
        {
            GameObject[] allObjects = GameObject.FindObjectsOfType<GameObject>();
            foreach (var obj in allObjects)
            {
                if (obj.name.Contains("Market") && !obj.name.Contains("Stall"))
                {
                    market = obj;
                    break;
                }
            }
        }

        if (market != null)
        {
            marketStallLocation = market.transform;
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
        if (!IsMarketHours() && !hasLeftMarket)
        {

            ForceLeaveMarket();
            hasLeftMarket = true;
        }

        if (IsMarketHours())
        {
            hasLeftMarket = false;
        }
        if (shopOpenIndicator != null && shopOpenIndicator.activeSelf)
        {
            Vector3 indicatorPos = transform.position + Vector3.up * indicatorOffset;
            shopOpenIndicator.transform.position = indicatorPos;
        }

        // CheckMarketProximity();

    }
    public void ForceLeaveMarket()
    {

        if (routineAI != null && routineAI.currentActivity == NPCActivity.FlowerHunting)
        {
            Debug.Log($"🌸 {name}: Skip ForceLeaveMarket (NPC is gathering flowers)");
            return;
        }

        if (shopOpenIndicator != null)
            shopOpenIndicator.SetActive(false);

        if (instantiatedStall != null)
        {
            Destroy(instantiatedStall);
            instantiatedStall = null;
        }

        MarketTrigger trigger = FindObjectOfType<MarketTrigger>();
        if (trigger != null)
        {
            Debug.Log($"🏪 Forcing {name} to leave market and reset state.");
            trigger.ShowNPC(gameObject);
            trigger.ResetMarketToNormal();
        }

        if (routineAI != null)
        {
            routineAI.EnablePhysicsAfterMarket(); 
            routineAI.requestGoHome = false; 
            routineAI.UpdateCurrentActivity();      
            routineAI.currentState = NPCState.Idle;
        }
    }


    void CreateShopIndicator()
    {
        shopOpenIndicator = new GameObject($"{name}_ShopIndicator");
        shopOpenIndicator.transform.SetParent(transform);

        indicatorRenderer = shopOpenIndicator.AddComponent<SpriteRenderer>();
        indicatorRenderer.sortingOrder = 100;

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
            indicatorRenderer.color = Color.yellow;
        }

        shopOpenIndicator.SetActive(false);
    }

    // void CheckMarketProximity()
    // {
    //     if (marketStallLocation == null) return;
    //     Debug.Log($"⛳ [ahgpiqehgpjgqpjgq] Checking distance to market stall at {marketStallLocation.position}");
    //     float distance = Vector3.Distance(transform.position, marketStallLocation.position);
    //     bool wasAtMarket = isAtMarket;
    //     isAtMarket = distance <= marketProximity;
    //     Debug.Log($"⛳ [ahgpiqehgpjgqpjgq] IsAtMarket: {isAtMarket} wasAtMarket: {wasAtMarket}");
    //     if (isAtMarket && !wasAtMarket)
    //     {
    //         OnArrivedAtMarket();
    //     }
    //     else if (!isAtMarket && wasAtMarket)
    //     {
    //         OnLeftMarket();
    //     }
    //     if (marketStallLocation == null)
    //     {
    //         Debug.LogError($"❌ ahgpiqehgpjgqpjgaq {name}: marketStallLocation is NULL!");
    //         return;
    //     }
    //     else
    //     {
    //         Debug.Log($"📍 Stall reference = {marketStallLocation.name} at {marketStallLocation.position}");
    //     }
    // }

    void OnArrivedAtMarket()
    {
        Debug.Log($"🏪 {name} LEVI has arrived at the market stall.");

        if (marketStallPrefab != null && instantiatedStall == null)
        {
            instantiatedStall = Instantiate(marketStallPrefab, marketStallLocation.position, Quaternion.identity);
            instantiatedStall.name = $"{name}_MarketStall";
        }

        if (IsMarketHours())
        {
            if (shopOpenIndicator != null)
                shopOpenIndicator.SetActive(true);

        }
    }

    void OnLeftMarket()
    {
        Debug.Log($"🏪 {name} LEVI has left the market stall.");

        if (shopOpenIndicator != null)
            shopOpenIndicator.SetActive(false);

    }


    public bool IsShopOpen()
    {
        if (!useTimeBasedTrading) return true;

        return IsMarketHours();
    }

    public bool IsMarketHours()
    {
        if (!useTimeBasedTrading) return true;

        if (TimeManager.Instance == null) return false;

        float currentHour = TimeManager.Instance.GetCurrentHour();
        return currentHour >= marketOpenHour && currentHour < marketCloseHour;
    }

    IEnumerator LoadShopInventoryFromDatabase()
    {
        if (shopInventoryLoaded) yield break;
        if (DatabaseShopLoader.Instance == null)
        {
            GenerateMockInventoryByRole();
            shopInventoryLoaded = true;
            yield break;
        }

        yield return StartCoroutine(DatabaseShopLoader.Instance.LoadShopForTrader(this));

        if (shopInventory.Count == 0)
        {
            GenerateMockInventoryByRole();
        }

        shopInventoryLoaded = true;
    }


    void GenerateMockInventoryByRole()
    {
        shopInventory.Clear();

        switch (npcRole.ToLower())
        {
            case "flower_merchant":
                AddFlowerItems();
                break;

            case "hunter":
                AddHunterItems();
                break;

            case "blacksmith":
                AddBlacksmithItems();
                break;

            case "alchemist":
                AddAlchemistItems();
                break;

            default:
                AddGeneralItems();
                break;
        }
    }

    void AddFlowerItems()
    {

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


    public void OpenShop()
    {
        if (!IsShopOpen())
        {
            string message = !IsMarketHours()
                ? $"I'm not selling right now. Come back between {marketOpenHour}:00 - {marketCloseHour}:00."
                : "I need to be at my market stall to trade.";

            Debug.Log($"💬 {name}: {message}");

            return;
        }

        Debug.Log($"🏪 Opening {name}'s shop with {shopInventory.Count} items");

    }

    public void OnPlayerRequestTrade()
    {
        OpenShop();
    }

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

        // item.stock -= quantity;

        Debug.Log($"✅ Player bought {quantity}x {item.itemName} for {totalCost} {item.coinType}");
        return true;
    }


    public bool SellItem(int itemId, int quantity)
    {


        Debug.Log($"💰 Sell feature not yet implemented");
        return false;
    }


    public int GetNPCId() => npcId;
    public string GetNPCRole() => npcRole;
    public Transform GetMarketLocation() => marketStallLocation;
    public List<ShopItem> GetShopInventory() => shopInventory;
    public bool IsAtMarket() => isAtMarket;

    public void SetNPCId(int id) => npcId = id;
    public void SetNPCRole(string role)
    {
        npcRole = role;
        shopInventoryLoaded = false;
        StartCoroutine(LoadShopInventoryFromDatabase());
    }
    public void SetMarketLocation(Transform location) => marketStallLocation = location;

    void OnDrawGizmosSelected()
    {
        if (marketStallLocation != null)
        {
            Gizmos.color = Color.green;
            Gizmos.DrawWireSphere(marketStallLocation.position, marketProximity);

            Gizmos.color = Color.yellow;
            Gizmos.DrawLine(transform.position, marketStallLocation.position);
        }
    }


}


[System.Serializable]
public class ShopItem
{
    public int itemId;
    public string itemName;
    public int price;
    public string coinType = "Obal";
    public int stock;
    public bool isUnlimited = false;

    [Header("Display Info")]
    public Sprite icon;
    public string description;
}
