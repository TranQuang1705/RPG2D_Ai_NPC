using UnityEngine;
using UnityEngine.Tilemaps;
using Cinemachine;
using System.Collections.Generic;
using System.Collections;

public class MapGenerator : MonoBehaviour
{
    public struct RectangleBridge
    {
        public int minX, maxX, minY, maxY;
        public bool isHorizontal;
        public bool IsValid;

        public int Width => maxX - minX + 1;
        public int Height => maxY - minY + 1;
    }

    [Header("Ground Generation")]
    public bool onlyRoadIsDirt = true; 

    [Header("Refs")]
    public BiomeDefinition biome;
    public Tilemap groundTM;
    public Tilemap overlayTM;
    [Tooltip("Tilemap chỉ để vẽ nước (cosmetic).")]
    public Tilemap waterTM;
    public Tilemap foregroundTM; 

    public Tilemap roadTM;  

    public Transform propsParent; 

    [Header("Size & Seed")]
    public int width = 80;
    public int height = 48;
    public int seed = 0;
    public float noiseScale = 0.08f;
    public Vector2 noiseOffset;

    [Header("Main Road (always on)")]
    [Tooltip("Luôn tạo đường chính trái->phải, rộng đúng 3 tile")]
    public int roadWidth = 3;
    [Header("Branches")]
    [Tooltip("Số nhánh rẽ từ đường chính (cấp 1).")]
    public int firstLevelBranches = 3;
    [Tooltip("Độ sâu tối đa của hệ nhánh (ví dụ 2 = nhánh mẹ + nhánh con).")]
    public int maxBranchDepth = 2;
    [Tooltip("Bề rộng nhánh (tile, số lẻ).")]
    public int branchWidth = 3;
    [Tooltip("Khoảng cách tối thiểu giữa các điểm rẽ trên cùng một đường mẹ.")]
    public int branchSpacingMin = 10;
    [Tooltip("Nhánh đi đến GÓC của map (true) hay chỉ đến mép (false).")]
    public bool branchEndsAtCorner = true;
    [Tooltip("Khoảng cách tối thiểu (tile) nhánh phải cách MỌI đường đã có.")]
    public int branchSeparation = 6;

    [Tooltip("Mức phạt khi đi gần đường (càng cao càng né mạnh).")]
    public float nearRoadPenalty = 50f;

    [Tooltip("Luân phiên hướng rẽ trên cấp 1: Lên, Xuống, Lên, ...")]
    public bool alternateFirstLevelUpDown = true;
    [Header("Props")]
    public float minPropSpacing = 1.6f;

    [Header("Connected Water / River")]
    [Tooltip("Bật để tạo một con sông lớn liên tục thay vì rải lấm tấm theo noise.")]
    public bool useConnectedRiver = true;

    [Tooltip("Bề rộng trung bình của sông (tile).")]
    public int riverMeanWidth = 4;

    [Tooltip("Độ dao động bề rộng tối đa xung quanh mean (tile).")]
    public int riverWidthVariation = 2;

    [Tooltip("Độ uốn lượn: biên độ lệch theo noise.")]
    public float riverMeanderAmplitude = 6f;

    [Tooltip("Tần số uốn lượn theo noise.")]
    public float riverMeanderFreq = 0.03f;

    [Tooltip("Tỉ lệ tạo đầm lầy nối với sông (0..1).")]
    public float swampChance = 0.25f;

    [Tooltip("Bán kính nở đầm lầy tối đa (tile).")]
    public int swampMaxRadius = 5;

    [Tooltip("Số hạt giống đầm lầy mọc dọc hai bên bờ sông.")]
    public int swampSeedsPer100Tiles = 6;
    [Tooltip("Xác suất xét tạo nhánh tại mỗi điểm ứng viên trên sông chính.")]
    public float riverBranchChance = 0.28f;

    [Tooltip("Số nhánh tối đa từ sông chính.")]
    public int riverBranchMax = 2;

    [Tooltip("Độ dài nhánh tối thiểu (tiles).")]
    public int riverBranchLenMin = 18;

    [Tooltip("Độ dài nhánh tối đa (tiles).")]
    public int riverBranchLenMax = 60;

    [Tooltip("Nhánh hẹp hơn sông chính bao nhiêu (tỉ lệ bề rộng).")]
    [Range(0.3f, 1.0f)]
    public float riverBranchWidthFactor = 0.75f;

    [Tooltip("Khoảng cách tối thiểu (tiles) giữa hai vị trí xét tạo nhánh trên sông chính.")]
    public int riverBranchSpacing = 12;
    [Header("Water props placement")]
    [Tooltip("Ô nước phải cách bờ ít nhất n tile (Chebyshev). 1 = không nằm sát bờ.")]
    public int waterPropMinDepth = 1;


    [Header("Camera & Spawn")]
    public PolygonCollider2D cameraConfiner;
    public CinemachineVirtualCamera vcam;
    public Transform player;
    public float confinerPadding = 0.0f;
    public bool snapPlayerToCenter = true;
    [Header("Village 2x2 (fixed layout)")]
    public GameObject[] housePrefabs;      
    public Transform villageParent;          
    [Range(1, 8)] public int houseTiles = 8; 
    [Range(1, 7)] public int roadW = 3;      
    [Range(0, 6)] public int plotMargin = 2; 
                                            
    [Header("Stone Path")]
    public Tilemap stonePathTM;         
    public RuleTile stoneRuleTile;      
    [Range(1, 7)] public int stonePathWidth = 3;   
    [Header("Organic Village")]
    public bool useOrganicVillage = true;
    [Range(5, 120)] public int targetHouses = 24;
    public int lotWidthMin = 6, lotWidthMax = 9;
    public int lotDepthMin = 5, lotDepthMax = 7;
    public int lotSetback = 2;           
    public int lotGap = 2;                
    public float housePickRetry = 0.25f;   
    public GameObject fountainPrefab;    
    public GameObject lampPrefab, benchPrefab; 

    [Header("Single Village Prefab (spawn đúng 1 cái)")]
    public GameObject villagePrefab;              
    [Tooltip("Kích thước footprint của prefab tính theo tiles (x = width, y = height).")]
    public Vector2Int villageFootprint = new Vector2Int(18, 14);
    [Tooltip("Vùng đệm xung quanh làng (tiles) để không sát đường/vật thể khác.")]
    public int villagePadding = 2;
    [Tooltip("Khoảng cách tối thiểu tới đường (Manhattan).")]
    public int villageMinRoadDist = 3;
    [Tooltip("Số lần thử tìm chỗ đặt làng.")]
    public int villageTryCount = 150;

    [Header("Village Bounds")]
    [Tooltip("Kích thước vùng làng (tile). Mọi nhà/quảng trường chỉ sinh trong vùng này.")]
    public int villageRectWidth = 48;
    public int villageRectHeight = 32;
    [Tooltip("Đặt ở giữa map (true) hoặc lệch theo offset (false + offset).")]
    public bool villageCentered = true;
    public Vector2Int villageOffset = Vector2Int.zero; 

    [Header("Trail Debug")]
    public GameObject fireflyTrailPrefab;
    public GridManager grid;

    [Header("Bridge System")]
    [Tooltip("Tilemap riêng cho cầu (Bridge).")]
    public Tilemap bridgeTM;

    [Tooltip("Tile cho cầu nằm ngang.")]
    public TileBase bridgeTileHorizontal;

    [Tooltip("Tile cho cầu hướng dọc.")]
    public TileBase bridgeTileVertical;
    [Tooltip("Chiều rộng hình chữ nhật cầu (theo chiều ngang).")]
    public int bridgeWidthHorizontal = 5;

    [Tooltip("Chiều cao hình chữ nhật cầu (theo chiều dọc).")]
    public int bridgeHeightVertical = 5;
    int villageX0, villageY0, villageX1, villageY1;



    System.Random rng;
    bool[,] roadMask;


    int[,] distToRoad;



    void Start()
    {
        if (seed == 0) seed = Random.Range(1, int.MaxValue);
        rng = new System.Random(seed);
        noiseOffset = new Vector2((float)rng.NextDouble() * 1000f, (float)rng.NextDouble() * 1000f);

        roadTM?.ClearAllTiles();

        if (propsParent != null) { foreach (Transform c in propsParent) Destroy(c.gameObject); }
        roadMask = new bool[width, height];

        GenerateBase();
        GenerateRandomRiverAndSwamps();     
        BuildGuaranteedMainRoad(roadWidth: 3); 
        BuildBridgesOverRivers();       

        int rx = Mathf.Max(6, (int)(width * 0.30f));
        int ry = Mathf.Max(4, (int)(height * 0.20f));
        GenerateCosmeticLake(new Vector2Int(width / 2, height / 2), rx, ry);

        if (useConnectedRiver) GenerateRandomRiverAndSwamps();
        else ScatterWaterOldStyle();


        var mainPath = BuildGuaranteedMainRoad(roadWidth: 3);
        distToRoad = ComputeRoadDistance();


        GenerateBranchesRecursive(mainPath, isParentHorizontal: true, depthLeft: Mathf.Max(0, maxBranchDepth - 1));
        SpawnVillageOnce();


        FixRoadDiagonals();
        BakeRoadMask();


        ScatterDecorTiles();
        ScatterPrefabs();
        ScatterWaterProps();
        ScatterForeground();

     
        AlignTilemaps();
        UpdateCameraConfiner();
        SnapOrSpawnPlayer();
        WipeVillageArea();
     
        grid = gameObject.AddComponent<GridManager>();
        grid.width = width;
        grid.height = height;
        grid.groundTilemap = groundTM;
        grid.obstacleTilemap = foregroundTM;
        grid.GenerateGrid();
        


        Vector3 playerWorld = player != null ? player.position : Vector3.zero;
        Vector3 villageWorld = Vector3.zero;

        GameObject village = GameObject.Find("Camp");
        if (village != null)
            villageWorld = village.transform.position;


        Vector3Int startCell = groundTM.WorldToCell(playerWorld);
        Vector3Int endCell = groundTM.WorldToCell(villageWorld);

        Vector2Int start = new Vector2Int(startCell.x, startCell.y);
        Vector2Int end = new Vector2Int(endCell.x, endCell.y);






    }



