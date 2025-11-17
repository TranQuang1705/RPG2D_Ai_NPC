from flask import Flask, jsonify, request
from flask_cors import CORS
import mysql.connector

app = Flask(__name__)
CORS(app)  # Cho phép Unity (localhost) truy cập

# ⚙️ Cấu hình MySQL
db_config = {
    "host": "localhost",
    "user": "root",
    "password": "17052003qQ@",   # đổi nếu bạn có mật khẩu khác
    "database": "GameRPG2d"
}

# 🧱 Hàm kết nối MySQL
def get_db_connection():
    conn = mysql.connector.connect(**db_config)
    return conn


# ================================
# 📦 API 1 — Lấy danh sách item
# ================================
@app.route("/items", methods=["GET"])
def get_items():
    conn = get_db_connection()
    cursor = conn.cursor(dictionary=True)
    cursor.execute("SELECT * FROM items")
    items = cursor.fetchall()
    cursor.close()
    conn.close()
    return jsonify(items)


# ================================
# 🎒 API 2 — Lấy inventory của player
# ================================
@app.route("/inventory/<int:player_id>", methods=["GET"])
def get_inventory(player_id):
    conn = get_db_connection()
    cursor = conn.cursor(dictionary=True)
    cursor.execute("""
        SELECT i.item_name, i.icon_path, inv.quantity, inv.slot_index
        FROM inventory inv
        JOIN items i ON inv.item_id = i.item_id
        WHERE inv.player_id = %s
        ORDER BY inv.slot_index ASC
    """, (player_id,))
    result = cursor.fetchall()
    cursor.close()
    conn.close()
    return jsonify(result)


# ================================
# ➕ API 3 — Thêm vật phẩm vào inventory
# ================================
@app.route("/inventory/add", methods=["POST"])
def add_item_to_inventory():
    data = request.get_json()
    player_id = data.get("player_id")
    item_id = data.get("item_id")
    quantity = data.get("quantity", 1)
    slot_index = data.get("slot_index", 0)

    conn = get_db_connection()
    cursor = conn.cursor()
    cursor.execute("""
        INSERT INTO inventory (player_id, bag_id, item_id, quantity, slot_index)
        VALUES (%s, (SELECT current_bag_id FROM players WHERE player_id=%s), %s, %s, %s)
    """, (player_id, player_id, item_id, quantity, slot_index))
    conn.commit()
    cursor.close()
    conn.close()
    return jsonify({"status": "success", "message": "Item added!"})


# ================================
# 🔄 API 4 — Cập nhật số lượng item
# ================================
@app.route("/inventory/update", methods=["PUT"])
def update_inventory():
    data = request.get_json()
    inv_id = data.get("inventory_id")
    quantity = data.get("quantity")

    conn = get_db_connection()
    cursor = conn.cursor()
    cursor.execute("UPDATE inventory SET quantity=%s WHERE inventory_id=%s", (quantity, inv_id))
    conn.commit()
    cursor.close()
    conn.close()
    return jsonify({"status": "success", "message": "Inventory updated!"})

# ================================
# 🧍 API 5 — Lấy danh sách người chơi
# ================================
@app.route("/players", methods=["GET"])
def get_players():
    conn = get_db_connection()
    cursor = conn.cursor(dictionary=True)
    cursor.execute("""
        SELECT 
            player_id, 
            player_name, 
            gold, 
            level, 
            exp, 
            exp_to_next_level, 
            current_bag_id,
            prefab_path,
            created_at,
            updated_at
        FROM players
    """)
    players = cursor.fetchall()
    cursor.close()
    conn.close()
    return jsonify(players)


# ================================
# 🧾 API 6 — Lấy chi tiết 1 người chơi cụ thể
# ================================
@app.route("/players/<int:player_id>", methods=["GET"])
def get_player_by_id(player_id):
    conn = get_db_connection()
    cursor = conn.cursor(dictionary=True)
    cursor.execute("""
        SELECT 
            player_id, 
            player_name, 
            gold, 
            level, 
            exp, 
            exp_to_next_level, 
            current_bag_id,
            prefab_path,
            created_at,
            updated_at
        FROM players
        WHERE player_id = %s
    """, (player_id,))
    player = cursor.fetchone()
    cursor.close()
    conn.close()

    if player:
        return jsonify(player)
    else:
        return jsonify({"error": "Player not found"}), 404
    
