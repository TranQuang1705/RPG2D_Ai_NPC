using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Quản lý giỏ hàng tạm khi mua sắm
/// Items trong giỏ chưa được thêm vào inventory thật cho đến khi thanh toán
/// </summary>
public class ShoppingCartManager : MonoBehaviour
{
    public static ShoppingCartManager Instance { get; private set; }

    [Header("Cart Settings")]
    [SerializeField] private int maxCartSize = 20;

    // Shopping cart data (items tạm chưa thanh toán)
    private List<ShoppingCartItem> cartItems = new List<ShoppingCartItem>();
    
    // Flag thanh toán
    private bool isPaid = false;

    // Events
    public System.Action<ShoppingCartItem> OnItemAddedToCart;
    public System.Action<ShoppingCartItem> OnItemRemovedFromCart;
    public System.Action OnCartCleared;
    public System.Action OnPaymentCompleted;

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    /// <summary>
    /// Thêm item vào giỏ hàng tạm (chưa thanh toán)
    /// </summary>
    public bool AddItemToCart(DatabaseShopItem shopItem, int quantity = 1)
    {
        if (cartItems.Count >= maxCartSize)
        {
            Debug.LogWarning($"⚠️ [ShoppingCart] Cart is full! Max {maxCartSize} items.");
            return false;
        }

        // Check if item already in cart
        ShoppingCartItem existingItem = cartItems.Find(x => x.itemId == shopItem.item_id);
        
        if (existingItem != null)
        {
            // Increase quantity
            existingItem.quantity += quantity;
            Debug.Log($"📦 [ShoppingCart] Updated {shopItem.item_name} quantity to {existingItem.quantity}");
        }
        else
        {
            // Add new item to cart
            ShoppingCartItem newItem = new ShoppingCartItem
            {
                itemId = shopItem.item_id,
                itemName = shopItem.item_name,
                iconPath = shopItem.icon_path,
                price = shopItem.price,
                coinType = shopItem.coin_type,
                quantity = quantity,
                shopItem = shopItem
            };
            
            cartItems.Add(newItem);
            Debug.Log($"✅ [ShoppingCart] Added {shopItem.item_name} x{quantity} to cart");
        }

        OnItemAddedToCart?.Invoke(cartItems[cartItems.Count - 1]);
        return true;
    }

    /// <summary>
    /// Remove item from cart (trả hàng)
    /// </summary>
    public bool RemoveItemFromCart(int itemId, int quantity = 1)
    {
        ShoppingCartItem cartItem = cartItems.Find(x => x.itemId == itemId);
        
        if (cartItem == null)
        {
            Debug.LogWarning($"⚠️ [ShoppingCart] Item {itemId} not found in cart");
            return false;
        }

        cartItem.quantity -= quantity;
        
        if (cartItem.quantity <= 0)
        {
            cartItems.Remove(cartItem);
            Debug.Log($"🗑️ [ShoppingCart] Removed {cartItem.itemName} from cart");
        }
        else
        {
            Debug.Log($"📦 [ShoppingCart] Decreased {cartItem.itemName} quantity to {cartItem.quantity}");
        }

        OnItemRemovedFromCart?.Invoke(cartItem);
        return true;
    }

    /// <summary>
    /// Clear cart (khi thoát không mua)
    /// </summary>
    public void ClearCart()
    {
        if (cartItems.Count > 0)
        {
            Debug.Log($"🗑️ [ShoppingCart] Clearing cart ({cartItems.Count} items)");
        }
        
        cartItems.Clear();
        isPaid = false;
        OnCartCleared?.Invoke();
    }

    /// <summary>
    /// Mark cart as paid (items are kept in inventory)
    /// </summary>
    public void MarkAsPaid()
    {
        isPaid = true;
        Debug.Log("✅ [ShoppingCart] Cart marked as paid");
        OnPaymentCompleted?.Invoke();
    }

    /// <summary>
    /// Check if cart has been paid
    /// </summary>
    public bool IsPaid()
    {
        return isPaid;
    }

    /// <summary>
    /// Get all items in cart
    /// </summary>
    public List<ShoppingCartItem> GetCartItems()
    {
        return new List<ShoppingCartItem>(cartItems);
    }

    /// <summary>
    /// Get item in cart
    /// </summary>
    public ShoppingCartItem GetCartItem(int itemId)
    {
        return cartItems.Find(x => x.itemId == itemId);
    }

    /// <summary>
    /// Check if item is in cart
    /// </summary>
    public bool IsItemInCart(int itemId)
    {
        return cartItems.Exists(x => x.itemId == itemId);
    }

    /// <summary>
    /// Get item quantity in cart
    /// </summary>
    public int GetItemQuantityInCart(int itemId)
    {
        ShoppingCartItem item = cartItems.Find(x => x.itemId == itemId);
        return item != null ? item.quantity : 0;
    }

    /// <summary>
    /// Calculate total price
    /// </summary>
    public Dictionary<string, int> CalculateTotalPrice()
    {
        Dictionary<string, int> totals = new Dictionary<string, int>();
        
        foreach (var item in cartItems)
        {
            if (!totals.ContainsKey(item.coinType))
            {
                totals[item.coinType] = 0;
            }
            totals[item.coinType] += item.price * item.quantity;
        }
        
        return totals;
    }

    /// <summary>
    /// Remove all items from player inventory that were added from shop
    /// (Call khi close shop mà chưa thanh toán)
    /// </summary>
    public void RemoveShoppingItemsFromInventory()
    {
        if (InventorySystem.Instance == null)
        {
            Debug.LogWarning("⚠️ [ShoppingCart] InventorySystem not found!");
            return;
        }

        if (cartItems.Count == 0)
        {
            Debug.Log("[ShoppingCart] No items to remove from inventory");
            return;
        }

        Debug.Log($"🗑️ [ShoppingCart] Removing {cartItems.Count} types of items from inventory...");

        foreach (var cartItem in cartItems)
        {
            // Find ItemSO by databaseItemId or name
            ItemSO itemToRemove = FindItemSOInInventory(cartItem.itemId, cartItem.itemName);
            
            if (itemToRemove != null)
            {
                int removed = InventorySystem.Instance.Remove(itemToRemove, cartItem.quantity);
                Debug.Log($"  🗑️ Removed {removed}x {cartItem.itemName} from inventory");
            }
            else
            {
                Debug.LogWarning($"  ⚠️ Could not find ItemSO for {cartItem.itemName} to remove");
            }
        }

        Debug.Log("✅ [ShoppingCart] Shopping items removed from inventory");
    }

    /// <summary>
    /// Find ItemSO in inventory by database ID or name
    /// </summary>
    private ItemSO FindItemSOInInventory(int databaseItemId, string itemName)
    {
        if (InventorySystem.Instance == null) return null;

        var slots = InventorySystem.Instance.GetInternalSlots();
        
        foreach (var slot in slots)
        {
            if (slot.IsEmpty || slot.item == null) continue;

            // Match by database ID first
            if (slot.item.databaseItemId > 0 && slot.item.databaseItemId == databaseItemId)
            {
                return slot.item;
            }

            // Match by name as fallback
            if (slot.item.displayName == itemName)
            {
                return slot.item;
            }
        }

        return null;
    }
}

/// <summary>
/// Data class for shopping cart item
/// </summary>
[System.Serializable]
public class ShoppingCartItem
{
    public int itemId;
    public string itemName;
    public string iconPath;
    public int price;
    public string coinType;
    public int quantity;
    public DatabaseShopItem shopItem;
}