    void GenerateBase()
    {
        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                float noise = Mathf.PerlinNoise(
                    noiseOffset.x + x * noiseScale,
                    noiseOffset.y + y * noiseScale
                );

                // GRASS nền mặc định
                groundTM.SetTile(new Vector3Int(x, y, 0),
                                 biome.Pick(biome.grassTiles, rng));

                // DIRT tự nhiên (tùy chọn) - spawn theo lưới 1x1
                if (!onlyRoadIsDirt && noise <= biome.dirtThreshold)
                {
                    groundTM.SetTile(new Vector3Int(x, y, 0),
                                     biome.Pick(biome.dirtTiles, rng));
                }
                // CLIFF - spawn theo lưới 1x1
                else if (noise > 0.8f && biome.cliffTiles.Length > 0)
                {
                    foregroundTM.SetTile(new Vector3Int(x, y, 0),
                                         biome.Pick(biome.cliffTiles, rng));
                }
            }
        }
    }


    // === Làng 2x2: 4 căn nhà 8x8, đường bao + đường chữ thập giữa ===
    // === Làng 2x2: 4 căn nhà 8x8, đường bao + đường chữ thập giữa + stone path ===
    // ======= SINGLE VILLAGE SPAWN =======
    void SpawnVillageOnce()
    {
        if (!villagePrefab) return;

        // Ưu tiên khu giữa bản đồ một chút
        int margin = Mathf.Max(3, villagePadding + 1);
        int tries = villageTryCount;
        Vector2Int size = new Vector2Int(
            Mathf.Max(2, villageFootprint.x),
            Mathf.Max(2, villageFootprint.y)
        );

        // Tìm chỗ đặt
        for (int t = 0; t < tries; t++)
        {
            int cx = rng.Next(margin + size.x / 2, width - margin - size.x / 2);
            int cy = rng.Next(margin + size.y / 2, height - margin - size.y / 2);

            if (RectOkForVillage(cx, cy, size, villagePadding))
            {
                PlaceVillageAtCenter(new Vector2Int(cx, cy), size);
                return;
            }
        }

        // Fallback: cố đặt gần giữa map nếu vòng lặp thất bại
        Vector2Int center = new Vector2Int(width / 2, height / 2);
        if (RectOkForVillage(center.x, center.y, size, villagePadding))
            PlaceVillageAtCenter(center, size);
    }

    bool RectOkForVillage(int cx, int cy, Vector2Int size, int pad)
    {
        // chuyển từ tâm & kích thước sang biên
        int x0 = cx - size.x / 2 - pad;
        int x1 = cx + (size.x - 1) / 2 + pad;
        int y0 = cy - size.y / 2 - pad;
        int y1 = cy + (size.y - 1) / 2 + pad;

        if (x0 < 1 || y0 < 1 || x1 > width - 2 || y1 > height - 2) return false;

        for (int x = x0; x <= x1; x++)
            for (int y = y0; y <= y1; y++)
            {
                // tránh ô đã bị chiếm, đường, nước, cliff
                if (occupied != null && occupied[x, y]) return false;
                if (roadMask != null && roadMask[x, y]) return false;

                var p = new Vector3Int(x, y, 0);
                var fg = foregroundTM ? foregroundTM.GetTile(p) : null;
                if (IsFromSet(fg, biome.waterTiles) || IsFromSet(fg, biome.cliffTiles)) return false;

                // mặt đất phải là grass hoặc dirt
                var g = groundTM.GetTile(p);
                if (!(IsFromSet(g, biome.grassTiles) || IsFromSet(g, biome.dirtTiles))) return false;

                // cách đường tối thiểu
                if (distToRoad != null && distToRoad[x, y] < villageMinRoadDist) return false;
            }
        return true;
    }

    void PlaceVillageAtCenter(Vector2Int center, Vector2Int size)
    {
        int x0Core = center.x - size.x / 2;
        int x1Core = center.x + (size.x - 1) / 2;
        int y0Core = center.y - size.y / 2;
        int y1Core = center.y + (size.y - 1) / 2;

        // vùng cấm = core + padding
        int x0 = Mathf.Max(1, x0Core - villagePadding);
        int x1 = Mathf.Min(width - 2, x1Core + villagePadding);
        int y0 = Mathf.Max(1, y0Core - villagePadding);
        int y1 = Mathf.Min(height - 2, y1Core + villagePadding);

        if (occupied == null) occupied = new bool[width, height];

        for (int x = x0; x <= x1; x++)
            for (int y = y0; y <= y1; y++)
            {
                var p = new Vector3Int(x, y, 0);
                // dọn sạch nền trong core (không bắt buộc dọn vùng padding)
                if (x >= x0Core && x <= x1Core && y >= y0Core && y <= y1Core)
                {
                    groundTM.SetTile(p, biome.Pick(biome.grassTiles, rng));
                    overlayTM?.SetTile(p, null);
                    if (foregroundTM != null)
                    {
                        var f = foregroundTM.GetTile(p);
                        if (IsFromSet(f, biome.waterTiles) || IsFromSet(f, biome.cliffTiles))
                            foregroundTM.SetTile(p, null);
                    }
                }
                occupied[x, y] = true; // 🔒 KHÓA HOÀN TOÀN khu làng + viền
            }

        Vector3 world = groundTM.CellToWorld(new Vector3Int(center.x, center.y, 0)) + new Vector3(0.5f, 0.5f, 0);
        var go = Instantiate(villagePrefab, world, Quaternion.identity, villageParent != null ? villageParent : propsParent);
        go.name = "Camp";
        // đánh dấu & đăng ký
        var anchor = go.GetComponent<POIAnchor>();
        if (anchor == null) anchor = go.AddComponent<POIAnchor>();
        anchor.poiId = "Camp";
        anchor.displayName = "Camp";

        // đăng ký vào registry để các hệ khác tra cứu
        if (POIRegistry.I != null) POIRegistry.I.Register(anchor.poiId, go.transform);
        distToRoad = ComputeRoadDistance();
    }


    // ===== Stone path helpers =====

    // Kiểm tra cell nằm trong biên làng
    bool InsideVillage(int x, int y)
        => x >= villageX0 && x <= villageX1 && y >= villageY0 && y <= villageY1;

    // Tìm ô road gần nhất (theo Manhattan) bên trong làng, xuất phát từ start
    Vector2Int FindNearestRoadInsideVillage(Vector2Int start)
    {
        Queue<Vector2Int> q = new Queue<Vector2Int>();
        HashSet<Vector2Int> vis = new HashSet<Vector2Int>();
        q.Enqueue(start); vis.Add(start);

        Vector2Int[] dirs4 = { new Vector2Int(1, 0), new Vector2Int(-1, 0), new Vector2Int(0, 1), new Vector2Int(0, -1) };

        while (q.Count > 0)
        {
            var cur = q.Dequeue();
            if (cur.x < 1 || cur.x >= width - 1 || cur.y < 1 || cur.y >= height - 1) continue;
            if (!InsideVillage(cur.x, cur.y)) continue;     // chỉ xét trong phạm vi làng
            if (roadMask[cur.x, cur.y]) return cur;         // chạm đường làng -> trả về

            foreach (var d in dirs4)
            {
                var nb = new Vector2Int(cur.x + d.x, cur.y + d.y);
                if (vis.Contains(nb)) continue;
                vis.Add(nb);
                q.Enqueue(nb);
            }
        }
        return start; // fallback
    }

    // Lấy đường đi 4 hướng (BFS) từ s -> g; có tùy chọn giới hạn trong làng
    List<Vector3Int> BFSPathGrid(Vector2Int s, Vector2Int g, bool limitToVillage)
    {
        Queue<Vector2Int> q = new Queue<Vector2Int>();
        Dictionary<Vector2Int, Vector2Int> prev = new Dictionary<Vector2Int, Vector2Int>();
        HashSet<Vector2Int> vis = new HashSet<Vector2Int>();
        q.Enqueue(s); vis.Add(s);

        Vector2Int[] dirs4 = { new Vector2Int(1, 0), new Vector2Int(-1, 0), new Vector2Int(0, 1), new Vector2Int(0, -1) };

        while (q.Count > 0)
        {
            var cur = q.Dequeue();
            if (cur == g) break;

            foreach (var d in dirs4)
            {
                var nb = new Vector2Int(cur.x + d.x, cur.y + d.y);
                if (nb.x < 1 || nb.x >= width - 1 || nb.y < 1 || nb.y >= height - 1) continue;
                if (limitToVillage && !InsideVillage(nb.x, nb.y)) continue;
                if (vis.Contains(nb)) continue;

                // tránh nước/cliff để lối đá không đè lên
                var fg = foregroundTM?.GetTile(new Vector3Int(nb.x, nb.y, 0));
                if (IsFromSet(fg, biome.waterTiles) || IsFromSet(fg, biome.cliffTiles)) continue;

                vis.Add(nb);
                prev[nb] = cur;
                q.Enqueue(nb);
            }
        }

        // reconstruct
        List<Vector3Int> cells = new List<Vector3Int>();
        Vector2Int cur2 = g;
        if (!prev.ContainsKey(cur2) && cur2 != s) return cells; // không tìm thấy
        while (cur2 != s)
        {
            cells.Add(new Vector3Int(cur2.x, cur2.y, 0));
            if (!prev.ContainsKey(cur2)) break;
            cur2 = prev[cur2];
        }
        cells.Add(new Vector3Int(s.x, s.y, 0));
        cells.Reverse();
        return cells;
    }

    // Vẽ đường đá theo danh sách cell; dừng ngay khi chạm roadMask
    void DrawStonePath(List<Vector3Int> cells, int w)
    {
        if (stonePathTM == null || stoneRuleTile == null || cells == null || cells.Count == 0) return;
        w = Mathf.Max(1, w | 1);
        int r = w / 2;

        foreach (var c in cells)
        {
            bool hitRoad = roadMask[c.x, c.y];

            // tô dải rộng w×w quanh tâm
            for (int dx = -r; dx <= r; dx++)
                for (int dy = -r; dy <= r; dy++)
                {
                    int xx = c.x + dx, yy = c.y + dy;
                    if (xx < 1 || xx >= width - 1 || yy < 1 || yy >= height - 1) continue;
                    if (!InsideVillage(xx, yy)) continue; // CHỈ vẽ trong làng

                    // né nước/cliff
                    var fg = foregroundTM?.GetTile(new Vector3Int(xx, yy, 0));
                    if (IsFromSet(fg, biome.waterTiles) || IsFromSet(fg, biome.cliffTiles)) continue;

                    stonePathTM.SetTile(new Vector3Int(xx, yy, 0), stoneRuleTile);
                }

            if (hitRoad) break; // chạm đường làng -> kết thúc, không vẽ ra ngoài
        }
    }




    // Helper: vẽ khung đường dày w tiles quanh hình chữ nhật [x0..x1, y0..y1]
    void DrawFrameRoad(int x0, int y0, int x1, int y1, int w)
    {
        w = Mathf.Max(1, w | 1); // bảo đảm lẻ
                                 // dưới + trên
        FillRectRoad(x0, y0, x1, y0 + w - 1);
        FillRectRoad(x0, y1 - w + 1, x1, y1);
        // trái + phải
        FillRectRoad(x0, y0, x0 + w - 1, y1);
        FillRectRoad(x1 - w + 1, y0, x1, y1);
    }


    // Vẽ đường (dirt) cho một hình chữ nhật bao gồm cả biên
    void FillRectRoad(int x0, int y0, int x1, int y1)
    {
        int xa = Mathf.Max(1, Mathf.Min(x0, x1));
        int xb = Mathf.Min(width - 2, Mathf.Max(x0, x1));
        int ya = Mathf.Max(1, Mathf.Min(y0, y1));
        int yb = Mathf.Min(height - 2, Mathf.Max(y0, y1));

        for (int x = xa; x <= xb; x++)
            for (int y = ya; y <= yb; y++)
            {
                var p = new Vector3Int(x, y, 0);
                groundTM.SetTile(p, biome.Pick(biome.dirtTiles, rng));
                roadMask[x, y] = true;

                // Xoá nước/cliff che đường
                if (foregroundTM != null)
                {
                    var f = waterTM != null ? waterTM.GetTile(p) : null;
                    if (IsFromSet(f, biome.waterTiles) || IsFromSet(f, biome.cliffTiles))
                        foregroundTM.SetTile(p, null);
                }
                // Xoá overlay decor trên đường
                overlayTM?.SetTile(p, null);
            }
    }

    // Dọn nền cho footprint nhà (x0..x1, y0..y1)
    // Dọn nền cho footprint nhà (x0..x1, y0..y1) = GRASS, xoá overlay/foreground
    void ClearRectForBuilding(int x0, int y0, int x1, int y1)
    {
        for (int x = x0; x <= x1; x++)
            for (int y = y0; y <= y1; y++)
            {
                if (x < 1 || y < 1 || x >= width - 1 || y >= height - 1) continue;
                var p = new Vector3Int(x, y, 0);

                // nền nhà = GRASS
                groundTM.SetTile(p, biome.Pick(biome.grassTiles, rng));
                overlayTM?.SetTile(p, null);
                foregroundTM?.SetTile(p, null);

                // footprint nhà KHÔNG là road
                roadMask[x, y] = false;
            }
    }
    // Vẽ một "khung" (frame) dày w tiles quanh hình chữ nhật [x0..x1, y0..y1]

    bool IsVillageBlocked(int x, int y)
        => occupied != null && x >= 1 && y >= 1 && x < width && y < height && occupied[x, y];
    void WipeVillageArea()
    {
        if (occupied == null) return;
        for (int x = 1; x < width - 1; x++)
            for (int y = 1; y < height - 1; y++)
            {
                if (!occupied[x, y]) continue;
                var p = new Vector3Int(x, y, 0);

                // nền sạch: GRASS, không road
                groundTM.SetTile(p, biome.Pick(biome.grassTiles, rng));
                roadMask[x, y] = false;

                // xoá mọi lớp trên
                overlayTM?.SetTile(p, null);
                foregroundTM?.SetTile(p, null);
                waterTM?.SetTile(p, null);
            }
    }

    // =========================
    // Foreground scatter (cliff nhẹ)
    // =========================
    void ScatterForeground()
    {
        if (foregroundTM == null) return;
        if (biome.cliffTiles == null || biome.cliffTiles.Length == 0) return;

        for (int x = 0; x < width; x += 4)
            for (int y = 0; y < height; y += 4)
            {
                float avg = 0;
                for (int dx = 0; dx < 4; dx++)
                    for (int dy = 0; dy < 4; dy++)
                        avg += Mathf.PerlinNoise(noiseOffset.x + (x + dx) * noiseScale * 0.5f,
                                                 noiseOffset.y + (y + dy) * noiseScale * 0.5f);
                avg /= 16f;

                if (avg > 0.78f)
                {
                    for (int dx = 0; dx < 4 && x + dx < width; dx++)
                        for (int dy = 0; dy < 4 && y + dy < height; dy++)
                        {
                            int xx = x + dx, yy = y + dy;
                            if (occupied != null && occupied[xx, yy]) continue; // ⛔ tránh làng
                            var p = new Vector3Int(xx, yy, 0);
                            var under = groundTM.GetTile(p);
                            if (IsFromSet(under, biome.grassTiles))
                                foregroundTM.SetTile(p, biome.Pick(biome.cliffTiles, rng));
                        }
                }
            }
    }


    void AlignTilemaps()
    {
        if (groundTM == null || foregroundTM == null) return;

        groundTM.tileAnchor = new Vector3(0.5f, 0.5f, 0f);
        foregroundTM.tileAnchor = new Vector3(0.5f, 0.5f, 0f);

        foregroundTM.transform.localPosition = Vector3.zero;
        foregroundTM.transform.localRotation = Quaternion.identity;
        foregroundTM.transform.localScale = Vector3.one;

        groundTM.transform.localPosition = Vector3.zero;
        groundTM.transform.localRotation = Quaternion.identity;
        groundTM.transform.localScale = Vector3.one;

        
    }


    List<Vector2Int> BuildGuaranteedMainRoad(int roadWidth)
    {
        roadWidth = 3; 
        Vector2Int start = new Vector2Int(1, height / 2);
        Vector2Int goal = new Vector2Int(width - 2, height / 2);

        List<Vector2Int> path = AStarPath(start, goal);
        if (path == null || path.Count == 0)
        {
            path = new List<Vector2Int>();
            for (int x = start.x; x <= goal.x; x++)
                path.Add(new Vector2Int(x, start.y));
        }

        foreach (var p in path)
            DrawRoadSegment(p.x, p.y, roadWidth);

        return path;
    }



    List<Vector2Int> AStarPath(Vector2Int start, Vector2Int goal)
    {
        var open = new MinHeapPriorityQueue<Vector2Int>();
        var cameFrom = new Dictionary<Vector2Int, Vector2Int>();
        var gScore = new Dictionary<Vector2Int, float>();
        var fScore = new Dictionary<Vector2Int, float>();

        gScore[start] = 0f;
        fScore[start] = Heuristic(start, goal);
        open.Enqueue(start, fScore[start]);

        Vector2Int[] dirs = new Vector2Int[] {
            new Vector2Int(1,0), new Vector2Int(-1,0),
            new Vector2Int(0,1), new Vector2Int(0,-1)
        };

        while (open.Count > 0)
        {
            var current = open.DequeueMin();

            if (current == goal)
                return ReconstructPath(cameFrom, current);

            foreach (var d in dirs)
            {
                var nb = new Vector2Int(current.x + d.x, current.y + d.y);
                if (nb.x < 1 || nb.x >= width - 1 || nb.y < 1 || nb.y >= height - 1) continue;

                float stepCost = TerrainCost(nb);
                float tentative = gScore[current] + stepCost;

                if (!gScore.ContainsKey(nb) || tentative < gScore[nb])
                {
                    cameFrom[nb] = current;
                    gScore[nb] = tentative;
                    fScore[nb] = tentative + Heuristic(nb, goal);
                    open.Enqueue(nb, fScore[nb]);
                }
            }
        }

        return null;
    }

    float Heuristic(Vector2Int a, Vector2Int b)
    {
        return Mathf.Abs(a.x - b.x) + Mathf.Abs(a.y - b.y);
    }

    float TerrainCost(Vector2Int cell)
    {
        var p = new Vector3Int(cell.x, cell.y, 0);
        TileBase g = groundTM.GetTile(p);
        TileBase f = foregroundTM.GetTile(p);

        if (IsFromSet(f, biome.waterTiles)) return 20f;
        if (IsFromSet(f, biome.cliffTiles)) return 12f;

        if (roadMask[cell.x, cell.y]) return 0.4f;

        if (IsFromSet(g, biome.dirtTiles)) return 1.0f;
        if (IsFromSet(g, biome.grassTiles)) return 1.2f;

        return 2.0f;
    }

    List<Vector2Int> ReconstructPath(Dictionary<Vector2Int, Vector2Int> cameFrom, Vector2Int cur)
    {
        var path = new List<Vector2Int>() { cur };
        while (cameFrom.ContainsKey(cur))
        {
            cur = cameFrom[cur];
            path.Add(cur);
        }
        path.Reverse();
        return path;
    }

    void DrawRoadSegment(int centerX, int centerY, int segmentWidth)
    {
        segmentWidth = 3;
        int r = segmentWidth / 2; 

        for (int dx = -r; dx <= r; dx++)
        {
            for (int dy = -r; dy <= r; dy++)
            {
                int px = centerX + dx;
                int py = centerY + dy;
                if (px < 0 || px >= width || py < 0 || py >= height) continue;

                var p = new Vector3Int(px, py, 0);

    
                if (roadTM != null)
                    roadTM.SetTile(p, biome.Pick(biome.dirtTiles, rng));
                else
                    groundTM.SetTile(p, biome.Pick(biome.dirtTiles, rng));

                roadMask[px, py] = true;

                if (foregroundTM != null)
                {
                    var f = waterTM != null ? waterTM.GetTile(p) : null;
                    if (IsFromSet(f, biome.waterTiles) || IsFromSet(f, biome.cliffTiles))
                        foregroundTM.SetTile(p, null);
                }
            }
        }
    }

    void FixRoadDiagonals()
    {
        if (roadMask == null) return;

        for (int x = 0; x < width - 1; x++)
            for (int y = 0; y < height - 1; y++)
            {
            
                if (roadMask[x, y] && roadMask[x + 1, y + 1] && !roadMask[x + 1, y] && !roadMask[x, y + 1])
                {
                    if (!IsWater(x + 1, y)) roadMask[x + 1, y] = true;
                    if (!IsWater(x, y + 1)) roadMask[x, y + 1] = true;
                }
               
                if (roadMask[x + 1, y] && roadMask[x, y + 1] && !roadMask[x, y] && !roadMask[x + 1, y + 1])
                {
                    if (!IsWater(x, y)) roadMask[x, y] = true;
                    if (!IsWater(x + 1, y + 1)) roadMask[x + 1, y + 1] = true;
                }
            }

    }


    void BakeRoadMask()
    {
        for (int x = 0; x < width; x++)
            for (int y = 0; y < height; y++)
            {
                if (!roadMask[x, y]) continue;

                var p = new Vector3Int(x, y, 0);
                if (IsFromSet(foregroundTM.GetTile(p), biome.waterTiles)) continue;

                var cur = groundTM.GetTile(p);
                if (roadTM != null)
                {
                    if (!IsFromSet(roadTM.GetTile(p), biome.dirtTiles))
                        roadTM.SetTile(p, biome.Pick(biome.dirtTiles, rng));
                }
                else
                {
                    if (!IsFromSet(cur, biome.dirtTiles))
                        groundTM.SetTile(p, biome.Pick(biome.dirtTiles, rng));
                }

            }
    }

    bool IsWater(int x, int y)
    {
        if (x < 0 || y < 0 || x >= width || y >= height) return false;
        var t = waterTM != null ? waterTM.GetTile(new Vector3Int(x, y, 0)) : null;
        return IsFromSet(t, biome.waterTiles);
    }
    bool IsWater(Vector3Int c) => IsWater(c.x, c.y);



    void ScatterDecorTiles()
    {
        for (int x = 1; x < width - 1; x++)
            for (int y = 1; y < height - 1; y++)
            {
                if (occupied != null && occupied[x, y]) continue; 
                if (Random.value > biome.decorChance) continue;

                var p = new Vector3Int(x, y, 0);
                var under = groundTM.GetTile(p);

                if (roadMask[x, y]) continue;
                if (IsFromSet(under, biome.grassTiles))
                    overlayTM?.SetTile(p, biome.Pick(biome.decorTiles, rng));
            }
    }


    void ScatterPrefabs()
    {
        if (biome.props == null || biome.props.Length == 0 || propsParent == null) return;


        for (int x = 1; x < width - 1; x++)
            for (int y = 1; y < height - 1; y++)
            {
                if (occupied != null && occupied[x, y]) continue; 
                if (roadMask[x, y]) continue;                     
                Vector3Int cell = new Vector3Int(x, y, 0);

                var under = groundTM.GetTile(cell);
                var fg = foregroundTM != null ? foregroundTM.GetTile(cell) : null;

                bool isGrass = IsFromSet(under, biome.grassTiles);
                bool isDirt = IsFromSet(under, biome.dirtTiles);
                bool hasWaterOnTop = IsFromSet(fg, biome.waterTiles);
                bool hasCliffOnTop = IsFromSet(fg, biome.cliffTiles);

                if (!(isGrass || isDirt)) continue;
                if (hasWaterOnTop || hasCliffOnTop) continue; 

                foreach (var wp in biome.props)
                {
                    if (wp.prefab == null) continue;
                    if (Random.value < wp.density)
                    {
                        Vector2 pos = new Vector2(cell.x + 0.5f, cell.y + 0.5f);
                        if (Physics2D.OverlapCircle(pos, minPropSpacing) != null) continue;

                        var obj = Instantiate(wp.prefab, (Vector3)cell + new Vector3(0.5f, 0.5f, 0), Quaternion.identity, propsParent);
                        obj.name = wp.prefab.name;
                        break;
                    }
                }
            }
    }
    void ScatterWaterProps()
    {
        if (biome.waterProps == null || biome.waterProps.Length == 0 || propsParent == null) return;
        if (foregroundTM == null) return;


        const int minRoadDist = 2;

        for (int x = 1; x < width - 1; x++)
            for (int y = 1; y < height - 1; y++)
            {
                if (occupied != null && occupied[x, y]) continue;

                Vector3Int cell = new Vector3Int(x, y, 0);

                if (!IsDeepWater(cell, waterPropMinDepth)) continue;

                if (distToRoad != null && distToRoad[x, y] <= minRoadDist) continue;

                if (roadMask != null && roadMask[x, y]) continue;

                Vector2 pos = new Vector2(cell.x + 0.5f, cell.y + 0.5f);
                if (Physics2D.OverlapCircle(pos, minPropSpacing) != null) continue;

                foreach (var wp in biome.waterProps)
                {
                    if (wp.prefab == null) continue;
                    if (Random.value < wp.density)
                    {
                        var obj = Instantiate(
                            wp.prefab,
                            (Vector3)cell + new Vector3(0.5f, 0.5f, 0),
                            Quaternion.identity,
                            propsParent
                        );
                        obj.name = wp.prefab.name;

                        if (occupied != null) occupied[x, y] = true;
                        break;
                    }
                }
            }
    }




    bool IsFromSet(TileBase t, BiomeDefinition.WeightedTile[] set)
    {
        foreach (var w in set) if (w.tile == t) return true;
        return false;
    }

    void UpdateCameraConfiner()
    {
        if (cameraConfiner == null || groundTM == null) return;

        Bounds lb = groundTM.localBounds;

        lb.Expand(new Vector3(confinerPadding * 2f, confinerPadding * 2f, 0f));

        Vector3 wMin = groundTM.transform.TransformPoint(lb.min);
        Vector3 wMax = groundTM.transform.TransformPoint(lb.max);

        Vector2 p0 = (Vector2)cameraConfiner.transform.InverseTransformPoint(new Vector3(wMin.x, wMin.y, 0));
        Vector2 p1 = (Vector2)cameraConfiner.transform.InverseTransformPoint(new Vector3(wMin.x, wMax.y, 0));
        Vector2 p2 = (Vector2)cameraConfiner.transform.InverseTransformPoint(new Vector3(wMax.x, wMax.y, 0));
        Vector2 p3 = (Vector2)cameraConfiner.transform.InverseTransformPoint(new Vector3(wMax.x, wMin.y, 0));

        cameraConfiner.pathCount = 1;
        cameraConfiner.SetPath(0, new Vector2[] { p0, p1, p2, p3 });

#if UNITY_2022_2_OR_NEWER
        var confinerExt = Camera.main?.GetComponent<Cinemachine.CinemachineConfiner2D>();
        if (confinerExt != null) confinerExt.InvalidateCache();
#endif
        var ext = vcam ? vcam.GetComponent<CinemachineConfiner2D>() : null;
        if (ext != null)
        {
            ext.m_BoundingShape2D = cameraConfiner;
            ext.InvalidateCache();
            StartCoroutine(InvalidateNextFrame(ext));
        }
        System.Collections.IEnumerator InvalidateNextFrame(CinemachineConfiner2D ext)
        {
            yield return null; 
            ext.InvalidateCache();
        }
    }


    void SnapOrSpawnPlayer()
    {
        if (!snapPlayerToCenter || groundTM == null) return;
        if (player == null) return;

        Vector3Int center = new Vector3Int(width / 2, height / 2, 0);
        Vector3Int spawnCell = FindNearestWalkable(center, maxRadius: Mathf.Max(width, height));
        Vector3 spawnPos = groundTM.CellToWorld(spawnCell) + new Vector3(0.5f, 0.5f, 0);

        player.position = spawnPos;
    }

    Vector3Int FindNearestWalkable(Vector3Int start, int maxRadius)
    {
        for (int r = 0; r <= maxRadius; r++)
        {
            for (int dx = -r; dx <= r; dx++)
                for (int dy = -r; dy <= r; dy++)
                {
                    var p = new Vector3Int(start.x + dx, start.y + dy, 0);
                    var t = groundTM.GetTile(p);
                    if (t == null) continue;
                    if (IsFromSet(t, biome.waterTiles)) continue; 
                    if (IsFromSet(t, biome.dirtTiles) || IsFromSet(t, biome.grassTiles))
                        return p;
                }
        }
        return start; 
    }



    enum Dir { Up, Down, Left, Right }

    void GenerateBranchesRecursive(List<Vector2Int> parentPath, bool isParentHorizontal, int depthLeft)
    {
        if (parentPath == null || parentPath.Count < 8) return;

        var branchPoints = PickBranchPoints(parentPath, isParentHorizontal ? firstLevelBranches : Mathf.Max(1, firstLevelBranches - 1));

        foreach (var bp in branchPoints)
        {
            List<Dir> candidateDirs = isParentHorizontal
                ? new List<Dir> { rng.NextDouble() < 0.5 ? Dir.Up : Dir.Down } 
                : new List<Dir> { rng.NextDouble() < 0.5 ? Dir.Left : Dir.Right }; 

            foreach (var d in candidateDirs)
            {
                Vector2Int goal = PickBranchGoal(bp, d);
                var path = AStarPath(bp, goal);
                if (path == null || path.Count == 0)
                {
                    path = StraightToEdge(bp, d);
                }

                foreach (var p in path)
                    DrawRoadSegment(p.x, p.y, Mathf.Max(1, branchWidth | 1));
                distToRoad = ComputeRoadDistance();
                if (depthLeft > 0)
                {
                    bool childIsHorizontal = !isParentHorizontal; 
                    GenerateBranchesRecursive(path, childIsHorizontal, depthLeft - 1);
                }
            }
        }
    }

    List<Vector2Int> PickBranchPoints(List<Vector2Int> path, int count)
    {
        var picks = new List<Vector2Int>();
        if (count <= 0) return picks;

        int startIdx = Mathf.RoundToInt(path.Count * 0.15f);
        int endIdx = Mathf.RoundToInt(path.Count * 0.85f);

        int lastPick = -9999;
        int tries = 0;
        while (picks.Count < count && tries < 200)
        {
            int i = rng.Next(startIdx, endIdx);
            if (i - lastPick >= branchSpacingMin)
            {
                picks.Add(path[i]);
                lastPick = i;
            }
            tries++;
        }
        return picks;
    }

    Vector2Int PickBranchGoal(Vector2Int start, Dir d)
    {
        if (branchEndsAtCorner)
        {
            switch (d)
            {
                case Dir.Up: return new Vector2Int(width - 2, height - 2); 
                case Dir.Down: return new Vector2Int(width - 2, 1);         
                case Dir.Left: return new Vector2Int(1, start.y < height / 2 ? 1 : height - 2); 
                case Dir.Right: return new Vector2Int(width - 2, start.y < height / 2 ? 1 : height - 2);
            }
        }
        else
        {
            switch (d)
            {
                case Dir.Up: return new Vector2Int(start.x, height - 2);
                case Dir.Down: return new Vector2Int(start.x, 1);
                case Dir.Left: return new Vector2Int(1, start.y);
                case Dir.Right: return new Vector2Int(width - 2, start.y);
            }
        }
        return start;
    }

    List<Vector2Int> StraightToEdge(Vector2Int s, Dir d)
    {
        var list = new List<Vector2Int>();
        Vector2Int cur = s;
        list.Add(cur);
        switch (d)
        {
            case Dir.Up:
                for (int y = s.y + 1; y <= height - 2; y++) list.Add(new Vector2Int(s.x, y));
                break;
            case Dir.Down:
                for (int y = s.y - 1; y >= 1; y--) list.Add(new Vector2Int(s.x, y));
                break;
            case Dir.Left:
                for (int x = s.x - 1; x >= 1; x--) list.Add(new Vector2Int(x, s.y));
                break;
            case Dir.Right:
                for (int x = s.x + 1; x <= width - 2; x++) list.Add(new Vector2Int(x, s.y));
                break;
        }
        return list;
    }
    int[,] ComputeRoadDistance()
    {
        int[,] dist = new int[width, height];
        Queue<Vector2Int> q = new Queue<Vector2Int>();

        // init
        for (int x = 0; x < width; x++)
            for (int y = 0; y < height; y++)
            {
                if (roadMask[x, y]) { dist[x, y] = 0; q.Enqueue(new Vector2Int(x, y)); }
                else dist[x, y] = int.MaxValue;
            }

        Vector2Int[] dirs = { new Vector2Int(1, 0), new Vector2Int(-1, 0), new Vector2Int(0, 1), new Vector2Int(0, -1) };

        while (q.Count > 0)
        {
            var c = q.Dequeue();
            int cd = dist[c.x, c.y];
            foreach (var d in dirs)
            {
                int nx = c.x + d.x, ny = c.y + d.y;
                if (nx < 0 || nx >= width || ny < 0 || ny >= height) continue;
                if (dist[nx, ny] > cd + 1)
                {
                    dist[nx, ny] = cd + 1;
                    q.Enqueue(new Vector2Int(nx, ny));
                }
            }
        }

        return dist;
    }

    enum Border { Left, Right, Top, Bottom }

    Vector2Int RandomBorderCell(Border b)
    {
        switch (b)
        {
            case Border.Left: return new Vector2Int(1, Mathf.Clamp(rng.Next(2, height - 2), 2, height - 3));
            case Border.Right: return new Vector2Int(width - 2, Mathf.Clamp(rng.Next(2, height - 2), 2, height - 3));
            case Border.Top: return new Vector2Int(Mathf.Clamp(rng.Next(2, width - 2), 2, width - 3), height - 2);
            case Border.Bottom: return new Vector2Int(Mathf.Clamp(rng.Next(2, width - 2), 2, width - 3), 1);
        }
        return new Vector2Int(1, height / 2);
    }

    Border RandomBorderExcept(Border except)
    {
        var options = new List<Border> { Border.Left, Border.Right, Border.Top, Border.Bottom };
        options.Remove(except);
        return options[rng.Next(options.Count)];
    }

    void GenerateRandomRiverAndSwamps()
    {
        if (biome.waterTiles == null || biome.waterTiles.Length == 0 || foregroundTM == null) return;

        Border b0 = (Border)rng.Next(0, 4);
        Border b1 = RandomBorderExcept(b0);
        Vector2Int start = RandomBorderCell(b0);
        Vector2Int goal = RandomBorderCell(b1);

   
        var riverPath = RiverAStar(start, goal);
        if (riverPath == null || riverPath.Count == 0)
        {
            riverPath = StraightBorderFallback(start, goal);
        }

        foreach (var c in riverPath)
        {
            int w = riverMeanWidth + rng.Next(-riverWidthVariation, riverWidthVariation + 1);
            w = Mathf.Max(2, w);
            PaintDiskWater(c.x, c.y, w / 2);

            for (int dy = -w / 2; dy <= w / 2; dy++)
            {
                int yy = c.y + dy; if (yy < 1 || yy >= height - 1) continue;
                for (int dx = -w / 2; dx <= w / 2; dx++)
                {
                    int xx = c.x + dx; if (xx < 1 || xx >= width - 1) continue;
                    if ((dx * dx + dy * dy) <= (w * w) * 0.30f)
                    {
                        var p = new Vector3Int(xx, yy, 0);
                        foregroundTM.SetTile(p, biome.Pick(biome.waterTiles, rng));
                        if (!IsFromSet(groundTM.GetTile(p), biome.grassTiles))
                            groundTM.SetTile(p, biome.Pick(biome.grassTiles, rng));
                    }
                }
            }
        }

  
        int numSeeds = Mathf.RoundToInt((riverPath.Count / 100f) * swampSeedsPer100Tiles);
        for (int i = 0; i < numSeeds; i++)
        {
            if (rng.NextDouble() > swampChance) continue;
            var c = riverPath[rng.Next(riverPath.Count)];
            int side = rng.Next(0, 2) == 0 ? -1 : 1;
            int offY = side * rng.Next(Mathf.Max(2, riverMeanWidth / 2), Mathf.Max(3, riverMeanWidth));
         
            int sx = Mathf.Clamp(c.x + rng.Next(-2, 3), 2, width - 3);
            int sy = Mathf.Clamp(c.y + offY, 2, height - 3);
            GrowSwampFromSeed(new Vector2Int(sx, sy), rng.Next(2, swampMaxRadius + 1));
        }
    }

    List<Vector2Int> RiverAStar(Vector2Int start, Vector2Int goal)
    {
        var open = new MinHeapPriorityQueue<Vector2Int>();
        var cameFrom = new Dictionary<Vector2Int, Vector2Int>();
        var gScore = new Dictionary<Vector2Int, float>();
        var fScore = new Dictionary<Vector2Int, float>();

        Vector2Int[] dirs8 = new Vector2Int[] {
        new Vector2Int(1,0), new Vector2Int(-1,0), new Vector2Int(0,1), new Vector2Int(0,-1),
        new Vector2Int(1,1), new Vector2Int(1,-1), new Vector2Int(-1,1), new Vector2Int(-1,-1)
    };

        gScore[start] = 0f;
        fScore[start] = RiverHeuristic(start, goal);
        open.Enqueue(start, fScore[start]);

        int guard = width * height * 8;

        while (open.Count > 0 && guard-- > 0)
        {
            var current = open.DequeueMin();
            if (current == goal)
                return ReconstructPath(cameFrom, current);

            foreach (var d in dirs8)
            {
                var nb = new Vector2Int(current.x + d.x, current.y + d.y);
                if (nb.x < 1 || nb.x >= width - 1 || nb.y < 1 || nb.y >= height - 1) continue;

                float step = RiverStepCost(current, nb);
                float tentative = gScore[current] + step;

                if (!gScore.ContainsKey(nb) || tentative < gScore[nb])
                {
                    cameFrom[nb] = current;
                    gScore[nb] = tentative;
                    fScore[nb] = tentative + RiverHeuristic(nb, goal);
                    open.Enqueue(nb, fScore[nb]);
                }
            }
        }
        return null;
    }

    float RiverHeuristic(Vector2Int a, Vector2Int b)
    {
        int dx = Mathf.Abs(a.x - b.x);
        int dy = Mathf.Abs(a.y - b.y);
        return (dx + dy) + (1.4142f - 2f) * Mathf.Min(dx, dy);
    }

    float RiverStepCost(Vector2Int from, Vector2Int to)
    {
        float nx = (noiseOffset.x + to.x) * noiseScale;
        float ny = (noiseOffset.y + to.y) * noiseScale;
        float height01 = Mathf.PerlinNoise(nx, ny); 
        float alpha = 6.0f;                         
        float baseCost = 1.0f + alpha * height01;

        var f = foregroundTM.GetTile(new Vector3Int(to.x, to.y, 0));
        if (IsFromSet(f, biome.cliffTiles)) baseCost += 8f;

        float turnPenalty = 0f;
        if (from != to)
        {
            Vector2 df = new Vector2(to.x - from.x, to.y - from.y).normalized;
            turnPenalty = 0.1f * (1f + Mathf.Abs(df.x * df.y));
        }

        return baseCost + turnPenalty;
    }

    List<Vector2Int> StraightBorderFallback(Vector2Int s, Vector2Int g)
    {
        var path = new List<Vector2Int>();
        int x = s.x, y = s.y;
        path.Add(new Vector2Int(x, y));
        while (x != g.x || y != g.y)
        {
            if (x < g.x) x++; else if (x > g.x) x--;
            if (y < g.y) y++; else if (y > g.y) y--;
            path.Add(new Vector2Int(x, y));
            if (path.Count > width * height) break;
        }
        return path;
    }

    void BuildBridgesOverRivers()
    {
        if (bridgeTM == null || bridgeTileHorizontal == null || bridgeTileVertical == null)
            return;

        bool[,] visited = new bool[width, height];
        List<RectangleBridge> bridgeRectangles = new List<RectangleBridge>();

        for (int x = 1; x < width - 1; x++)
        {
            for (int y = 1; y < height - 1; y++)
            {
                if (!roadMask[x, y] || visited[x, y]) continue;

                bool hasWater = IsWater(new Vector3Int(x + 1, y, 0)) ||
                                IsWater(new Vector3Int(x - 1, y, 0)) ||
                                IsWater(new Vector3Int(x, y + 1, 0)) ||
                                IsWater(new Vector3Int(x, y - 1, 0));

                if (!hasWater) continue;

                if (IsRoadStillConnectedAcrossWater(new Vector2Int(x, y)))
                    continue;
                if (!IsActuallyBlockedByRiver(new Vector2Int(x, y)))
                    continue;

                List<Vector2Int> allAffectedRoads = FindAllConnectedRoadsByRiver(x, y);

                RectangleBridge unifiedBridge = CreateUnifiedRectangleBridge(allAffectedRoads, visited);
                if (unifiedBridge.IsValid)
                {
                    bridgeRectangles.Add(unifiedBridge);
                }
            }
        }

        List<RectangleBridge> finalBridges = MergeNearbyBridges(bridgeRectangles);

        foreach (var bridge in finalBridges)
        {
            DrawRectangleBridge(bridge);
        }
    }

    List<Vector2Int> FindAllConnectedRoadsByRiver(int startX, int startY)
    {
        List<Vector2Int> allRoads = new List<Vector2Int>();
        bool[,] localVisited = new bool[width, height];
        Queue<Vector2Int> queue = new Queue<Vector2Int>();
        Vector2Int[] dirs = { new(1, 0), new(-1, 0), new(0, 1), new(0, -1) };

        Vector2Int direction = FindMainDirection(startX, startY);

        queue.Enqueue(new Vector2Int(startX, startY));
        localVisited[startX, startY] = true;
        allRoads.Add(new Vector2Int(startX, startY));

        while (queue.Count > 0)
        {
            Vector2Int current = queue.Dequeue();

            foreach (Vector2Int dir in dirs)
            {
                Vector2Int next = current + dir;

                if (!InBounds(next.x, next.y) || localVisited[next.x, next.y]) continue;

                if (dir != direction && dir != -direction) continue;

                if (roadMask[next.x, next.y] && IsAffectedByRiver(next.x, next.y))
                {
                    localVisited[next.x, next.y] = true;
                    allRoads.Add(next);
                    queue.Enqueue(next);
                }
            }
        }

        return allRoads;
    }

    Vector2Int FindMainDirection(int x, int y)
    {
        int horizontalCount = 0;
        int verticalCount = 0;

        for (int i = -3; i <= 3; i++)
        {
            if (InBounds(x + i, y) && roadMask[x + i, y]) horizontalCount++;
            if (InBounds(x, y + i) && roadMask[x, y + i]) verticalCount++;
        }

        return horizontalCount > verticalCount ? new Vector2Int(1, 0) : new Vector2Int(0, 1);
    }

    bool IsAffectedByRiver(int x, int y)
    {
        bool hasWater = IsWater(new Vector3Int(x + 1, y, 0)) ||
                        IsWater(new Vector3Int(x - 1, y, 0)) ||
                        IsWater(new Vector3Int(x, y + 1, 0)) ||
                        IsWater(new Vector3Int(x, y - 1, 0));

        return hasWater && !IsRoadStillConnectedAcrossWater(new Vector2Int(x, y));
    }

    RectangleBridge CreateUnifiedRectangleBridge(List<Vector2Int> allRoads, bool[,] visited)
    {
        RectangleBridge bridge = new RectangleBridge();

        if (allRoads == null || allRoads.Count == 0)
        {
            bridge.IsValid = false;
            return bridge;
        }

        int minX = width, maxX = 0, minY = height, maxY = 0;
        foreach (var p in allRoads)
        {
            if (p.x < minX) minX = p.x;
            if (p.x > maxX) maxX = p.x;
            if (p.y < minY) minY = p.y;
            if (p.y > maxY) maxY = p.y;
        }

        bool horizontal = DetectClusterOrientation(allRoads);
        bridge.isHorizontal = horizontal;

        int bridgeWidth = 7; 
        int centerOffset = bridgeWidth / 2;

        if (horizontal)
        {
            bridge.minX = minX - 1; 
            bridge.maxX = maxX + 1;
            bridge.minY = minY - centerOffset;
            bridge.maxY = maxY + centerOffset;
        }
        else
        {
            bridge.minY = minY - 1;
            bridge.maxY = maxY + 1;

            bridge.minX = minX - centerOffset;
            bridge.maxX = maxX + centerOffset;
        }

        bridge.minX = Mathf.Max(1, bridge.minX);
        bridge.maxX = Mathf.Min(width - 2, bridge.maxX);
        bridge.minY = Mathf.Max(1, bridge.minY);
        bridge.maxY = Mathf.Min(height - 2, bridge.maxY);

        for (int x = bridge.minX; x <= bridge.maxX; x++)
        {
            for (int y = bridge.minY; y <= bridge.maxY; y++)
            {
                visited[x, y] = true;
            }
        }

        bridge.IsValid = true;
        return bridge;
    }

    bool IsRoadStillConnectedAcrossWater(Vector2Int roadPos)
    {
        Queue<Vector2Int> q = new Queue<Vector2Int>();
        bool[,] visitedLocal = new bool[width, height];
        Vector2Int[] dirs = { new(1, 0), new(-1, 0), new(0, 1), new(0, -1) };

        q.Enqueue(roadPos);
        visitedLocal[roadPos.x, roadPos.y] = true;

        while (q.Count > 0)
        {
            var cur = q.Dequeue();
            foreach (var d in dirs)
            {
                Vector2Int nb = cur + d;
                if (!InBounds(nb.x, nb.y) || visitedLocal[nb.x, nb.y]) continue;
                if (IsWater(new Vector3Int(nb.x, nb.y, 0))) continue;

                if (roadMask[nb.x, nb.y])
                {
                    if (Vector2Int.Distance(roadPos, nb) > 8f)
                    {
                        return true; 
                    }

                    visitedLocal[nb.x, nb.y] = true;
                    q.Enqueue(nb);
                }
            }
        }

        return false; 
    }

    bool IsActuallyBlockedByRiver(Vector2Int roadPos)
    {
        foreach (int dist in new[] { 1, 2, 3, 4, 5 })
        {
            if (CheckStraightLineBlocking(roadPos, new Vector2Int(1, 0), dist)) return true;  
            if (CheckStraightLineBlocking(roadPos, new Vector2Int(-1, 0), dist)) return true; 
            if (CheckStraightLineBlocking(roadPos, new Vector2Int(0, 1), dist)) return true;  
            if (CheckStraightLineBlocking(roadPos, new Vector2Int(0, -1), dist)) return true;
        }

        return false;
    }

    bool CheckStraightLineBlocking(Vector2Int start, Vector2Int dir, int maxDist)
    {
        int waterCount = 0;
        bool foundRoadAfterWater = false;

        for (int i = 1; i <= maxDist; i++)
        {
            Vector2Int check = start + dir * i;
            if (!InBounds(check.x, check.y)) break;

            if (IsWater(new Vector3Int(check.x, check.y, 0)))
            {
                waterCount++;
            }
            else if (roadMask[check.x, check.y] && waterCount > 0)
            {
                foundRoadAfterWater = true;
                break;
            }
        }

        return waterCount >= 2 && foundRoadAfterWater;
    }

    

    RectangleBridge CreateOptimalRectangleBridge(List<Vector2Int> cluster, bool[,] visited)
    {
        RectangleBridge bridge = new RectangleBridge();

        if (cluster == null || cluster.Count == 0)
        {
            bridge.IsValid = false;
            return bridge;
        }

        int minX = width, maxX = 0, minY = height, maxY = 0;
        foreach (var p in cluster)
        {
            if (p.x < minX) minX = p.x;
            if (p.x > maxX) maxX = p.x;
            if (p.y < minY) minY = p.y;
            if (p.y > maxY) maxY = p.y;
        }

        bool horizontal = DetectClusterOrientation(cluster);
        bridge.isHorizontal = horizontal;
        int bridgeWidth = 7; 
        int centerOffset = bridgeWidth / 2;

        if (horizontal)
        {
            bridge.minX = minX - 1; 
            bridge.maxX = maxX + 1;

            int centerY = (minY + maxY) / 2;
            bridge.minY = centerY - centerOffset;
            bridge.maxY = centerY + centerOffset;
        }
        else
        {
            bridge.minY = minY - 1; 
            bridge.maxY = maxY + 1;

            int centerX = (minX + maxX) / 2;
            bridge.minX = centerX - centerOffset;
            bridge.maxX = centerX + centerOffset;
        }

        bridge.minX = Mathf.Max(1, bridge.minX);
        bridge.maxX = Mathf.Min(width - 2, bridge.maxX);
        bridge.minY = Mathf.Max(1, bridge.minY);
        bridge.maxY = Mathf.Min(height - 2, bridge.maxY);

        for (int x = bridge.minX; x <= bridge.maxX; x++)
        {
            for (int y = bridge.minY; y <= bridge.maxY; y++)
            {
                visited[x, y] = true;
            }
        }

        bridge.IsValid = true;
        return bridge;
    }

    void DrawRectangleBridge(RectangleBridge bridge)
    {
        TileBase bridgeTile = bridge.isHorizontal ? bridgeTileHorizontal : bridgeTileVertical;

        if (bridgeTile == null) return;

        for (int x = bridge.minX; x <= bridge.maxX; x++)
        {
            for (int y = bridge.minY; y <= bridge.maxY; y++)
            {
                if (!InBounds(x, y)) continue;
                var bp = new Vector3Int(x, y, 0);

                waterTM?.SetTile(bp, null);
                foregroundTM?.SetTile(bp, null);
                roadTM?.SetTile(bp, null);
                groundTM?.SetTile(bp, null);
                bridgeTM.SetTile(bp, bridgeTile);
            }
        }
    }

    List<Vector2Int> FloodFillRoadCluster(int sx, int sy, int range, bool[,] visited)
    {
        List<Vector2Int> result = new();
        Queue<Vector2Int> q = new();
        q.Enqueue(new Vector2Int(sx, sy));
        visited[sx, sy] = true;

        Vector2Int[] dirs = { new(1, 0), new(-1, 0), new(0, 1), new(0, -1) };

        while (q.Count > 0)
        {
            var p = q.Dequeue();
            result.Add(p);
            foreach (var d in dirs)
            {
                var n = p + d;
                if (!InBounds(n.x, n.y)) continue;
                if (visited[n.x, n.y]) continue;
                if (Vector2Int.Distance(new(sx, sy), n) > range) continue;
                if (!roadMask[n.x, n.y]) continue;
                visited[n.x, n.y] = true;
                q.Enqueue(n);
            }
        }
        return result;
    }

    bool DetectClusterOrientation(List<Vector2Int> pts)
    {
        int minX = int.MaxValue, maxX = int.MinValue;
        int minY = int.MaxValue, maxY = int.MinValue;
        foreach (var p in pts)
        {
            if (p.x < minX) minX = p.x;
            if (p.x > maxX) maxX = p.x;
            if (p.y < minY) minY = p.y;
            if (p.y > maxY) maxY = p.y;
        }
        return (maxX - minX) >= (maxY - minY);
    }

    bool InBounds(int x, int y) => x >= 0 && y >= 0 && x < width && y < height;
    void PaintDiskWater(int cx, int cy, int r)
    {
        r = Mathf.Max(1, r);
        bool bridgePlaced = false;

        for (int dy = -r; dy <= r; dy++)
            for (int dx = -r; dx <= r; dx++)
            {
                if (dx * dx + dy * dy > r * r) continue;
                int x = cx + dx, y = cy + dy;
                if (x < 1 || x >= width - 1 || y < 1 || y >= height - 1) continue;

                var p = new Vector3Int(x, y, 0);
                if (roadMask[x, y])
                {
                    if (!bridgePlaced)
                    {
                        bool horizontal =
                            (x > 0 && roadMask[x - 1, y]) || (x < width - 1 && roadMask[x + 1, y]);
                        bool vertical =
                            (y > 0 && roadMask[x, y - 1]) || (y < height - 1 && roadMask[x, y + 1]);

                        TileBase bridgeTile = horizontal ? bridgeTileHorizontal : bridgeTileVertical;
                        if (bridgeTile == null) return;

                        int halfW = horizontal ? bridgeWidthHorizontal / 2 : 1;
                        int halfH = vertical ? bridgeHeightVertical / 2 : 1;
                        for (int bx = -halfW; bx <= halfW; bx++)
                            for (int by = -halfH; by <= halfH; by++)
                            {
                                int px = x + bx;
                                int py = y + by;
                                if (px < 0 || py < 0 || px >= width || py >= height) continue;
                                var bp = new Vector3Int(px, py, 0);
                                foregroundTM.SetTile(bp, null);
                                bridgeTM.SetTile(bp, bridgeTile);
                            }

                        bridgePlaced = true;
                    }
                    continue;
                }
                foregroundTM.SetTile(p, biome.Pick(biome.waterTiles, rng));
                if (!IsFromSet(groundTM.GetTile(p), biome.grassTiles))
                    groundTM.SetTile(p, biome.Pick(biome.grassTiles, rng));
            }
    }







    void GrowSwampFromSeed(Vector2Int seed, int maxRadius)
    {
        Queue<Vector2Int> q = new Queue<Vector2Int>();
        HashSet<Vector2Int> vis = new HashSet<Vector2Int>();
        q.Enqueue(seed); vis.Add(seed);

        while (q.Count > 0)
        {
            var c = q.Dequeue();
            if (Vector2Int.Distance(seed, c) > maxRadius) continue;

            var p = new Vector3Int(c.x, c.y, 0);
            var f = waterTM != null ? waterTM.GetTile(p) : null;
            if (IsFromSet(f, biome.cliffTiles)) continue;
            if (occupied != null && occupied[c.x, c.y]) continue;

            if (!IsFromSet(f, biome.waterTiles))
            {
                foregroundTM.SetTile(p, biome.Pick(biome.waterTiles, rng));
                if (!IsFromSet(groundTM.GetTile(p), biome.grassTiles))
                    groundTM.SetTile(p, biome.Pick(biome.grassTiles, rng));
            }

            Vector2Int[] dirs = { new Vector2Int(1, 0), new Vector2Int(-1, 0), new Vector2Int(0, 1), new Vector2Int(0, -1) };
            foreach (var d in dirs)
            {
                var nb = new Vector2Int(c.x + d.x, c.y + d.y);
                if (nb.x < 1 || nb.x >= width - 1 || nb.y < 1 || nb.y >= height - 1) continue;
                if (vis.Contains(nb)) continue;
                float dist = Vector2Int.Distance(seed, nb);
                float keepProb = Mathf.Lerp(0.95f, 0.15f, dist / (maxRadius + 0.0001f));
                float jitter = Mathf.PerlinNoise(noiseOffset.x + nb.x * 0.11f, noiseOffset.y + nb.y * 0.11f) * 0.25f;
                keepProb = Mathf.Clamp01(keepProb + jitter - 0.1f);

                if (rng.NextDouble() < keepProb)
                {
                    vis.Add(nb);
                    q.Enqueue(nb);
                }
            }
        }
    }
    void ScatterWaterOldStyle()
    {
        for (int x = 0; x < width; x += 4)
        {
            for (int y = 0; y < height; y += 4)
            {
                float avg = 0;
                for (int dx = 0; dx < 4; dx++)
                    for (int dy = 0; dy < 4; dy++)
                        avg += Mathf.PerlinNoise(
                            noiseOffset.x + (x + dx) * noiseScale,
                            noiseOffset.y + (y + dy) * noiseScale
                        );
                avg /= 16f;

                if (avg <= biome.waterThreshold)
                {
                    for (int dx = 0; dx < 4 && x + dx < width; dx++)
                        for (int dy = 0; dy < 4 && y + dy < height; dy++)
                            foregroundTM.SetTile(new Vector3Int(x + dx, y + dy, 0),
                                                 biome.Pick(biome.waterTiles, rng));
                }
            }
        }
    }
    bool IsDeepWater(Vector3Int c, int depth = 1)
    {
        if (!IsWater(c)) return false;
        depth = Mathf.Max(1, depth);

        for (int dx = -depth; dx <= depth; dx++)
            for (int dy = -depth; dy <= depth; dy++)
            {
                if (dx == 0 && dy == 0) continue;
                int nx = c.x + dx, ny = c.y + dy;

                if (nx < 0 || ny < 0 || nx >= width || ny >= height) return false;

                if (!IsWater(nx, ny)) return false;
            }
        return true;
    }

    bool[,] occupied;



    struct Frontage { public Vector2Int cell; public Vector2Int normal; }
    void GenerateCosmeticLake(Vector2Int center, int radiusX, int radiusY)
    {
        if (waterTM == null) return;

        radiusX = Mathf.Max(3, radiusX);
        radiusY = Mathf.Max(3, radiusY);

        bool[,] inside = new bool[2 * radiusX + 1, 2 * radiusY + 1];
        for (int dy = -radiusY; dy <= radiusY; dy++)
        {
            for (int dx = -radiusX; dx <= radiusX; dx++)
            {
                float nx = (dx) / (float)radiusX;
                float ny = (dy) / (float)radiusY;
                if (nx * nx + ny * ny <= 1.0f) inside[dx + radiusX, dy + radiusY] = true;
            }
        }

        List<Vector3Int> borderCells = new List<Vector3Int>();
        Vector2Int[] dirs4 = { new(1, 0), new(-1, 0), new(0, 1), new(0, -1) };

        for (int dy = -radiusY; dy <= radiusY; dy++)
        {
            for (int dx = -radiusX; dx <= radiusX; dx++)
            {
                if (!inside[dx + radiusX, dy + radiusY]) continue;

                bool isBorder = false;
                foreach (var d in dirs4)
                {
                    int lx = dx + d.x, ly = dy + d.y;
                    bool nbInside =
                        lx >= -radiusX && lx <= radiusX &&
                        ly >= -radiusY && ly <= radiusY &&
                        inside[lx + radiusX, ly + radiusY];

                    if (!nbInside) { isBorder = true; break; }
                }

                if (isBorder)
                {
                    int x = center.x + dx, y = center.y + dy;
                    if (x < 1 || x >= width - 1 || y < 1 || y >= height - 1) continue;
                    borderCells.Add(new Vector3Int(x, y, 0));
                }
            }
        }

        foreach (var c in borderCells)
        {
            groundTM.SetTile(c, biome.Pick(biome.grassTiles, rng));
            waterTM.SetTile(c, biome.Pick(biome.waterTiles, rng));
            overlayTM?.SetTile(c, null);
            if (foregroundTM != null && IsFromSet(foregroundTM.GetTile(c), biome.cliffTiles))
                foregroundTM.SetTile(c, null);
        }

        FloodFillLakeInterior(center, radiusX, radiusY);
    }

    void FloodFillLakeInterior(Vector2Int center, int radiusX, int radiusY)
    {
        Queue<Vector2Int> q = new Queue<Vector2Int>();
        HashSet<Vector2Int> vis = new HashSet<Vector2Int>();
        Vector2Int[] dirs4 = { new(1, 0), new(-1, 0), new(0, 1), new(0, -1) };


        q.Enqueue(center); vis.Add(center);

        int x0 = Mathf.Max(1, center.x - radiusX);
        int x1 = Mathf.Min(width - 2, center.x + radiusX);
        int y0 = Mathf.Max(1, center.y - radiusY);
        int y1 = Mathf.Min(height - 2, center.y + radiusY);

        while (q.Count > 0)
        {
            var c = q.Dequeue();
            var p = new Vector3Int(c.x, c.y, 0);

            if (!IsFromSet(waterTM.GetTile(p), biome.waterTiles))
            {
                groundTM.SetTile(p, biome.Pick(biome.grassTiles, rng));
                waterTM.SetTile(p, biome.Pick(biome.waterTiles, rng));
                overlayTM?.SetTile(p, null);
                if (foregroundTM != null && IsFromSet(foregroundTM.GetTile(p), biome.cliffTiles))
                    foregroundTM.SetTile(p, null);
            }

            foreach (var d in dirs4)
            {
                int nx = c.x + d.x, ny = c.y + d.y;
                if (nx < x0 || nx > x1 || ny < y0 || ny > y1) continue;
                var np = new Vector3Int(nx, ny, 0);
                if (vis.Contains(new Vector2Int(nx, ny))) continue;
                vis.Add(new Vector2Int(nx, ny));
                q.Enqueue(new Vector2Int(nx, ny));
            }
        }
    }


    private IEnumerator AnimateFirefly(List<Vector2Int> path)
    {
        GameObject firefly = Instantiate(fireflyTrailPrefab, groundTM.CellToWorld((Vector3Int)path[0]) + new Vector3(0.5f, 0.5f, 0), Quaternion.identity);
        for (int i = 1; i < path.Count; i++)
        {
            Vector3 start = firefly.transform.position;
            Vector3 end = groundTM.CellToWorld((Vector3Int)path[i]) + new Vector3(0.5f, 0.5f, 0);
            float t = 0;
            while (t < 1)
            {
                t += Time.deltaTime * 3f; 
                firefly.transform.position = Vector3.Lerp(start, end, t);
                yield return null;
            }
        }
       
    }

    public void ShowDirectionPath(string target)
    {
        GameObject village = GameObject.Find("Camp");
        if (village == null)
        {
            Debug.LogWarning("❌ Không tìm thấy Camp để dẫn đường!");
            return;
        }

       
        Vector3 playerWorld = player != null ? player.position : Vector3.zero;
        Vector3 villageWorld = village.transform.position;

   
        Vector3Int startCell = groundTM.WorldToCell(playerWorld);
        Vector3Int endCell = groundTM.WorldToCell(villageWorld);
        Vector2Int start = new Vector2Int(startCell.x, startCell.y);
        Vector2Int end = new Vector2Int(endCell.x, endCell.y);

     
        List<Vector2Int> path = AStarPathfinder.FindPath(grid.walkableGrid, start, end);

        if (path != null && path.Count > 0)
        {
            Debug.Log($"✨ Player hỏi đường → Spawn đom đóm dẫn tới {target}");
            StartCoroutine(AnimateFirefly(path));
        }
        else
        {
            Debug.LogWarning("❌ Không tìm thấy đường đến Camp!");
        }
    }
    List<RectangleBridge> MergeNearbyBridges(List<RectangleBridge> bridges)
    {
        List<RectangleBridge> merged = new List<RectangleBridge>();
        bool[] mergedFlags = new bool[bridges.Count];

        for (int i = 0; i < bridges.Count; i++)
        {
            if (mergedFlags[i]) continue;
            RectangleBridge currentBridge = bridges[i];
            mergedFlags[i] = true;
            for (int j = i + 1; j < bridges.Count; j++)
            {
                if (mergedFlags[j]) continue;
                RectangleBridge otherBridge = bridges[j];
                if (ShouldMergeBridges(currentBridge, otherBridge))
                {
                    currentBridge = MergeTwoBridges(currentBridge, otherBridge);
                    mergedFlags[j] = true;
                }
            }

            merged.Add(currentBridge);
        }

        return merged;
    }

    bool ShouldMergeBridges(RectangleBridge bridge1, RectangleBridge bridge2)
    {
        if (bridge1.isHorizontal != bridge2.isHorizontal) return false;

        int mergeThreshold = 5; 

        if (bridge1.isHorizontal)
        {
            int xDistance = Mathf.Max(0, bridge2.minX - bridge1.maxX - 1);
            if (xDistance <= mergeThreshold)
            {
                int yOverlap = Mathf.Min(bridge1.maxY, bridge2.maxY) -
                              Mathf.Max(bridge1.minY, bridge2.minY) + 1;
                return yOverlap > 2; 
            }
        }
        else
        {
            int yDistance = Mathf.Max(0, bridge2.minY - bridge1.maxY - 1);
            if (yDistance <= mergeThreshold)
            {
                int xOverlap = Mathf.Min(bridge1.maxX, bridge2.maxX) -
                              Mathf.Max(bridge1.minX, bridge2.minX) + 1;
                return xOverlap > 2;
            }
        }

        return false;
    }

    RectangleBridge MergeTwoBridges(RectangleBridge bridge1, RectangleBridge bridge2)
    {
        RectangleBridge merged = new RectangleBridge();
        merged.isHorizontal = bridge1.isHorizontal;
        merged.minX = Mathf.Min(bridge1.minX, bridge2.minX);
        merged.maxX = Mathf.Max(bridge1.maxX, bridge2.maxX);
        merged.minY = Mathf.Min(bridge1.minY, bridge2.minY);
        merged.maxY = Mathf.Max(bridge1.maxY, bridge2.maxY);
        int bridgeWidth = 7;
        int centerOffset = bridgeWidth / 2;

        if (merged.isHorizontal)
        {
            int centerY = (merged.minY + merged.maxY) / 2;
            merged.minY = centerY - centerOffset;
            merged.maxY = centerY + centerOffset;
        }
        else
        {
            int centerX = (merged.minX + merged.maxX) / 2;
            merged.minX = centerX - centerOffset;
            merged.maxX = centerX + centerOffset;
        }

        merged.minX = Mathf.Max(1, merged.minX);
        merged.maxX = Mathf.Min(width - 2, merged.maxX);
        merged.minY = Mathf.Max(1, merged.minY);
        merged.maxY = Mathf.Min(height - 2, merged.maxY);

        merged.IsValid = true;
        return merged;
    }

}