@app.route("/bags", methods=["GET"])
def get_bags():
    conn = get_db_connection()
    cursor = conn.cursor(dictionary=True)
    cursor.execute("""
        SELECT 
            bag_id, 
            bag_name, 
            slot_count, 
            width, 
            height, 
            description, 
            equipable, 
            rarity, 
            value, 
            model_path, 
            created_at
        FROM bags
    """)
    bags = cursor.fetchall()
    cursor.close()
    conn.close()
    return jsonify(bags)


# ================================
# 📜 QUEST SYSTEM APIs
# ================================

# API 7 — Lấy danh sách tất cả các quest
@app.route("/quests", methods=["GET"])
def get_quests():
    conn = get_db_connection()
    cursor = conn.cursor(dictionary=True)
    cursor.execute("""
        SELECT 
            quest_id, 
            quest_name, 
            description, 
            quest_type, 
            min_level, 
            reward_gold, 
            reward_exp, 
            reward_item_id, 
            is_repeatable, 
            difficulty, 
            created_at
        FROM quests
    """)
    quests = cursor.fetchall()
    cursor.close()
    conn.close()
    return jsonify(quests)


# API 8 — Lấy chi tiết 1 quest cụ thể
@app.route("/quests/<int:quest_id>", methods=["GET"])
def get_quest_by_id(quest_id):
    conn = get_db_connection()
    cursor = conn.cursor(dictionary=True)
    cursor.execute("""
        SELECT 
            quest_id, 
            quest_name, 
            description, 
            quest_type, 
            min_level, 
            reward_gold, 
            reward_exp, 
            reward_item_id, 
            is_repeatable, 
            difficulty, 
            created_at
        FROM quests
        WHERE quest_id = %s
    """, (quest_id,))
    quest = cursor.fetchone()
    cursor.close()
    conn.close()

    if quest:
        return jsonify(quest)
    else:
        return jsonify({"error": "Quest not found"}), 404


# API 9 — Lấy danh sách các mục tiêu của quest
@app.route("/quest_objectives", methods=["GET"])
def get_quest_objectives():
    quest_id = request.args.get("quest_id", type=int)
    
    conn = get_db_connection()
    cursor = conn.cursor(dictionary=True)
    
    if quest_id:
        cursor.execute("""
            SELECT 
                objective_id, 
                quest_id, 
                objective_type, 
                target_id, 
                target_name, 
                quantity, 
                description
            FROM quest_objectives
            WHERE quest_id = %s
        """, (quest_id,))
    else:
        cursor.execute("""
            SELECT 
                objective_id, 
                quest_id, 
                objective_type, 
                target_id, 
                target_name, 
                quantity, 
                description
            FROM quest_objectives
        """)
    
    objectives = cursor.fetchall()
    cursor.close()
    conn.close()
    return jsonify(objectives)


# API 10 — Lấy danh sách quest của người chơi
@app.route("/player_quests", methods=["GET"])
def get_player_quests():
    player_id = request.args.get("player_id", type=int)
    
    if not player_id:
        return jsonify({"error": "player_id is required"}), 400
    
    conn = get_db_connection()
    cursor = conn.cursor(dictionary=True)
    cursor.execute("""
        SELECT 
            player_quest_id, 
            player_id, 
            quest_id, 
            status, 
            accepted_at, 
            completed_at
        FROM player_quests
        WHERE player_id = %s
    """, (player_id,))
    player_quests = cursor.fetchall()
    cursor.close()
    conn.close()
    return jsonify(player_quests)


# API 11 — Lấy tiến độ quest của người chơi
@app.route("/quest_progress", methods=["GET"])
def get_quest_progress():
    player_id = request.args.get("player_id", type=int)
    quest_id = request.args.get("quest_id", type=int)
    
    if not player_id:
        return jsonify({"error": "player_id is required"}), 400
    
    conn = get_db_connection()
    cursor = conn.cursor(dictionary=True)
    
    if quest_id:
        cursor.execute("""
            SELECT 
                player_id, 
                quest_id, 
                objective_id, 
                current_count
            FROM player_quest_progress
            WHERE player_id = %s AND quest_id = %s
        """, (player_id, quest_id))
    else:
        cursor.execute("""
            SELECT 
                player_id, 
                quest_id, 
                objective_id, 
                current_count
            FROM player_quest_progress
            WHERE player_id = %s
        """, (player_id,))
    
    progress = cursor.fetchall()
    cursor.close()
    conn.close()
    return jsonify(progress)


