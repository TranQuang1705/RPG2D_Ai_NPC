# 🌐 API CURL Commands - Complete List

**Server URL**: `http://127.0.0.1:5002`

---

## 📦 ITEMS APIs

### API 1 — Get all items
```bash
curl -X GET http://127.0.0.1:5002/items
```

---

## 🎒 INVENTORY APIs

### API 2 — Get player inventory
```bash
curl -X GET http://127.0.0.1:5002/inventory/1
```

### API 3 — Add item to inventory
```bash
curl -X POST http://127.0.0.1:5002/inventory/add \
  -H "Content-Type: application/json" \
  -d '{
    "player_id": 1,
    "item_id": 2,
    "quantity": 5,
    "slot_index": 1
  }'
```

### API 4 — Update inventory quantity
```bash
curl -X PUT http://127.0.0.1:5002/inventory/update \
  -H "Content-Type: application/json" \
  -d '{
    "inventory_id": 1,
    "quantity": 10
  }'
```

---

## 🧍 PLAYER APIs

### API 5 — Get all players
```bash
curl -X GET http://127.0.0.1:5002/players
```

### API 6 — Get player by ID
```bash
curl -X GET http://127.0.0.1:5002/players/1
```

### API 6.1 — Update player info
```bash
curl -X PUT http://127.0.0.1:5002/players/1 \
  -H "Content-Type: application/json" \
  -d '{
    "level": 5,
    "exp": 250,
    "exp_to_next_level": 500,
    "gold": 1000
  }'
```

---

## 🎒 BAG APIs

### API — Get all bags
```bash
curl -X GET http://127.0.0.1:5002/bags
```

---

## 📜 QUEST APIs

### API 7 — Get all quests
```bash
curl -X GET http://127.0.0.1:5002/quests
```

### API 8 — Get quest by ID
```bash
curl -X GET http://127.0.0.1:5002/quests/1
```

### API 9 — Get quest objectives
```bash
# All objectives
curl -X GET http://127.0.0.1:5002/quest_objectives

# Objectives for specific quest
curl -X GET "http://127.0.0.1:5002/quest_objectives?quest_id=1"
```

### API 10 — Get player quests
```bash
curl -X GET "http://127.0.0.1:5002/player_quests?player_id=1"
```

### API 11 — Get quest progress
```bash
# All progress for player
curl -X GET "http://127.0.0.1:5002/quest_progress?player_id=1"

# Progress for specific quest
curl -X GET "http://127.0.0.1:5002/quest_progress?player_id=1&quest_id=1"
```

### API 12 — Get NPC quests
```bash
# All NPC quests
curl -X GET http://127.0.0.1:5002/npc_quests

# Quests for specific NPC
curl -X GET "http://127.0.0.1:5002/npc_quests?npc_id=1"
```

### API 13 — Accept quest
```bash
curl -X POST http://127.0.0.1:5002/player_quests/accept \
  -H "Content-Type: application/json" \
  -d '{
    "player_id": 1,
    "quest_id": 1,
    "status": "in_progress"
  }'
```

### API 14 — Update quest progress
```bash
curl -X POST http://127.0.0.1:5002/quest_progress/update \
  -H "Content-Type: application/json" \
  -d '{
    "player_id": 1,
    "quest_id": 1,
    "objective_id": 1,
    "current_count": 5
  }'
```

### API 15 — Complete quest
```bash
curl -X POST http://127.0.0.1:5002/player_quests/complete \
  -H "Content-Type: application/json" \
  -d '{
    "player_id": 1,
    "quest_id": 1
  }'
```

### API — Update progress (collect items)
```bash
curl -X POST http://127.0.0.1:5002/update_progress \
  -H "Content-Type: application/json" \
  -d '{
    "player_id": 1,
    "item_id": 2,
    "amount": 1
  }'
```

---

## 💰 COIN APIs

### API 16 — Get all coins
```bash
curl -X GET http://127.0.0.1:5002/coins
```

### API 17 — Get player coins
```bash
curl -X GET "http://127.0.0.1:5002/player_coins?player_id=1"
```

