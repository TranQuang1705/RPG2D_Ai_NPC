# Shop UI Setup Guide

## Tổng quan
Hệ thống load shop inventory từ database và hiển thị trên UI TradeUI.

## Files đã tạo
1. `DatabaseShopLoader.cs` - Load shop inventory từ SQL
2. `UIShopController.cs` - Populate UI với data từ database

## Setup Steps

### 1. Setup Database Loader
1. Tạo Empty GameObject trong scene tên `   `
2. Attach script `DatabaseShopLoader.cs`
3. Set API URL: `http://127.0.0.1:5002`

### 2. Setup TradeUI
1. Tìm GameObject `TradeUI` trong scene (Canvas > TradeUI)
2. Attach script `UIShopController.cs` vào TradeUI
3. Setup fields:
   - **Items Container**: Kéo object `Items` (parent chứa các ItemTradePanel)
   - **Item Slot Prefab**: Kéo prefab `ItemTradePanel` từ Prefabs/UI
   - **Current NPC Id**: Để 1 (hoặc NPC ID bạn muốn test)

### 3. Setup ItemTradePanel Template
Đảm bảo ItemTradePanel có cấu trúc:
```
ItemTradePanel
├── ItemsIcon (Image)
├── ItemStock (TextMeshProUGUI)
├── ItemsName (TextMeshProUGUI)
├── PriceText (TextMeshProUGUI)
└── PriceIcon (Image)
```

### 4. Chuẩn bị Resources
Đảm bảo các icon đã có trong `Resources/`:
- `Icons/daisy_flower` 
- `Icons/sunpetal_flower`
- `Icons/moonblossom`
- `Icons/crimson_lily`
- `Icons/Coins/obal`
- `Icons/Coins/varos`
- `Icons/Coins/sylv`

### 5. Test
1. Chạy game
2. Player lại gần MarketSell
3. TradeUI sẽ tự động load shop inventory cho NPC ID = 1 (Snow)
4. Các item sẽ hiển thị với:
   - Icon: Icons/xxx
   - Stock: x20 (hoặc ∞ nếu unlimited)
   - Name: Daisy Flower
   - Price: 2 (với icon Obal)

## Cách hoạt động

### Flow
1. Player trigger MarketSell
2. MarketSellTrigger mở TradeUI
3. UIShopController.OnEnable() được gọi
4. LoadShopInventory(npcId) fetch data từ database
5. PopulateShopUI() tạo các ItemTradePanel slots
6. Mỗi slot được populate với data từ database

### Database Query
Script gọi API: `GET /npc_shop_inventory?npc_id=1`

Response:
```json
[
  {
    "shop_inventory_id": 1,
    "npc_id": 1,
    "item_id": 2,
    "item_name": "Daisy Flower",
    "icon_path": "Icons/daisy_flower",
    "stock": 20,
    "price": 2,
    "coin_type": "Obal",
    "is_available": true
  }
]
```

### Mapping
- `item_name` → ItemsName (TextMeshProUGUI)
- `icon_path` → ItemsIcon (Image) via Resources.Load
- `stock` → ItemStock (TextMeshProUGUI) format "x20"
- `price` → PriceText (TextMeshProUGUI)
- `coin_type` → PriceIcon (Image) via `Icons/Coins/{coin_type}`

## Tùy chỉnh

### Thay đổi NPC ID
Có 2 cách:

**Cách 1: Set trực tiếp trong Inspector**
```
UIShopController > Current NPC Id = 1
```

**Cách 2: Set từ code (MarketSellTrigger hoặc script khác)**
```csharp
UIShopController shopController = tradePanelUI.GetComponent<UIShopController>();
if (shopController != null)
{
    int npcId = npcTrader.GetNPCId(); // Lấy từ NPCTrader
    shopController.SetCurrentNPC(npcId);
}
```

### Refresh shop
```csharp
shopController.RefreshShop(); // Clear cache và reload từ database
```

## Database Schema

### Table: npc_shop_inventory
```sql
CREATE TABLE npc_shop_inventory (
    shop_inventory_id INT PRIMARY KEY AUTO_INCREMENT,
    npc_id INT NOT NULL,
    item_id INT NOT NULL,
    stock INT DEFAULT -1,
    price INT NOT NULL,
    coin_type VARCHAR(20) DEFAULT 'Obal',
    discount_percent FLOAT DEFAULT 0,
    is_available BOOLEAN DEFAULT TRUE,
    FOREIGN KEY (npc_id) REFERENCES npcs(npc_id),
    FOREIGN KEY (item_id) REFERENCES items(item_id)
);
```

### Sample Data (NPC ID = 1, Snow)
```sql
INSERT INTO npc_shop_inventory (npc_id, item_id, stock, price, coin_type)
VALUES
(1, 2, 20, 2, 'Obal'),   -- Daisy Flower
(1, 3, 20, 4, 'Obal'),   -- Sunpetal Flower
(1, 4, 20, 6, 'Obal'),   -- Moonblossom
(1, 5, 20, 5, 'Obal');   -- Crimson Lily
```

## Troubleshooting

### UI không hiển thị items
1. Check log: `[UIShopController] No shop items to display`
2. Kiểm tra database có data không
3. Kiểm tra API server đang chạy: `http://127.0.0.1:5002/npc_shop_inventory?npc_id=1`

### Icon không load
1. Check log: `Could not load icon: Icons/xxx`
2. Đảm bảo icon nằm trong folder `Resources/Icons/`
3. Tên file phải khớp với `icon_path` trong database (không cần .png)

### Items không spawn
1. Check `Items Container` đã được gán chưa
2. Check `ItemTradePanel` prefab có đúng structure không
3. Xem log: `Created slot: Daisy Flower - 2 Obal`

## API Endpoints Cần Thiết

### Python Flask Server

```python
@app.route('/npc_shop_inventory', methods=['GET'])
def get_npc_shop_inventory():
    npc_id = request.args.get('npc_id', type=int)
    
    cursor = db.cursor(dictionary=True)
    query = """
        SELECT 
            nsi.*,
            i.item_name,
            i.item_type,
            i.description,
            i.rarity,
            i.icon_path,
            i.model_path
        FROM npc_shop_inventory nsi
        JOIN items i ON nsi.item_id = i.item_id
        WHERE nsi.npc_id = %s AND nsi.is_available = 1
        ORDER BY nsi.shop_inventory_id
    """
    cursor.execute(query, (npc_id,))
    items = cursor.fetchall()
    cursor.close()
    
    return jsonify(items)
```

## Notes
- Script tự động clear và recreate slots mỗi khi UI mở
- Hỗ trợ unlimited stock (stock = -1 hiển thị "∞")
- Hỗ trợ discount (discount_percent > 0)
- Coin icon tự động load theo coin_type
