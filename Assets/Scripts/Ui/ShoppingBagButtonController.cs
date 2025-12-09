using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Controller cho nút ShoppingBag (<<<  hoặc >>>)
/// Xử lý chuyển item giữa shop và inventory
/// </summary>
[RequireComponent(typeof(Button))]
public class ShoppingBagButtonController : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private ShopInventorySelectionManager selectionManager;
    [SerializeField] private ShoppingCartManager cartManager;
    [SerializeField] private UIShopController shopController;

    [Header("Auto Find")]
    [SerializeField] private bool autoFind = true;

    [Header("Visual Feedback")]
    [SerializeField] private float clickScaleDuration = 0.1f;
    [SerializeField] private float clickScaleAmount = 0.9f;

    [Header("Double Click Settings")]
    [SerializeField] private float doubleClickThreshold = 0.3f;

    [Header("Visual States - Bag Items")]
    [SerializeField] private GameObject backItem;      // State 1: 1 item
    [SerializeField] private GameObject itemInBag2;    // State 2: 2 items
    [SerializeField] private GameObject itemInBag3;    // State 3: 3+ items

    [Header("Audio")]
    [SerializeField] private AudioClip itemAddSound;   // Sound when adding item to bag
    [SerializeField] private AudioClip itemRemoveSound; // Sound when removing item from bag
    [SerializeField] private AudioSource audioSource;   // Optional: assign or will create

    private Button button;
    private bool isProcessing = false;
    
    // Double click detection
    private float lastClickTime = 0f;
    private int clickCount = 0;
    private Coroutine clickCoroutine;
    
    // Track items in bag for visual state
    private int itemsInBag = 0;

    void Start()
    {
        button = GetComponent<Button>();
        button.onClick.AddListener(OnButtonClicked);

        // Auto-find references
        if (autoFind)
        {
            if (selectionManager == null)
            {
                selectionManager = ShopInventorySelectionManager.Instance;
            }

            if (cartManager == null)
            {
                cartManager = ShoppingCartManager.Instance;
            }

            if (shopController == null)
            {
                shopController = FindObjectOfType<UIShopController>();
            }
        }
        
        // Initialize visual state (all hidden - empty bag)
        UpdateBagVisualState(0);
        
        // Setup audio source
        if (audioSource == null)
        {
            audioSource = GetComponent<AudioSource>();
            if (audioSource == null)
            {
                audioSource = gameObject.AddComponent<AudioSource>();
                audioSource.playOnAwake = false;
            }
        }
    }

    /// <summary>
    /// Handle button click with double click detection
    /// </summary>
    private void OnButtonClicked()
    {
        Debug.Log("🔘 [ShoppingBag] Button clicked!");
        
        // Prevent spam clicking
        if (isProcessing)
        {
            Debug.Log("⚠️ [ShoppingBag] Processing, ignoring click");
            return;
        }

        if (selectionManager == null)
        {
            Debug.LogError("❌ [ShoppingBag] SelectionManager not found!");
            return;
        }

        // Check if item is selected
        if (!selectionManager.HasSelection())
        {
            Debug.LogWarning("⚠️ [ShoppingBag] No item selected!");
            return;
        }

        // Double click detection
        clickCount++;
        
        if (clickCoroutine != null)
        {
            StopCoroutine(clickCoroutine);
        }
        
        clickCoroutine = StartCoroutine(HandleClickCoroutine());
    }
    
    /// <summary>
    /// Coroutine to handle single vs double click
    /// </summary>
    private System.Collections.IEnumerator HandleClickCoroutine()
    {
        yield return new WaitForSeconds(doubleClickThreshold);
        
        bool isDoubleClick = clickCount >= 2;
        int transferQuantity = isDoubleClick ? -1 : 1; // -1 = transfer all
        
        Debug.Log($"🔍 [ShoppingBag] {(isDoubleClick ? "DOUBLE" : "Single")} click detected (count={clickCount})");
        
        // Update visual state based on double click
        if (isDoubleClick)
        {
            // Double click: show all 3 states instantly
            UpdateBagVisualState(3);
        }
        
        // Reset click count
        clickCount = 0;
        
        // Debug selection state
        Debug.Log($"🔍 [ShoppingBag] Selection state: IsShop={selectionManager.IsShopItemSelected()}, IsInventory={selectionManager.IsInventoryItemSelected()}");

        // Visual feedback
        StartCoroutine(ButtonClickAnimation());

        // Check source
        if (selectionManager.IsShopItemSelected())
        {
            // Chuyển từ shop → cart (<<<)
            Debug.Log($"◀️ [ShoppingBag] Calling TransferShopToCart (quantity={transferQuantity})");
            TransferShopToCart(transferQuantity);
        }
        else if (selectionManager.IsInventoryItemSelected())
        {
            // Trả từ cart → shop (>>>)
            Debug.Log($"▶️ [ShoppingBag] Calling TransferCartToShop (quantity={transferQuantity})");
            TransferCartToShop(transferQuantity);
        }
        else
        {
            Debug.LogWarning("⚠️ [ShoppingBag] No valid selection source!");
        }
    }

    /// <summary>
    /// Animation khi click button
    /// </summary>
    private System.Collections.IEnumerator ButtonClickAnimation()
    {
        isProcessing = true;

        // Scale down
        Vector3 originalScale = transform.localScale;
        Vector3 targetScale = originalScale * clickScaleAmount;
        
        float elapsed = 0f;
        while (elapsed < clickScaleDuration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / clickScaleDuration;
            transform.localScale = Vector3.Lerp(originalScale, targetScale, t);
            yield return null;
        }

        // Scale back up
        elapsed = 0f;
        while (elapsed < clickScaleDuration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / clickScaleDuration;
            transform.localScale = Vector3.Lerp(targetScale, originalScale, t);
            yield return null;
        }

        transform.localScale = originalScale;
        isProcessing = false;
    }

    /// <summary>
    /// Chuyển item từ shop vào cart (<<<)
    /// </summary>
    /// <param name="quantity">Số lượng cần chuyển. -1 = chuyển hết stock</param>
    private void TransferShopToCart(int quantity = 1)
    {
        DatabaseShopItem shopItem = selectionManager.GetSelectedShopItem();
        
        if (shopItem == null)
        {
            Debug.LogError("❌ [ShoppingBag] Shop item not found!");
            return;
        }

        // Check stock
        if (shopItem.stock != -1 && shopItem.stock <= 0)
        {
            Debug.LogWarning($"⚠️ [ShoppingBag] {shopItem.item_name} is out of stock!");
            
            // Clear selection vì hết hàng
            selectionManager.ClearSelection();
            return;
        }

        // Determine actual quantity to transfer
        int actualQuantity = quantity;
        if (quantity == -1) // Transfer all
        {
            actualQuantity = shopItem.stock == -1 ? 999 : shopItem.stock; // Unlimited stock = 999
        }
        else
        {
            // Limit by stock
            if (shopItem.stock != -1)
            {
                actualQuantity = Mathf.Min(quantity, shopItem.stock);
            }
        }

        Debug.Log($"📦 [ShoppingBag] Transferring {actualQuantity} x {shopItem.item_name} from shop to inventory");

        // Thêm TRỰC TIẾP vào inventory người chơi
        if (InventorySystem.Instance != null)
        {
            // Load ItemSO từ Resources
            ItemSO itemSO = LoadItemSO(shopItem);
            
            if (itemSO != null)
            {
                // Add item vào inventory
                int leftover = InventorySystem.Instance.AddItem(itemSO, actualQuantity);
                int actuallyAdded = actualQuantity - leftover;
                
                if (actuallyAdded > 0)
                {
                    // Thành công - Giảm stock trong shop
                    if (shopItem.stock != -1)
                    {
                        shopItem.stock -= actuallyAdded;
                        Debug.Log($"📉 [ShoppingBag] Decreased stock of {shopItem.item_name}: {shopItem.stock}");
                    }

                    // Thêm vào cart để tracking (tạm)
                    if (cartManager != null)
                    {
                        cartManager.AddItemToCart(shopItem, actuallyAdded);
                    }

                    // Refresh shop UI
                    if (shopController != null)
                    {
                        RefreshShopDisplay();
                    }

                    Debug.Log($"✅ [ShoppingBag] Added {actuallyAdded} x {shopItem.item_name} to player inventory");
                    
                    // Update visual state (increment)
                    itemsInBag += actuallyAdded;
                    UpdateBagVisualState(itemsInBag);
                    
                    // Play add sound
                    PlaySound(itemAddSound);
                    
                    if (leftover > 0)
                    {
                        Debug.LogWarning($"⚠️ [ShoppingBag] Inventory full! {leftover} items could not be added");
                    }
                }
                else
                {
                    Debug.LogWarning($"⚠️ [ShoppingBag] Inventory full! Cannot add {shopItem.item_name}");
                }
            }
            else
            {
                Debug.LogError($"❌ [ShoppingBag] Could not load ItemSO for {shopItem.item_name}");
            }
        }
        else
        {
            Debug.LogError("❌ [ShoppingBag] InventorySystem not found!");
        }
    }

    /// <summary>
    /// Trả item từ cart về shop (>>>)
    /// </summary>
    /// <param name="quantity">Số lượng cần trả. -1 = trả hết trong inventory</param>
    private void TransferCartToShop(int quantity = 1)
    {
        ItemSO inventoryItem = selectionManager.GetSelectedInventoryItem();
        int slotIndex = selectionManager.GetSelectedInventorySlotIndex();
        
        if (inventoryItem == null || slotIndex < 0)
        {
            Debug.LogError("❌ [ShoppingBag] Inventory item not found!");
            return;
        }

        // Check nếu item có databaseItemId (từ shop)
        if (inventoryItem.databaseItemId <= 0)
        {
            Debug.LogWarning($"⚠️ [ShoppingBag] {inventoryItem.displayName} không phải từ shop!");
            return;
        }

        // Check nếu item có trong cart
        if (cartManager == null || !cartManager.IsItemInCart(inventoryItem.databaseItemId))
        {
            Debug.LogWarning($"⚠️ [ShoppingBag] {inventoryItem.displayName} không có trong cart!");
            return;
        }

        // Determine actual quantity to transfer
        int actualQuantity = quantity;
        if (quantity == -1) // Transfer all from inventory
        {
            // Get quantity in inventory slot
            if (InventorySystem.Instance != null && slotIndex < InventorySystem.Instance.Slots.Count)
            {
                var slot = InventorySystem.Instance.Slots[slotIndex];
                actualQuantity = slot.IsEmpty ? 0 : slot.quantity;
            }
            else
            {
                actualQuantity = 1;
            }
        }

        if (actualQuantity <= 0)
        {
            Debug.LogWarning($"⚠️ [ShoppingBag] No items to transfer!");
            return;
        }

        Debug.Log($"📦 [ShoppingBag] Transferring {actualQuantity} x {inventoryItem.displayName} from inventory to shop");

        // XÓA items khỏi inventory
        if (InventorySystem.Instance != null)
        {
            int removed = InventorySystem.Instance.Remove(inventoryItem, actualQuantity);
            
            if (removed > 0)
            {
                // Tìm item trong shop để tăng stock
                DatabaseShopItem shopItem = FindShopItemById(inventoryItem.databaseItemId);
                
                if (shopItem != null)
                {
                    // Tăng stock trong shop (nếu không phải unlimited)
                    if (shopItem.stock != -1)
                    {
                        shopItem.stock += removed;
                        Debug.Log($"📈 [ShoppingBag] Increased stock of {shopItem.item_name}: {shopItem.stock}");
                    }

                    // Remove from cart
                    if (cartManager != null)
                    {
                        cartManager.RemoveItemFromCart(inventoryItem.databaseItemId, removed);
                    }

                    // Refresh shop UI
                    if (shopController != null)
                    {
                        shopController.UpdateItemStock(shopItem.item_id, shopItem.stock);
                    }

                    Debug.Log($"✅ [ShoppingBag] Returned {removed} x {inventoryItem.displayName} to shop");
                    
                    // Update visual state (decrement)
                    itemsInBag -= removed;
                    if (itemsInBag < 0) itemsInBag = 0;
                    UpdateBagVisualState(itemsInBag);
                    
                    // Play remove sound
                    PlaySound(itemRemoveSound);
                }
                else
                {
                    Debug.LogError($"❌ [ShoppingBag] Could not find shop item for {inventoryItem.displayName}");
                }
            }
            else
            {
                Debug.LogWarning($"⚠️ [ShoppingBag] Failed to remove {inventoryItem.displayName} from inventory");
            }
        }
        else
        {
            Debug.LogError("❌ [ShoppingBag] InventorySystem not found!");
        }
    }

    /// <summary>
    /// Tìm shop item by database ID
    /// </summary>
    private DatabaseShopItem FindShopItemById(int itemId)
    {
        if (shopController == null) return null;

        // Get shop items from UIShopController
        var shopLoader = DatabaseShopLoader.Instance;
        if (shopLoader == null) return null;

        // Tìm trong danh sách items của shop hiện tại
        var shopItems = shopLoader.GetShopInventory(shopController.GetCurrentNPCId());
        
        foreach (var item in shopItems)
        {
            if (item.item_id == itemId)
            {
                return item;
            }
        }

        return null;
    }

    /// <summary>
    /// Load ItemSO từ Resources dựa vào shop item
    /// </summary>
    private ItemSO LoadItemSO(DatabaseShopItem shopItem)
    {
        if (shopItem == null) return null;

        // Try to load from model_path
        if (!string.IsNullOrEmpty(shopItem.model_path))
        {
            // Remove extension and "Prefabs/" prefix if exists
            string path = shopItem.model_path
                .Replace(".prefab", "")
                .Replace("Prefabs/", "")
                .Replace(".asset", "");
            
            // Try loading as ScriptableObject from Resources
            ItemSO itemSO = Resources.Load<ItemSO>(path);
            
            if (itemSO != null)
            {
                Debug.Log($"📦 [ShoppingBag] Loaded ItemSO from: {path}");
                return itemSO;
            }
        }

        // Try to load by item name
        string itemName = shopItem.item_name.Replace(" ", "_").ToLower();
        ItemSO item = Resources.Load<ItemSO>($"Items/{itemName}");
        
        if (item != null)
        {
            Debug.Log($"📦 [ShoppingBag] Loaded ItemSO by name: Items/{itemName}");
            return item;
        }

        // Try without folder
        item = Resources.Load<ItemSO>(itemName);
        if (item != null)
        {
            Debug.Log($"📦 [ShoppingBag] Loaded ItemSO: {itemName}");
            return item;
        }

        // Create ItemSO runtime nếu không tìm thấy
        Debug.LogWarning($"⚠️ [ShoppingBag] ItemSO not found, creating runtime ItemSO for {shopItem.item_name}");
        return CreateRuntimeItemSO(shopItem);
    }

    /// <summary>
    /// Tạo ItemSO runtime từ DatabaseShopItem
    /// </summary>
    private ItemSO CreateRuntimeItemSO(DatabaseShopItem shopItem)
    {
        ItemSO itemSO = ScriptableObject.CreateInstance<ItemSO>();
        
        // Basic info
        itemSO.displayName = shopItem.item_name;
        itemSO.description = shopItem.description;
        itemSO.itemType = shopItem.item_type;
        itemSO.rarity = shopItem.rarity;
        
        // Stats
        itemSO.value = shopItem.price;
        itemSO.stackable = true;
        itemSO.maxStack = 99;
        
        // Database ref
        itemSO.databaseItemId = shopItem.item_id;
        
        // Load icon
        if (!string.IsNullOrEmpty(shopItem.icon_path))
        {
            string iconPath = shopItem.icon_path.Replace(".png", "").Replace(".jpg", "");
            Sprite icon = Resources.Load<Sprite>(iconPath);
            if (icon != null)
            {
                itemSO.icon = icon;
                itemSO.RuntimeIcon = icon;
            }
        }
        
        // Load prefab
        if (!string.IsNullOrEmpty(shopItem.model_path))
        {
            string prefabPath = shopItem.model_path.Replace(".prefab", "");
            GameObject prefab = Resources.Load<GameObject>(prefabPath);
            if (prefab != null)
            {
                itemSO.prefab = prefab;
                itemSO.RuntimePrefab = prefab;
            }
        }
        
        Debug.Log($"🔧 [ShoppingBag] Created runtime ItemSO for {shopItem.item_name}");
        return itemSO;
    }

    /// <summary>
    /// Refresh shop display để update stock
    /// </summary>
    private void RefreshShopDisplay()
    {
        if (shopController == null) return;

        DatabaseShopItem shopItem = selectionManager.GetSelectedShopItem();
        if (shopItem != null)
        {
            // Update stock display for this item
            shopController.UpdateItemStock(shopItem.item_id, shopItem.stock);
        }
    }

    /// <summary>
    /// Update visual state of bag based on item count
    /// </summary>
    /// <param name="count">Number of items in bag</param>
    private void UpdateBagVisualState(int count)
    {
        Debug.Log($"🎒 [ShoppingBag] Updating visual state for {count} items");

        // State 0: All hidden (empty bag)
        // State 1: BackItem visible (1 item)
        // State 2: BackItem + ItemInBag2 visible (2 items)
        // State 3+: BackItem + ItemInBag2 + ItemInBag3 visible (3+ items)

        if (backItem != null)
        {
            backItem.SetActive(count >= 1);
        }

        if (itemInBag2 != null)
        {
            itemInBag2.SetActive(count >= 2);
        }

        if (itemInBag3 != null)
        {
            itemInBag3.SetActive(count >= 3);
        }

        Debug.Log($"  State: BackItem={count >= 1}, ItemInBag2={count >= 2}, ItemInBag3={count >= 3}");
    }

    /// <summary>
    /// Play audio clip
    /// </summary>
    private void PlaySound(AudioClip clip)
    {
        if (clip == null)
        {
            Debug.LogWarning("⚠️ [ShoppingBag] Audio clip is null!");
            return;
        }

        if (audioSource != null)
        {
            audioSource.PlayOneShot(clip);
            Debug.Log($"🔊 [ShoppingBag] Playing sound: {clip.name}");
        }
        else
        {
            // Fallback: play at position
            AudioSource.PlayClipAtPoint(clip, Camera.main.transform.position);
            Debug.Log($"🔊 [ShoppingBag] Playing sound at camera position: {clip.name}");
        }
    }

    void OnDestroy()
    {
        if (button != null)
        {
            button.onClick.RemoveListener(OnButtonClicked);
        }
    }
}