### API 18 — Add coins to player
```bash
curl -X POST http://127.0.0.1:5002/player_coins/add \
  -H "Content-Type: application/json" \
  -d '{
    "player_id": 1,
    "coin_id": 1,
    "amount": 100
  }'
```

### API 19 — Update player coins
```bash
curl -X PUT http://127.0.0.1:5002/player_coins/update \
  -H "Content-Type: application/json" \
  -d '{
    "player_id": 1,
    "coin_id": 1,
    "amount": 50
  }'
```

### API 20 — Remove coins from player
```bash
curl -X POST http://127.0.0.1:5002/player_coins/remove \
  -H "Content-Type: application/json" \
  -d '{
    "player_id": 1,
    "coin_id": 1,
    "amount": 10
  }'
```

---

## 💎 EXP ITEM APIs

### API 21 — Get all EXP items
```bash
curl -X GET http://127.0.0.1:5002/exp_items
```

### API 22 — Get EXP item by ID
```bash
curl -X GET http://127.0.0.1:5002/exp_items/1
```

### API 23 — Get EXP item by name
```bash
curl -X GET http://127.0.0.1:5002/exp_items/name/Ember%20EXP
```

---

## 🏪 SHOP APIs (NEW)

### API 24 — Get NPC shop inventory
```bash
curl -X GET "http://127.0.0.1:5002/npc_shop_inventory?npc_id=1"
```

### API 25 — Update shop stock
```bash
curl -X POST http://127.0.0.1:5002/npc_shop_inventory/update_stock \
  -H "Content-Type: application/json" \
  -d '{
    "npc_id": 1,
    "item_id": 2,
    "stock": 15
  }'
```

### API 26 — Buy item from shop
```bash
curl -X POST http://127.0.0.1:5002/shop/buy \
  -H "Content-Type: application/json" \
  -d '{
    "player_id": 1,
    "npc_id": 1,
    "item_id": 2,
    "quantity": 1
  }'
```

---

## 🏘️ NPC APIs (NEW)

### API 27 — Get all NPCs
```bash
curl -X GET http://127.0.0.1:5002/npcs
```

### API 28 — Get NPC by ID
```bash
curl -X GET http://127.0.0.1:5002/npcs/1
```

---

## 🧪 Testing Examples

### Test complete shop flow:

```bash
# 1. Check NPC Snow's shop inventory
curl -X GET "http://127.0.0.1:5002/npc_shop_inventory?npc_id=1"

# 2. Check player's coins before buying
curl -X GET "http://127.0.0.1:5002/player_coins?player_id=1"

# 3. Buy 2 Daisy Flowers from Snow
curl -X POST http://127.0.0.1:5002/shop/buy \
  -H "Content-Type: application/json" \
  -d '{
    "player_id": 1,
    "npc_id": 1,
    "item_id": 2,
    "quantity": 2
  }'

# 4. Check player's coins after buying
curl -X GET "http://127.0.0.1:5002/player_coins?player_id=1"

# 5. Check player's inventory
curl -X GET http://127.0.0.1:5002/inventory/1
```

### Test quest flow:

```bash
# 1. Get available quests
curl -X GET http://127.0.0.1:5002/quests

# 2. Accept quest
curl -X POST http://127.0.0.1:5002/player_quests/accept \
  -H "Content-Type: application/json" \
  -d '{
    "player_id": 1,
    "quest_id": 1,
    "status": "in_progress"
  }'

# 3. Update progress (collect 1 flower)
curl -X POST http://127.0.0.1:5002/update_progress \
  -H "Content-Type: application/json" \
  -d '{
    "player_id": 1,
    "item_id": 2,
    "amount": 1
  }'

# 4. Check progress
curl -X GET "http://127.0.0.1:5002/quest_progress?player_id=1&quest_id=1"

# 5. Complete quest (when all objectives done)
curl -X POST http://127.0.0.1:5002/player_quests/complete \
  -H "Content-Type: application/json" \
  -d '{
    "player_id": 1,
    "quest_id": 1
  }'
```

### Test coin management:

