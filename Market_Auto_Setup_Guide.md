# 🏪 Market Auto-Setup Guide (For MapGenerator Camp)

## 📋 Vấn đề

Camp prefab được spawn tự động bởi MapGenerator → Trong Camp có Market prefab → NPC cần tự động tìm và đi tới Market này.

---

## ✅ Giải pháp đã implement

### **NPCTrader.cs** đã được update với `FindMarketLocation()`

Hàm này tự động tìm Market location bằng 3 phương pháp:

1. **Method 1**: Tìm theo tag `"MarketStall"`
2. **Method 2**: Tìm Market child trong Camp GameObject
3. **Method 3**: Tìm theo name pattern (chứa "Market")

---

## 🛠️ Setup trong Unity

### Bước 1: Setup Camp Prefab

```
1. Mở: Assets/Prefabs/Camp.prefab
2. Tìm Market GameObject trong hierarchy (có thể là Market_0, FlowerMarket_0, Market_1)
3. Chọn Market GameObject
4. Inspector → Tag → Chọn "MarketStall"
   (Nếu chưa có tag này, tạo mới: Add Tag → "MarketStall")
5. Save prefab
```

**Hierarchy trong Camp.prefab nên như thế này:**
```
Camp (Root)
├── House
├── Tree
├── Well
├── Market_0  ← Tag: "MarketStall"
│   ├── Sprite
│   └── Collider (optional)
└── ... (other objects)
```

### Bước 2: Setup NPC(Snow) Prefab

```
1. Mở: Assets/Prefabs/NPC(Snow).prefab
2. Chọn root GameObject
3. Inspector → Add Component → NPCTrader (nếu chưa có)
4. Cấu hình NPCTrader:
   - NPC ID: 1
   - NPC Role: "flower_merchant"
   - Use Time Based Trading: TRUE
   - Market Open Hour: 8
   - Market Close Hour: 12
   - Market Stall Location: (Leave EMPTY - auto-find)
   - Market Proximity: 2.0
   
5. Cấu hình NPCRoutineAI:
   - Is Trader: TRUE
   - Market Open Hour: 8
   - Market Close Hour: 12
   - Market Stall Location: (Leave EMPTY - auto-find)
   
6. Save prefab
```

### Bước 3: Verify in Scene

```
1. Play game
2. Chờ MapGenerator spawn Camp
3. Check Console logs:
   - "🏪 Found Market in Camp: Market_0" (hoặc tương tự)
   - "✅ Auto-assigned market location: Market_0 at (x, y, 0)"
   
4. Đợi đến 8:00 AM trong game
5. Check NPC Snow:
   - Snow sẽ tự động di chuyển đến Market location
   - Icon shop xuất hiện trên đầu Snow khi đến chợ
```

---

## 🔍 Auto-Find Logic

### FindMarketLocation() hoạt động như sau:

```csharp
void FindMarketLocation()
{
    // 1️⃣ TÌM THEO TAG (Fastest)
    GameObject market = GameObject.FindWithTag("MarketStall");
    
    if (market == null)
    {
        // 2️⃣ TÌM TRONG CAMP
        GameObject camp = GameObject.Find("Camp");
        if (camp != null)
        {
            // Tìm child có tên: Market, Market_0, FlowerMarket_0
            Transform marketTransform = 
                camp.transform.Find("Market") ?? 
                camp.transform.Find("Market_0") ??
                camp.transform.Find("FlowerMarket_0");
            
            if (marketTransform != null)
                market = marketTransform.gameObject;
        }
    }
    
    if (market == null)
    {
        // 3️⃣ TÌM THEO TÊN (Slowest - last resort)
        GameObject[] allObjects = FindObjectsOfType<GameObject>();
        foreach (var obj in allObjects)
        {
            if (obj.name.Contains("Market") && !obj.name.Contains("Stall"))
            {
                market = obj;
                break;
            }
        }
    }
    
    // Auto-assign to both NPCTrader and NPCRoutineAI
    if (market != null)
    {
        marketStallLocation = market.transform;
        routineAI.marketStallLocation = marketStallLocation;
    }
}
```

---

## 🎮 Runtime Flow

```
[Game Start]
    ↓
MapGenerator spawns Camp
    ↓
Camp contains Market_0 (tagged "MarketStall")
    ↓
NPC(Snow) spawns
    ↓
NPCTrader.Start() runs
    ↓
FindMarketLocation() executes
    ↓
[Method 1] Find by tag "MarketStall" → ✅ Found!
    ↓
marketStallLocation = Market_0.transform
routineAI.marketStallLocation = Market_0.transform
    ↓
Console: "✅ Auto-assigned market location: Market_0 at (10, 5, 0)"
    ↓
[8:00 AM in game]
    ↓
NPCRoutineAI.UpdateCurrentActivity()
    ↓
currentActivity = MarketTrading
    ↓
StartCoroutine(MarketTradingRoutine())
    ↓
MoveToPosition(marketStallLocation.position)
    ↓
Snow walks to (10, 5, 0)
    ↓
Distance < 2.0 → OnArrivedAtMarket()
    ↓
shopOpenIndicator.SetActive(true)
    ↓
[Shop is OPEN! 🏪]
```