public class GridManager : MonoBehaviour
{
    public int width;
    public int height;
    public Tilemap groundTilemap;
    public Tilemap obstacleTilemap;
    public bool[,] walkableGrid;

    public void GenerateGrid()
    {
        walkableGrid = new bool[width, height];
        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                Vector3Int cell = new Vector3Int(x, y, 0);
                bool isBlocked = false;
                TileBase fg = obstacleTilemap != null ? obstacleTilemap.GetTile(cell) : null;
                if (fg != null)
                {
                    isBlocked = true;
                }

                walkableGrid[x, y] = !isBlocked;
            }
        }
    }
}
class MinHeapPriorityQueue<T>
{
    private readonly List<(T item, float priority)> heap = new List<(T, float)>();

    public int Count => heap.Count;

    public void Enqueue(T item, float priority)
    {
        heap.Add((item, priority));
        SiftUp(heap.Count - 1);
    }

    public T DequeueMin()
    {
        var root = heap[0].item;
        var last = heap[heap.Count - 1];
        heap.RemoveAt(heap.Count - 1);
        if (heap.Count > 0)
        {
            heap[0] = last;
            SiftDown(0);
        }
        return root;
    }

    void SiftUp(int i)
    {
        while (i > 0)
        {
            int p = (i - 1) / 2;
            if (heap[i].priority >= heap[p].priority) break;
            (heap[i], heap[p]) = (heap[p], heap[i]);
            i = p;
        }
    }

