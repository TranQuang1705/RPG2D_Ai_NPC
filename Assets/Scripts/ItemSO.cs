using UnityEngine;

// ScriptableObject đại diện cho một loại item trong game
// Có thể tạo từ database hoặc manual
[CreateAssetMenu(fileName = "New ItemSO", menuName = "Inventory/Item SO")]
public class ItemSO : ScriptableObject
{
    [Header("Basic Info")]
    public string displayName;
    [TextArea(3, 5)]
    public string description;
    public string itemType = "food";
    public string rarity = "common";
    
    [Header("Stats")]
    public int value = 0;
    public float weight = 0f;
    public bool stackable = true;
    public int maxStack = 99;
    
    [Header("Usage")]
    public bool usable = true;
    public bool equipable = false;
    
    [Header("Effects")]
    public string effectType = "restore_stamina";
    public float effectValue = 10f;
    public string targetType = "self";
    
    [Header("Visuals")]
    public Sprite icon;
    public GameObject prefab;
    
    [Header("Database Reference")]
    [Tooltip("ID của item trong database (để cập nhật về server)")]
    public int databaseItemId = 0;
    
    // Runtime data cache
    private Sprite _runtimeIcon;
    private GameObject _runtimePrefab;
    
    public Sprite RuntimeIcon
    {
        get
        {
            if (_runtimeIcon != null) return _runtimeIcon;
            _runtimeIcon = icon;
            return _runtimeIcon;
        }
        set { _runtimeIcon = value; }
    }
    
    public GameObject RuntimePrefab
    {
        get
        {
            if (_runtimePrefab != null) return _runtimePrefab;
            _runtimePrefab = prefab;
            return _runtimePrefab;
        }
        set { _runtimePrefab = value; }
    }
    
    // Method để sử dụng item
    public virtual bool Use(GameObject user)
    {
        if (!usable) return false;
        
        switch (effectType.ToLower())
        {
            case "restore_health":
                var health = user.GetComponent<PlayerHealth>();
                if (health != null)
                {
                    health.HealhPlayer(); // Implement proper health healing with effectValue
                    return true;
                }
                break;
                
            case "restore_stamina":
                var stamina = FindObjectOfType<Stamina>();
                if (stamina != null)
                {
                    stamina.RefreshStamina();
                    return true;
                }
                break;
                
            case "add_gold":
                var economy = FindObjectOfType<EconomyManagement>();
                if (economy != null)
                {
                    economy.UpdateCurrentGold();
                    return true;
                }
                break;
        }
        
        return false;
    }
    
    // Check if this item is valid
    public bool IsValid()
    {
        return !string.IsNullOrEmpty(displayName) && icon != null;
    }
}
