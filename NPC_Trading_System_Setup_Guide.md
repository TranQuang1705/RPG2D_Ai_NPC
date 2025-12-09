# 🏪 NPC Trading System - Setup Guide

## 📋 Tổng quan

Hệ thống trading đã được implement với các tính năng:
- ⏰ **Time-based trading**: NPC bán hàng từ 8h-12h
- 🗺️ **Location-based**: NPC phải ở market stall mới bán được
- 🎭 **Role-based inventory**: Shop inventory theo role NPC (flower_merchant, hunter, etc.)
- 💬 **Dialogue integration**: Có thể mở shop qua chat với NPC
- 💾 **Database integration**: Load shop inventory từ MySQL database

---

## 🛠️ Components đã tạo

### 1. **NPCTrader.cs**
- Quản lý shop inventory
- Time-based shop (8h-12h)
- Location-based trading (phải ở market stall)
- Visual indicator khi shop mở
- Tích hợp với DatabaseShopLoader

**Vị trí**: `Assets/Scripts/NPCTrader.cs`

### 2. **DatabaseShopLoader.cs**
- Load shop inventory từ database theo NPC ID
- Convert DatabaseShopItem → ShopItem
- Update stock sau khi mua
- Pattern tương tự DatabaseCoinLoader

**Vị trí**: `Assets/Scripts/Database/DatabaseShopLoader.cs`

### 3. **NPCRoutineAI.cs** (Updated)
- Thêm `NPCActivity.MarketTrading` enum
- Thêm `MarketTradingRoutine()` coroutine
- Update `UpdateCurrentActivity()` để support market trading
- NPC tự động đi tới market stall vào 8h-12h

### 4. **NPC.cs** (Updated)
- Thêm handler cho OPEN_SHOP/TRADE/BUY actions trong `HandleChatbotAction()`
- Tích hợp với NPCTrader component

### 5. **Python API Endpoints** (database.py)
- `GET /npc_shop_inventory?npc_id={id}` - Lấy shop inventory
- `POST /npc_shop_inventory/update_stock` - Update stock
- `POST /shop/buy` - Mua item từ shop
- `GET /npcs` - Lấy danh sách NPCs
- `GET /npcs/{id}` - Lấy NPC theo ID

---

## 📦 Setup trong Unity Scene

### Bước 1: Tạo DatabaseShopLoader GameObject

```
1. Hierarchy → Create Empty GameObject
2. Đặt tên: "DatabaseShopLoader"
3. Add Component → DatabaseShopLoader
4. Cấu hình:
   - API URL: http://127.0.0.1:5002
   - Load On Start: FALSE (load manually khi cần)
```

### Bước 2: Setup NPC(Snow) với NPCTrader

```
1. Mở Prefab: Assets/Prefabs/NPC(Snow).prefab
2. Add Component → NPCTrader
3. Cấu hình NPCTrader:
   - NPC ID: 1
   - NPC Role: "flower_merchant"
   - Use Time Based Trading: TRUE
   - Market Open Hour: 8
   - Market Close Hour: 12
   - Market Stall Location: [Gán GameObject market location]
   - Market Proximity: 2.0
   
4. Cấu hình NPCRoutineAI (nếu chưa có):
   - Is Trader: TRUE
   - Market Open Hour: 8
   - Market Close Hour: 12
   - Market Stall Location: [Gán GameObject market location]
```

### Bước 3: Tạo Market Stall Location

```
1. Hierarchy → Create Empty GameObject
2. Đặt tên: "MarketStall_Snow"
3. Position: Đặt ở vị trí bạn muốn Snow bán hàng (ví dụ: x=10, y=5)
4. Add Tag: "MarketStall" (optional)
5. Gizmo: Add Icon để dễ nhìn thấy trong Scene
```

### Bước 4: Đặt Market Stall Prefab (Optional)

```
1. Drag "FlowerMarket_0.prefab" hoặc "Market_0.prefab" vào scene
2. Đặt tại vị trí market stall
3. Gán prefab này vào NPCTrader.marketStallPrefab (nếu muốn spawn động)
```

---

## 🗄️ Database Setup

### Bước 1: Tạo tables (nếu chưa có)