```bash
# 1. Check all coin types
curl -X GET http://127.0.0.1:5002/coins

# 2. Add 100 Obal to player
curl -X POST http://127.0.0.1:5002/player_coins/add \
  -H "Content-Type: application/json" \
  -d '{
    "player_id": 1,
    "coin_id": 1,
    "amount": 100
  }'

# 3. Check player's coins
curl -X GET "http://127.0.0.1:5002/player_coins?player_id=1"

# 4. Remove 10 Obal
curl -X POST http://127.0.0.1:5002/player_coins/remove \
  -H "Content-Type: application/json" \
  -d '{
    "player_id": 1,
    "coin_id": 1,
    "amount": 10
  }'
```

---

## 📊 API Summary Table

| # | Method | Endpoint | Description |
|---|--------|----------|-------------|
| 1 | GET | `/items` | Get all items |
| 2 | GET | `/inventory/{player_id}` | Get player inventory |
| 3 | POST | `/inventory/add` | Add item to inventory |
| 4 | PUT | `/inventory/update` | Update inventory quantity |
| 5 | GET | `/players` | Get all players |
| 6 | GET | `/players/{player_id}` | Get player by ID |
| 6.1 | PUT | `/players/{player_id}` | Update player info |
| - | GET | `/bags` | Get all bags |
| 7 | GET | `/quests` | Get all quests |
| 8 | GET | `/quests/{quest_id}` | Get quest by ID |
| 9 | GET | `/quest_objectives` | Get quest objectives |
| 10 | GET | `/player_quests` | Get player quests |
| 11 | GET | `/quest_progress` | Get quest progress |
| 12 | GET | `/npc_quests` | Get NPC quests |
| 13 | POST | `/player_quests/accept` | Accept quest |
| 14 | POST | `/quest_progress/update` | Update quest progress |
| 15 | POST | `/player_quests/complete` | Complete quest |
| - | POST | `/update_progress` | Update progress (collect) |
| 16 | GET | `/coins` | Get all coins |
| 17 | GET | `/player_coins` | Get player coins |
| 18 | POST | `/player_coins/add` | Add coins |
| 19 | PUT | `/player_coins/update` | Update coins |
| 20 | POST | `/player_coins/remove` | Remove coins |
| 21 | GET | `/exp_items` | Get all EXP items |
| 22 | GET | `/exp_items/{exp_id}` | Get EXP item by ID |
| 23 | GET | `/exp_items/name/{name}` | Get EXP item by name |
| 24 | GET | `/npc_shop_inventory` | Get NPC shop inventory |
| 25 | POST | `/npc_shop_inventory/update_stock` | Update shop stock |
| 26 | POST | `/shop/buy` | Buy item from shop |
| 27 | GET | `/npcs` | Get all NPCs |
| 28 | GET | `/npcs/{npc_id}` | Get NPC by ID |

**Total**: 28 API endpoints

---

## 🔍 Query Parameters Guide

### `/quest_objectives`
- `?quest_id=1` - Get objectives for specific quest

### `/player_quests`
- `?player_id=1` - Get quests for specific player (required)

### `/quest_progress`
- `?player_id=1` - Get progress for player (required)
- `?player_id=1&quest_id=1` - Get progress for specific quest

### `/npc_quests`
- `?npc_id=1` - Get quests for specific NPC

### `/player_coins`
- `?player_id=1` - Get coins for specific player (required)

### `/npc_shop_inventory`
- `?npc_id=1` - Get shop inventory for specific NPC (required)

---

## 💡 Tips

### Pretty print JSON output:
```bash
curl -X GET http://127.0.0.1:5002/items | python -m json.tool
```

### Save response to file:
```bash
curl -X GET http://127.0.0.1:5002/npcs > npcs.json
```

### Check HTTP status:
```bash
curl -i -X GET http://127.0.0.1:5002/players/1
```

### Verbose output (debug):
```bash
curl -v -X GET http://127.0.0.1:5002/coins
```

---

**Last Updated**: 2025-11-24  
**Server**: Python Flask + MySQL  
**Port**: 5002
