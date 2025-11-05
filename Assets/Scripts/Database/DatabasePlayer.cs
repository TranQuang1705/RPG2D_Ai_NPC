using UnityEngine;
using UnityEngine.Networking;
using System.Collections;
using System.Collections.Generic;
using System;

[System.Serializable]
public class DatabasePlayer
{
    public int player_id;
    public string player_name;
    public int gold;
    public int level;
    public int exp;
    public int exp_to_next_level;
    public int current_bag_id;
    public string prefab_path;   // ⬅️ Nếu bạn thêm cột này vào bảng players
    public string created_at;
    public string updated_at;

    [NonSerialized] public GameObject prefab;  // để load prefab trong Resources
}

[System.Serializable]
public class DatabasePlayerList
{
    public List<DatabasePlayer> players;
}

public class DatabasePlayerManager : MonoBehaviour
{
    public static DatabasePlayerManager Instance { get; private set; }

    [Header("API Configuration")]
    [SerializeField] private string apiUrl = "http://127.0.0.1:5002/players";

    private Dictionary<int, DatabasePlayer> playerCache = new Dictionary<int, DatabasePlayer>();

    public static event Action OnPlayersLoaded;

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
        StartCoroutine(FetchPlayers());
    }

    IEnumerator FetchPlayers()
    {
        Debug.Log("🚀 Fetching players from API...");

        using (UnityWebRequest req = UnityWebRequest.Get(apiUrl))
        {
            yield return req.SendWebRequest();

            if (req.result == UnityWebRequest.Result.Success)
            {
                string json = req.downloadHandler.text;
                Debug.Log($"✅ Players data: {json}");

                DatabasePlayerList playerList = JsonUtility.FromJson<DatabasePlayerList>("{\"players\":" + json + "}");
                playerCache.Clear();

                foreach (var player in playerList.players)
                {
                    playerCache[player.player_id] = player;

                    // Nếu có prefab_path → load prefab từ Resources
                    if (!string.IsNullOrEmpty(player.prefab_path))
                    {
                        string prefabName = System.IO.Path.GetFileNameWithoutExtension(player.prefab_path);
                        player.prefab = Resources.Load<GameObject>($"Prefabs/{prefabName}");

                        Debug.Log($"🎮 Loaded prefab for player {player.player_name}: Prefabs/{prefabName}");
                    }
                }

                Debug.Log($"✅ Loaded {playerCache.Count} players.");
                OnPlayersLoaded?.Invoke();
            }
            else
            {
                Debug.LogError($"❌ Failed to load players: {req.error}");
            }
        }
    }

    public DatabasePlayer GetPlayerById(int playerId)
    {
        playerCache.TryGetValue(playerId, out DatabasePlayer player);
        return player;
    }

    public DatabasePlayer GetFirstPlayer()
    {
        foreach (var p in playerCache.Values)
            return p;
        return null;
    }

    public bool IsLoaded()
    {
        return playerCache.Count > 0;
    }
}
