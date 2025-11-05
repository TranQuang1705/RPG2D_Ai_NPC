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
# 🚀 Chạy server
# ================================
if __name__ == "__main__":
    app.run(host="0.0.0.0", port=5002, debug=True)
