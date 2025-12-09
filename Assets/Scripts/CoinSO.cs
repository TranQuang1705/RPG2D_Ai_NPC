using UnityEngine;

/// <summary>
/// ScriptableObject representing a coin type from the coins table in database
/// Coins are separate from regular items and stored in player_coins table
/// </summary>
[CreateAssetMenu(fileName = "New CoinSO", menuName = "Inventory/Coin SO")]
public class CoinSO : ScriptableObject
{
    [Header("Basic Info")]
    public string coinName;             // Obal, Varos, Sylv, Feron, Astryl, Aurum
    [TextArea(3, 5)]
    public string description;
    
    [Header("Stats")]
    public int coinValue = 1;           // 1, 10, 100, 1000, 10000
    public string rarity = "common";    // common, uncommon, rare, epic, legendary
    
    [Header("Visuals")]
    public Sprite icon;                 // Icon for UI display
    public GameObject prefab;           // Prefab for world drop
    
    [Header("Database Reference")]
    [Tooltip("ID from coins table")]
    public int databaseCoinId = 0;
    
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
        return !string.IsNullOrEmpty(coinName) && icon != null && coinValue > 0;
    }
}
