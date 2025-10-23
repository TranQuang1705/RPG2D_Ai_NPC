using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

// NOTE: file giả định bạn đã có BiomeDefinition giống code cũ (biome.Pick, biome.props, waterTiles, etc.)

public class ChunkedWorld : MonoBehaviour
{
    [Header("World / Chunk")]
    public int chunkWidth = 80;
    public int chunkHeight = 48;
    public int viewRadius = 1;     // 1 => load 3x3 chunks
    public int seed = 12345;
    public BiomeDefinition biome;
    public Grid grid;             // Grid cha (bắt buộc)
    public Transform player;

    [Header("Chunk Prefab")]
    public GameObject chunkPrefab; // chứa GroundTilemap, ForegroundTilemap, PropsParent
    public bool usePooling = true;
    public int poolInitial = 4;

    // trạng đang loaded (chunk index -> instance)
    Dictionary<Vector2Int, ChunkInstance> loaded = new Dictionary<Vector2Int, ChunkInstance>();
    // save data (lưu lại những thay đổi do player) - tồn tại dù chunk unload
    Dictionary<Vector2Int, ChunkSaveData> chunkSaves = new Dictionary<Vector2Int, ChunkSaveData>();
    // pool để reuse GameObject chunk
    Stack<GameObject> pool = new Stack<GameObject>();

    Vector2Int lastPlayerChunk = new Vector2Int(int.MinValue, int.MinValue);

    void Start()
    {
        if (grid == null) grid = GetComponentInParent<Grid>();
        // prefill pool (optional)
        for (int i=0;i<poolInitial;i++){
            if (chunkPrefab != null) {
                var go = Instantiate(chunkPrefab, grid.transform);
                go.SetActive(false);
                pool.Push(go);
            }
        }

        // initial update (force load chunks around current player)
        ForceUpdateChunks();
    }

    void Update()
    {
        if (player == null) return;
        Vector3Int cell = WorldToCell(player.position);
        Vector2Int curChunk = CellToChunk(cell);

        if (curChunk != lastPlayerChunk)
        {
            lastPlayerChunk = curChunk;
            UpdateVisibleChunks();
        }
    }

    void ForceUpdateChunks()
    {
        if (player == null) return;
        lastPlayerChunk = new Vector2Int(int.MinValue, int.MinValue);
        UpdateVisibleChunks();
    }

    // -------------------------
    // Load / Unload chunks
    // -------------------------
    void UpdateVisibleChunks()
    {
        Vector3Int playerCell = WorldToCell(player.position);
        Vector2Int playerChunk = CellToChunk(playerCell);

        HashSet<Vector2Int> wanted = new HashSet<Vector2Int>();
        for (int dx=-viewRadius; dx<=viewRadius; dx++)
            for (int dy=-viewRadius; dy<=viewRadius; dy++)
                wanted.Add(new Vector2Int(playerChunk.x+dx, playerChunk.y+dy));

        // unload those not wanted
        List<Vector2Int> toUnload = new List<Vector2Int>();
        foreach (var kv in loaded)
            if (!wanted.Contains(kv.Key)) toUnload.Add(kv.Key);

        foreach (var idx in toUnload) UnloadChunk(idx);

        // load missing
        foreach (var idx in wanted)
            if (!loaded.ContainsKey(idx)) LoadChunk(idx);
    }

