// BiomeDefinition.cs
using UnityEngine;
using UnityEngine.Tilemaps;
using System;

[CreateAssetMenu(fileName = "Biome_Meadow", menuName = "ProcGen/Biome")]
public class BiomeDefinition : ScriptableObject
{
    [Serializable] public struct WeightedTile { public TileBase tile; [Range(0, 1)] public float weight; }
    [Serializable] public struct WeightedPrefab { public GameObject prefab; [Range(0, 1)] public float density; }

    [Header("Base Ground (cỏ ở Meadow)")]
    public WeightedTile[] grassTiles;         // nhiều biến thể sprite cỏ
    [Header("Đường/đất trống")]
    public WeightedTile[] dirtTiles;
    [Header("Nước/Hồ")]
    public WeightedTile[] waterTiles;
    [Header("Trang trí (tile)")]
    public WeightedTile[] decorTiles;
    [Header("Vật thể (Prefab)")]
    public WeightedPrefab[] props;
    [Header("Vách núi / Foreground")]
    public WeightedTile[] cliffTiles;
    [Header("Vật thể (Prefab) trên nước (ví dụ: bèo, sen, khúc gỗ nổi...)")]
    public WeightedPrefab[] waterProps;      // cây, thùng, đá...

    [Header("Ngưỡng Perlin")]
    [Tooltip("<= waterThreshold => nước, <= dirtThreshold => đất/đường, còn lại => cỏ")]
    [Range(0, 1)] public float waterThreshold = 0.28f;
    [Range(0, 1)] public float dirtThreshold = 0.45f;

    [Header("Xác suất phủ decor tile trên nền cỏ")]
    [Range(0, 1)] public float decorChance = 0.08f;

    // ========== ENEMY SPAWN SYSTEM (NEW) ==========
    [Serializable]
    public struct EnemySpawnData
    {
        [Tooltip("Prefab enemy (ví dụ: Grape, Slime, Wolf)")]
        public GameObject enemyPrefab;
        
        [Tooltip("Trọng số spawn (càng cao càng thường xuất hiện)")]
        [Range(0f, 1f)]
        public float spawnWeight;
        
        [Tooltip("Số lượng tối thiểu mỗi spawn point")]
        [Range(1, 20)]
        public int minCount;
        
        [Tooltip("Số lượng tối đa mỗi spawn point")]
        [Range(1, 20)]
        public int maxCount;
    }

    [Header("━━━ ENEMY SPAWN SETTINGS ━━━")]
    [Tooltip("Danh sách enemy có thể spawn trong biome này")]
    public EnemySpawnData[] enemySpawns;

    [Header("Spawn Point Configuration")]
    [Tooltip("Số lượng spawn points trong biome này")]
    [Range(1, 50)]
    public int spawnPointCount = 10;

    [Tooltip("Khoảng cách tối thiểu giữa các spawn points (tiles)")]
    [Range(5, 30)]
    public int minSpawnPointDistance = 15;

    [Tooltip("Spawn points cách player tối thiểu bao nhiêu (tiles)")]
    [Range(10, 50)]
    public int safeZoneRadius = 20;

    [Header("Respawn Configuration")]
    [Tooltip("Có cho phép respawn không")]
    public bool enableRespawn = true;

    [Tooltip("Thời gian chờ trước khi respawn (giây)")]
    [Range(10f, 300f)]
    public float respawnDelay = 60f;

    [Tooltip("Số lần respawn tối đa (-1 = vô hạn)")]
    public int maxRespawnCount = -1;





    public TileBase Pick(WeightedTile[] arr, System.Random rng)
    {
        if (arr == null || arr.Length == 0) return null;
        float sum = 0f; foreach (var w in arr) sum += Mathf.Max(0.0001f, w.weight);
        float r = (float)(rng.NextDouble() * sum);
        foreach (var w in arr) { r -= Mathf.Max(0.0001f, w.weight); if (r <= 0) return w.tile; }
        return arr[arr.Length - 1].tile;
    }
}
