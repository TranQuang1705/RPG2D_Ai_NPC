using UnityEngine;

/// <summary>
/// ScriptableObject representing an EXP item type
/// Similar to CoinSO but for experience points
/// </summary>
[CreateAssetMenu(fileName = "New ExpSO", menuName = "Inventory/Exp SO")]
public class ExpSO : ScriptableObject
{
    [Header("Basic Info")]
    public string expName;              // Ember EXP, Grove EXP, Tide EXP, etc.
    [TextArea(3, 5)]
    public string description;
    
    [Header("Stats")]
    public int expValue = 1;            // 1, 10, 100, 1000, 10000, 100000
    public string rarity = "common";    // common, uncommon, rare, epic, legendary, mythic
    
    [Header("Visuals")]
    public Sprite icon;                 // Icon for UI display
    public GameObject prefab;           // Prefab for world drop
    public Color tierColor = Color.white; // Color tier for visual distinction
    
    [Header("Database Reference")]
    [Tooltip("ID from exp_items table")]
    public int databaseExpId = 0;
    
    // Runtime cache
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
    
    public bool IsValid()
    {
        return !string.IsNullOrEmpty(expName) && icon != null && expValue > 0;
    }
}
