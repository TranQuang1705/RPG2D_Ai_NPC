using System;
using System.Collections;
using UnityEngine;
using UnityEngine.Networking;

/// <summary>
/// Manages player level and EXP with hard progression formula
/// Based on Dark Souls + Genshin + RO style exponential growth
/// </summary>
public class PlayerLevelSystem : Singleton<PlayerLevelSystem>
{
    [Header("Player Stats")]
    [SerializeField] private int currentLevel = 1;
    [SerializeField] private int currentExp = 0;
    [SerializeField] private int expToNextLevel = 100;
    [SerializeField] private int playerId = 1; // TODO: Get from PlayerController or GameManager
    
    [Header("API Configuration")]
    [SerializeField] private string apiUrl = "http://127.0.0.1:5002/players";
    
    public int CurrentLevel => currentLevel;
    public int CurrentExp => currentExp;
    public int ExpToNextLevel => expToNextLevel;
    public float ExpProgress => (float)currentExp / expToNextLevel;
    
    public static event Action<int, int> OnExpGained; // (expAmount, newTotalExp)
    public static event Action<int> OnLevelUp; // (newLevel)
    
    protected override void Awake()
    {
        base.Awake();
    }
    
    private void Start()
    {
        // Load player data from database
        StartCoroutine(LoadPlayerData());
    }
    
    /// <summary>
    /// Hard progression EXP formula
    /// Level 1-50: Moderate growth (power 1.6)
    /// Level 50-80: Steep growth (power 2.0)
    /// Level 80-100: Extreme growth (power 2.4)
    /// </summary>
    public int GetExpToNextLevel(int level)
    {
        if (level <= 1) return 100;

        float baseExp = 100f;
        float exp;

        if (level < 50)
        {
            // Phase 1: Strong but not insane
            exp = baseExp * Mathf.Pow(level, 1.6f);
        }
        else if (level < 80)
        {
            // Phase 2: Starting to climb
            exp = baseExp * Mathf.Pow(level, 2.0f);
        }
        else
        {
            // Phase 3: End-game ultra-hard
            exp = baseExp * Mathf.Pow(level, 2.4f);
        }

        // Random 15% variance for natural feel
        float randomFactor = UnityEngine.Random.Range(0.85f, 1.15f);

        return Mathf.RoundToInt(exp * randomFactor);
    }
    
    /// <summary>
    /// Add EXP to player and handle level ups
    /// </summary>
    public void AddExp(int amount)
    {
        if (amount <= 0) return;
        
        currentExp += amount;
        OnExpGained?.Invoke(amount, currentExp);
        
        
        // Check for level up(s)
        while (currentExp >= expToNextLevel && currentLevel < 100)
        {
            LevelUp();
        }
        
        // Save to database
        StartCoroutine(SaveExpToDatabase());
    }
    
    private void LevelUp()
    {
        currentExp -= expToNextLevel;
        currentLevel++;
        expToNextLevel = GetExpToNextLevel(currentLevel);
        
        OnLevelUp?.Invoke(currentLevel);
        
        
        // Save to database
        StartCoroutine(SaveLevelToDatabase());
    }
    
    /// <summary>
    /// Load player data from database
    /// </summary>
    private IEnumerator LoadPlayerData()
    {
        string url = $"{apiUrl}/{playerId}";
        
        using (UnityWebRequest req = UnityWebRequest.Get(url))
        {
            yield return req.SendWebRequest();
            
            if (req.result == UnityWebRequest.Result.Success)
            {
                string json = req.downloadHandler.text;
                DatabasePlayer player = JsonUtility.FromJson<DatabasePlayer>(json);
                
                currentLevel = player.level;
                currentExp = player.exp;
                expToNextLevel = player.exp_to_next_level;
                
            }
            else
            {
                Debug.LogWarning($"⚠️ Failed to load player data: {req.error}. Using defaults.");
            }
        }
    }
    
    /// <summary>
    /// Save EXP to database
    /// </summary>
    private IEnumerator SaveExpToDatabase()
    {
        string url = $"{apiUrl}/{playerId}";
        
        // Tạo JSON data
        string jsonData = "{\"exp\":" + currentExp + "}";
        byte[] bodyRaw = System.Text.Encoding.UTF8.GetBytes(jsonData);
        
        using (UnityWebRequest req = UnityWebRequest.Put(url, bodyRaw))
        {
            req.SetRequestHeader("Content-Type", "application/json");
            yield return req.SendWebRequest();
            
            if (req.result == UnityWebRequest.Result.Success)
            {
                Debug.Log($"💾 Saved EXP to database: {currentExp}");
            }
            else
            {
                Debug.LogError($"❌ Failed to save EXP: {req.error}\nResponse: {req.downloadHandler.text}");
            }
        }
    }
    
    /// <summary>
    /// Save level data to database
    /// </summary>
    private IEnumerator SaveLevelToDatabase()
    {
        string url = $"{apiUrl}/{playerId}";
        
        // Tạo JSON data
        string jsonData = "{\"level\":" + currentLevel + 
                         ",\"exp\":" + currentExp + 
                         ",\"exp_to_next_level\":" + expToNextLevel + "}";
        byte[] bodyRaw = System.Text.Encoding.UTF8.GetBytes(jsonData);
        
        using (UnityWebRequest req = UnityWebRequest.Put(url, bodyRaw))
        {
            req.SetRequestHeader("Content-Type", "application/json");
            yield return req.SendWebRequest();
            
            if (req.result == UnityWebRequest.Result.Success)
            {
                Debug.Log($"💾 Saved level data: Level {currentLevel}, EXP {currentExp}/{expToNextLevel}");
            }
            else
            {
                Debug.LogError($"❌ Failed to save level data: {req.error}\nResponse: {req.downloadHandler.text}");
            }
        }
    }
    
    /// <summary>
    /// Set player ID (call this from GameManager or PlayerController)
    /// </summary>
    public void SetPlayerId(int id)
    {
        playerId = id;
        StartCoroutine(LoadPlayerData());
    }
    
    /// <summary>
    /// Debug method to test level progression
    /// </summary>
    [ContextMenu("Test Level Progression")]
    private void TestLevelProgression()
    {
        for (int i = 1; i <= 100; i++)
        {
            int expNeeded = GetExpToNextLevel(i);
            if (i == 1 || i == 2 || i == 5 || i == 10 || i % 10 == 0)
            {
                Debug.Log($"Level {i} → {i + 1}: {expNeeded:N0} EXP");
            }
        }
    }
}
