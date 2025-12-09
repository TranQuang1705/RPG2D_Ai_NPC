# 🎮 Hướng Dẫn Gán Scripts Inventory (Chi Tiết)

## 📦 Phần 1: ITEM INVENTORY (Túi Đồ)

### **Script: InventoryToggle.cs**
**Gán vào:** GameObject riêng biệt (tạo mới)

### **Bước 1: Tạo GameObject**
```
Hierarchy → Right-click → Create Empty
Rename: "InventoryManager" hoặc "InventoryToggle"
```

### **Bước 2: Attach Script**
1. Select `InventoryManager`
2. Inspector → **Add Component** → `InventoryToggle`

### **Bước 3: Assign References trong Inspector**

**3 References cần gán:**

#### **a) inventoryCanvas** (GameObject - Panel túi đồ)
```
Hierarchy path: UI Canvas → Inventory (Panel)
```
- Kéo object `Inventory` (hoặc `ItemInventoryPanel`) vào field này
- **Đây là Panel chứa túi đồ** (có Background, Slots, Header...)

#### **b) playerRb** (Rigidbody2D)
```
Hierarchy path: Player (hoặc tên nhân vật của bạn)
```
- Tìm Player object
- Kéo Player vào field hoặc click vào circle → chọn Player
- Script sẽ tự lấy Rigidbody2D component

#### **c) activeWeapon** (GameObject)
```
Hierarchy path: Player → Weapon (hoặc Hand → Weapon)
```
- Tìm object vũ khí đang cầm của player
- Thường là child của Player, có thể tên: "Weapon", "ActiveWeapon", "Hand", etc.
- Kéo vào field này

### **Kết quả:**
- Nhấn **E** hoặc **Esc** → Mở/đóng túi đồ
- Player bị freeze khi mở
- Weapon tắt khi mở túi

---

## 💰 Phần 2: COIN INVENTORY (Túi Tiền)

### **Script: CoinInventoryToggle.cs**
**Gán vào:** GameObject riêng biệt HOẶC UI Canvas

### **Bước 1: Tạo GameObject (Nếu chưa có)**
```
Hierarchy → Right-click → Create Empty
Rename: "CoinInventoryManager"
Parent: UI Canvas (optional, để gọn)
```

### **Bước 2: Attach Script**
1. Select `CoinInventoryManager`
2. Inspector → **Add Component** → `CoinInventoryToggle`

### **Bước 3: Assign References**

**2 References cần gán:**

#### **a) coinInventoryPanel** (GameObject - Panel túi tiền)
```
Hierarchy path: UI Canvas → CoinInventory (Panel)
```
- Kéo object `CoinInventory` hoặc `CoinInventoryPanel` vào đây
- **Đây là Panel hiển thị tiền** (có 6 coin slots)

#### **b) toggleButton** (Button - Icon túi tiền)
```
Hierarchy path: UI Canvas → CoinBagButton (hoặc SlideTab → CoinIcon)
```
- Tìm button có icon túi tiền (CoinBagIcon.png)
- Kéo Button object vào field này
- Script sẽ tự add onClick listener

### **Settings (Optional):**
- `allowEKeyToClose`: ✅ (Cho phép đóng bằng phím E)
- `allowEscKeyToClose`: ✅ (Cho phép đóng bằng phím Esc)

### **Kết quả:**
- Click button → Mở túi tiền
- Nhấn **E** hoặc **Esc** → Đóng túi tiền
- Player KHÔNG bị freeze (có thể vừa chơi vừa xem)

---

## 🎯 Phần 3: UI INVENTORY PANEL (Logic hiển thị slots)

### **Script: UIInventoryPanel.cs**
**Gán vào:** Panel chứa slots (Inventory Panel)

### **Bước 1: Locate Panel**
```
Hierarchy: UI Canvas → Inventory → (Panel có Background + Slots)
```

### **Bước 2: Attach Script**
1. Select panel đó
2. Inspector → **Add Component** → `UIInventoryPanel`

### **Bước 3: Assign References**

#### **a) slotsParent** (Transform - Container chứa slots)
```
Path: Inventory → SlotsContainer (hoặc Grid)
```
- Object có **Grid Layout Group** component
- Kéo `SlotsContainer` vào đây

#### **b) slotPrefab** (UIInventoryItem - Prefab của 1 slot)
```
Path: Assets/Prefabs/UI/InventorySlot.prefab
```
- Kéo prefab `InventorySlot.prefab` từ Project window vào đây

