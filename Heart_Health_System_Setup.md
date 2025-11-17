# Heart-Based Health System Setup Guide

## 📋 Overview
New health system using hearts with 3 states (empty/half/full):
- Each **full heart** = **2 HP**
- **10 HP** = **5 full hearts**
- Taking **1 damage** = losing **half a heart**

## 🎨 Sprite Requirements

You need 3 heart sprites:
1. **Heart Full** (green/filled heart)
2. **Heart Half** (half-filled heart)  
3. **Heart Empty** (outline heart)

These should be extracted from `Assets/Picture_For_AI/UIForHealth.png`

## 🔧 Unity Setup Steps

### Step 1: Extract Heart Sprites
1. Select `UIForHealth.png` in Unity
2. Set **Sprite Mode** to **Multiple**
3. Open **Sprite Editor**
4. Slice into individual sprites:
   - Heart Full (green filled)
   - Heart Half (half filled)
   - Heart Empty (outline)
5. Name them: `heart_full`, `heart_half`, `heart_empty`

### Step 2: Create Heart Prefab
1. Create new **GameObject** → **UI** → **Image**
2. Name it `HeartPrefab`
3. Set **Image** component sprite to `heart_full`
4. Adjust **RectTransform**:
   - Width: 64
   - Height: 64
5. Save as **Prefab** in `Assets/Prefabs/UI/`

### Step 3: Setup HealthContainer
1. Find **HealthContainer** in your Canvas hierarchy
2. Remove old **Health Slider** (if exists)
3. Add **HorizontalLayoutGroup** component:
   - Spacing: 5
   - Child Alignment: Middle Left
   - Child Controls Size: ✓ Width, ✓ Height
4. Add **HeartHealthUI.cs** component:
   - Assign **Heart Full** sprite
   - Assign **Heart Half** sprite
   - Assign **Heart Empty** sprite
   - Set **Hearts Container** to itself
   - Assign **Heart Prefab**

### Step 4: Setup PlayerHealth
1. Select **Player** GameObject
2. Find **PlayerHealth** component
3. Set **Max Health** = `10` (or any even number)
4. Assign **Heart Health UI** reference to HealthContainer

### Step 5: Setup Level Display
1. Find **TextLevel** in Canvas hierarchy
2. Add **PlayerLevelUI.cs** component to its parent (LevelUI)
3. Assign **Level Text** reference to TextLevel
4. Configure API settings:
   - API Base URL: `http://127.0.0.1:5002`
   - Player ID: `1`

## 🗄️ Database Structure

Player table already has level support:
```sql
CREATE TABLE players (
    player_id     INTEGER PRIMARY KEY AUTO_INCREMENT,
    player_name   VARCHAR(50) NOT NULL,
    level         INT DEFAULT 1,
    exp           INT DEFAULT 0,
    exp_to_next_level INT DEFAULT 100,
    gold          INT DEFAULT 0,
    ...
);
```

## 🧪 Testing

1. **Health System:**
   - Start game with 10 HP (5 full hearts)
   - Take damage from enemy → should lose half heart per hit
   - Heal using item → should gain half heart

2. **Level Display:**
   - Check Unity console for: `📊 Player Level loaded: 1`
   - Level should display in UI
   - Update level in database:
     ```sql
     UPDATE players SET level = 5 WHERE player_id = 1;
     ```
   - Restart game → should show level 5

## 📝 New Scripts Created

1. **HeartHealthUI.cs** - Manages heart UI display
2. **PlayerLevelUI.cs** - Fetches and displays player level
3. **PlayerHealth.cs** - Updated to use heart system

## 🔍 Debug Logs

Look for these in Unity console:
- `💚 HeartHealthUI: Initialized X hearts for Y HP`
- `💔 Player took X damage: Y/Z HP`
- `📊 Player Level loaded: X`

## ⚠️ Common Issues

**Hearts not showing?**
- Check if Heart Prefab is assigned
- Verify sprites are assigned in HeartHealthUI
- Check HorizontalLayoutGroup settings

**Level not loading?**
- Verify Flask server is running on port 5002
- Check database connection
- Look for error logs: `❌ Failed to fetch player level`

**Health not updating?**
- Ensure HeartHealthUI reference is assigned in PlayerHealth
- Check console for initialization logs

## 🎮 Controls

Health system works automatically with existing combat:
- Collision with enemies → TakeDamage()
- Use healing items → HealPlayer()
- Level loads on game start automatically
