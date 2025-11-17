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

    private void UpdateUI()
    {
        if (levelText != null)
        {
            levelText.text = currentLevel.ToString();
        }
        
        if (goldText != null)
        {
            goldText.text = currentGold.ToString();
        }
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
