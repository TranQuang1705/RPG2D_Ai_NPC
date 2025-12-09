using UnityEngine;

/// <summary>
/// Quản lý item selection giữa Shop và Inventory
/// Track item nào đang được chọn và từ nguồn nào (Shop hay Inventory)
/// </summary>
public class ShopInventorySelectionManager : MonoBehaviour
{
    public static ShopInventorySelectionManager Instance { get; private set; }

    [Header("References")]
    [SerializeField] private ShoppingBagArrowController arrowController;

    [Header("Auto Find")]
    [SerializeField] private bool autoFindArrow = true;

    // Current selected item
    private GameObject selectedItemSlot;
    private ItemSource selectedItemSource = ItemSource.None;
    private DatabaseShopItem selectedShopItem;
    private ItemSO selectedInventoryItem;
    private int selectedInventorySlotIndex = -1;
    
    // Debounce to prevent accidental shop selection after inventory selection
    private float lastInventorySelectionTime = 0f;
    private const float SELECTION_DEBOUNCE_TIME = 0.5f; // Increased to 0.5s
    
    // Enum cho nguồn item
    public enum ItemSource
    {
        None,
        Shop,      // Item từ shop
        Inventory  // Item từ inventory/cart
    }

    // Events
    public System.Action<GameObject, ItemSource> OnItemSelected;
    public System.Action OnSelectionCleared;

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
        // Auto-find arrow controller
        if (autoFindArrow && arrowController == null)
        {
            arrowController = FindObjectOfType<ShoppingBagArrowController>();
            
            if (arrowController == null)
            {
                // Try to find ShoppingBag object
                GameObject shoppingBag = GameObject.Find("ShoppingBag");
                if (shoppingBag != null)
                {
                    arrowController = shoppingBag.GetComponent<ShoppingBagArrowController>();
                }
            }
        }
    }

    /// <summary>
    /// Select item from shop
    /// </summary>
    public void SelectShopItem(GameObject itemSlot, DatabaseShopItem shopItem)
    {
        Debug.Log($"🛒 [Selection] SelectShopItem called - Before: source={selectedItemSource}, Time={Time.time:F3}");
        
        // IGNORE shop selection nếu vừa mới select inventory (raycast bug)
        float timeSinceInventorySelection = Time.time - lastInventorySelectionTime;
        Debug.Log($"🛒 [Selection] Debounce check: timeSince={timeSinceInventorySelection:F3}s, lastTime={lastInventorySelectionTime:F3}, threshold={SELECTION_DEBOUNCE_TIME}s");
        
        if (timeSinceInventorySelection < SELECTION_DEBOUNCE_TIME)
        {
            Debug.LogWarning($"⚠️ [Selection] IGNORING shop selection (debounce {timeSinceInventorySelection:F3}s < {SELECTION_DEBOUNCE_TIME}s)");
            return;
        }
        
        selectedItemSlot = itemSlot;
        selectedItemSource = ItemSource.Shop;
        selectedShopItem = shopItem;
        selectedInventoryItem = null;
        selectedInventorySlotIndex = -1;

        Debug.Log($"🛒 [Selection] SelectShopItem - After: source={selectedItemSource}, item={shopItem.item_name}");

        // Show left arrow (<<< - chuyển vào giỏ)
        if (arrowController != null)
        {
            arrowController.ShowLeftArrow();
        }

        Debug.Log($"🛒 [Selection] Selected shop item: {shopItem.item_name}");

        OnItemSelected?.Invoke(itemSlot, ItemSource.Shop);
    }

    /// <summary>
    /// Select item from inventory/cart
    /// </summary>
    public void SelectInventoryItem(GameObject itemSlot)
    {
        selectedItemSlot = itemSlot;
        selectedItemSource = ItemSource.Inventory;
        selectedShopItem = null;
        selectedInventoryItem = null;
        selectedInventorySlotIndex = -1;

        // Show right arrow (>>> - trả về shop)
        if (arrowController != null)
        {
            arrowController.ShowRightArrow();
        }

        Debug.Log($"📦 [Selection] Selected inventory item");

        OnItemSelected?.Invoke(itemSlot, ItemSource.Inventory);
    }

    /// <summary>
    /// Select item from inventory/cart (with ItemSO)
    /// </summary>
    public void SelectInventoryItem(ItemSO item, int slotIndex)
    {
        Debug.Log($"📦 [Selection] SelectInventoryItem called - Before: source={selectedItemSource}, Time={Time.time:F3}");
        
        selectedItemSlot = null;
        selectedItemSource = ItemSource.Inventory;
        selectedShopItem = null;
        selectedInventoryItem = item;
        selectedInventorySlotIndex = slotIndex;
        
        // Record time to prevent accidental shop selection
        lastInventorySelectionTime = Time.time;
        Debug.Log($"📦 [Selection] Set debounce time: lastInventorySelectionTime={lastInventorySelectionTime:F3}");

        Debug.Log($"📦 [Selection] SelectInventoryItem - After: source={selectedItemSource}, item={item.displayName}, slot={slotIndex}");

        // Show right arrow (>>> - trả về shop)
        if (arrowController != null)
        {
            arrowController.ShowRightArrow();
        }

        Debug.Log($"📦 [Selection] Selected inventory item: {item.displayName} (slot {slotIndex})");

        OnItemSelected?.Invoke(null, ItemSource.Inventory);
    }

    /// <summary>
    /// Clear selection
    /// </summary>
    public void ClearSelection()
    {
        selectedItemSlot = null;
        selectedItemSource = ItemSource.None;
        selectedShopItem = null;
        selectedInventoryItem = null;
        selectedInventorySlotIndex = -1;

        // Hide arrow
        if (arrowController != null)
        {
            arrowController.HideAllArrows();
        }

        Debug.Log($"❌ [Selection] Cleared selection");

        OnSelectionCleared?.Invoke();
    }

    /// <summary>
    /// Get current selected item slot
    /// </summary>
    public GameObject GetSelectedItemSlot()
    {
        return selectedItemSlot;
    }

    /// <summary>
    /// Get current selected item source
    /// </summary>
    public ItemSource GetSelectedItemSource()
    {
        return selectedItemSource;
    }

    /// <summary>
    /// Get selected shop item (null if not from shop)
    /// </summary>
    public DatabaseShopItem GetSelectedShopItem()
    {
        return selectedShopItem;
    }

    /// <summary>
    /// Check if item is selected
    /// </summary>
    public bool HasSelection()
    {
        return (selectedItemSlot != null || selectedInventoryItem != null) && selectedItemSource != ItemSource.None;
    }

    /// <summary>
    /// Check if selected from shop
    /// </summary>
    public bool IsShopItemSelected()
    {
        return selectedItemSource == ItemSource.Shop;
    }

    /// <summary>
    /// Check if selected from inventory
    /// </summary>
    public bool IsInventoryItemSelected()
    {
        return selectedItemSource == ItemSource.Inventory;
    }

    /// <summary>
    /// Get selected inventory item (null if not from inventory)
    /// </summary>
    public ItemSO GetSelectedInventoryItem()
    {
        return selectedInventoryItem;
    }

    /// <summary>
    /// Get selected inventory slot index (-1 if not selected)
    /// </summary>
    public int GetSelectedInventorySlotIndex()
    {
        return selectedInventorySlotIndex;
    }
}
