using System.Collections.Generic;
using UnityEngine;

public class GlobalItemManager : MonoBehaviour
{
    // 🔹 Danh sách item toàn cục, tra cứu nhanh theo ID
    public static Dictionary<int, ItemData> Items = new();

    // 🔹 Lưu danh sách item từ API sau khi tải
    public static void RegisterItems(List<ItemData> itemList)
    {
        Items.Clear();
        foreach (var item in itemList)
        {
            if (!Items.ContainsKey(item.item_id))
            {
                Items[item.item_id] = item;
            }
        }

        Debug.Log($"📦 Registered {Items.Count} items into ItemManager.");
    }

    // 🔹 Lấy item theo ID
    public static ItemData GetItemById(int id)
    {
        if (Items.TryGetValue(id, out var item))
            return item;

        Debug.LogWarning($"⚠️ Item ID {id} not found!");
        return null;
    }

    // 🔹 Tìm item theo tên
    public static ItemData GetItemByName(string name)
    {
        foreach (var kv in Items)
        {
            if (kv.Value.item_name.ToLower() == name.ToLower())
                return kv.Value;
        }
        Debug.LogWarning($"⚠️ Item '{name}' not found!");
        return null;
    }

    // 🔹 Sinh vật phẩm ra scene (ví dụ spawn khi rơi đồ hoặc thu hoạch)
    public static GameObject SpawnItemById(int id, Vector3 position)
    {
        var item = GetItemById(id);
        if (item != null && item.prefab != null)
        {
            GameObject obj = GameObject.Instantiate(item.prefab, position, Quaternion.identity);
            Debug.Log($"🍎 Spawned item '{item.item_name}' at {position}");
            return obj;
        }
        Debug.LogWarning($"⚠️ Không thể spawn item {id}, prefab null hoặc không tồn tại!");
        return null;
    }
}
