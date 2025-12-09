# Shopping Bag Return System - Trả hàng về Shop

## Tổng quan
Cho phép người chơi trả items đã lấy từ shop về lại shop:
- Click item trong Inventory (Bag) → Arrow xoay >>> 
- Click ShoppingBag button → Item về shop, stock +1, inventory -1

## Flow hoạt động

### 1. Click item trong Inventory
```
Player click item trong Bag UI
  ↓
UIInventoryPanel.HandleItemClicked()
  ↓
Check: IsShopOpen() && IsItemInCart()
  ↓
ShopInventorySelectionManager.SelectInventoryItem(item, slotIndex)
  ↓
ShoppingBagArrowController.ShowRightArrow() (>>>)
```

### 2. Click ShoppingBag button (>>>)
```
Player click ShoppingBag button
  ↓
ShoppingBagButtonController.OnButtonClicked()
  ↓
Check: IsInventoryItemSelected()
  ↓
TransferCartToShop()
  ↓
1. Remove 1 item from inventory
2. Increase shop stock +1
3. Remove from cart
4. Update shop UI
5. KHÔNG clear selection (để bấm tiếp)
```

## Files modified

### 1. ShopInventorySelectionManager.cs
**Thêm:**
- `selectedInventoryItem` - Track selected ItemSO
- `selectedInventorySlotIndex` - Track slot index
- `SelectInventoryItem(ItemSO, int)` - Overload method
- `GetSelectedInventoryItem()` - Getter
- `GetSelectedInventorySlotIndex()` - Getter

### 2. UIInventoryController.cs (UIInventoryPanel)
**Thêm trong HandleItemClicked():**
```csharp
// Check nếu đang trong shop
if (ShopInventorySelectionManager.Instance != null && IsShopOpen())
{
    // Chỉ cho phép chọn items có trong cart
    if (ShoppingCartManager.Instance != null && slot.item.databaseItemId > 0)
    {
        if (ShoppingCartManager.Instance.IsItemInCart(slot.item.databaseItemId))
        {
            ShopInventorySelectionManager.Instance.SelectInventoryItem(slot.item, clicked.Index);
            Debug.Log($"🔄 [Inventory] Selected for return: {slot.item.displayName}");
        }
    }
}
```

**Thêm method:**
```csharp
private bool IsShopOpen()
{
    GameObject tradeUI = GameObject.FindWithTag("TradeUI");
    if (tradeUI == null) tradeUI = GameObject.Find("TradeUI");
    return tradeUI != null && tradeUI.activeInHierarchy;
}
```

### 3. ShoppingBagButtonController.cs
**Implement TransferCartToShop():**
```csharp
private void TransferCartToShop()
{
    // 1. Get selected inventory item
    ItemSO inventoryItem = selectionManager.GetSelectedInventoryItem();
    int slotIndex = selectionManager.GetSelectedInventorySlotIndex();
    
    // 2. Validate
    if (inventoryItem.databaseItemId <= 0) return; // Không từ shop
    if (!cartManager.IsItemInCart(itemId)) return; // Không trong cart
    
    // 3. Remove from inventory
    int removed = InventorySystem.Instance.Remove(inventoryItem, 1);
    
    // 4. Increase shop stock
    DatabaseShopItem shopItem = FindShopItemById(inventoryItem.databaseItemId);
    shopItem.stock += 1;
    
    // 5. Remove from cart
    cartManager.RemoveItemFromCart(itemId, 1);
    
    // 6. Update shop UI
    shopController.UpdateItemStock(itemId, shopItem.stock);
}
```

**Thêm helper method:**
```csharp
private DatabaseShopItem FindShopItemById(int itemId)
{
    var shopLoader = DatabaseShopLoader.Instance;
    var shopItems = shopLoader.GetShopInventory(shopController.GetCurrentNPCId());
    return shopItems.Find(x => x.item_id == itemId);
}
```

### 4. UIShopController.cs
**Thêm getter:**
```csharp
public int GetCurrentNPCId()
{
    return currentNPCId;
}
```

## Conditions để trả hàng

### Item phải thỏa mãn:
1. ✅ **Có trong Cart** - `IsItemInCart(databaseItemId)`
2. ✅ **Có databaseItemId > 0** - Từ shop, không phải item khác
3. ✅ **Shop đang mở** - TradeUI active

### Items KHÔNG thể trả:
- ❌ Items có sẵn từ trước (không mua từ shop)
- ❌ Items không có databaseItemId
- ❌ Items không có trong cart

## UI Behavior

### Arrow direction:
```
Shop item selected    → <<< (left arrow)
Inventory item selected → >>> (right arrow)
No selection          → (arrows hidden)
```

### Border highlighting:
```
Shop item selected    → Shop item có border
                       → Inventory item KHÔNG border
                       
Inventory item selected → Inventory item có border (default UIInventoryPanel)
                        → Shop item KHÔNG border (TODO: implement unborder)
```

## Testing

### Test case 1: Mua rồi trả
```
1. Click Daisy Flower trong shop (stock = 20)
2. Click <<< 5 lần
   → Inventory có 5x Daisy Flower
   → Shop stock = 15
3. Click Daisy Flower trong inventory
   → Arrow >>> hiện
4. Click >>> 3 lần
   → Inventory còn 2x Daisy Flower
   → Shop stock = 18
```

### Test case 2: Không thể trả items cũ
```
1. Inventory có sẵn 10x Old Item (không mua từ shop)
2. Click Old Item trong inventory
   → Arrow KHÔNG hiện (không trong cart)
```

### Test case 3: Close shop
```
1. Mua 5x Daisy Flower
2. Trả 2x về shop
3. Close shop (đi ra khỏi trigger)
   → 3x Daisy Flower còn lại bị XÓA
   → Shop stock restore
```

## Console Logs

### Khi chọn inventory item:
```
[UI] Click slot 2: Daisy Flower x5
🔄 [Inventory] Selected for return: Daisy Flower
📦 [Selection] Selected inventory item: Daisy Flower (slot 2)
```

### Khi trả hàng:
```
📈 [ShoppingBag] Increased stock of Daisy Flower: 16
✅ [ShoppingBag] Returned Daisy Flower to shop
📊 [UIShopController] Updated stock for item 2: 16
```

### Khi không thể trả:
```
⚠️ [ShoppingBag] Daisy Flower không có trong cart!
```

## Troubleshooting

### Arrow không xoay khi click inventory item
1. Check shop có đang mở không (TradeUI active)
2. Check item có trong cart không: `IsItemInCart()`
3. Check item có `databaseItemId > 0` không

### Click >>> nhưng không trả được
1. Check console log: "Inventory item not found!"
2. Check `GetSelectedInventoryItem()` có trả về null không
3. Check `InventorySystem.Remove()` có success không

### Stock không tăng
1. Check `FindShopItemById()` có tìm thấy shop item không
2. Check `UpdateItemStock()` có được gọi không
3. Check stock có phải unlimited (-1) không

### Item không xóa khỏi inventory
1. Check `InventorySystem.Instance.Remove()` return value
2. Check ItemSO reference có đúng không
3. Check `CanStackWith()` logic

## Next Steps
- [x] Implement TransferCartToShop()
- [x] Connect inventory click to SelectionManager
- [x] Track inventory items in cart
- [ ] Add unborder logic cho shop items khi select inventory
- [ ] Implement continuous return (không clear selection)
- [ ] Add sound effects
- [ ] Add return confirmation dialog
