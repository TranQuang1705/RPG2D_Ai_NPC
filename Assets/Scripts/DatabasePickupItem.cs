using UnityEngine;
using System.Collections;

// Gắn component này lên các prefab pickup trong game
// Tự động nhận diện item từ database và tạo ItemSO phù hợp
[RequireComponent(typeof(Collider2D))]
public class DatabasePickupItem : MonoBehaviour
{
    [Header("Database Item Configuration")]
    [Tooltip("ID của item trong database. Nếu để 0, sẽ tìm theo itemName")]
    public int databaseItemId = 0;

    [Tooltip("Tên item trong database (dùng khi databaseItemId = 0)")]
    public string databaseItemName = "";

    [Header("Pickup Settings")]
    [Min(1)] public int amount = 1;
    public string playerTag = "Player";
    public bool destroyWhenPicked = true;
    public bool autoRegisterMapping = true;

    [Header("Visual Settings")]
    [Tooltip("Tự động cập nhật sprite từ icon trong database")]
    public bool autoUpdateSprite = true;
    [Tooltip("Scale cho sprite khi được update từ database (1 = giữ nguyên)")]
    public float spriteScale = 1f;

    [Header("Visual Effects")]
    public GameObject pickupEffect;
    public float pickupEffectDuration = 2f;
    
    [Header("Audio")]
    [Tooltip("Âm thanh khi nhặt item (optional)")]
    public AudioClip pickupSound;

    // Runtime data
    private DatabaseItem databaseItem;
    private ItemSO runtimeItemSO;
    private bool isInitialized = false;
    private AudioSource audioSource;

    void Awake()
    {
        // Setup AudioSource nếu cần
        audioSource = GetComponent<AudioSource>();
        if (audioSource == null && pickupSound != null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
            audioSource.playOnAwake = false;
        }
    }

    void Start()
    {
        // Đợi cho DatabaseItemManager tải database xong
        if (!DatabaseItemManager.Instance.IsDatabaseLoaded())
        {
            StartCoroutine(WaitForDatabaseLoad());
        }
        else
        {
            InitializeFromDatabase();
        }
    }

    IEnumerator WaitForDatabaseLoad()
    {

        while (!DatabaseItemManager.Instance.IsDatabaseLoaded())
        {
            yield return new WaitForSeconds(0.5f);
        }

        Debug.Log($"📚 {name}: Database loaded, initializing item...");
        InitializeFromDatabase();
    }

    void InitializeFromDatabase()
    {
        // Lấy database item
        if (databaseItemId > 0)
        {
            databaseItem = DatabaseItemManager.Instance.GetDatabaseItem(databaseItemId);
        }
        else if (!string.IsNullOrEmpty(databaseItemName))
        {
            databaseItem = DatabaseItemManager.Instance.GetDatabaseItemByName(databaseItemName);
        }
        else
        {
            // Nếu không có ID hoặc tên, thử lấy từ tên object
            databaseItem = DatabaseItemManager.Instance.GetDatabaseItemByName(gameObject.name);
        }

        if (databaseItem == null)
        {
            Debug.LogError($"❌ {name}: Could not find database item! ID={databaseItemId}, Name='{databaseItemName}'");
            return;
        }

        Debug.Log($"✅ {name}: Found database item: {databaseItem.item_name} (ID: {databaseItem.item_id})");

        // Tạo hoặc tìm ItemSO tương ứng
        runtimeItemSO = DatabaseItemManager.Instance.FindItemSO(databaseItem.item_id);

        if (runtimeItemSO == null)
        {
            // Tạo ItemSO mới từ database
            runtimeItemSO = DatabaseItemManager.Instance.CreateItemSOFromDatabase(databaseItem);

            if (autoRegisterMapping)
            {
                DatabaseItemManager.Instance.RegisterItemMapping(databaseItem.item_id, runtimeItemSO);
            }
        }


        UpdateVisuals();

        isInitialized = true;
    }

    void UpdateVisuals()
    {
        // Cập nhật sprite nếu prefab có SpriteRenderer và autoUpdateSprite = true
        if (!autoUpdateSprite)
        {
            return;
        }

        var spriteRenderer = GetComponent<SpriteRenderer>();
        if (spriteRenderer != null && runtimeItemSO?.icon != null)
        {
            spriteRenderer.sprite = runtimeItemSO.icon;

            // Áp dụng scale nếu được thiết lập
            if (spriteScale != 1f)
            {
                transform.localScale = new Vector3(spriteScale, spriteScale, 1f);
                Debug.Log($"🖼️ {name}: Updated sprite with ItemSO icon (scale: {spriteScale})");
            }
            else
            {
                Debug.Log($"🖼️ {name}: Updated sprite with ItemSO icon");
            }
        }

        // Cập nhật model/prefab nếu cần
        if (runtimeItemSO?.prefab != null)
        {
            // Có thể instantiate child prefabs ở đây nếu cần
        }
    }



