using UnityEngine;
using UnityEngine.Networking;
using System.Collections;
using System.Collections.Generic;

// Dữ liệu ánh xạ 1 item từ JSON
[System.Serializable]
public class ItemData
{
    public int item_id;
    public string item_name;
    public string item_type;
    public string description;
    public string rarity;
    public float weight;
    public int value;
    public bool stackable;
    public int max_stack;
    public bool usable;
    public bool equipable;
    public string effect_type;
    public float effect_value;
    public string target_type;
    public string category;
    public string icon_path;
    public string model_path;
    public string created_at;
    public string updated_at;

    // 🔹 Gán asset sau khi load từ Resources
    [System.NonSerialized] public Sprite icon;
    [System.NonSerialized] public GameObject prefab;
}

public class ItemFetcher : MonoBehaviour
{
    private string apiUrl = "http://127.0.0.1:5002/items";

    IEnumerator Start()
    {
        using (UnityWebRequest req = UnityWebRequest.Get(apiUrl))
        {
            yield return req.SendWebRequest();

            if (req.result == UnityWebRequest.Result.Success)
            {
                Debug.Log("✅ Dữ liệu nhận được từ Flask:\n" + req.downloadHandler.text);

                // Parse JSON mảng → List<ItemData>
                List<ItemData> items = JsonHelper.FromJsonList<ItemData>(req.downloadHandler.text);

                // 🔸 THÊM NGAY TẠI ĐÂY (ánh xạ icon và prefab)
                foreach (var item in items)
                {
                    string iconName = "";
                    string prefabName = "";
                    string iconPath = "";
                    string prefabPath = "";

                    // 🔹 Gán icon (Sprite)
                    if (!string.IsNullOrEmpty(item.icon_path))
                    {
                        iconName = System.IO.Path.GetFileNameWithoutExtension(item.icon_path);
                        iconPath = $"Icons/{iconName}";
                        item.icon = Resources.Load<Sprite>(iconPath);

                        Debug.Log($"🖼️ Trying to load ICON → {iconPath} | Found={(item.icon != null ? "✅ YES" : "❌ NO")}");
                    }
                    else
                    {
                        Debug.Log($"⚠️ Item {item.item_name} has no icon_path.");
                    }

                    // 🔹 Gán prefab (GameObject)
                    if (!string.IsNullOrEmpty(item.model_path))
                    {
                        prefabName = System.IO.Path.GetFileNameWithoutExtension(item.model_path);
                        prefabPath = $"Prefabs/{prefabName}";
                        item.prefab = Resources.Load<GameObject>(prefabPath);

                        Debug.Log($"🎁 Trying to load PREFAB → {prefabPath} | Found={(item.prefab != null ? "✅ YES" : "❌ NO")}");
                    }
                    else
                    {
                        Debug.Log($"⚠️ Item {item.item_name} has no model_path.");
                    }

                    // 🔹 Tổng kết từng item
                    Debug.Log($"🧱 ITEM: {item.item_name}\n" +
                              $"   icon_path(raw): {item.icon_path}\n" +
                              $"   model_path(raw): {item.model_path}\n" +
                              $"   load_icon_path(used): {iconPath}\n" +
                              $"   load_prefab_path(used): {prefabPath}\n" +
                              $"   ✅ Icon={(item.icon ? "Loaded" : "NULL")} | Prefab={(item.prefab ? "Loaded" : "NULL")}\n");
                }


                // (Tùy chọn) Đăng ký item vào hệ thống quản lý
                GlobalItemManager.RegisterItems(items);
            }
            else
            {
                Debug.LogError("❌ Lỗi khi gọi API Flask: " + req.error);
            }
        }
    }
}