# API 12 — Lấy danh sách quest của NPC
@app.route("/npc_quests", methods=["GET"])
def get_npc_quests():
    npc_id = request.args.get("npc_id", type=int)
    
    conn = get_db_connection()
    cursor = conn.cursor(dictionary=True)
    
    if npc_id:
        cursor.execute("""
            SELECT npc_id, quest_id
            FROM npc_quests
            WHERE npc_id = %s
        """, (npc_id,))
    else:
        cursor.execute("""
            SELECT npc_id, quest_id
            FROM npc_quests
        """)
    
    npc_quests = cursor.fetchall()
    cursor.close()
    conn.close()
    return jsonify(npc_quests)


# API 13 — Nhận quest (Accept quest)
@app.route("/player_quests/accept", methods=["POST"])
def accept_quest():
    data = request.form if request.form else request.get_json()
    player_id = data.get("player_id")
    quest_id = data.get("quest_id")
    status = data.get("status", "in_progress")
    
    if not player_id or not quest_id:
        return jsonify({"error": "player_id and quest_id are required"}), 400
    
    conn = get_db_connection()
    cursor = conn.cursor()
    
    try:
        # Insert player quest
        cursor.execute("""
            INSERT INTO player_quests (player_id, quest_id, status, accepted_at)
            VALUES (%s, %s, %s, NOW())
            ON DUPLICATE KEY UPDATE status = %s, accepted_at = NOW()
        """, (player_id, quest_id, status, status))
        
        # Initialize quest progress for all objectives
        cursor.execute("""
            INSERT INTO player_quest_progress (player_id, quest_id, objective_id, current_count)
            SELECT %s, %s, objective_id, 0
            FROM quest_objectives
            WHERE quest_id = %s
            ON DUPLICATE KEY UPDATE current_count = current_count
        """, (player_id, quest_id, quest_id))
        
        conn.commit()
        cursor.close()
        conn.close()
        
        return jsonify({"status": "success", "message": "Quest accepted!"})
    except Exception as e:
        conn.rollback()
        cursor.close()
        conn.close()
        return jsonify({"error": str(e)}), 500


# API 14 — Cập nhật tiến độ quest
@app.route("/quest_progress/update", methods=["POST"])
def update_quest_progress():
    data = request.form if request.form else request.get_json()
    player_id = data.get("player_id")
    quest_id = data.get("quest_id")
    objective_id = data.get("objective_id")
    current_count = data.get("current_count")
    
    if not all([player_id, quest_id, objective_id, current_count is not None]):
        return jsonify({"error": "All fields are required"}), 400
    
    conn = get_db_connection()
    cursor = conn.cursor()
    
    try:
        cursor.execute("""
            INSERT INTO player_quest_progress (player_id, quest_id, objective_id, current_count)
            VALUES (%s, %s, %s, %s)
            ON DUPLICATE KEY UPDATE current_count = %s
        """, (player_id, quest_id, objective_id, current_count, current_count))
        
        conn.commit()
        cursor.close()
        conn.close()
        
        return jsonify({"status": "success", "message": "Quest progress updated!"})
    except Exception as e:
        conn.rollback()
        cursor.close()
        conn.close()
        return jsonify({"error": str(e)}), 500


