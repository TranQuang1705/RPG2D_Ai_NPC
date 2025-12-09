using UnityEngine;
using UnityEngine.Networking;
using System.Collections;
using System.Collections.Generic;
using System;

/// <summary>
/// Database model for coins table
/// </summary>
[System.Serializable]
public class DatabaseCoin
{
    public int coin_id;
    public string coin_name;
    public int coin_value;
    public string description;
    public string rarity;
    public string icon_path;
    public string model_path;
    public string created_at;

    [NonSerialized] public CoinSO coinSO;  // Reference to loaded CoinSO
}

/// <summary>
/// Database model for player_coins table
/// </summary>
[System.Serializable]
public class DatabasePlayerCoin
{
    public int player_id;
    public int coin_id;
    public int amount;
    public string coin_name;
    public int coin_value;
    public string description;
    public string rarity;
    public string icon_path;
    public string model_path;
}

[System.Serializable]
public class DatabaseCoinList
{
    public List<DatabaseCoin> coins;
}

[System.Serializable]
public class DatabasePlayerCoinList
{
    public List<DatabasePlayerCoin> player_coins;
}

/// <summary>
/// Loads coin data from database and syncs with CoinInventorySystem
/// </summary>
public class DatabaseCoinLoader : MonoBehaviour
{
    public static DatabaseCoinLoader Instance { get; private set; }

    [Header("API Configuration")]
    [SerializeField] private string apiUrl = "http://127.0.0.1:5002";
    [SerializeField] private int defaultPlayerId = 1;

    [Header("Auto Load")]
    [SerializeField] private bool loadOnStart = true;
    [SerializeField] private float loadDelay = 0.5f;

    private Dictionary<int, DatabaseCoin> coinCache = new Dictionary<int, DatabaseCoin>();
    private Dictionary<string, CoinSO> coinSOCache = new Dictionary<string, CoinSO>();

    public static event Action OnCoinsLoaded;
    public static event Action OnPlayerCoinsLoaded;

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    void Start()
    {
        Debug.Log($"🪙 [DatabaseCoinLoader] Starting... loadOnStart={loadOnStart}");
        
        if (loadOnStart)
        {
            StartCoroutine(LoadAllCoinsDelayed());
        }
        else
        {
            Debug.LogWarning("⚠️ [DatabaseCoinLoader] loadOnStart is FALSE! Coins will not be loaded automatically.");
        }
    }

    IEnumerator LoadAllCoinsDelayed()
    {
        Debug.Log($"🪙 [DatabaseCoinLoader] Waiting {loadDelay}s before loading...");
        yield return new WaitForSeconds(loadDelay);
        
        // First load coin definitions
        Debug.Log("🪙 [DatabaseCoinLoader] Step 1: Loading coin definitions...");
        yield return StartCoroutine(FetchCoins());
        
        // Then load player's coins
        Debug.Log($"🪙 [DatabaseCoinLoader] Step 2: Loading player coins (player_id={defaultPlayerId})...");
        yield return StartCoroutine(FetchPlayerCoins(defaultPlayerId));
        
        Debug.Log("✅ [DatabaseCoinLoader] All coins loaded!");
    }