#### **c) Settings:**
- `rebuildOnAwake`: ✅ (Tự động tạo slots khi start)
- `useInventoryCapacity`: ✅ (Dùng số slots từ InventorySystem)
- `manualSlotCount`: 24 (Nếu không dùng InventorySystem)

### **Kết quả:**
- Slots tự động được tạo theo capacity
- Items tự động hiển thị từ InventorySystem
- Click slot → Show ItemDetailPanel

---

## 💎 Phần 4: COIN INVENTORY CONTROLLER

### **Script: UICoinInventoryController.cs**
**Gán vào:** CoinInventory Panel

### **Bước 1: Locate Panel**
```
Hierarchy: UI Canvas → CoinInventory (Panel)
```

### **Bước 2: Attach Script**
1. Select `CoinInventory` panel
2. Inspector → **Add Component** → `UICoinInventoryController`

### **Bước 3: Assign References**

#### **a) Container chứa coin slots**
```
Path: CoinInventory → CoinSlotsContainer (Vertical Layout)
```

#### **b) Coin Slot Prefab**
```
Path: Assets/Prefabs/UI/CoinSlot.prefab
```

### **Kết quả:**
- 6 coin slots tự động tạo (Obal, Varos, Sylv, Feron, Astryl, Aurum)
- Số lượng tiền tự động update từ CoinInventorySystem

---

## 📝 CHECKLIST HOÀN THÀNH

### Scene Hierarchy nên có:
```
UI Canvas
├─ Inventory (Panel) [UIInventoryPanel.cs]
│   ├─ Background (Image)
│   ├─ Header (Text)
│   ├─ CloseButton (Button)
│   └─ SlotsContainer (Grid Layout)
│
├─ CoinInventory (Panel) [UICoinInventoryController.cs]
│   ├─ Background (Image)
│   ├─ Header (Text)
│   ├─ CloseButton (Button)
│   └─ CoinSlotsContainer (Vertical Layout)
│
├─ BagToggleButton (Button)
└─ CoinBagButton (Button)

InventoryManager [InventoryToggle.cs]
  → inventoryCanvas = Inventory Panel
  → playerRb = Player
  → activeWeapon = Player → Weapon

CoinInventoryManager [CoinInventoryToggle.cs]
  → coinInventoryPanel = CoinInventory Panel
  → toggleButton = CoinBagButton
```

### ✅ Testing:
- [ ] Nhấn **E** → Mở/đóng túi đồ
- [ ] Player freeze khi túi đồ mở
- [ ] Weapon tắt khi túi đồ mở
- [ ] Click coin button → Mở túi tiền
- [ ] Nhấn **E** hoặc **Esc** → Đóng túi tiền
- [ ] Player KHÔNG freeze khi túi tiền mở
- [ ] Slots hiển thị items đúng
- [ ] Coin amounts hiển thị đúng

---

## 🔍 TÌM OBJECTS TRONG SCENE

### Tìm Player:
```
Hierarchy → Search: "Player" hoặc tên nhân vật
Hoặc: Tag = "Player"
```

### Tìm Weapon:
```
Select Player → Inspector → xem children
Tìm object có SpriteRenderer hiển thị vũ khí
```

### Tìm Inventory Panel:
```
UI Canvas → tìm Panel có sprite túi (ShoppingBag.png)
Hoặc: Search "Inventory" trong Hierarchy
```

### Tìm Coin Panel:
```
UI Canvas → tìm Panel có sprite túi tiền (CoinBagIcon.png)
Hoặc: Search "Coin" trong Hierarchy
```

---

## ⚠️ TROUBLESHOOTING

**"Nhấn E không mở túi":**
- Check InventoryToggle script attached vào object nào đó
- Check inventoryCanvas reference đã gán chưa
- Check Console có lỗi không

**"Player không freeze khi mở túi":**
- Check playerRb reference
- Check player có Rigidbody2D không

**"Weapon không tắt khi mở túi":**
- Check activeWeapon reference
- Check weapon object có active không

**"Slots không hiển thị":**
- Check UIInventoryPanel → slotsParent reference
- Check slotPrefab reference
- Right-click script → "Rebuild Slots"

**"Coin inventory không mở bằng button":**
- Check toggleButton reference
- Check button có Event System không
- Check button có onClick event không

---

## 💡 TIPS

1. **Debug Mode:** Add `Debug.Log()` trong Update() để check phím có nhận không
2. **Test từng bước:** Gán từng reference một, test ngay
3. **Prefab Instance:** Nếu UI là prefab instance, phải "Unpack" trước khi edit
4. **Event System:** Cần có `EventSystem` object trong scene cho buttons work

---

Chúc bạn setup thành công! 🎉
