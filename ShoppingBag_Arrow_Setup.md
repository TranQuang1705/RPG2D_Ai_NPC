# Shopping Bag Arrow Controller Setup Guide

## Tổng quan
Hệ thống đổi hướng mũi tên ShoppingBag dựa vào item được chọn:
- **Chọn item từ Shop** → Mũi tên trái `<<<` (chuyển vào giỏ)
- **Chọn item từ Inventory** → Mũi tên phải `>>>` (trả về shop)

## Files đã tạo
1. `ShoppingBagArrowController.cs` - Control hiển thị mũi tên
2. `ShopInventorySelectionManager.cs` - Quản lý selection
3. Update `ItemTradePanelController.cs` - Integrate với selection manager

## Setup trong Unity

### 1. Setup ShoppingBag GameObject

```
ShoppingBag (GameObject)
├── DirectionL (GameObject - Left Arrow <<<)
│   └── Image (Image component với sprite <<<)
├── Direction (GameObject - Right Arrow >>>)
│   └── Image (Image component với sprite >>>)
```

### 2. Attach Scripts

#### A. ShoppingBagArrowController
1. Select `ShoppingBag` GameObject
2. Add Component → `ShoppingBagArrowController`
3. Assign fields:
   - **Left Arrow Prefab**: Kéo `DirectionL` object
   - **Right Arrow Prefab**: Kéo `Direction` object
   - **Arrow Container**: Để trống (auto = this transform)
   - **Auto Find Arrows**: ✓ (checked)

#### B. ShopInventorySelectionManager
1. Tạo Empty GameObject tên `ShopInventorySelectionManager` trong TradeUI
2. Add Component → `ShopInventorySelectionManager`
3. Assign fields:
   - **Arrow Controller**: Kéo ShoppingBag object (có ShoppingBagArrowController)
   - **Auto Find Arrow**: ✓ (checked)

### 3. Sprites Setup

#### Left Arrow (<<<) Sprite:
- File: `DirectionL.png`
- Direction: Trái (pointing left)
- Use cho "Add to cart"

#### Right Arrow (>>>) Sprite:
- File: `Direction.png`
- Direction: Phải (pointing right)
- Use cho "Return to shop"

## Cách hoạt động

### Flow khi chọn item từ Shop:
```
1. Player click vào ItemTradePanel (shop item)
2. ItemTradePanelController.OnPointerClick() được gọi
3. Gọi ShopInventorySelectionManager.SelectShopItem()
4. SelectShopItem() gọi arrowController.ShowLeftArrow()
5. Mũi tên <<< hiển thị
```

### Flow khi chọn item từ Inventory:
```
1. Player click vào inventory slot
2. Inventory slot script gọi ShopInventorySelectionManager.SelectInventoryItem()
3. SelectInventoryItem() gọi arrowController.ShowRightArrow()
4. Mũi tên >>> hiển thị
```

### Flow khi clear selection:
```
1. Player click lại vào cùng item (toggle off)
2. Hoặc close UI
3. ShopInventorySelectionManager.ClearSelection() được gọi
4. arrowController.HideAllArrows()
5. Cả 2 mũi tên đều ẩn
```

## API Reference

### ShoppingBagArrowController

#### Methods:
```csharp
// Show left arrow (<<< - shop to cart)
public void ShowLeftArrow()

// Show right arrow (>>> - cart to shop)
public void ShowRightArrow()

// Hide all arrows
public void HideAllArrows()

// Get current direction
public ArrowDirection GetCurrentDirection()

// Check arrow state
public bool IsLeftArrowShowing()
public bool IsRightArrowShowing()
```

### ShopInventorySelectionManager

#### Methods:
```csharp
// Select shop item
public void SelectShopItem(GameObject itemSlot, DatabaseShopItem shopItem)

// Select inventory item
public void SelectInventoryItem(GameObject itemSlot)

// Clear selection
public void ClearSelection()

// Get selection info
public GameObject GetSelectedItemSlot()
public ItemSource GetSelectedItemSource()
public DatabaseShopItem GetSelectedShopItem()

// Check selection state
public bool HasSelection()
public bool IsShopItemSelected()
public bool IsInventoryItemSelected()
```

#### Events:
```csharp
public System.Action<GameObject, ItemSource> OnItemSelected;
public System.Action OnSelectionCleared;
```

## Integration với Inventory

Khi player click vào inventory slot, gọi:

```csharp
// Example trong inventory slot script
void OnInventorySlotClicked()
{
    if (ShopInventorySelectionManager.Instance != null)
    {
        ShopInventorySelectionManager.Instance.SelectInventoryItem(gameObject);
    }
}
```

## Debugging

### Check arrow hiển thị:
```csharp
void Update()
{
    if (Input.GetKeyDown(KeyCode.L))
    {
        // Test left arrow
        ShoppingBagArrowController arrow = FindObjectOfType<ShoppingBagArrowController>();
        arrow?.ShowLeftArrow();
    }
    
    if (Input.GetKeyDown(KeyCode.R))
    {
        // Test right arrow
        ShoppingBagArrowController arrow = FindObjectOfType<ShoppingBagArrowController>();
        arrow?.ShowRightArrow();
    }
}
```

### Check selection:
```csharp
void Update()
{
    if (Input.GetKeyDown(KeyCode.D))
    {
        var manager = ShopInventorySelectionManager.Instance;
        if (manager != null)
        {
            Debug.Log($"Has Selection: {manager.HasSelection()}");
            Debug.Log($"Source: {manager.GetSelectedItemSource()}");
        }
    }
}
```

## Troubleshooting

### Mũi tên không hiển thị
1. Check DirectionL và Direction objects có trong ShoppingBag không
2. Check Auto Find Arrows được bật chưa
3. Check sprites đã assign cho Image components chưa
4. Xem log: `[Selection] Selected shop item: xxx`

### Mũi tên không đổi hướng
1. Check ShopInventorySelectionManager Instance có null không
2. Check arrow controller reference đã assign chưa
3. Check ItemTradePanelController có gọi SelectShopItem() không

### Selection không clear
1. Check ClearSelection() có được gọi khi toggle off không
2. Check OnDisable() của UI có gọi ClearSelection() không

## Next Steps
- Implement logic chuyển item vào giỏ (khi click ShoppingBag với left arrow)
- Implement logic trả item về shop (khi click ShoppingBag với right arrow)
- Tích hợp với ShoppingCartManager (sẽ tạo sau)