    void LoadChunk(Vector2Int idx)
    {
        GameObject go;
        if (usePooling && pool.Count > 0)
        {
            go = pool.Pop();
            go.SetActive(true);
        }
        else
        {
            go = Instantiate(chunkPrefab, grid.transform);
        }

        go.name = $"Chunk_{idx.x}_{idx.y}";
        // đặt vị trí thế cho ô (0,0) của chunk
        go.transform.position = ChunkOriginWorld(idx);
        go.transform.rotation = Quaternion.identity;
        go.transform.localScale = Vector3.one;

        // tìm tilemaps
        Tilemap groundTM = null, foregroundTM = null;
        Transform propsParent = null;
        var tms = go.GetComponentsInChildren<Tilemap>(true);
        foreach (var tm in tms)
        {
            if (tm.gameObject.name.IndexOf("Ground", StringComparison.OrdinalIgnoreCase) >= 0) groundTM = tm;
            else if (tm.gameObject.name.IndexOf("Foreground", StringComparison.OrdinalIgnoreCase) >= 0) foregroundTM = tm;
        }
        // tìm props parent
        foreach (Transform t in go.GetComponentsInChildren<Transform>(true))
            if (t.name.IndexOf("Props", StringComparison.OrdinalIgnoreCase) >= 0) propsParent = t;

        if (groundTM == null || foregroundTM == null)
            Debug.LogWarning("Chunk prefab thiếu Ground/Foreground Tilemap con hoặc tên không đúng!");

        // clear tilemaps (lưu ý: dùng local cell coords khi tilemap đặt origin tại chunk origin)
        groundTM?.ClearAllTiles();
        foregroundTM?.ClearAllTiles();
        if (propsParent != null)
        {
            foreach (Transform c in propsParent) Destroy(c.gameObject); // hoặc pool các props nếu cần
        }

        // ensure there is a save record for this chunk (may be empty)
        if (!chunkSaves.ContainsKey(idx)) chunkSaves[idx] = new ChunkSaveData();

        // generate base deterministic for chunk
        RectInt bounds = new RectInt(idx.x * chunkWidth, idx.y * chunkHeight, chunkWidth, chunkHeight);
        GenerateBaseChunk(bounds, groundTM, foregroundTM, idx);

        // apply saved overrides (player-made changes)
        var save = chunkSaves[idx];
        foreach (var kv in save.groundOverrides)
            groundTM.SetTile(kv.Key, kv.Value);
        foreach (var kv in save.foregroundOverrides)
            foregroundTM.SetTile(kv.Key, kv.Value);

        // spawn props: if there are saved prop entries, spawn those that are not collected.
        // if there are no saved props yet, GenerateBaseChunk already added deterministic props into save.props (see below).
        Dictionary<Vector3Int, GameObject> spawned = new Dictionary<Vector3Int, GameObject>();
        if (save.props != null && save.props.Count > 0)
        {
            for (int i = 0; i < save.props.Count; i++)
            {
                var ps = save.props[i];
                if (ps.collected) continue;
                GameObject prefab = GetPropPrefabByIndex(ps.prefabIndex);
                if (prefab == null) continue;
                Vector3 worldPos = groundTM.CellToWorld(ps.localCell) + new Vector3(0.5f, 0.5f, 0f);
                var obj = Instantiate(prefab, worldPos, Quaternion.identity, propsParent);
                spawned[ps.localCell] = obj;
            }
        }

        // store instance
        var inst = new ChunkInstance()
        {
            go = go,
            ground = groundTM,
            foreground = foregroundTM,
            propsParent = propsParent,
            spawnedProps = spawned
        };
        loaded[idx] = inst;
    }

    void UnloadChunk(Vector2Int idx)
    {
        if (!loaded.TryGetValue(idx, out var inst)) return;
        // instead of destroying, we can disable and push to pool
        if (usePooling)
        {
            // clear tilemaps + disable
            inst.ground?.ClearAllTiles();
            inst.foreground?.ClearAllTiles();
            if (inst.propsParent != null)
            {
                foreach (Transform c in inst.propsParent) Destroy(c.gameObject);
            }
            inst.go.SetActive(false);
            pool.Push(inst.go);
        }
        else
        {
            Destroy(inst.go);
        }

        loaded.Remove(idx);
    }

