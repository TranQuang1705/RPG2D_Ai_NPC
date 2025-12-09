using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Manages player spawn position when loading MainGameScene
/// Automatically spawns player at map center
/// </summary>
public class PlayerSpawnManager : MonoBehaviour
{
    [Header("Player Prefab")]
    [SerializeField] private GameObject playerPrefab;
    
    [Header("Spawn Settings")]
    [SerializeField] private bool useMapCenter = true;
    [SerializeField] private Vector3 customSpawnPosition = Vector3.zero;
    
    private static PlayerSpawnManager instance;
    
    void Awake()
    {
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }
        instance = this;
        DontDestroyOnLoad(gameObject);
        
        SceneManager.sceneLoaded += OnSceneLoaded;
    }
    
    void OnDestroy()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }
    
    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        // Only spawn player in MainGameScene
        if (scene.name == "MainGameScene")
        {
            SpawnPlayer();
        }
    }
    
    private void SpawnPlayer()
    {
        // Check if player already exists in scene
        PlayerController existingPlayer = FindObjectOfType<PlayerController>();
        if (existingPlayer != null)
        {
            // Player already exists, just reposition
            RepositionPlayer(existingPlayer.gameObject);
            return;
        }
        
        // Player doesn't exist, instantiate from prefab
        if (playerPrefab != null)
        {
            Vector3 spawnPos = GetSpawnPosition();
            GameObject player = Instantiate(playerPrefab, spawnPos, Quaternion.identity);
            Debug.Log($"✅ Player spawned at {spawnPos}");
        }
        else
        {
            Debug.LogWarning("⚠️ PlayerSpawnManager: Player prefab not assigned!");
        }
    }
    
    private void RepositionPlayer(GameObject player)
    {
        Vector3 spawnPos = GetSpawnPosition();
        player.transform.position = spawnPos;
        Debug.Log($"✅ Player repositioned to {spawnPos}");
    }
    
    private Vector3 GetSpawnPosition()
    {
        if (!useMapCenter)
        {
            return customSpawnPosition;
        }
        
        // Find MapGenerator to get map center
        MapGenerator mapGen = FindObjectOfType<MapGenerator>();
        if (mapGen != null)
        {
            // Calculate center of map
            float centerX = mapGen.width / 2f;
            float centerY = mapGen.height / 2f;
            Vector3 center = new Vector3(centerX, centerY, 0f);
            
            Debug.Log($"📍 Map center calculated: ({centerX}, {centerY}) from map size ({mapGen.width}x{mapGen.height})");
            return center;
        }
        else
        {
            Debug.LogWarning("⚠️ MapGenerator not found! Using default spawn (40, 24)");
            return new Vector3(40f, 24f, 0f); // Default center for 80x48 map
        }
    }
    
    /// <summary>
    /// Public method to manually spawn player at map center
    /// </summary>
    public static void SpawnPlayerAtCenter()
    {
        if (instance != null)
        {
            instance.SpawnPlayer();
        }
    }
}
