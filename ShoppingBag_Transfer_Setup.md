# Shopping Bag Transfer System Setup Guide

## Tổng quan
Hệ thống chuyển item giữa Shop và Shopping Cart:
- Click item trong shop → Click nút `<<<` → Item vào cart, stock -1
- Click item trong cart → Click nút `>>>` → Item về shop, stock +1

## Files đã tạo
1. `ShoppingCartManager.cs` - Quản lý giỏ hàng tạm
2. `ShoppingBagButtonController.cs` - Xử lý click nút ShoppingBag
3. Update `UIShopController.cs` - Thêm `UpdateItemStock()`

## Setup trong Unity

### 1. Create ShoppingCartManager
```
1. Tạo Empty GameObject tên "ShoppingCartManager" trong scene
2. Add Component → ShoppingCartManager
3. Set Max Cart Size = 20 (hoặc số lượng bạn muốn)
```

### 2. Setup ShoppingBag Button
```
ShoppingBag (GameObject)
├── DirectionL (<<< arrow)
├── Direction (>>> arrow)
└── Button (Image component - clickable area)
```

**Attach Script:**
1. Select ShoppingBag GameObject
2. Add Component → `ShoppingBagButtonController`
3. Fields (auto-find nếu để trống):
   - Selection Manager: Auto tìm Instance
   - Cart Manager: Auto tìm Instance
   - Shop Controller: Auto tìm UIShopController
   - Auto Find: ✓

**Quan trọng:** ShoppingBag phải có `Button` component hoặc `Image` với raycast target enabled!

### 3. Verify Scripts
Đảm bảo các scripts này đã được attach:
- ✓ `ShopInventorySelectionManager` (trong scene)
- ✓ `ShoppingCartManager` (trong scene)
- ✓ `UIShopController` (trên TradeUI)
- ✓ `ShoppingBagArrowController` (trên ShoppingBag)
- ✓ `ShoppingBagButtonController` (trên ShoppingBag)

## Flow hoạt động

### Transfer Shop → Cart (<<<)
```
1. Player click item trong shop
   → ItemTradePanelController.OnPointerClick()
   → SelectionManager.SelectShopItem()
   → Arrow hiện <<<

2. Player click nút ShoppingBag
   → ShoppingBagButtonController.OnButtonClicked()
   → TransferShopToCart()
   
3. TransferShopToCart():
   - Get selected shop item
   - Check stock > 0
   - CartManager.AddItemToCart()
   - shopItem.stock -= 1
   - UIShopController.UpdateItemStock() (update UI)
   - SelectionManager.ClearSelection()

4. Result:
   - Item added to cart (tạm)
   - Stock display giảm 1
   - Arrow ẩn
   - Selection cleared
```

### Transfer Cart → Shop (>>>)
```
1. Player click item trong cart
   → (Inventory script).OnClick()
   → SelectionManager.SelectInventoryItem()
   → Arrow hiện >>>

2. Player click nút ShoppingBag
   → ShoppingBagButtonController.OnButtonClicked()
   → TransferCartToShop()
   
3. TransferCartToShop() (chưa implement):
   - Get selected cart item
   - CartManager.RemoveItemFromCart()
   - shopItem.stock += 1
   - UIShopController.UpdateItemStock()
   - SelectionManager.ClearSelection()
```

## API Reference

### ShoppingCartManager

#### Add to Cart:
```csharp
bool AddItemToCart(DatabaseShopItem shopItem, int quantity = 1)
```

#### Remove from Cart:
```csharp
bool RemoveItemFromCart(int itemId, int quantity = 1)
```

#### Get Cart Info:
```csharp
List<ShoppingCartItem> GetCartItems()
ShoppingCartItem GetCartItem(int itemId)
bool IsItemInCart(int itemId)
int GetItemQuantityInCart(int itemId)
```

#### Clear Cart:
```csharp
void ClearCart() // Gọi khi close shop không mua
```

#### Events:
```csharp
System.Action<ShoppingCartItem> OnItemAddedToCart
System.Action<ShoppingCartItem> OnItemRemovedFromCart
System.Action OnCartCleared
System.Action OnPaymentCompleted
```

### UIShopController

#### Update Stock:
```csharp
// Update stock display cho item cụ thể
void UpdateItemStock(int itemId, int newStock)
```

## Kiểm tra Stock

### Out of Stock Display:
- Stock = 0 → Text màu đỏ "x0"
- Stock > 0 → Text trắng "x5"
- Stock = -1 (unlimited) → "∞"