# API 15 — Hoàn thành quest và nhận thưởng
@app.route("/player_quests/complete", methods=["POST"])
def complete_quest():
    data = request.form if request.form else request.get_json()
    player_id = data.get("player_id")
    quest_id = data.get("quest_id")
    
    if not player_id or not quest_id:
        return jsonify({"error": "player_id and quest_id are required"}), 400
    
    conn = get_db_connection()
    cursor = conn.cursor(dictionary=True)
    
    try:
        # Get quest rewards
        cursor.execute("""
            SELECT reward_gold, reward_exp, reward_item_id
            FROM quests
            WHERE quest_id = %s
        """, (quest_id,))
        quest = cursor.fetchone()
        
        if not quest:
            return jsonify({"error": "Quest not found"}), 404
        
        # Update player quest status
        cursor.execute("""
            UPDATE player_quests
            SET status = 'completed', completed_at = NOW()
            WHERE player_id = %s AND quest_id = %s
        """, (player_id, quest_id))
        
        # Give rewards to player
        if quest['reward_gold'] > 0:
            cursor.execute("""
                UPDATE players
                SET gold = gold + %s
                WHERE player_id = %s
            """, (quest['reward_gold'], player_id))
        
        if quest['reward_exp'] > 0:
            cursor.execute("""
                UPDATE players
                SET exp = exp + %s
                WHERE player_id = %s
            """, (quest['reward_exp'], player_id))
        
        # TODO: Add item reward if reward_item_id > 0
        if quest['reward_item_id'] and quest['reward_item_id'] > 0:
            # Find first available slot
            cursor.execute("""
                SELECT COALESCE(MAX(slot_index), 0) + 1 as next_slot
                FROM inventory
                WHERE player_id = %s
            """, (player_id,))
            result = cursor.fetchone()
            next_slot = result['next_slot'] if result else 1
            
            cursor.execute("""
                INSERT INTO inventory (player_id, bag_id, item_id, quantity, slot_index)
                VALUES (%s, 1, %s, 1, %s)
            """, (player_id, quest['reward_item_id'], next_slot))
        
        # Reset quest progress
        cursor.execute("""
            UPDATE player_quest_progress
            SET current_count = 0
            WHERE player_id = %s AND quest_id = %s
        """, (player_id, quest_id))
        
        conn.commit()
        cursor.close()
        conn.close()
        
        return jsonify({
            "status": "success", 
            "message": "Quest completed!",
            "rewards": {
                "gold": quest['reward_gold'],
                "exp": quest['reward_exp'],
                "item_id": quest['reward_item_id']
            }
        })
    except Exception as e:
        conn.rollback()
        cursor.close()
        conn.close()
        return jsonify({"error": str(e)}), 500
@app.route("/update_progress", methods=["POST"])
def update_progress():
    """
    Cập nhật tiến trình các quest dạng 'collect' khi người chơi nhặt item.
    JSON body:
    {
        "player_id": 1,
        "item_id": 2,
        "amount": 1
    }
    """
    data = request.get_json()
    player_id = data.get("player_id")
    item_id = data.get("item_id")
    amount = data.get("amount", 1)

    if not player_id or not item_id:
        return jsonify({"error": "player_id và item_id là bắt buộc"}), 400

    conn = get_db_connection()
    cursor = conn.cursor(dictionary=True)

    try:
        # 🔍 Tìm các quest mà người chơi đang làm có mục tiêu 'collect' đúng item này
        cursor.execute("""
            SELECT 
                pq.quest_id,
                pqp.objective_id,
                qo.quantity,
                pqp.current_count
            FROM player_quest_progress pqp
            JOIN quest_objectives qo ON qo.objective_id = pqp.objective_id
            JOIN player_quests pq ON pq.quest_id = pqp.quest_id
            WHERE pq.player_id = %s
              AND pq.status = 'in_progress'
              AND qo.objective_type = 'collect'
              AND qo.target_id = %s
        """, (player_id, item_id))
        
        quests = cursor.fetchall()
        updated = []

        # 🔄 Cập nhật tiến trình từng quest
        for q in quests:
            new_count = min(q["current_count"] + amount, q["quantity"])
            cursor.execute("""
                UPDATE player_quest_progress
                SET current_count = %s
                WHERE player_id = %s AND quest_id = %s AND objective_id = %s
            """, (new_count, player_id, q["quest_id"], q["objective_id"]))
            updated.append({
                "quest_id": q["quest_id"],
                "objective_id": q["objective_id"],
                "new_count": new_count,
                "goal": q["quantity"]
            })

        conn.commit()

        return jsonify({
            "status": "success",
            "updated": updated,
            "message": f"Updated {len(updated)} quest objectives for item {item_id}"
        })
    except Exception as e:
        conn.rollback()
        return jsonify({"error": str(e)}), 500
    finally:
        cursor.close()
        conn.close()
# ================================
# 🚀 Chạy server
# ================================
if __name__ == "__main__":
    app.run(host="0.0.0.0", port=5002, debug=True)