    void OnTriggerEnter2D(Collider2D other)
    {
        if (!other.CompareTag(playerTag) || !isInitialized)
            return;

        if (InventorySystem.Instance == null)
        {
            Debug.LogError($"❌ {name}: No InventorySystem in scene!");
            return;
        }

        // Thử thêm vào inventory
        int leftover = InventorySystem.Instance.AddItem(runtimeItemSO, amount);
        int picked = amount - leftover;

        if (picked > 0)
        {
            Debug.Log($"✨ {name}: Player picked up {databaseItem.item_name} x{picked}");

            // Thực hiện effects
            OnPickupSuccess(other.gameObject);

            // Cập nhật hoặc destroy
            if (destroyWhenPicked && leftover == 0)
            {
                Destroy(gameObject);
            }
            else if (leftover > 0)
            {
                amount = leftover; // update amount for remaining pickups
            }
        }
        else
        {
            Debug.LogWarning($"⚠️ {name}: Could not add {databaseItem.item_name} to inventory - full!");
        }
    }

    void OnPickupSuccess(GameObject player)
    {
        Debug.Log($"🔍 [PICKUP] OnPickupSuccess called - item_id: {databaseItem.item_id}, amount: {amount}");
        
        // Play pickup effect
        if (pickupEffect != null)
        {
            GameObject effect = Instantiate(pickupEffect, transform.position, Quaternion.identity);
            Destroy(effect, pickupEffectDuration);
        }

        // Play pickup sound
        if (pickupSound != null)
        {
            AudioSource.PlayClipAtPoint(pickupSound, transform.position);
        }
        
        // ✅ Gọi QuestManager để xử lý quest update (tránh coroutine bị hủy khi object destroy)
        Debug.Log($"🔍 [PICKUP] Notifying QuestManager about item pickup...");
        if (QuestManager.Instance != null)
        {
            QuestManager.Instance.NotifyItemPickup(databaseItem.item_id, amount);
        }
        else
        {
            Debug.LogWarning("⚠️ QuestManager not found! Quest progress won't be updated.");
        }
        // Execute item effects if usable
        if (databaseItem.usable && databaseItem.effect_type != null)
        {
            ExecuteItemEffect(player);
        }

        // Show pickup notification (thông qua HUD)
        var hud = FindObjectOfType<NavActionHandler>(); // hoặc class quản lý HUD khác
        if (hud != null)
        {
            string message = $"Nhặt được {databaseItem.item_name} x{amount - (amount - (amount - (InventorySystem.Instance.CountOf(runtimeItemSO) - (amount - 1))))}"; // Show pickup message
        }
    }


    void ExecuteItemEffect(GameObject target)
    {
        switch (databaseItem.effect_type.ToLower())
        {
            case "restore_health":
                var health = target.GetComponent<PlayerHealth>();
                if (health != null)
                {
                    health.HealPlayer(); // hoặc gọi phương thức heal với amount
                    Debug.Log($"💚 {name}: Restored health by {databaseItem.effect_value}");
                }
                break;

            case "restore_stamina":
                var stamina = FindObjectOfType<Stamina>();
                if (stamina != null)
                {
                    stamina.RefreshStamina();
                    Debug.Log($"⚡ {name}: Restored stamina by {databaseItem.effect_value}");
                }
                break;

            case "add_gold":
                var economy = FindObjectOfType<EconomyManagement>();
                if (economy != null)
                {
                    economy.UpdateCurrentGold();
                    Debug.Log($"💰 {name}: Added gold");
                }
                break;

            default:
                Debug.LogWarning($"⚠️ {name}: Unknown effect type: {databaseItem.effect_type}");
                break;
        }
    }

    // Editor helper methods
    [ContextMenu("Find Item By Name")]
    public void FindItemByName()
    {
        if (DatabaseItemManager.Instance != null && DatabaseItemManager.Instance.IsDatabaseLoaded())
        {
            var item = DatabaseItemManager.Instance.GetDatabaseItemByName(gameObject.name);
            if (item != null)
            {
                databaseItemId = item.item_id;
                databaseItemName = item.item_name;
                Debug.Log($"🔍 Found item: {item.item_name} (ID: {item.item_id})");
            }
            else
            {
                Debug.LogWarning($"⚠️ No item found with name: {gameObject.name}");
            }
        }
        else
        {
            Debug.LogWarning("⚠️ DatabaseItemManager not ready or database not loaded");
        }
    }

    [ContextMenu("Refresh From Database")]
    public void RefreshFromDatabase()
    {
        InitializeFromDatabase();
    }

    void OnValidate()
    {
        // Reset collider to trigger
        var collider = GetComponent<Collider2D>();
        if (collider != null && !collider.isTrigger)
        {
            collider.isTrigger = true;
        }
    }
}
