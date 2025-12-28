using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;
using System.Collections.Generic;


/// <summary>
/// Controller for Shop UI - populates item slots with data from database
/// Attach to TradeUI GameObject
/// </summary>
public class UIShopController : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Transform itemsContainer; // Parent object chứa các ItemTradePanel
    [SerializeField] private GameObject itemSlotPrefab; // Prefab của ItemTradePanel
    
    [Header("NPC Reference")]
    [SerializeField] private int currentNPCId = 1; // NPC hiện tại đang mở shop
    
    [Header("Auto Setup")]
    [SerializeField] private bool autoFindContainer = true;
    
    [Header("Hover & Pick")]
    [SerializeField] private GameObject hoverPanel; // Panel hiển thị khi hover
    [SerializeField] private bool autoFindHoverPanel = true;
    
    private List<GameObject> spawnedSlots = new List<GameObject>();
    private GameObject currentPickedSlot = null;

    void Start()
    {
        // Auto-find container if not assigned
        if (autoFindContainer && itemsContainer == null)
        {
            // Tìm object tên "Items" trong children
            Transform found = transform.Find("Items");
            if (found != null)
            {
                itemsContainer = found;
            }
            else
            {
                // Tìm ItemTradePanel parent
                Transform parent = transform.Find("ItemTradePanel");
                if (parent != null)
                {
                    itemsContainer = parent;
                }
            }
        }

        if (itemsContainer == null)
        {
            Debug.LogError($"❌ [UIShopController] Items container not found!");
        }

        // Auto-find hover panel
        if (autoFindHoverPanel && hoverPanel == null)
        {
            Transform found = transform.Find("HoverPanel");
            if (found == null)
            {
                found = transform.Find("Pick");
            }
            if (found != null)
            {
                hoverPanel = found.gameObject;
            }
        }

        // Hide hover panel initially
        if (hoverPanel != null)
        {
            hoverPanel.SetActive(false);
        }

        // Hide existing ItemTradePanel templates in container
        HideTemplateSlots();
    }

    /// <summary>
    /// Hide template ItemTradePanel slots that exist in scene
    /// </summary>
    private void HideTemplateSlots()
    {
        if (itemsContainer == null) return;

        // Find all ItemTradePanel children and hide them
        foreach (Transform child in itemsContainer)
        {
            if (child.name.Contains("ItemTradePanel"))
            {
                child.gameObject.SetActive(false);
            }
        }
    }

    void OnEnable()
    {
        // Reload shop khi UI được mở
        LoadShopInventory(currentNPCId);
    }

    void Update()
    {
        // Close UI with E key (ESC handled by EscapeKeyManager)
        if (Input.GetKeyDown(KeyCode.E))
        {
            CloseShopUI();
        }
    }

    /// <summary>
    /// Load shop inventory for specific NPC
    /// </summary>
    public void LoadShopInventory(int npcId)
    {
        currentNPCId = npcId;
        
        if (DatabaseShopLoader.Instance == null)
        {
            Debug.LogError($"❌ [UIShopController] DatabaseShopLoader not found!");
            return;
        }

        // Fetch inventory from database
        StartCoroutine(FetchAndPopulateShop(npcId));
    }

    private IEnumerator FetchAndPopulateShop(int npcId)
    {
        // Fetch from database
        yield return StartCoroutine(DatabaseShopLoader.Instance.FetchShopInventory(npcId));

        // Get shop items
        List<DatabaseShopItem> shopItems = DatabaseShopLoader.Instance.GetShopInventory(npcId);

        // Populate UI
        PopulateShopUI(shopItems);
    }

    /// <summary>
    /// Populate UI with shop items
    /// </summary>
    private void PopulateShopUI(List<DatabaseShopItem> shopItems)
    {
        // Clear existing slots
        ClearShopSlots();

        if (shopItems == null || shopItems.Count == 0)
        {
            Debug.LogWarning($"⚠️ [UIShopController] No shop items to display");
            return;
        }

        // Create slot for each item
        foreach (var item in shopItems)
        {
            if (!item.is_available) continue;

            CreateItemSlot(item);
        }

        // Force rebuild layout after spawning all items
        if (itemsContainer != null)
        {
            Canvas.ForceUpdateCanvases();
            LayoutRebuilder.ForceRebuildLayoutImmediate(itemsContainer.GetComponent<RectTransform>());
        }

        Debug.Log($"✅ [UIShopController] Populated {spawnedSlots.Count} item slots");
    }

    /// <summary>
    /// Create item slot UI from data
    /// </summary>
    private void CreateItemSlot(DatabaseShopItem item)
    {
        GameObject slotObj;

        // Use prefab if assigned, otherwise duplicate existing
        if (itemSlotPrefab != null)
        {
            slotObj = Instantiate(itemSlotPrefab, itemsContainer);
        }
        else
        {
            // Find first ItemTradePanel child as template
            Transform template = itemsContainer.Find("ItemTradePanel");
            if (template == null)
            {
                Debug.LogError($"❌ [UIShopController] No ItemTradePanel template found!");
                return;
            }

            slotObj = Instantiate(template.gameObject, itemsContainer);
        }

        // Reset local position và scale
        RectTransform rectTransform = slotObj.GetComponent<RectTransform>();
        if (rectTransform != null)
        {
            rectTransform.localPosition = Vector3.zero;
            rectTransform.localScale = Vector3.one;
            rectTransform.localRotation = Quaternion.identity;
        }

        slotObj.SetActive(true);
        spawnedSlots.Add(slotObj);

        // Populate slot with data
        PopulateSlotData(slotObj, item);

        // Setup ItemTradePanelController
        SetupPanelController(slotObj, item);
    }

    /// <summary>
    /// Populate slot UI elements with item data
    /// </summary>
    private void PopulateSlotData(GameObject slot, DatabaseShopItem item)
    {
        // Find UI elements
        Transform itemsIconTransform = FindChildRecursive(slot.transform, "ItemsIcon");
        Transform itemStockTransform = FindChildRecursive(slot.transform, "ItemStock");
        Transform itemsNameTransform = FindChildRecursive(slot.transform, "ItemsName");
        Transform priceTextTransform = FindChildRecursive(slot.transform, "PriceText");
        Transform priceIconTransform = FindChildRecursive(slot.transform, "PriceIcon");

        // Set Item Icon
        if (itemsIconTransform != null)
        {
            Image iconImage = itemsIconTransform.GetComponent<Image>();
            if (iconImage != null && !string.IsNullOrEmpty(item.icon_path))
            {
                Sprite icon = LoadIcon(item.icon_path);
                if (icon != null)
                {
                    iconImage.sprite = icon;
                }
            }
        }

        // Set Item Stock
        if (itemStockTransform != null)
        {
            TextMeshProUGUI stockText = itemStockTransform.GetComponent<TextMeshProUGUI>();
            if (stockText != null)
            {
                if (item.stock == -1)
                {
                    stockText.text = "∞"; // Unlimited stock
                }
                else
                {
                    stockText.text = $"x{item.stock}";
                }
            }
        }

        // Set Item Name
        if (itemsNameTransform != null)
        {
            TextMeshProUGUI nameText = itemsNameTransform.GetComponent<TextMeshProUGUI>();
            if (nameText != null)
            {
                nameText.text = item.item_name;
            }
        }

        // Set Price
        if (priceTextTransform != null)
        {
            TextMeshProUGUI priceText = priceTextTransform.GetComponent<TextMeshProUGUI>();
            if (priceText != null)
            {
                // Calculate discount if applicable
                int finalPrice = item.price;
                if (item.discount_percent > 0)
                {
                    finalPrice = Mathf.RoundToInt(item.price * (1f - item.discount_percent / 100f));
                }
                
                priceText.text = finalPrice.ToString();
            }
        }

        // Set Price Icon (coin type)
        if (priceIconTransform != null)
        {
            Image priceIcon = priceIconTransform.GetComponent<Image>();
            if (priceIcon != null)
            {
                Sprite coinIcon = LoadCoinIcon(item.coin_type);
                if (coinIcon != null)
                {
                    priceIcon.sprite = coinIcon;
                }
            }
        }

        Debug.Log($"  📦 Created slot: {item.item_name} - {item.price} {item.coin_type}");
    }

    /// <summary>
    /// Load item icon from Resources
    /// </summary>
    private Sprite LoadIcon(string iconPath)
    {
        // Remove file extension
        string path = iconPath.Replace(".png", "").Replace(".jpg", "");
        
        Sprite icon = Resources.Load<Sprite>(path);
        
        if (icon == null)
        {
            Debug.LogWarning($"⚠️ [UIShopController] Could not load icon: {path}");
        }
        
        return icon;
    }

    /// <summary>
    /// Load coin icon based on coin type
    /// </summary>
    private Sprite LoadCoinIcon(string coinType)
    {
        string path = $"Icons/Coins/{coinType.ToLower()}";
        Sprite coinIcon = Resources.Load<Sprite>(path);
        
        if (coinIcon == null)
        {
            Debug.LogWarning($"⚠️ [UIShopController] Could not load coin icon: {path}");
        }
        
        return coinIcon;
    }

    /// <summary>
    /// Find child recursively by name
    /// </summary>
    private Transform FindChildRecursive(Transform parent, string childName)
    {
        // Check direct children first
        Transform found = parent.Find(childName);
        if (found != null) return found;

        // Search recursively
        foreach (Transform child in parent)
        {
            found = FindChildRecursive(child, childName);
            if (found != null) return found;
        }

        return null;
    }

    /// <summary>
    /// Clear all spawned shop slots
    /// </summary>
    private void ClearShopSlots()
    {
        foreach (var slot in spawnedSlots)
        {
            if (slot != null)
            {
                Destroy(slot);
            }
        }
        spawnedSlots.Clear();
        
        // Force rebuild layout after clear
        if (itemsContainer != null)
        {
            LayoutRebuilder.ForceRebuildLayoutImmediate(itemsContainer.GetComponent<RectTransform>());
        }
    }

    /// <summary>
    /// Set current NPC for shop
    /// </summary>
    public void SetCurrentNPC(int npcId)
    {
        currentNPCId = npcId;
        LoadShopInventory(npcId);
    }

    /// <summary>
    /// Refresh shop display
    /// </summary>
    public void RefreshShop()
    {
        if (DatabaseShopLoader.Instance != null)
        {
            DatabaseShopLoader.Instance.ClearShopCache(currentNPCId);
        }
        LoadShopInventory(currentNPCId);
    }

    /// <summary>
    /// Update stock display for specific item
    /// </summary>
    public void UpdateItemStock(int itemId, int newStock)
    {
        // Find the slot with this item
        foreach (var slot in spawnedSlots)
        {
            if (slot == null) continue;

            ItemTradePanelController controller = slot.GetComponent<ItemTradePanelController>();
            if (controller != null)
            {
                DatabaseShopItem itemData = controller.GetItemData();
                if (itemData != null && itemData.item_id == itemId)
                {
                    // Update stock in data
                    itemData.stock = newStock;

                    // Update stock text display
                    Transform stockTransform = FindChildRecursive(slot.transform, "ItemStock");
                    if (stockTransform != null)
                    {
                        TextMeshProUGUI stockText = stockTransform.GetComponent<TextMeshProUGUI>();
                        if (stockText != null)
                        {
                            if (newStock == -1)
                            {
                                stockText.text = "∞";
                            }
                            else if (newStock <= 0)
                            {
                                stockText.text = "x0";
                                stockText.color = Color.red; // Out of stock indicator
                            }
                            else
                            {
                                stockText.text = $"x{newStock}";
                                stockText.color = Color.white;
                            }
                        }
                    }

                    Debug.Log($"📊 [UIShopController] Updated stock for item {itemId}: {newStock}");
                    break;
                }
            }
        }
    }

    void OnDisable()
    {
        ItemTradePanelController.CloseAllPickPanels();
        
        // Clear selection
        if (ShopInventorySelectionManager.Instance != null)
        {
            ShopInventorySelectionManager.Instance.ClearSelection();
        }
        
        // Clear PricePanel
        if (PricePanelController.Instance != null)
        {
            PricePanelController.Instance.ClearAll();
        }
        
        // XÓA items vừa thêm vào inventory (vì chưa thanh toán)
        // NHƯNG nếu đã thanh toán thì GIỮ LẠI items
        if (ShoppingCartManager.Instance != null)
        {
            if (!ShoppingCartManager.Instance.IsPaid())
            {
                // Chưa thanh toán → xóa items khỏi inventory
                ShoppingCartManager.Instance.RemoveShoppingItemsFromInventory();
            }
            else
            {
                // Đã thanh toán → giữ items trong inventory
                Debug.Log("✅ [UIShop] Items kept in inventory (paid)");
            }
            
            ShoppingCartManager.Instance.ClearCart();
        }
        
        // Clear slots when UI is hidden
        ClearShopSlots();
    }

    /// <summary>
    /// Get current NPC ID
    /// </summary>
    public int GetCurrentNPCId()
    {
        return currentNPCId;
    }

    /// <summary>
    /// Close shop UI
    /// </summary>
    public void CloseShopUI()
    {
        gameObject.SetActive(false);
    }

    /// <summary>
    /// Setup ItemTradePanelController for slot
    /// </summary>
    private void SetupPanelController(GameObject slot, DatabaseShopItem item)
    {
        // Add or get ItemTradePanelController
        ItemTradePanelController controller = slot.GetComponent<ItemTradePanelController>();
        if (controller == null)
        {
            controller = slot.AddComponent<ItemTradePanelController>();
        }

        // Initialize với item data
        controller.Initialize(item);
    }
}