    void SiftDown(int i)
    {
        int n = heap.Count;
        while (true)
        {
            int l = i * 2 + 1;
            int r = l + 1;
            int smallest = i;
            if (l < n && heap[l].priority < heap[smallest].priority) smallest = l;
            if (r < n && heap[r].priority < heap[smallest].priority) smallest = r;
            if (smallest == i) break;
            (heap[i], heap[smallest]) = (heap[smallest], heap[i]);
            i = smallest;
        }
    }


}

public class AStarPathfinder
{
    private class Node
    {
        public Vector2Int pos;
        public int gCost;  
        public int hCost;  
        public int fCost => gCost + hCost;
        public Node parent;

        public Node(Vector2Int position)
        {
            pos = position;
        }
    }

    private static readonly Vector2Int[] Directions =
    {
        new Vector2Int(0, 1),   
        new Vector2Int(1, 0),   
        new Vector2Int(0, -1), 
        new Vector2Int(-1, 0),  

    };

    public static List<Vector2Int> FindPath(bool[,] grid, Vector2Int start, Vector2Int end)
    {
        int width = grid.GetLength(0);
        int height = grid.GetLength(1);
        if (!IsInBounds(start, width, height) || !IsInBounds(end, width, height) ||
            !grid[start.x, start.y] || !grid[end.x, end.y])
        {
            Debug.LogWarning("⚠️ A* start hoặc end không hợp lệ!");
            return new List<Vector2Int>();
        }

        Dictionary<Vector2Int, Node> allNodes = new();
        Node startNode = new Node(start);
        Node endNode = new Node(end);

        List<Node> openList = new() { startNode };
        HashSet<Vector2Int> closedList = new();

        allNodes[start] = startNode;

        while (openList.Count > 0)
        {
            Node current = openList[0];
            foreach (var node in openList)
                if (node.fCost < current.fCost ||
                   (node.fCost == current.fCost && node.hCost < current.hCost))
                    current = node;

            openList.Remove(current);
            closedList.Add(current.pos);

            if (current.pos == end)
                return ReconstructPath(current);
            foreach (var dir in Directions)
            {
                Vector2Int neighborPos = current.pos + dir;

                if (!IsInBounds(neighborPos, width, height)) continue;
                if (!grid[neighborPos.x, neighborPos.y]) continue; 
                if (closedList.Contains(neighborPos)) continue;

                int newG = current.gCost + 10;

                Node neighbor;
                if (allNodes.ContainsKey(neighborPos))
                    neighbor = allNodes[neighborPos];
                else
                {
                    neighbor = new Node(neighborPos);
                    allNodes[neighborPos] = neighbor;
                }

                if (newG < neighbor.gCost || !openList.Contains(neighbor))
                {
                    neighbor.gCost = newG;
                    neighbor.hCost = Heuristic(neighbor.pos, end);
                    neighbor.parent = current;

                    if (!openList.Contains(neighbor))
                        openList.Add(neighbor);
                }
            }
        }


        return new List<Vector2Int>();
    }

    private static List<Vector2Int> ReconstructPath(Node endNode)
    {
        List<Vector2Int> path = new();
        Node current = endNode;
        while (current != null)
        {
            path.Add(current.pos);
            current = current.parent;
        }
        path.Reverse();
        return path;
    }

    private static bool IsInBounds(Vector2Int p, int width, int height)
    {
        return p.x >= 0 && p.y >= 0 && p.x < width && p.y < height;
    }

    private static int Heuristic(Vector2Int a, Vector2Int b)
    {
        return (Mathf.Abs(a.x - b.x) + Mathf.Abs(a.y - b.y)) * 10;
    }
}

