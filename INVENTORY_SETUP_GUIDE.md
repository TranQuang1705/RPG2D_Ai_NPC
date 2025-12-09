# 📦 Hướng Dẫn Tạo UI Inventory/Bag (Chi Tiết)

## 📋 Tổng Quan Hệ Thống

### **Cấu trúc gồm 2 loại Inventory:**
1. **Item Inventory** - Đồ thường (weapon, food, materials)
2. **Coin Inventory** - Tiền tệ (Obal, Varos, Sylv, Feron, Astryl, Aurum)

---

## 🎨 PHẦN 1: TẠO ITEM INVENTORY UI

### **Hierarchy Structure:**
```
UI Canvas
└─ ItemInventoryPanel (Panel)
    ├─ Background (Image - sprite: ShoppingBag.png)
    ├─ Header (Text - "Túi Đồ")
    ├─ CloseButton (Button)
    └─ SlotsContainer (Grid Layout)
        └─ [Slots sẽ được tạo tự động bởi script]
```

### **📝 Bước 1: Tạo ItemInventoryPanel**

1. **Hierarchy** → Right-click **UI Canvas** → **UI** → **Panel**
2. Rename: `ItemInventoryPanel`
3. **Inspector** → **RectTransform**:
   - Anchor: Center-Middle
   - Width: 600, Height: 700
   - Pos X: 0, Y: 0

### **📝 Bước 2: Thêm Background Image**

1. Select `ItemInventoryPanel` → Right-click → **UI** → **Image**
2. Rename: `Background`
3. **Inspector**:
   - Source Image: `Sprites/UI/ShoppingBag.png` (hoặc itemIBag.png)
   - Color: White (255, 255, 255, 255)
   - Image Type: Sliced (nếu có 9-slice)
   - **RectTransform**: Stretch to fill parent

### **📝 Bước 3: Tạo Header Text**

1. Select `ItemInventoryPanel` → Right-click → **UI** → **Text - TextMeshPro**
2. Rename: `HeaderText`
3. **Inspector**:
   - Text: "Túi Đồ" hoặc "Inventory"
   - Font: Gixel SDF (hoặc font bạn dùng)
   - Font Size: 36
   - Alignment: Center
   - Color: White hoặc Yellow
   - **RectTransform**: 
     - Anchor: Top-Center
     - Width: 400, Height: 60
     - Pos Y: -30

### **📝 Bước 4: Tạo Close Button**

1. Select `ItemInventoryPanel` → Right-click → **UI** → **Button - TextMeshPro**
2. Rename: `CloseButton`
3. **Inspector**:
   - **RectTransform**: 
     - Anchor: Top-Right
     - Width: 50, Height: 50
     - Pos X: -30, Y: -30
   - **Button** → OnClick: Add listener → `ItemInventoryPanel.SetActive(false)`
   - Child Text: "X" (hoặc icon đóng)

### **📝 Bước 5: Tạo SlotsContainer (Grid Layout)**

1. Select `ItemInventoryPanel` → Right-click → **Create Empty**
2. Rename: `SlotsContainer`
3. Add Component: **Grid Layout Group**
   - Cell Size: 80 x 80
   - Spacing: 10 x 10
   - Start Corner: Top Left
   - Start Axis: Horizontal
   - Child Alignment: Upper Left
   - Constraint: Fixed Column Count = 6
4. Add Component: **Content Size Fitter**
   - Horizontal Fit: Preferred Size
   - Vertical Fit: Preferred Size
5. **RectTransform**:
   - Anchor: Top-Left
   - Pivot: (0.5, 1)
   - Pos X: 300, Y: -100
   - Width: 520, Height: 500

### **📝 Bước 6: Tạo Slot Prefab**

1. **Hierarchy** → Right-click → **UI** → **Image**
2. Rename: `InventorySlot`
3. **Inspector**:
   - Width: 80, Height: 80
   - Source Image: Frame/Border sprite (hoặc để trống)
   - Color: (100, 100, 100, 200) - Semi-transparent gray
4. Add child **Image** → Rename: `ItemIcon`
   - Anchor: Stretch all
   - Margins: 5 (left/right/top/bottom)
   - Color: White
   - Raycast Target: OFF
5. Add child **TextMeshPro** → Rename: `QuantityText`
   - Anchor: Bottom-Right
   - Width: 40, Height: 30
   - Pos X: -5, Y: 5
   - Font Size: 18
   - Alignment: Bottom-Right
   - Color: White with black outline
6. **Save as Prefab**: Drag `InventorySlot` to `Assets/Prefabs/UI/InventorySlot.prefab`
7. Delete from Hierarchy

### **📝 Bước 7: Attach Script vào Panel**

1. Select `ItemInventoryPanel`
2. Add Component: **UIInventoryPanel** (script đã có)
3. **Inspector**:
   - Slots Parent: Drag `SlotsContainer`
   - Slot Prefab: Drag `InventorySlot.prefab`
   - Rebuild On Awake: ✅
   - Use Inventory Capacity: ✅
   - Manual Slot Count: 24

### **📝 Bước 8: Tạo Toggle Button (Bag Icon)**