    // --------------------------
    // Generation for one chunk
    // --------------------------
    void GenerateBaseChunk(RectInt B, Tilemap groundTM, Tilemap foregroundTM, Vector2Int idx)
    {
        // deterministic RNG per chunk
        System.Random rng = new System.Random(Hash2(idx.x, idx.y, seed));
        Vector2 noiseOffset = new Vector2((float)(rng.NextDouble() * 10000f), (float)(rng.NextDouble() * 10000f));
        float noiseScale = 0.08f;

        // ensure save entry exists
        var save = chunkSaves[idx];
        if (save.props == null) save.props = new List<PropSave>();

        // iterate in blocks like trước (4x4 water blocks)
        for (int x = B.xMin; x < B.xMax; x += 4)
        {
            for (int y = B.yMin; y < B.yMax; y += 4)
            {
                float avg = 0f;
                for (int dx = 0; dx < 4; dx++)
                    for (int dy = 0; dy < 4; dy++)
                        avg += Mathf.PerlinNoise(noiseOffset.x + (x + dx) * noiseScale, noiseOffset.y + (y + dy) * noiseScale);
                avg /= 16f;

                // set ground local cells (local coords relative to chunk origin)
                for (int dx = 0; dx < 4 && x + dx < B.xMax; dx++)
                    for (int dy = 0; dy < 4 && y + dy < B.yMax; dy++)
                    {
                        Vector3Int worldCell = new Vector3Int(x + dx, y + dy, 0);
                        Vector3Int localCell = WorldToLocalCell(worldCell, idx);
                        groundTM.SetTile(localCell, biome.Pick(biome.grassTiles, rng));
                    }

                // water or cliff on foreground
                if (avg <= biome.waterThreshold)
                {
                    for (int dx = 0; dx < 4 && x + dx < B.xMax; dx++)
                        for (int dy = 0; dy < 4 && y + dy < B.yMax; dy++)
                        {
                            Vector3Int worldCell = new Vector3Int(x + dx, y + dy, 0);
                            Vector3Int localCell = WorldToLocalCell(worldCell, idx);
                            foregroundTM.SetTile(localCell, biome.Pick(biome.waterTiles, rng));
                        }
                }
                else if (avg > 0.8f && biome.cliffTiles.Length > 0)
                {
                    for (int dx = 0; dx < 4 && x + dx < B.xMax; dx++)
                        for (int dy = 0; dy < 4 && y + dy < B.yMax; dy++)
                        {
                            Vector3Int worldCell = new Vector3Int(x + dx, y + dy, 0);
                            Vector3Int localCell = WorldToLocalCell(worldCell, idx);
                            foregroundTM.SetTile(localCell, biome.Pick(biome.cliffTiles, rng));
                        }
                }
            }
        }

        // props (deterministic): nếu chưa có saved props data, sinh và lưu vào save.props.
        if (save.props == null || save.props.Count == 0)
        {
            // iterate each cell and use biome.props densities
            for (int x = B.xMin + 1; x < B.xMax - 1; x++) // avoid border
            {
                for (int y = B.yMin + 1; y < B.yMax - 1; y++)
                {
                    Vector3Int worldCell = new Vector3Int(x, y, 0);
                    Vector3Int localCell = WorldToLocalCell(worldCell, idx);

                    // only spawn on grass
                    var under = groundTM.GetTile(localCell);
                    if (!IsFromSet(under, biome.grassTiles)) continue;

                    // attempt each prop type
                    for (int pi = 0; pi < biome.props.Length; pi++)
                    {
                        var wp = biome.props[pi];
                        if (wp.prefab == null) continue;
                        if ((float)rng.NextDouble() < wp.density)
                        {
                            // check spacing (basic): if already have close prop, skip
                            bool tooClose = false;
                            foreach (var ps in save.props)
                            {
                                if (Vector3Int.Distance(ps.localCell, localCell) < 2f) { tooClose = true; break; }
                            }
                            if (tooClose) continue;

                            // register prop save (not instantiated here - instantiation will be done in LoadChunk after GenerateBaseChunk)
                            save.props.Add(new PropSave() { prefabIndex = pi, localCell = localCell, collected = false });
                            break; // only one prop per tile
                        }
                    }
                }
            }
        }
    }

    // --------------------------
    // Public API: modify tiles & collect props
    // --------------------------
    /// <summary> Thay tile (ground hoặc foreground) tại vị trí worldCell. newTile có thể là null để clear. </summary>
    public void ModifyTileAtWorldCell(Vector3Int worldCell, TileBase newTile, bool foreground = false)
    {
        Vector2Int chunkIdx = CellToChunk(worldCell);
        if (!chunkSaves.ContainsKey(chunkIdx)) chunkSaves[chunkIdx] = new ChunkSaveData();
        var save = chunkSaves[chunkIdx];
        Vector3Int local = WorldToLocalCell(worldCell, chunkIdx);
        if (foreground)
        {
            if (newTile == null) save.foregroundOverrides.Remove(local);
            else save.foregroundOverrides[local] = newTile;

            if (loaded.TryGetValue(chunkIdx, out var inst)) inst.foreground.SetTile(local, newTile);
        }
        else
        {
            if (newTile == null) save.groundOverrides.Remove(local);
            else save.groundOverrides[local] = newTile;

            if (loaded.TryGetValue(chunkIdx, out var inst)) inst.ground.SetTile(local, newTile);
        }
    }