```sql
-- Bảng shop inventory
CREATE TABLE IF NOT EXISTS npc_shop_inventory (
    shop_inventory_id INT PRIMARY KEY AUTO_INCREMENT,
    npc_id INT NOT NULL,
    item_id INT NOT NULL,
    stock INT DEFAULT -1,
    price INT NOT NULL,
    coin_type VARCHAR(20) DEFAULT 'Obal',
    discount_percent FLOAT DEFAULT 0,
    is_available BOOLEAN DEFAULT TRUE,
    created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    updated_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP,
    FOREIGN KEY (npc_id) REFERENCES npcs(npc_id) ON DELETE CASCADE,
    FOREIGN KEY (item_id) REFERENCES items(item_id) ON DELETE CASCADE
);

-- Bảng shop templates
CREATE TABLE IF NOT EXISTS shop_templates (
    template_id INT PRIMARY KEY AUTO_INCREMENT,
    role VARCHAR(50) NOT NULL UNIQUE,
    template_name VARCHAR(100) NOT NULL,
    description TEXT,
    created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP
);

-- Bảng template items
CREATE TABLE IF NOT EXISTS shop_template_items (
    template_item_id INT PRIMARY KEY AUTO_INCREMENT,
    template_id INT NOT NULL,
    item_id INT NOT NULL,
    default_stock INT DEFAULT 10,
    default_price INT NOT NULL,
    coin_type VARCHAR(20) DEFAULT 'Obal',
    FOREIGN KEY (template_id) REFERENCES shop_templates(template_id) ON DELETE CASCADE,
    FOREIGN KEY (item_id) REFERENCES items(item_id) ON DELETE CASCADE
);
```

### Bước 2: Thêm data mẫu cho Snow (flower_merchant)

```sql
-- Insert template cho flower_merchant
INSERT INTO shop_templates (role, template_name, description)
VALUES ('flower_merchant', 'Flower Merchant Shop', 'Sells various flowers and plants');

-- Lấy template_id
SET @template_id = (SELECT template_id FROM shop_templates WHERE role = 'flower_merchant');

-- Thêm items vào template (cần có items này trong bảng items trước)
INSERT INTO shop_template_items (template_id, item_id, default_stock, default_price, coin_type)
VALUES
(@template_id, 2, 20, 5, 'Obal'),   -- Daisy Flower
(@template_id, 3, 15, 15, 'Obal'),  -- Rose (nếu có)
(@template_id, 4, 15, 8, 'Obal');   -- Tulip (nếu có)

-- Populate shop inventory cho Snow (NPC ID = 1)
INSERT INTO npc_shop_inventory (npc_id, item_id, stock, price, coin_type)
SELECT 
    1,  -- Snow's NPC ID
    sti.item_id,
    sti.default_stock,
    sti.default_price,
    sti.coin_type
FROM shop_template_items sti
WHERE sti.template_id = @template_id;
```

### Bước 3: Update role của Snow nếu cần

```sql
UPDATE npcs
SET role = 'flower_merchant'
WHERE npc_id = 1;
```

---

## 🎮 Cách sử dụng

### In-game flow:

1. **Vào giờ 8h-12h**: NPC Snow sẽ tự động di chuyển đến market stall location
2. **Visual indicator**: Icon giỏ hàng xuất hiện trên đầu Snow khi đứng ở chợ
3. **Player interaction**:
   - Cách 1: Lại gần Snow và nói "trade", "buy", "shop"
   - Cách 2: Ấn E khi gần Snow (nếu có interact key)
4. **Mở shop**: NPCTrader kiểm tra điều kiện (giờ + vị trí) → Mở shop UI
5. **Mua hàng**: Chọn item, kiểm tra tiền → Trừ coin → Thêm vào inventory → Update stock

### Debug/Testing:

```csharp
// Test trong Console hoặc script
NPCTrader trader = FindObjectOfType<NPCTrader>();

// Kiểm tra shop có mở không
Debug.Log($"Shop open: {trader.IsShopOpen()}");

// Force mở shop (bypass time check)
trader.OpenShop();

// Load lại inventory từ database
StartCoroutine(DatabaseShopLoader.Instance.FetchShopInventory(1));
```

---

## 🔧 Cấu hình nâng cao

### Thêm role mới (ví dụ: hunter)