### Prevent Adding Out of Stock:
```csharp
// Trong TransferShopToCart()
if (shopItem.stock != -1 && shopItem.stock <= 0)
{
    Debug.LogWarning("Out of stock!");
    return;
}
```

## Events Usage

### Subscribe to cart events:
```csharp
void Start()
{
    if (ShoppingCartManager.Instance != null)
    {
        ShoppingCartManager.Instance.OnItemAddedToCart += OnItemAdded;
        ShoppingCartManager.Instance.OnItemRemovedFromCart += OnItemRemoved;
        ShoppingCartManager.Instance.OnCartCleared += OnCartCleared;
    }
}

void OnItemAdded(ShoppingCartItem item)
{
    Debug.Log($"Added: {item.itemName} x{item.quantity}");
    // Update cart UI
}

void OnItemRemoved(ShoppingCartItem item)
{
    Debug.Log($"Removed: {item.itemName}");
    // Update cart UI
}

void OnCartCleared()
{
    Debug.Log("Cart cleared");
    // Clear cart UI
}
```

## Clear Cart khi Close Shop

### Trong UIShopController.OnDisable():
```csharp
void OnDisable()
{
    // Close pick panels
    ItemTradePanelController.CloseAllPickPanels();
    
    // Clear selection
    if (ShopInventorySelectionManager.Instance != null)
    {
        ShopInventorySelectionManager.Instance.ClearSelection();
    }
    
    // XÓA items vừa thêm vào inventory (vì chưa thanh toán)
    if (ShoppingCartManager.Instance != null)
    {
        ShoppingCartManager.Instance.RemoveShoppingItemsFromInventory();
        ShoppingCartManager.Instance.ClearCart();
    }
    
    // Clear slots
    ClearShopSlots();
}
```

### Item Stacking Fix:
Items được stack theo:
1. **Reference equality** (cùng ItemSO instance)
2. **Database ID** (cùng databaseItemId)
3. **Display Name** (cùng tên)

```csharp
// Trong InventorySlotBag.CanStackWith()
public bool CanStackWith(ItemSO other)
{
    if (IsEmpty || other == null || !item.stackable || quantity >= item.maxStack)
        return false;
    
    // So sánh bằng reference trước
    if (item == other)
        return true;
    
    // Nếu cả 2 có databaseItemId > 0, so sánh bằng ID
    if (item.databaseItemId > 0 && other.databaseItemId > 0)
        return item.databaseItemId == other.databaseItemId;
    
    // So sánh bằng displayName
    return item.displayName == other.displayName;
}
```

## Debugging

### Test add to cart:
```csharp
void Update()
{
    if (Input.GetKeyDown(KeyCode.T))
    {
        var cart = ShoppingCartManager.Instance;
        if (cart != null)
        {
            var items = cart.GetCartItems();
            Debug.Log($"Cart has {items.Count} items:");
            foreach (var item in items)
            {
                Debug.Log($"  - {item.itemName} x{item.quantity}");
            }
        }
    }
}
```

### Log stock updates:
- Check console: `📉 Decreased stock of Daisy Flower: 19`
- Check console: `📊 Updated stock for item 2: 19`

## Troubleshooting

### Button không hoạt động
1. Check ShoppingBag có `Button` component không
2. Check Button có `OnClick` listener không (script tự add)
3. Check Canvas có `GraphicRaycaster` không
4. Check scene có `EventSystem` không

### Stock không update
1. Check log: `📊 Updated stock...`
2. Check UIShopController.UpdateItemStock() có được gọi không
3. Check ItemStock TextMeshProUGUI có trong prefab không

### Item không vào cart
1. Check ShoppingCartManager Instance có null không
2. Check log: `✅ Added xxx to cart`
3. Check cart có full không (max 20 items)

### Selection không clear
1. Check ClearSelection() có được gọi sau transfer không
2. Check arrow có ẩn không

### Items không stack
1. Check ItemSO có `databaseItemId` được set không
2. Check console log khi add item: "Stack xxx +N -> slot Y"
3. Verify CanStackWith() logic trong InventoryItem.cs

### Items không bị xóa khi close shop
1. Check ShoppingCartManager.RemoveShoppingItemsFromInventory() có được gọi không
2. Check console log: "🗑️ Removing X types of items from inventory..."
3. Check cartItems có items không

## Next Steps
- [x] Fix item stacking by database ID
- [x] Remove shopping items from inventory on shop close
- [ ] Implement TransferCartToShop() (trả hàng)
- [ ] Create Cart UI để hiển thị items trong giỏ
- [ ] Implement payment system
- [ ] Update database stock sau khi mua
