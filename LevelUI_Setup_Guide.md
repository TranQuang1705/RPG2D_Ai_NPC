# Level UI Setup Guide (với Avatar Frame)

## 🎨 UI Structure Overview

Dựa trên `UIForHealth.png`, Level UI bao gồm:
1. **Avatar Frame** (khung tròn xanh)
2. **Race Icon** (hình sói/fox ở giữa khung)
3. **Level Text** (số cấp độ)
4. **Gold Text** (số vàng - số 100 trong hình)

## 📐 Unity UI Hierarchy Setup

### Bước 1: Tạo UI Structure

Trong Canvas, tạo hierarchy như sau:

```
Canvas
└── LevelUI (Empty GameObject)
    ├── AvatarFrame (Image - khung tròn xanh)
    │   └── RaceIcon (Image - hình sói/fox con)
    ├── LevelText (TextMeshPro - số cấp)
    └── GoldText (TextMeshPro - số vàng)
```

### Bước 2: Tách Sprites từ UIForHealth.png

1. Select `UIForHealth.png` trong Project
2. Inspector → **Sprite Mode: Multiple**
3. **Sprite Editor** → Slice thành các phần:
   - `avatar_frame` (khung tròn xanh)
   - `wolf_icon` (hình sói/fox)
   - `heart_full` (tim đầy)
   - `heart_half` (nửa tim)
   - `heart_empty` (tim rỗng)

### Bước 3: Setup LevelUI Container

1. **Create Empty GameObject** → Rename: `LevelUI`
2. **Add Component**: `PlayerLevelUI` script
3. **RectTransform** settings:
   - Anchor: Top-Left
   - Pos X: 80, Pos Y: -80
   - Width: 200, Height: 150

### Bước 4: Setup AvatarFrame

1. **Right-click LevelUI** → **UI → Image**
2. Rename: `AvatarFrame`
3. **Image Component**:
   - Source Image: `avatar_frame` sprite (khung tròn xanh)
   - Preserve Aspect: ✓
4. **RectTransform**:
   - Width: 100, Height: 100
   - Anchor: Top-Left
   - Pos X: 50, Pos Y: -50

### Bước 5: Setup RaceIcon (con sói)

1. **Right-click AvatarFrame** → **UI → Image**
2. Rename: `RaceIcon`
3. **Image Component**:
   - Source Image: `wolf_icon` sprite
   - Preserve Aspect: ✓
4. **RectTransform**:
   - Anchors: Stretch (để fill parent)
   - Left: 10, Right: 10, Top: 10, Bottom: 10
   - Or: Width: 80, Height: 80, centered

### Bước 6: Setup LevelText

1. **Right-click LevelUI** → **UI → Text - TextMeshPro**
2. Rename: `LevelText`
3. **TextMeshProUGUI Component**:
   - Text: "1" (default)
   - Font Size: 36
   - Alignment: Center
   - Color: White
   - Font Style: Bold
   - Outline: Enable (để chữ nổi bật)
4. **RectTransform**:
   - Width: 60, Height: 40
   - Pos X: 50, Pos Y: -110

### Bước 7: Setup GoldText (Optional)

1. **Right-click LevelUI** → **UI → Text - TextMeshPro**
2. Rename: `GoldText`
3. **TextMeshProUGUI Component**:
   - Text: "100"
   - Font Size: 24
   - Alignment: Center
   - Color: Gold/Yellow (#FFD700)
4. **RectTransform**:
   - Width: 80, Height: 30
   - Pos X: 50, Pos Y: -140

### Bước 8: Assign References trong PlayerLevelUI

1. Select **LevelUI** GameObject
2. Trong **PlayerLevelUI** component:
   
   **UI References:**
   - Avatar Frame → Drag `AvatarFrame` vào
   - Race Icon → Drag `RaceIcon` vào
   - Level Text → Drag `LevelText` vào
   - Gold Text → Drag `GoldText` vào
   
   **Avatar Sprites:**
   - Wolf Race Icon → Drag `wolf_icon` sprite vào
   
   **Database Settings:**
   - API Base URL: `http://127.0.0.1:5002`
   - Player ID: `1`

## 🎨 Alternative: Simplified Layout (Chỉ Level)

Nếu không muốn Gold text:

```
LevelUI
├── AvatarFrame
│   └── RaceIcon
└── LevelText
```

Chỉ cần assign:
- Avatar Frame
- Race Icon  
- Level Text
- Wolf Race Icon sprite

## 🧪 Testing

1. **Play game**
2. Check Console:
   - `Player data loaded - Level: 1, Gold: 0`
3. Level và Gold sẽ hiển thị từ database
4. Avatar sói sẽ xuất hiện trong khung

## 🗄️ Update Database Level

Để test thay đổi level:

```sql
UPDATE players SET level = 5, gold = 250 WHERE player_id = 1;
```

Restart game → Level = 5, Gold = 250

## 📊 Layout Position Suggestions

**Top-Left Corner (recommended):**
- Anchor: Top-Left
- Pos X: 80, Pos Y: -80

**Top-Right Corner:**
- Anchor: Top-Right
- Pos X: -80, Pos Y: -80

**Bottom-Left (với Hearts):**
- Anchor: Bottom-Left
- Pos X: 80, Pos Y: 80

## 🎨 Visual Enhancement Tips

1. **Add Shadow** to texts:
   - TextMeshPro → Material Preset: Distance Field (SDF)
   - Enable Shadow
   
2. **Glow effect** cho Avatar:
   - Add second Image behind AvatarFrame
   - Set color to semi-transparent green
   - Scale slightly larger (105%)

3. **Animation**:
   - Level up → Scale pulse animation
   - Gold change → Flash yellow

## ⚠️ Common Issues

**Wolf icon không hiện?**
- Check sprite được assign trong `Wolf Race Icon` field
- Verify RaceIcon Image có sprite source

**Level không load?**
- Check Console errors
- Verify Flask server đang chạy port 5002
- Test API: `http://127.0.0.1:5002/players/1`

**Text bị mờ?**
- TextMeshPro chưa import → Window → TextMeshPro → Import TMP Essentials