    /// <summary>
    /// Fetch all coin definitions from database
    /// </summary>
    public IEnumerator FetchCoins()
    {
        Debug.Log("🪙 [DatabaseCoinLoader] Fetching coins from database...");

        string url = $"{apiUrl}/coins";
        using (UnityWebRequest req = UnityWebRequest.Get(url))
        {
            yield return req.SendWebRequest();

            if (req.result == UnityWebRequest.Result.Success)
            {
                string json = req.downloadHandler.text;
                Debug.Log($"✅ [DatabaseCoinLoader] Coins data: {json}");

                try
                {
                    DatabaseCoinList coinList = JsonUtility.FromJson<DatabaseCoinList>("{\"coins\":" + json + "}");
                    coinCache.Clear();

                    foreach (var coin in coinList.coins)
                    {
                        coinCache[coin.coin_id] = coin;

                        // Try to load CoinSO from Resources
                        CoinSO coinSO = LoadCoinSO(coin.coin_name);
                        if (coinSO != null)
                        {
                            coin.coinSO = coinSO;
                            coinSOCache[coin.coin_name] = coinSO;
                            Debug.Log($"💰 Loaded CoinSO: {coin.coin_name}");
                        }
                        else
                        {
                            Debug.LogWarning($"⚠️ CoinSO not found for: {coin.coin_name}. Create it at Resources/Coins/{coin.coin_name}.asset");
                        }
                    }

                    Debug.Log($"✅ [DatabaseCoinLoader] Loaded {coinCache.Count} coins from database");
                    OnCoinsLoaded?.Invoke();
                }
                catch (Exception e)
                {
                    Debug.LogError($"❌ [DatabaseCoinLoader] Failed to parse coins: {e.Message}");
                }
            }
            else
            {
                Debug.LogError($"❌ [DatabaseCoinLoader] Failed to fetch coins: {req.error}");
            }
        }
    }

    /// <summary>
    /// Fetch player's coins from database and sync with CoinInventorySystem
    /// </summary>
    public IEnumerator FetchPlayerCoins(int playerId)
    {
        Debug.Log($"🪙 [DatabaseCoinLoader] Fetching coins for player {playerId}...");

        string url = $"{apiUrl}/player_coins?player_id={playerId}";
        using (UnityWebRequest req = UnityWebRequest.Get(url))
        {
            yield return req.SendWebRequest();

            if (req.result == UnityWebRequest.Result.Success)
            {
                string json = req.downloadHandler.text;
                Debug.Log($"✅ [DatabaseCoinLoader] Player coins data: {json}");

                try
                {
                    // Handle empty array case
                    if (json == "[]")
                    {
                        Debug.Log($"ℹ️ [DatabaseCoinLoader] Player {playerId} has no coins yet");
                        OnPlayerCoinsLoaded?.Invoke();
                        yield break;
                    }

                    DatabasePlayerCoinList playerCoinList = JsonUtility.FromJson<DatabasePlayerCoinList>("{\"player_coins\":" + json + "}");

                    if (CoinInventorySystem.Instance == null)
                    {
                        Debug.LogError("❌ [DatabaseCoinLoader] CoinInventorySystem not found in scene!");
                        Debug.LogError("   → Solution: Tạo GameObject 'CoinInventorySystem' trong Scene");
                        yield break;
                    }

                    Debug.Log($"[DatabaseCoinLoader] Found {playerCoinList.player_coins.Count} coin types in database");

                    // KHÔNG clear existing coins - chỉ load từ database
                    // Nếu clear thì coins sẽ bị mất khi reload

                    // Add coins to inventory
                    int loadedCount = 0;
                    foreach (var playerCoin in playerCoinList.player_coins)
                    {
                        CoinSO coinSO = GetCoinSOByName(playerCoin.coin_name);
                        
                        if (coinSO != null && playerCoin.amount > 0)
                        {
                            // Set the amount directly in CoinInventorySystem
                            int leftover = CoinInventorySystem.Instance.AddCoin(coinSO, playerCoin.amount);
                            
                            if (leftover == 0)
                            {
                                loadedCount++;
                                Debug.Log($"💰 Loaded {playerCoin.amount}x {playerCoin.coin_name} for player {playerId}");
                            }
                            else
                            {
                                Debug.LogWarning($"⚠️ Could not load all coins: {playerCoin.coin_name} (leftover: {leftover})");
                            }
                        }
                        else if (coinSO == null)
                        {
                            Debug.LogWarning($"⚠️ CoinSO not found for {playerCoin.coin_name}. Create it in Resources/Coins/");
                        }
                    }

                    Debug.Log($"✅ [DatabaseCoinLoader] Loaded {loadedCount} coin types for player {playerId}");
                    
                    // Tính tổng giá trị coins
                    if (CoinInventorySystem.Instance != null)
                    {
                        int totalValue = CoinInventorySystem.Instance.GetTotalCoinValueInObal();
                        Debug.Log($"💰 Total coin value: {totalValue} Obal (Gold)");
                        
                        // Update PlayerLevelUI nếu có
                        var playerUI = FindObjectOfType<PlayerLevelUI>();
                        if (playerUI != null)
                        {
                            playerUI.UpdateGoldFromCoins();
                            Debug.Log($"✅ Updated PlayerLevelUI with gold: {totalValue}");
                        }
                    }
                    
                    OnPlayerCoinsLoaded?.Invoke();
                }
                catch (Exception e)
                {
                    Debug.LogError($"❌ [DatabaseCoinLoader] Failed to parse player coins: {e.Message}\nJSON: {json}");
                }
            }
            else
            {
                Debug.LogError($"❌ [DatabaseCoinLoader] Failed to fetch player coins: {req.error}");
            }
        }
    }