    /// <summary> Gọi khi player "nhặt" / "thu thập" prop tại ô worldCell. Trả về true nếu có prop bị thu. </summary>
    public bool CollectPropAtWorldCell(Vector3Int worldCell)
    {
        Vector2Int chunkIdx = CellToChunk(worldCell);
        if (!chunkSaves.TryGetValue(chunkIdx, out var save)) return false;
        Vector3Int local = WorldToLocalCell(worldCell, chunkIdx);

        for (int i = 0; i < save.props.Count; i++)
        {
            var ps = save.props[i];
            if (ps.localCell == local && !ps.collected)
            {
                ps.collected = true;
                save.props[i] = ps; // update

                // destroy instance if loaded
                if (loaded.TryGetValue(chunkIdx, out var inst))
                {
                    if (inst.spawnedProps.TryGetValue(local, out var go))
                    {
                        Destroy(go);
                        inst.spawnedProps.Remove(local);
                    }
                }
                return true;
            }
        }
        return false;
    }

    // --------------------------
    // Helpers
    // --------------------------
    GameObject GetPropPrefabByIndex(int idx)
    {
        if (biome == null || biome.props == null || idx < 0 || idx >= biome.props.Length) return null;
        return biome.props[idx].prefab;
    }

    bool IsFromSet(TileBase t, BiomeDefinition.WeightedTile[] set)
    {
        if (t == null) return false;
        foreach (var w in set) if (w.tile == t) return true;
        return false;
    }

    Vector3Int WorldToCell(Vector3 worldPos)
    {
        var tm = GetAnyChunkTilemap();
        return tm.layoutGrid.WorldToCell(worldPos);
    }

    Tilemap GetAnyChunkTilemap()
    {
        if (chunkPrefab == null) return GetComponentInChildren<Tilemap>();
        return chunkPrefab.GetComponentInChildren<Tilemap>();
    }

    Vector2Int CellToChunk(Vector3Int cell)
    {
        int cx = Mathf.FloorToInt((float)cell.x / chunkWidth);
        int cy = Mathf.FloorToInt((float)cell.y / chunkHeight);
        return new Vector2Int(cx, cy);
    }

    Vector3 ChunkOriginWorld(Vector2Int idx)
    {
        var tm = GetAnyChunkTilemap();
        var cell = new Vector3Int(idx.x * chunkWidth, idx.y * chunkHeight, 0);
        return tm.layoutGrid.CellToWorld(cell);
    }

    Vector3Int WorldToLocalCell(Vector3Int worldCell, Vector2Int chunkIdx)
    {
        int lx = worldCell.x - chunkIdx.x * chunkWidth;
        int ly = worldCell.y - chunkIdx.y * chunkHeight;
        return new Vector3Int(lx, ly, 0);
    }

    int Hash2(int x, int y, int s)
    {
        unchecked { return s ^ (x * 73856093) ^ (y * 19349663); }
    }

    // --------------------------
    // Internal helper types
    // --------------------------
    class ChunkInstance
    {
        public GameObject go;
        public Tilemap ground;
        public Tilemap foreground;
        public Transform propsParent;
        public Dictionary<Vector3Int, GameObject> spawnedProps = new Dictionary<Vector3Int, GameObject>();
    }

    [Serializable]
    class ChunkSaveData
    {
        // key = local cell (relative to chunk origin)
        public Dictionary<Vector3Int, TileBase> groundOverrides = new Dictionary<Vector3Int, TileBase>();
        public Dictionary<Vector3Int, TileBase> foregroundOverrides = new Dictionary<Vector3Int, TileBase>();
        public List<PropSave> props = new List<PropSave>();
    }

    [Serializable]
    struct PropSave
    {
        public int prefabIndex;      // index in biome.props
        public Vector3Int localCell; // trong local chunk
        public bool collected;
    }
}