1. **Hierarchy** → Select **UI Canvas**
2. Right-click → **UI** → **Button - TextMeshPro**
3. Rename: `BagToggleButton`
4. **Inspector**:
   - **RectTransform**:
     - Anchor: Top-Right
     - Width: 64, Height: 64
     - Pos X: -80, Y: -80
   - **Image**: Source = `Sprites/UI/ShoppingBag.png`
   - **Button** → OnClick:
     - Add `ItemInventoryPanel.SetActive(true/false)` toggle logic
5. Add Component: **InventoryToggle** script
   - Inventory Panel: Drag `ItemInventoryPanel`

---

## 💰 PHẦN 2: TẠO COIN INVENTORY UI

### **Hierarchy Structure:**
```
UI Canvas
└─ CoinInventoryPanel (Panel)
    ├─ Background (Image - CoinBagIcon.png)
    ├─ Header (Text - "Túi Tiền")
    ├─ CloseButton (Button)
    ├─ TotalDisplay (Text - "Total: 1,298 Obal")
    └─ CoinSlotsContainer (Vertical Layout)
        └─ CoinSlots (6 slots for each coin type)
```

### **📝 Bước 1-5: Giống Item Inventory**
- Làm tương tự Item Inventory
- Background image: `CoinBagIcon.png`
- Header: "Túi Tiền" hoặc "Coin Pouch"

### **📝 Bước 6: Coin Slots Container**

1. Tạo `CoinSlotsContainer`
2. Add Component: **Vertical Layout Group**
   - Spacing: 10
   - Child Alignment: Upper Center
   - Child Force Expand: Width ✅
3. **RectTransform**:
   - Width: 400, Height: 400

### **📝 Bước 7: Coin Slot Prefab**

1. Tạo **Panel** → Rename: `CoinSlot`
2. Width: 380, Height: 60
3. Add children:
   - **Image** `CoinIcon` (left) - 50x50
   - **TextMeshPro** `CoinName` (center-left) - "Obal"
   - **TextMeshPro** `AmountText` (right) - "x 58"
4. Add Component: **Layout Element**
   - Min Height: 60
   - Preferred Height: 60
5. Save as Prefab: `Assets/Prefabs/UI/CoinSlot.prefab`

### **📝 Bước 8: Attach Scripts**

1. Select `CoinInventoryPanel`
2. Add Component: **UICoinInventoryController**
3. Assign references:
   - Coin Slots Container: `CoinSlotsContainer`
   - Coin Slot Prefab: `CoinSlot.prefab`

---

## 🎯 PHẦN 3: SCRIPTS CẦN THIẾT

### **InventoryToggle.cs** (Nếu chưa có)
```csharp
using UnityEngine;

public class InventoryToggle : MonoBehaviour
{
    [SerializeField] private GameObject inventoryPanel;
    private bool isOpen = false;

    public void ToggleInventory()
    {
        isOpen = !isOpen;
        inventoryPanel.SetActive(isOpen);
    }

    void Update()
    {
        // Phím tắt I hoặc B để mở/đóng
        if (Input.GetKeyDown(KeyCode.I) || Input.GetKeyDown(KeyCode.B))
        {
            ToggleInventory();
        }
    }
}
```

---

## ✅ CHECKLIST HOÀN THÀNH

### Item Inventory:
- [ ] ItemInventoryPanel created với Background
- [ ] Header Text và Close Button
- [ ] SlotsContainer với Grid Layout Group
- [ ] InventorySlot prefab (Icon + Quantity text)
- [ ] UIInventoryPanel script attached và configured
- [ ] Bag Toggle Button với InventoryToggle script

### Coin Inventory:
- [ ] CoinInventoryPanel created
- [ ] CoinSlotsContainer với Vertical Layout
- [ ] CoinSlot prefab (Icon + Name + Amount)
- [ ] UICoinInventoryController attached
- [ ] Coin Toggle Button

### Testing:
- [ ] Mở/đóng inventory bằng button
- [ ] Mở/đóng bằng phím I/B
- [ ] Slots hiển thị đúng items
- [ ] Quantity text update đúng
- [ ] Coin amounts hiển thị đúng

---

## 🔧 TROUBLESHOOTING

**Slots không hiển thị:**
- Check SlotsParent reference trong UIInventoryPanel
- Check Slot Prefab reference
- Rebuild Slots (Right-click script → Rebuild Slots)

**Grid layout bị lỗi:**
- Check Cell Size và Spacing
- Check Constraint = Fixed Column Count
- Check Content Size Fitter settings

**Scripts không work:**
- Check InventorySystem.Instance != null
- Check event subscriptions trong OnEnable/OnDisable
- Check Console logs

---

## 📸 Tham Khảo Sprites

**Item Inventory:**
- Background: `Sprites/UI/ShoppingBag.png` hoặc `itemIBag.png`

**Coin Inventory:**
- Background: `Sprites/UI/CoinBagIcon.png`
- Coin Icons: `Sprites/UI/NPC/` (các icon tiền)

---

## 💡 TIPS

1. **Prefab Variants**: Tạo variant của CoinSlot cho từng loại tiền (khác màu)
2. **Animation**: Thêm Animator cho open/close inventory (scale tween)
3. **Sound**: Thêm AudioSource cho click sounds
4. **Drag & Drop**: Implement IDragHandler, IDropHandler cho item dragging
5. **ScrollView**: Nếu có nhiều items, wrap SlotsContainer trong ScrollView

---

Chúc bạn setup thành công! 🎉