    /// <summary>
    /// Save player's current coins to database
    /// </summary>
    public IEnumerator SavePlayerCoins(int playerId)
    {
        if (CoinInventorySystem.Instance == null)
        {
            Debug.LogError("❌ [DatabaseCoinLoader] CoinInventorySystem not found!");
            yield break;
        }

        Debug.Log($"💾 [DatabaseCoinLoader] Saving coins for player {playerId}...");

        var coinSlots = CoinInventorySystem.Instance.CoinSlots;
        int savedCount = 0;

        foreach (var slot in coinSlots)
        {
            if (slot.IsEmpty) continue;

            CoinSlot coinSlot = slot as CoinSlot;
            if (coinSlot?.coin == null) continue;

            // Get coin_id from database
            int coinId = GetCoinIdByName(coinSlot.coin.coinName);
            if (coinId <= 0)
            {
                Debug.LogWarning($"⚠️ Coin ID not found for: {coinSlot.coin.coinName}");
                continue;
            }

            // Send update to database
            yield return StartCoroutine(UpdatePlayerCoinAmount(playerId, coinId, coinSlot.amount));
            savedCount++;
        }

        Debug.Log($"✅ [DatabaseCoinLoader] Saved {savedCount} coin types to database");
    }

    /// <summary>
    /// Update specific coin amount in database
    /// </summary>
    IEnumerator UpdatePlayerCoinAmount(int playerId, int coinId, int amount)
    {
        string url = $"{apiUrl}/player_coins/update";

        WWWForm form = new WWWForm();
        form.AddField("player_id", playerId);
        form.AddField("coin_id", coinId);
        form.AddField("amount", amount);

        using (UnityWebRequest req = UnityWebRequest.Post(url, form))
        {
            req.SetRequestHeader("Content-Type", "application/x-www-form-urlencoded");
            yield return req.SendWebRequest();

            if (req.result != UnityWebRequest.Result.Success)
            {
                Debug.LogError($"❌ Failed to update coin {coinId}: {req.error}");
            }
        }
    }

    /// <summary>
    /// Load or create CoinSO (runtime creation like ItemSO)
    /// </summary>
    CoinSO LoadCoinSO(string coinName)
    {
        // Try loading from Resources first (if manually created)
        string[] paths = {
            $"Coins/{coinName}",
            $"ScriptableObjects/Coins/{coinName}",
            $"SO/Coins/{coinName}",
            coinName
        };

        foreach (string path in paths)
        {
            CoinSO coinSO = Resources.Load<CoinSO>(path);
            if (coinSO != null)
            {
                return coinSO;
            }
        }

        // Not found in Resources - create runtime CoinSO from database
        return CreateCoinSOFromDatabase(coinName);
    }

