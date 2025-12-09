using UnityEngine;
using Inventory.UI;

/// <summary>
/// Handle inventory item clicks when shopping
/// Attach to Bag UI để intercept clicks khi shop đang mở
/// </summary>
public class ShopInventoryClickHandler : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private UIInventoryPanel inventoryPanel;
    [SerializeField] private GameObject tradeUI; // Để check xem shop có đang mở không
    
    [Header("Auto Find")]
    [SerializeField] private bool autoFind = true;

    private ShopInventorySelectionManager selectionManager;
    
    void Start()
    {
        if (autoFind)
        {
            if (inventoryPanel == null)
            {
                inventoryPanel = FindObjectOfType<UIInventoryPanel>();
            }
            
            if (tradeUI == null)
            {
                tradeUI = GameObject.FindWithTag("TradeUI");
                if (tradeUI == null)
                {
                    tradeUI = GameObject.Find("TradeUI");
                }
            }
        }
        
        selectionManager = ShopInventorySelectionManager.Instance;
        
        if (inventoryPanel != null)
        {
            Debug.Log("✅ [ShopInventoryClick] Connected to UIInventoryPanel");
        }
    }
    
    void Update()
    {
        // Check nếu shop đang mở và có click vào inventory
        if (!IsShopOpen()) return;
        if (selectionManager == null) return;
        
        // Lắng nghe click events từ inventory UI
        // Note: UIInventoryPanel đã handle clicks internally
    }
    
    /// <summary>
    /// Call từ UIInventoryPanel khi click item
    /// </summary>
    public void OnInventoryItemClicked(int slotIndex, InventorySlotBag slot)
    {
        if (!IsShopOpen()) return;
        if (slot == null || slot.IsEmpty) return;
        if (selectionManager == null) return;
        
        // Check nếu item này có trong cart (từ shop)
        if (ShoppingCartManager.Instance != null && slot.item.databaseItemId > 0)
        {
            if (ShoppingCartManager.Instance.IsItemInCart(slot.item.databaseItemId))
            {
                // Item này từ shop → Cho phép trả về
                selectionManager.SelectInventoryItem(slot.item, slotIndex);
                Debug.Log($"🔄 [ShopInventoryClick] Selected inventory item: {slot.item.displayName} (can return to shop)");
                return;
            }
        }
        
        Debug.Log($"⚠️ [ShopInventoryClick] Item {slot.item.displayName} not from shop, cannot return");
    }
    
    private bool IsShopOpen()
    {
        return tradeUI != null && tradeUI.activeInHierarchy;
    }
}