---

## ⚠️ Troubleshooting

### Market không được tìm thấy

**Console log:**
```
⚠️ Could not find Market location! Market stall prefab name should contain 'Market'
```

**Solutions:**
1. Kiểm tra Camp prefab có Market GameObject không
2. Đảm bảo Market được tag "MarketStall"
3. Kiểm tra tên Market có chứa "Market" không
4. Thử Manual assign trong Inspector (nếu auto-find fail)

### NPC không đi tới Market

**Kiểm tra:**
```csharp
// Trong Console log
Debug.Log($"Market location: {trader.GetMarketLocation()}");
Debug.Log($"Is trader: {routineAI.isTrader}");
Debug.Log($"Current hour: {TimeManager.Instance.GetCurrentHour()}");
Debug.Log($"Market hours: 8-12");
```

**Solutions:**
1. Verify `isTrader = true` trong NPCRoutineAI
2. Check giờ hiện tại (phải trong khoảng 8h-12h)
3. Check marketStallLocation đã được assign chưa
4. Verify Pathfinding hoạt động (không có obstacles block)

### Market ở vị trí sai

**Kiểm tra trong Scene view:**
1. Chọn Market GameObject
2. Check Transform.position
3. Verify Camp prefab có đúng layout không

**Manual fix:**
```csharp
// Trong Inspector, override auto-find:
NPCTrader:
  - Market Stall Location: Drag Market GameObject here
  
NPCRoutineAI:
  - Market Stall Location: Drag Market GameObject here
```

---

## 🎨 Camp Prefab Structure Examples

### Example 1: Simple Market
```
Camp
├── Market_0  ← Tag: "MarketStall"
```

### Example 2: Nested Market
```
Camp
├── Buildings
│   └── Market_0  ← Tag: "MarketStall"
```

### Example 3: Multiple Markets
```
Camp
├── FlowerMarket_0  ← Tag: "MarketStall" (for Snow)
├── HunterMarket_0  ← Tag: "HunterMarket" (for other NPC)
```

**Note:** Nếu có nhiều markets, dùng tag khác nhau hoặc filter theo role trong FindMarketLocation()

---

## 🔧 Advanced: Filter Market by NPC Role

Nếu bạn muốn mỗi NPC tìm market riêng theo role:

```csharp
void FindMarketLocation()
{
    // Find market based on NPC role
    string marketName = GetMarketNameForRole(npcRole);
    
    GameObject camp = GameObject.Find("Camp");
    if (camp != null)
    {
        Transform marketTransform = camp.transform.Find(marketName);
        if (marketTransform != null)
        {
            marketStallLocation = marketTransform.transform;
        }
    }
}

string GetMarketNameForRole(string role)
{
    switch (role.ToLower())
    {
        case "flower_merchant":
            return "FlowerMarket_0";
        case "hunter":
            return "HunterMarket_0";
        case "blacksmith":
            return "BlacksmithMarket_0";
        default:
            return "Market_0";
    }
}
```

---

## 📝 Checklist

Setup Camp Prefab:
- [ ] Mở Camp.prefab
- [ ] Tìm Market GameObject
- [ ] Tag Market = "MarketStall"
- [ ] Save prefab

Setup NPC Prefab:
- [ ] Add NPCTrader component
- [ ] Set npcRole = "flower_merchant"
- [ ] Set isTrader = true (NPCRoutineAI)
- [ ] Leave marketStallLocation EMPTY (auto-find)
- [ ] Save prefab

Test in Game:
- [ ] Play game
- [ ] Check Console for "Auto-assigned market location"
- [ ] Wait until 8:00 AM
- [ ] Verify NPC walks to Market
- [ ] Check shop indicator appears

---

## 🎯 Summary

**Key Points:**
1. ✅ Market được spawn tự động trong Camp bởi MapGenerator
2. ✅ NPCTrader tự động tìm Market location khi Start()
3. ✅ Không cần manual assign trong Inspector
4. ✅ NPC tự động đi tới Market vào 8h-12h
5. ✅ Shop indicator xuất hiện khi NPC ở Market

**Requirements:**
- Camp prefab phải có Market GameObject
- Market GameObject nên được tag "MarketStall" (recommended)
- Hoặc tên Market chứa "Market" (fallback)

---

**Last Updated**: 2025-11-24  
**Version**: 1.1 - Auto-find Market  
**Related Files**: 
- NPCTrader.cs
- NPCRoutineAI.cs
- MapGenerator.cs
- Camp.prefab