    /// <summary>
    /// Create runtime CoinSO from database coin data (like ItemSO does)
    /// </summary>
    CoinSO CreateCoinSOFromDatabase(string coinName)
    {
        // Find coin in database cache
        DatabaseCoin dbCoin = null;
        foreach (var kvp in coinCache)
        {
            if (kvp.Value.coin_name.Equals(coinName, StringComparison.OrdinalIgnoreCase))
            {
                dbCoin = kvp.Value;
                break;
            }
        }

        if (dbCoin == null)
        {
            Debug.LogError($"❌ Coin '{coinName}' not found in database cache!");
            return null;
        }

        return CreateCoinSOFromDatabase(dbCoin);
    }

    /// <summary>
    /// Create runtime CoinSO from DatabaseCoin
    /// </summary>
    CoinSO CreateCoinSOFromDatabase(DatabaseCoin dbCoin)
    {
        // Create ScriptableObject instance at runtime
        var newCoinSO = ScriptableObject.CreateInstance<CoinSO>();

        // Map database fields to CoinSO
        newCoinSO.coinName = dbCoin.coin_name;
        newCoinSO.description = dbCoin.description;
        newCoinSO.coinValue = dbCoin.coin_value;
        newCoinSO.rarity = dbCoin.rarity;
        newCoinSO.databaseCoinId = dbCoin.coin_id;

        // Load icon from Resources based on database path
        if (!string.IsNullOrEmpty(dbCoin.icon_path))
        {
            // icon_path = "Icons/Coins/obal.png" or "Icons/Coins/obal"
            string iconPath = dbCoin.icon_path.Replace(".png", "").Replace(".jpg", "");
            Sprite icon = Resources.Load<Sprite>(iconPath);
            
            if (icon != null)
            {
                newCoinSO.icon = icon;
                Debug.Log($"🪙 Loaded icon for {dbCoin.coin_name}: {iconPath}");
            }
            else
            {
                Debug.LogWarning($"⚠️ Could not find icon: {iconPath}");
            }
        }

        // Load prefab from Resources based on database path
        if (!string.IsNullOrEmpty(dbCoin.model_path))
        {
            // model_path = "Prefabs/Coins/obal.prefab" or "Prefabs/Coins/obal"
            string prefabPath = dbCoin.model_path.Replace(".prefab", "");
            GameObject prefab = Resources.Load<GameObject>(prefabPath);
            
            if (prefab != null)
            {
                newCoinSO.prefab = prefab;
                Debug.Log($"🎁 Loaded prefab for {dbCoin.coin_name}: {prefabPath}");
            }
            else
            {
                Debug.LogWarning($"⚠️ Could not find prefab: {prefabPath}");
            }
        }

        Debug.Log($"✅ Created runtime CoinSO for {dbCoin.coin_name}");
        return newCoinSO;
    }

    /// <summary>
    /// Get CoinSO by name from cache
    /// </summary>
    public CoinSO GetCoinSOByName(string coinName)
    {
        if (coinSOCache.TryGetValue(coinName, out CoinSO coinSO))
        {
            return coinSO;
        }

        // Try to load if not in cache
        coinSO = LoadCoinSO(coinName);
        if (coinSO != null)
        {
            coinSOCache[coinName] = coinSO;
        }

        return coinSO;
    }

    /// <summary>
    /// Get coin_id from database cache by name
    /// </summary>
    public int GetCoinIdByName(string coinName)
    {
        foreach (var kvp in coinCache)
        {
            if (kvp.Value.coin_name.Equals(coinName, StringComparison.OrdinalIgnoreCase))
            {
                return kvp.Key;
            }
        }
        return -1;
    }

    /// <summary>
    /// Get DatabaseCoin by ID
    /// </summary>
    public DatabaseCoin GetCoinById(int coinId)
    {
        coinCache.TryGetValue(coinId, out DatabaseCoin coin);
        return coin;
    }

    /// <summary>
    /// Manual refresh - reload player coins
    /// </summary>
    public void RefreshPlayerCoins(int playerId)
    {
        StartCoroutine(FetchPlayerCoins(playerId));
    }

    /// <summary>
    /// Manual save - save current coins to database
    /// </summary>
    public void SaveCurrentCoins(int playerId)
    {
        StartCoroutine(SavePlayerCoins(playerId));
    }
}
