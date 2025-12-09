using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.EventSystems;

/// <summary>
/// Controller cho ItemTradePanel - xử lý click và hiển thị Pick panel
/// Attach vào từng ItemTradePanel prefab
/// </summary>
public class ItemTradePanelController : MonoBehaviour, IPointerClickHandler
{
    [Header("References")]
    [SerializeField] private GameObject pickPanel; // Pick panel để hiển thị
    [SerializeField] private Image borderImage; // Border/highlight image
    [SerializeField] private bool autoFindPickPanel = true;
    [SerializeField] private bool autoFindBorder = true;

    [Header("Item Data")]
    private DatabaseShopItem itemData;
    private bool isInitialized = false;

    // Static reference to track currently opened panel
    private static ItemTradePanelController currentOpenPanel = null;

    void Start()
    {
        // Auto-find Pick panel
        if (autoFindPickPanel && pickPanel == null)
        {
            // Tìm Pick panel trong TradeUI parent
            Transform tradeUI = transform;
            while (tradeUI != null && tradeUI.name != "TradeUI")
            {
                tradeUI = tradeUI.parent;
            }

            if (tradeUI != null)
            {
                Transform pick = tradeUI.Find("Pick");
                if (pick != null)
                {
                    pickPanel = pick.gameObject;
                }
            }
        }

        // Auto-find border image
        if (autoFindBorder && borderImage == null)
        {
            // Try to find "Border", "Highlight", "Selected" child
            Transform border = transform.Find("Border");
            if (border == null) border = transform.Find("Highlight");
            if (border == null) border = transform.Find("Selected");
            if (border == null) border = transform.Find("Hover");
            
            if (border != null)
            {
                borderImage = border.GetComponent<Image>();
            }
        }

        // Add Button component if not exists
        Button button = GetComponent<Button>();
        if (button == null)
        {
            button = gameObject.AddComponent<Button>();
        }

        // Initialize border state
        Deselect();
    }

    /// <summary>
    /// Initialize panel với item data
    /// </summary>
    public void Initialize(DatabaseShopItem item)
    {
        itemData = item;
        isInitialized = true;
    }

    /// <summary>
    /// Handle click event
    /// </summary>
    public void OnPointerClick(PointerEventData eventData)
    {
        if (!isInitialized || itemData == null) return;

        // Toggle if clicking same panel
        if (currentOpenPanel == this)
        {
            HidePickPanel();
            currentOpenPanel = null;
            Deselect();
            
            // Clear selection
            if (ShopInventorySelectionManager.Instance != null)
            {
                ShopInventorySelectionManager.Instance.ClearSelection();
            }
        }
        else
        {
            // Close previous panel if exists
            if (currentOpenPanel != null)
            {
                currentOpenPanel.HidePickPanel();
                currentOpenPanel.Deselect();
            }

            // Show pick panel for this item
            ShowPickPanel();
            currentOpenPanel = this;
            Select();
            
            // Unborder all inventory items (khi chọn shop item)
            DeselectAllInventoryItems();
            
            // Notify selection manager (item from shop)
            if (ShopInventorySelectionManager.Instance != null)
            {
                ShopInventorySelectionManager.Instance.SelectShopItem(gameObject, itemData);
            }
        }
    }

    /// <summary>
    /// Show Pick panel với thông tin item
    /// </summary>
    private void ShowPickPanel()
    {
        if (pickPanel == null) return;

        // Position Pick panel
        PositionPickPanel();

        // Populate Pick panel với item data
        PopulatePickPanel();

        // Show panel
        pickPanel.SetActive(true);
    }

    /// <summary>
    /// Hide Pick panel
    /// </summary>
    private void HidePickPanel()
    {
        if (pickPanel != null)
        {
            pickPanel.SetActive(false);
        }
    }

    /// <summary>
    /// Position Pick panel bên cạnh item slot
    /// </summary>
    private void PositionPickPanel()
    {
        if (pickPanel == null) return;

        RectTransform slotRect = GetComponent<RectTransform>();
        RectTransform pickRect = pickPanel.GetComponent<RectTransform>();

        if (slotRect != null && pickRect != null)
        {
            // KHÔNG thay đổi position của Pick panel
            // Pick panel nên có position cố định trong UI
            // Chỉ cần bật/tắt nó là đủ
            
            // Nếu muốn position động, dùng anchored position thay vì world position
            // pickRect.anchoredPosition = new Vector2(x, y);
        }
    }