1. **Database**:
```sql
-- Thêm template mới
INSERT INTO shop_templates (role, template_name, description)
VALUES ('hunter', 'Hunter Lodge', 'Sells animal products and hunting gear');

-- Thêm items cho hunter
INSERT INTO shop_template_items (template_id, item_id, default_stock, default_price, coin_type)
VALUES
((SELECT template_id FROM shop_templates WHERE role = 'hunter'), 10, 8, 20, 'Obal'),  -- Rabbit Pelt
((SELECT template_id FROM shop_templates WHERE role = 'hunter'), 11, 5, 30, 'Varos'); -- Deer Meat
```

2. **Unity**: Tạo NPC hunter, thêm NPCTrader, set role = "hunter"

### Điều chỉnh giờ mở cửa

```csharp
// Trong NPCTrader Inspector
Market Open Hour: 8  // 8:00 AM
Market Close Hour: 12 // 12:00 PM

// Hoặc trong code
trader.SetMarketHours(8f, 12f);
```

### Thay đổi market location runtime

```csharp
Transform newLocation = GameObject.Find("NewMarketStall").transform;
trader.SetMarketLocation(newLocation);
```

---

## ⚠️ Troubleshooting

### Shop không mở được

**Kiểm tra**:
1. TimeManager có đang chạy không?
2. Giờ hiện tại có trong khoảng 8h-12h không?
3. NPC có đang ở market stall location không? (kiểm tra distance)
4. `isTrader` trong NPCRoutineAI có = TRUE không?

**Debug**:
```csharp
Debug.Log($"Current hour: {TimeManager.Instance.GetCurrentHour()}");
Debug.Log($"Market hours: {trader.IsMarketHours()}");
Debug.Log($"At market: {trader.IsAtMarket()}");
Debug.Log($"Shop open: {trader.IsShopOpen()}");
```

### Shop inventory trống

**Kiểm tra**:
1. Database có data trong `npc_shop_inventory` cho NPC này không?
2. DatabaseShopLoader có trong scene không?
3. API server có chạy không? (test: http://127.0.0.1:5002/npc_shop_inventory?npc_id=1)

**Fix**:
```csharp
// Force reload shop inventory
StartCoroutine(DatabaseShopLoader.Instance.LoadShopForTrader(trader));
```

### NPC không đi tới market stall

**Kiểm tra**:
1. `marketStallLocation` có được gán không?
2. `isTrader` trong NPCRoutineAI = TRUE?
3. Pathfinding có hoạt động không? (kiểm tra obstacles)

**Debug**:
```csharp
Debug.Log($"Is trader: {routineAI.isTrader}");
Debug.Log($"Market location: {routineAI.marketStallLocation}");
Debug.Log($"Current activity: {routineAI.currentActivity}");
```

---

## 📝 TODO - Các tính năng chưa implement

- [ ] Shop UI Panel (TradePanel.cs)
- [ ] Sell items to NPC (player bán đồ cho NPC)
- [ ] Dynamic pricing (giá thay đổi theo mùa/thời gian)
- [ ] Reputation/discount system
- [ ] Shop refresh/restock system
- [ ] Multiple market locations per NPC
- [ ] Bartering/negotiation dialogue

---

## 📚 Files quan trọng

```
Assets/
├── Scripts/
│   ├── NPCTrader.cs ✅ (NEW)
│   ├── NPC.cs ⚡ (UPDATED)
│   ├── NPCRoutineAI.cs ⚡ (UPDATED)
│   └── Database/
│       ├── DatabaseShopLoader.cs ✅ (NEW)
│       └── DatabaseCoinLoader.cs (reference)
├── Web_Item/
│   └── python_sever/
│       └── database.py ⚡ (UPDATED - added shop APIs)
└── Prefabs/
    ├── NPC(Snow).prefab (needs setup)
    ├── FlowerMarket_0.prefab
    └── Market_0.prefab
```

**Legend**: ✅ New | ⚡ Updated

---

## 🎯 Next Steps

1. Tạo DatabaseShopLoader GameObject trong scene
2. Setup NPC(Snow) prefab với NPCTrader component
3. Tạo market stall location GameObject
4. Test trong game: đợi 8h → Snow đi tới chợ → lại gần nói "trade"
5. Implement TradePanel UI để hiển thị shop inventory

---

**Created**: 2025-11-24  
**Author**: Factory AI Assistant  
**Version**: 1.0
