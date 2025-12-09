using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.Networking;
using System.Collections;

public class PlayerLevelUI : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private Image avatarImage;      // Khung tròn xanh + hình sói (1 sprite duy nhất)
    [SerializeField] private TextMeshProUGUI levelText;
    [SerializeField] private TextMeshProUGUI goldText; // Số vàng (100 trong hình)
    
    [Header("Database Settings")]
    [SerializeField] private string apiBaseUrl = "http://127.0.0.1:5002";
    [SerializeField] private int playerId = 1;

    private int currentLevel = 1;
    private int currentGold = 0;

    void Start()
    {
        StartCoroutine(FetchPlayerData());
    }
    
    void OnEnable()
    {
        // Subscribe to PlayerLevelSystem events để tự động cập nhật khi lên cấp
        PlayerLevelSystem.OnExpGained += OnExpGained;
        PlayerLevelSystem.OnLevelUp += OnLevelUp;
        
        // Subscribe to CoinInventorySystem để tự động cập nhật gold khi nhặt coins
        if (CoinInventorySystem.Instance != null)
        {
            CoinInventorySystem.Instance.OnCoinInventoryChanged += UpdateGoldFromCoins;
        }
        
        // Subscribe to DatabaseCoinLoader để update khi load xong
        DatabaseCoinLoader.OnPlayerCoinsLoaded += OnPlayerCoinsLoaded;
    }
    
    void OnDisable()
    {
        // Unsubscribe để tránh memory leak
        PlayerLevelSystem.OnExpGained -= OnExpGained;
        PlayerLevelSystem.OnLevelUp -= OnLevelUp;
        
        if (CoinInventorySystem.Instance != null)
        {
            CoinInventorySystem.Instance.OnCoinInventoryChanged -= UpdateGoldFromCoins;
        }
        
        DatabaseCoinLoader.OnPlayerCoinsLoaded -= OnPlayerCoinsLoaded;
    }
    
    private void OnPlayerCoinsLoaded()
    {
        // Khi coins load xong từ database → update gold UI
        UpdateGoldFromCoins();
    }
    
    /// <summary>
    /// Callback khi nhận EXP (optional - có thể hiển thị animation)
    /// </summary>
    private void OnExpGained(int expAmount, int newTotalExp)
    {
        // Optional: Có thể thêm animation hoặc effect ở đây
        Debug.Log($"[PlayerLevelUI] EXP gained: +{expAmount}");
    }
    
    /// <summary>
    /// Callback khi lên cấp - CẬP NHẬT NGAY LẬP TỨC
    /// </summary>
    private void OnLevelUp(int newLevel)
    {
        currentLevel = newLevel;
        UpdateUI();
        Debug.Log($"🎉 [PlayerLevelUI] Level updated to: {currentLevel}");
    }

    IEnumerator FetchPlayerData()
    {
        string url = apiBaseUrl + "/players/" + playerId;
        
        using (UnityWebRequest req = UnityWebRequest.Get(url))
        {
            yield return req.SendWebRequest();

            if (req.result == UnityWebRequest.Result.Success)
            {
                string json = req.downloadHandler.text;
                PlayerData playerData = JsonUtility.FromJson<PlayerData>(json);
                
                currentLevel = playerData.level;
                currentGold = playerData.gold;
                
                UpdateUI();
                
                Debug.Log("Player data loaded - Level: " + currentLevel + ", Gold: " + currentGold);
                
                // ⚠️ QUAN TRỌNG: Sau khi load, chờ coins load xong rồi update lại gold
                // Vì coins load sau player data
                StartCoroutine(WaitForCoinsAndUpdateGold());
            }
            else
            {
                Debug.LogError("Failed to fetch player data: " + req.error);
                currentLevel = 1;
                currentGold = 0;
                UpdateUI();
            }
        }
    }
    
    /// <summary>
    /// Chờ coins load xong rồi update gold từ coins
    /// </summary>
    private IEnumerator WaitForCoinsAndUpdateGold()
    {
        // Chờ tối đa 3 giây để coins load xong
        float waitTime = 0f;
        while (waitTime < 3f)
        {
            if (CoinInventorySystem.Instance != null && 
                CoinInventorySystem.Instance.CoinSlots.Count > 0)
            {
                // Check xem có coins nào không
                bool hasCoins = false;
                foreach (var slot in CoinInventorySystem.Instance.CoinSlots)
                {
                    if (slot != null && !slot.IsEmpty)
                    {
                        hasCoins = true;
                        break;
                    }
                }
                
                if (hasCoins)
                {
                    // Coins đã load xong, update gold
                    UpdateGoldFromCoins();
                    Debug.Log("[PlayerLevelUI] Updated gold from coins after initial load");
                    yield break;
                }
            }
            
            yield return new WaitForSeconds(0.1f);
            waitTime += 0.1f;
        }
        
        Debug.LogWarning("[PlayerLevelUI] Timeout waiting for coins to load");
    }

    private void UpdateUI()
    {
        if (levelText != null)
        {
            levelText.text = currentLevel.ToString();
            // ⚠️ Force enable GameObject nếu bị tắt
            if (!levelText.gameObject.activeSelf)
            {
                levelText.gameObject.SetActive(true);
                Debug.LogWarning("[PlayerLevelUI] levelText was disabled, re-enabling");
            }
        }
        else
        {
            Debug.LogError("[PlayerLevelUI] levelText is NULL! Assign it in Inspector");
        }
        
        if (goldText != null)
        {
            goldText.text = currentGold.ToString();
            // ⚠️ Force enable GameObject nếu bị tắt
            if (!goldText.gameObject.activeSelf)
            {
                goldText.gameObject.SetActive(true);
                Debug.LogWarning("[PlayerLevelUI] goldText was disabled, re-enabling");
            }
        }
        else
        {
            Debug.LogError("[PlayerLevelUI] goldText is NULL! Assign it in Inspector");
        }
        
        // ⚠️ Check parent icon/avatar
        if (avatarImage != null && !avatarImage.gameObject.activeSelf)
        {
            avatarImage.gameObject.SetActive(true);
            Debug.LogWarning("[PlayerLevelUI] avatarImage was disabled, re-enabling");
        }
        
        Debug.Log($"[PlayerLevelUI] UpdateUI called - Level: {currentLevel}, Gold: {currentGold}");
    }

    public void UpdateLevel(int newLevel)
    {
        currentLevel = newLevel;
        UpdateUI();
        Debug.Log("Player Level updated to: " + currentLevel);
    }
    
    public void UpdateGold(int newGold)
    {
        currentGold = newGold;
        UpdateUI();
        Debug.Log("Player Gold updated to: " + currentGold);
    }
    
    /// <summary>
    /// Refresh UI manually từ database (gọi từ Inspector hoặc code khác)
    /// </summary>
    [ContextMenu("Refresh Player UI")]
    public void RefreshUI()
    {
        StartCoroutine(FetchPlayerData());
    }
    
    /// <summary>
    /// Update gold từ CoinInventorySystem (tổng giá trị tất cả coins)
    /// </summary>
    public void UpdateGoldFromCoins()
    {
        if (CoinInventorySystem.Instance != null)
        {
            int totalValue = CoinInventorySystem.Instance.GetTotalCoinValueInObal();
            UpdateGold(totalValue);
        }
    }

    [System.Serializable]
    private class PlayerData
    {
        public int player_id;
        public string player_name;
        public int level;
        public int exp;
        public int exp_to_next_level;
        public int gold;
    }
}