    /// <summary>
    /// Populate Pick panel với item data
    /// </summary>
    private void PopulatePickPanel()
    {
        if (pickPanel == null || itemData == null) return;

        // Find and update UI elements in Pick panel
        UpdateTextField("ItemName", itemData.item_name);
        UpdateTextField("ItemsName", itemData.item_name); // Alternative name
        UpdateTextField("Description", itemData.description);
        UpdateTextField("Price", $"{itemData.price}");
        UpdateTextField("PriceText", $"{itemData.price}");
        
        // Stock
        string stockText = itemData.stock == -1 ? "∞" : itemData.stock.ToString();
        UpdateTextField("Stock", stockText);
        UpdateTextField("ItemStock", $"x{stockText}");

        // Rarity
        UpdateTextField("Rarity", itemData.rarity);

        // Coin type
        UpdateTextField("CoinType", itemData.coin_type);

        // Update item icon in Pick panel
        UpdateItemIcon("ItemIcon", itemData.icon_path);
        UpdateItemIcon("ItemsIcon", itemData.icon_path);

        // Update coin icon
        UpdateCoinIcon("CoinIcon", itemData.coin_type);
        UpdateCoinIcon("PriceIcon", itemData.coin_type);
    }

    /// <summary>
    /// Update text field in Pick panel
    /// </summary>
    private void UpdateTextField(string fieldName, string value)
    {
        if (string.IsNullOrEmpty(value)) return;

        Transform field = FindChildRecursive(pickPanel.transform, fieldName);
        if (field != null)
        {
            TextMeshProUGUI text = field.GetComponent<TextMeshProUGUI>();
            if (text != null)
            {
                text.text = value;
            }
            else
            {
                // Try UnityEngine.UI.Text
                Text uiText = field.GetComponent<Text>();
                if (uiText != null)
                {
                    uiText.text = value;
                }
            }
        }
    }

    /// <summary>
    /// Update item icon in Pick panel
    /// </summary>
    private void UpdateItemIcon(string iconName, string iconPath)
    {
        if (string.IsNullOrEmpty(iconPath)) return;

        Transform iconTransform = FindChildRecursive(pickPanel.transform, iconName);
        if (iconTransform != null)
        {
            Image iconImage = iconTransform.GetComponent<Image>();
            if (iconImage != null)
            {
                string path = iconPath.Replace(".png", "").Replace(".jpg", "");
                Sprite icon = Resources.Load<Sprite>(path);
                if (icon != null)
                {
                    iconImage.sprite = icon;
                }
            }
        }
    }

    /// <summary>
    /// Update coin icon in Pick panel
    /// </summary>
    private void UpdateCoinIcon(string iconName, string coinType)
    {
        if (string.IsNullOrEmpty(coinType)) return;

        Transform iconTransform = FindChildRecursive(pickPanel.transform, iconName);
        if (iconTransform != null)
        {
            Image iconImage = iconTransform.GetComponent<Image>();
            if (iconImage != null)
            {
                string path = $"Icons/Coins/{coinType.ToLower()}";
                Sprite coinIcon = Resources.Load<Sprite>(path);
                if (coinIcon != null)
                {
                    iconImage.sprite = coinIcon;
                }
            }
        }
    }

    /// <summary>
    /// Find child recursively
    /// </summary>
    private Transform FindChildRecursive(Transform parent, string childName)
    {
        Transform found = parent.Find(childName);
        if (found != null) return found;

        foreach (Transform child in parent)
        {
            found = FindChildRecursive(child, childName);
            if (found != null) return found;
        }

        return null;
    }

    /// <summary>
    /// Get item data
    /// </summary>
    public DatabaseShopItem GetItemData()
    {
        return itemData;
    }

    /// <summary>
    /// Select this shop item (show border)
    /// </summary>
    public void Select()
    {
        if (borderImage != null)
        {
            borderImage.enabled = true;
        }
    }

    /// <summary>
    /// Deselect this shop item (hide border)
    /// </summary>
    public void Deselect()
    {
        if (borderImage != null)
        {
            borderImage.enabled = false;
        }
    }

    /// <summary>
    /// Deselect all shop items
    /// </summary>
    public static void DeselectAllShopItems()
    {
        ItemTradePanelController[] allPanels = FindObjectsOfType<ItemTradePanelController>();
        foreach (var panel in allPanels)
        {
            panel.Deselect();
        }
    }

    /// <summary>
    /// Deselect all inventory items
    /// </summary>
    private static void DeselectAllInventoryItems()
    {
        // Find UIInventoryPanel
        var inventoryPanel = FindObjectOfType<Inventory.UI.UIInventoryPanel>();
        if (inventoryPanel != null)
        {
            inventoryPanel.DeselectAll();
            Debug.Log("🔄 [ItemTradePanel] Deselected all inventory items");
        }
    }

    /// <summary>
    /// Close all open pick panels (call when UI closes)
    /// </summary>
    public static void CloseAllPickPanels()
    {
        if (currentOpenPanel != null)
        {
            currentOpenPanel.HidePickPanel();
            currentOpenPanel = null;
        }
    }

    void OnDisable()
    {
        // Hide pick panel if this slot is disabled
        if (currentOpenPanel == this)
        {
            HidePickPanel();
            currentOpenPanel = null;
        }
    }
}
