using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Linq;


// Enum định nghĩa các hoạt động trong ngày
public enum NPCActivity
{
    Sleep,              // Ngủ (23:00 - 6:00)
    MorningRoutine,     // Dọn dẹp, ăn sáng (6:00 - 8:00)
    MarketTrading,     // Bán hàng ở chợ (8:00 - 12:00) - for traders
    LunchBreak,        // Nghỉ trưa ở làng (12:00 - 13:00)
    FlowerHunting,      // Đi hái hoa (13:00 - 17:00)
    ExploreVillage,   // Lang thang gặp gỡ làng (17:00 - 18:00)
    EveningRoutine,    // Nấu ăn, trò chuyện (18:00 - 20:00)
    SocialTime,        // Gặp gỡ buổi tối (20:00 - 22:00)
    NightRoutine       // Chuẩn bị đi ngủ (22:00 - 23:00)
}

// Enum định nghĩa trạng thái NPC
public enum NPCState
{
    Idle,
    MovingToTarget,
    GatheringFlower,
    ReturningHome,
    Resting,
    Socializing
}

public class FlowerObject
{
    public GameObject gameObject;
    public bool isAvailable = true;
    public Vector3 position;

    public FlowerObject(GameObject obj)
    {
        gameObject = obj;
        position = obj.transform.position;
        isAvailable = true;
    }
}

public class NPCRoutineAI : MonoBehaviour
{
    [Header("Routine Settings")]
    public NPCActivity currentActivity = NPCActivity.FlowerHunting;
    public NPCState currentState = NPCState.GatheringFlower;

    [Header("Home/Village Settings")]
    public Transform homeLocation;     // Vị trí nhà/CAMP
    public Transform villageCenter;   // Trung tâm làng để lang thang
    public float wanderRadius = 10f;    // Bán kính lang thang ở làng

    [Header("Flower Gathering")]
    public List<GameObject> flowerPrefabs;
    public float flowerDetectionRadius = 5f; // ✅ TĂNG từ 3f lên 5f
    public float gatheringTime = 3f;    // ✅ GIẢM từ 5s xuống 3s cho nhanh hơn
    public LayerMask flowerLayer;

    [Header("Movement Settings")]
    public float moveSpeed = 3.5f; // ✅ TĂNG từ 2f lên 3.5f
    public float detectionRadius = 5f;
    [Header("Movement Direction")]
    public bool useOnlyHorizontalMovement = false; // Chỉ đi ngang/dọc

    [Header("Time Settings")]
    public float dayDurationInMinutes = 24f; // 24 phút = 1 ngày game
    [Header("References")]
    public MapGenerator mapGenerator;

    // Internal variables
    private List<FlowerObject> availableFlowers = new List<FlowerObject>();
    private FlowerObject currentTargetFlower;
    private Vector3 currentTargetPosition;
    private Animator animator;
    private float currentGameTime = 6f; // 6:00 AM
    private bool playerMadeRequest = false; // Player requested flower gathering

    [Header("Time-based Flower Hunting")]
    public bool useRealTimeManager = true; // Sử dụng TimeManager thật
    public float flowerHuntingStartHour = 14f; // 2:00 PM
    public float flowerHuntingEndHour = 16f; // 4:00 PM
    private Coroutine activityCoroutine;
    private Coroutine gatheringCoroutine;

    [Header("Market Trading (for traders)")]
    public bool isTrader = false; // NPC có vai trò bán hàng không
    public float marketOpenHour = 8f; // 8:00 AM
    public float marketCloseHour = 12f; // 12:00 PM
    public Transform marketStallLocation; // Vị trí sạp hàng

    // Pause/resume system
    private bool isPaused = false;
    private NPCActivity pausedActivity;
    private IEnumerator pausedCoroutine;

    // Singleton để quản lý tất cả NPCs
    public static NPCRoutineAI Instance;
    private Coroutine moveRoutine;
    private bool physicsLockedForMarket = false;




    void Start()
    {
        Rigidbody2D rb = GetComponent<Rigidbody2D>();
        if (rb)
        {
            rb.gravityScale = 0;
            rb.mass = 1;
            rb.constraints = RigidbodyConstraints2D.FreezeRotation;
            rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
        }

        if (mapGenerator == null)
            mapGenerator = FindObjectOfType<MapGenerator>();

        animator = GetComponent<Animator>();

        // Đảm bảo sử dụng TimeManager nếu có
        if (TimeManager.Instance != null)
        {
            useRealTimeManager = true;
            Debug.Log($"✅ {gameObject.name}: TimeManager found, enabled useRealTimeManager");
        }
        else
        {
            Debug.LogWarning($"⚠️ {gameObject.name}: No TimeManager found, using internal time");
        }

        InitializeFlowerHunter();
    }


    void LateUpdate()
    {
        if (currentState != NPCState.MovingToTarget && currentState != NPCState.GatheringFlower)
            ClampToMapBounds();
    }

    void FixedUpdate()
    {
        return;
    }




    void InitializeFlowerHunter()
    {
        // ✅ REDUCE movement radius to prevent border running
        wanderRadius = 15f; // Giảm từ 20 xuống 15
        flowerDetectionRadius = 3f; // Giảm detection radius từ 5 xuống 3

        // ✅ Validate Village Center
        if (villageCenter == null)
        {
            // Debug.LogWarning($"⚠️ Village Center NULL - using current position as center");
            villageCenter = transform;
        }

        // Debug.Log($"📏 Settings: WanderRadius={wanderRadius}, FlowerDetection={flowerDetectionRadius}");

        StartCoroutine(SimpleFlowerHunting());
    }

    // ✅ SIMPLE FLOWER HUNTING - NO TIME ROUTINES!
    IEnumerator SimpleFlowerHunting()
    {
        Debug.Log($"🌸 {gameObject.name}: SimpleFlowerHunting started — only active from {flowerHuntingStartHour}:00 to {flowerHuntingEndHour}:00 OR when requested");

        while (true)
        {
            // ⏸️ Skip logic khi đang pause (đang nói chuyện với player)
            if (isPaused)
            {
                yield return new WaitForSeconds(0.5f);
                continue;
            }

            // ✅ PRIORITY: Nếu đang MarketTrading, KHÔNG update activity cho đến khi hết giờ
            if (currentActivity == NPCActivity.MarketTrading)
            {
                // Kiểm tra xem còn trong giờ market không
                NPCTrader trader = GetComponent<NPCTrader>();
                if (trader != null && trader.IsMarketHours())
                {
                    // Vẫn còn giờ market → tiếp tục ở market
                    yield return new WaitForSeconds(1f);
                    continue;
                }
                else
                {
                    // Hết giờ market → cho phép update activity
                    Debug.Log($"🏪 {gameObject.name}: Market time ended, switching to other activity");
                    UpdateCurrentActivity();
                }
            }
            else
            {
                // Cập nhật trạng thái hoạt động
                UpdateCurrentActivity();
            }

            // ✅ Nếu NPC là trader và đang giờ market, chuyển sang MarketTrading
            if (currentActivity == NPCActivity.MarketTrading)
            {
                Debug.Log($"🏪 {gameObject.name}: Switching to MarketTrading activity!");
                yield return StartCoroutine(MarketTradingRoutine());
                yield return new WaitForSeconds(1f);
                continue; // Restart loop after market closes
            }

            bool isFlowerTime = IsFlowerHuntingTime();
            float currentHour = GetCurrentGameTime();

            // ⏰ LOG THỜI GIAN CHI TIẾT
            Debug.Log($"⏰ {gameObject.name}: Time check - Current: {currentHour:F2}h | useRealTimeManager: {useRealTimeManager} | TimeManager exists: {TimeManager.Instance != null}");
            if (TimeManager.Instance != null)
            {
                Debug.Log($"⏰ {gameObject.name}: TimeManager.GetCurrentHour() = {TimeManager.Instance.GetCurrentHour():F2}h");
            }
            Debug.Log($"⏰ {gameObject.name}: IsFlowerTime: {isFlowerTime} | PlayerRequest: {playerMadeRequest} | Range: {flowerHuntingStartHour}-{flowerHuntingEndHour}h");

            // ✅ CHỈ HÁI HOA KHI: trong giờ hái hoa HOẶC có request từ người chơi
            if (!playerMadeRequest && !isFlowerTime)
            {
                Debug.Log($"🔒 {gameObject.name}: No player request and not flower time (current: {currentHour:F1}h, range: {flowerHuntingStartHour}-{flowerHuntingEndHour}h) — standing idle");
                currentState = NPCState.Idle;
                yield return StartCoroutine(IdleRoutine());

                // Check again every few seconds
                yield return new WaitForSeconds(3f);
                continue; // Restart the loop
            }

            string reason = playerMadeRequest ? "Player requested" : $"Flower time ({currentHour:F1}h)";
            Debug.Log($"🌸 {gameObject.name}: {reason} - going to gather flowers!");

            // Proceed with flower gathering logic
            FlowerObject nearestFlower = FindNearestFlowerSimple();

            if (nearestFlower != null)
            {
                // Di chuyển đến hoa
                bool reachedFlowerMove = false;
                moveRoutine = StartCoroutine(MoveToPosition(nearestFlower.position, r => reachedFlowerMove = r));
                yield return moveRoutine;
                if (!reachedFlowerMove)
                {
                    Debug.LogError($"{name}: FAILED to reach flower!");
                    continue;
                }
                // Kiểm tra khoảng cách
                float distance = Vector3.Distance(transform.position, nearestFlower.position);
                if (distance <= flowerDetectionRadius)
                {
                    // Hái hoa
                    yield return StartCoroutine(GatherFlower(nearestFlower));

                    // Nghỉ ngắn sau khi hái và reset player request if needed
                    yield return new WaitForSeconds(2f);

                    // Complete player request if it was player initiated
                    if (playerMadeRequest)
                    {
                        Debug.Log($"🌸 {gameObject.name}: Completed player's flower gathering request!");
                        playerMadeRequest = false;
                    }
                }
                else
                {
                    Debug.Log($"⚠️ {gameObject.name}: Couldn't reach flower ({distance:F2})");
                }
            }
            else
            {
                // Không có hoa nào → hoàn thành player request
                if (playerMadeRequest)
                {
                    Debug.Log($"🌸 {gameObject.name}: No flowers found, player request completed.");
                    playerMadeRequest = false; // Reset request
                    currentState = NPCState.Idle;
                    yield return StartCoroutine(IdleRoutine());
                }
                else
                {
                    // Không có request và không có hoa → không có gì làm, chỉ đợi
                    Debug.Log($"🌿 {gameObject.name}: Waiting for flowers or player request...");
                    yield return StartCoroutine(IdleRoutine());
                }
            }

            // Kiểm tra lại mỗi 1 giây
            yield return new WaitForSeconds(1f);
        }
    }


    // ✅ ORIGINAL FINDER - KEEP ALL FLOWERS!
    FlowerObject FindNearestFlowerSimple()
    {
        if (mapGenerator == null)
            mapGenerator = FindObjectOfType<MapGenerator>();

        Vector3 mapCenter = new Vector3(mapGenerator.width / 2f, mapGenerator.height / 2f, 0);

        GameObject[] allFlowers = GameObject.FindGameObjectsWithTag("Flower");
        if (allFlowers == null || allFlowers.Length == 0)
        {
            // Debug.LogWarning($"⚠️ {gameObject.name}: Không tìm thấy hoa nào trong scene!");
            return null;
        }

        GameObject nearestFlower = null;
        float minDistance = float.MaxValue;

        foreach (GameObject flower in allFlowers)
        {
            if (flower == null || !flower.activeInHierarchy)
                continue;

            Vector3 flowerPos = flower.transform.position;
            flowerPos.z = 0f;

            // ✅ Giới hạn tuyệt đối
            if (flowerPos.x < 0 || flowerPos.x > mapGenerator.width ||
                flowerPos.y < 0 || flowerPos.y > mapGenerator.height)
            {
                // Debug.LogWarning($"🚫 {gameObject.name}: Bỏ qua hoa '{flower.name}' (ngoài biên map)");
                continue;
            }

            // ✅ Giới hạn theo tâm map
            float distFromCenter = Vector3.Distance(flowerPos, mapCenter);
            if (distFromCenter > 90f)
            {
                // Debug.LogWarning($"🚫 {gameObject.name}: Bỏ qua hoa '{flower.name}' (xa tâm {distFromCenter:F1})");
                continue;
            }

            // ✅ Tính khoảng cách NPC - hoa
            float distFromNPC = Vector3.Distance(transform.position, flowerPos);
            if (distFromNPC < minDistance)
            {
                minDistance = distFromNPC;
                nearestFlower = flower;
            }
        }

        if (nearestFlower != null)
        {
            // Debug.Log($"🎯 {gameObject.name}: Tìm thấy hoa '{nearestFlower.name}' tại {nearestFlower.transform.position:F2} (cách NPC {minDistance:F2})");
            return new FlowerObject(nearestFlower);
        }

        // Debug.LogWarning($"⚠️ {gameObject.name}: Không có hoa nào hợp lệ trong phạm vi!");
        return null;
    }





    // ✅ TIME-BASED ROUTINE - CHỈ HÁI HOA 15:00-18:00
    IEnumerator TimeBasedRoutine()
    {
        // Debug.Log($"⏰ {gameObject.name}: Time-based routine STARTED! Flower hunting: {flowerHuntingStartHour}:00-{flowerHuntingEndHour}:00");

        while (true)
        {
            // Cập nhật hoạt động dựa trên thời gian
            UpdateCurrentActivity();

            // Chỉ đi hái hoa nếu đúng giờ
            if (currentActivity == NPCActivity.FlowerHunting)
            {
                FlowerObject nearestFlower = FindNearestFlowerSimple();

                if (nearestFlower != null)
                {
                    bool reachFlower = false;
                    moveRoutine = StartCoroutine(MoveToPosition(nearestFlower.position, r => reachFlower = r));
                    yield return moveRoutine;
                    // Kiểm tra đã đến gần chưa
                    float distance = Vector3.Distance(transform.position, nearestFlower.position);
                    if (distance <= flowerDetectionRadius)
                    {
                        // Debug.Log($"✅ {gameObject.name}: Reached flower - time to gather!");

                        // Hái hoa
                        yield return StartCoroutine(GatherFlower(nearestFlower));

                        // Sau khi hái, short break
                        yield return new WaitForSeconds(2f);
                    }
                    else
                    {
                        // Debug.LogWarning($"⚠️ Couldn't get close enough to flower (distance: {distance})");
                    }
                }
                else
                {
                    // Debug.Log($"🔍 {gameObject.name}: No flowers found - wandering randomly...");

                    // Random wandering
                    Vector3 randomPoint = villageCenter.position +
                        new Vector3(Random.Range(-wanderRadius, wanderRadius), Random.Range(-wanderRadius, wanderRadius), 0f);

                    bool reachedRandom = false;
                    moveRoutine = StartCoroutine(MoveToPosition(randomPoint, r => reachedRandom = r));
                    yield return moveRoutine;

                }
            }
            else
            {
                // Không phải giờ hái hoa → đứng im
                // Debug.Log($"😴 {gameObject.name}: Không phải giờ hái hoa, đang đứng im ({TimeManager.Instance?.GetCurrentTimeString()})");
                yield return StartCoroutine(IdleRoutine());
            }

            // Kiểm tra lại sau 1 giây
            yield return new WaitForSeconds(1f);
        }
    }

    // ✅ IDLE ROUTINE - ĐỨNG IM KHI KHÔNG PHẢI GIỜ HÁI HOA
    IEnumerator IdleRoutine()
    {
        currentState = NPCState.Idle;

        // Đứng yên ở vị trí hiện tại
        Rigidbody2D rb = GetComponent<Rigidbody2D>();
        if (rb != null)
        {
            rb.velocity = Vector2.zero;
        }

        // Set animation idle
        if (animator)
        {
            animator.SetBool("Walking", false);
            animator.SetBool("Idle", true);
        }

        // Đứng yên trong 1 giây rồi kiểm tra lại thời gian
        yield return new WaitForSeconds(1f);
    }



    void FixVillageCenter()
    {
        // ✅ Validate Village Center BEFORE any logic runs
        if (villageCenter == null)
        {
            // Debug.LogError($"❌ CRITICAL: Village Center NULL in Inspector!");

            // Try find by tag
            GameObject villageObj = GameObject.FindWithTag("VillageCenter");
            if (villageObj != null)
            {
                villageCenter = villageObj.transform;
                // Debug.Log($"✅ Found VillageCenter by tag: {villageObj.name} at {villageCenter.position}");
                return;
            }

            // If still null - create persistent one
            // Debug.LogWarning($"⚠️ Creating VillageCenter at NPC position: {transform.position}");
            GameObject newCenter = new GameObject("VillageCenter");
            newCenter.transform.position = transform.position;
            newCenter.tag = "VillageCenter";
            villageCenter = newCenter.transform;

            // Don't destroy on load
            DontDestroyOnLoad(newCenter);
        }
        else
        {
            // Debug.Log($"✅ Village Center set: {villageCenter.name} at {villageCenter.position}");
        }

        // Also validate Home Location
        if (homeLocation == null)
        {
            // Debug.LogWarning($"⚠️ Home Location NULL, setting to NPC position");
            homeLocation = transform;
        }
    }

    void UpdateCurrentActivity()
    {
        // Lấy giờ hiện tại từ TimeManager hoặc dùng giá trị test
        float hour = useRealTimeManager && TimeManager.Instance != null
            ? TimeManager.Instance.GetCurrentHour()
            : currentGameTime;

        // Log nhẹ (mỗi giây)
        if (Time.frameCount % 60 == 0)
        {
            Debug.Log($"🕒 NPC {name}: Giờ hiện tại {hour:F1}h → Activity={currentActivity}");
        }

        // Priority 1: Market trading for traders (8h-12h)
        if (isTrader && hour >= marketOpenHour && hour < marketCloseHour)
        {
            currentActivity = NPCActivity.MarketTrading;
        }
        // Priority 2: Flower hunting (14h-16h)
        else if (hour >= flowerHuntingStartHour && hour < flowerHuntingEndHour)
        {
            currentActivity = NPCActivity.FlowerHunting;
        }
        else
        {
            // Ngoài khung giờ đặc biệt: NPC đứng im hoặc làm activity khác
            // Có thể mở rộng thêm các activity khác theo giờ
            currentActivity = NPCActivity.FlowerHunting;
        }
    }


    // === ACTIVITY ROUTINES ===

    IEnumerator SleepRoutine()
    {
        currentState = NPCState.Resting;

        // Di chuyển về nhà nếu chưa ở nhà
        if (Vector3.Distance(transform.position, homeLocation.position) > 2f)
        {
            currentState = NPCState.MovingToTarget;
            bool reachHome = false;
            moveRoutine = StartCoroutine(MoveToPosition(homeLocation.position, r => reachHome = r));
            yield return moveRoutine;

        }

        // Đứng yên/đi ngủ
        transform.rotation = Quaternion.LookRotation(Vector3.forward);
        // ⚠️ Animation disabled
        // if (animator) animator.SetTrigger("Sleep");

        while (currentActivity == NPCActivity.Sleep)
        {
            yield return new WaitForSeconds(1f);
        }
    }

    IEnumerator MorningRoutine()
    {
        currentState = NPCState.Resting;

        // Dọn dẹp gần nhà
        Vector3 cleanSpot = homeLocation.position + new Vector3(Random.Range(-2f, 2f), Random.Range(-2f, 2f), 0f);
        bool reachClean = false;
        moveRoutine = StartCoroutine(MoveToPosition(cleanSpot, r => reachClean = r));
        yield return moveRoutine;


        // Đóng giả làm việc nhà
        // ⚠️ Animation disabled
        // if (animator) animator.SetTrigger("Clean");
        yield return new WaitForSeconds(2f);

        while (currentActivity == NPCActivity.MorningRoutine)
        {
            yield return new WaitForSeconds(1f);
        }
    }

    IEnumerator FlowerHuntingRoutine()
    {
        currentState = NPCState.Idle;
        int failedAttempts = 0;
        int flowersGathered = 0;
        int maxFlowersPerSession = 3; // Giới hạn số hoa mỗi session

        // Debug.Log($"🌸 {gameObject.name}: Bắt đầu FlowerHuntingRoutine!");

        while (currentActivity == NPCActivity.FlowerHunting && flowersGathered < maxFlowersPerSession)
        {
            // ✅ Giảm log spam - chỉ log quan trọng
            if (failedAttempts == 0 || failedAttempts % 3 == 0)
            {
                // Debug.Log($"🔍 {gameObject.name}: Tìm kiếm bông hoa... (lần thử {failedAttempts + 1})");
            }

            FlowerObject nearestFlower = FindNearestAvailableFlower();

            if (nearestFlower != null)
            {
                failedAttempts = 0; // Reset counter khi tìm thấy hoa
                float initialDistance = Vector3.Distance(transform.position, nearestFlower.position);

                // Debug.Log($"🎯 {gameObject.name}: Tìm thấy hoa '{nearestFlower.gameObject.name}' ở khoảng cách {initialDistance:F2}");

                currentState = NPCState.MovingToTarget;
                currentTargetFlower = nearestFlower;

                // ✅ Di chuyển đến vị trí hoa với timeout
                bool reachedFlower = false;
                float moveTimeout = 10f; // 10 giây timeout
                float moveTimer = 0f;

                while (Vector3.Distance(transform.position, nearestFlower.position) > flowerDetectionRadius && moveTimer < moveTimeout)
                {
                    // Kiểm tra hoa vẫn tồn tại
                    if (nearestFlower.gameObject == null)
                    {
                        // Debug.LogWarning($"⚠️ {gameObject.name}: Hoa bị destroy trong lúc di chuyển!");
                        break;
                    }

                    bool reachedFlowerMove2 = false;
                    moveRoutine = StartCoroutine(MoveToPosition(nearestFlower.position, r => reachedFlowerMove2 = r));
                    yield return moveRoutine;


                    moveTimer += Time.deltaTime;

                    // Nếu đã đủ gần, break
                    if (Vector3.Distance(transform.position, nearestFlower.position) <= flowerDetectionRadius)
                    {
                        reachedFlower = true;
                        break;
                    }
                }

                // ✅ Kiểm tra đã đến gần hoa chưa
                float finalDistance = Vector3.Distance(transform.position, nearestFlower.position);
                if (reachedFlower || finalDistance <= flowerDetectionRadius * 1.5f)
                {
                    // Debug.Log($"✅ {gameObject.name}: Đã đủ gần để hái hoa!");
                    currentState = NPCState.GatheringFlower;

                    // Debug.Log($"🌸 {gameObject.name}: Bắt đầu quá trình hái hoa {nearestFlower.gameObject.name}");

                    yield return StartCoroutine(GatherFlower(nearestFlower));

                    // Nếu đến được đây tức là gathering không bị exception
                    bool gatheringSuccess = true;
                    flowersGathered++;
                    if (gatheringSuccess)
                    {
                        // Debug.Log($"✅ {gameObject.name}: Hoàn thành hái hoa! Tổng số đã hái: {flowersGathered}");
                        // ✅ Gọi NPC để nhận flower gathered event
                        NPC npcComponent = GetComponent<NPC>();
                        if (npcComponent != null)
                        {
                            npcComponent.OnFlowerGathered(nearestFlower.gameObject);
                        }
                    }
                }
                else
                {
                    // Debug.LogWarning($"⚠️ {gameObject.name}: Không thể đến đủ gần hoa (khoảng cách: {finalDistance:F2}, timeout: {moveTimer:F1}s)");
                    failedAttempts++;
                }

                currentTargetFlower = null;
            }
            else
            {
                failedAttempts++;

                // ✅ Giảm log spam cho failed attempts
                if (failedAttempts % 3 == 0)
                {
                    // Debug.LogWarning($"❌ {gameObject.name}: Không tìm thấy hoa nào (thử {failedAttempts})");
                }

                // ✅ Tìm kiếm random area nhưng giới hạn
                if (failedAttempts <= 5)
                {
                    Vector3 explorePoint = villageCenter.position +
                        new Vector3(Random.Range(-wanderRadius * 0.3f, wanderRadius * 0.3f),
                                    Random.Range(-wanderRadius * 0.3f, wanderRadius * 0.3f), 0f);

                    // ✅ Đảm bảo không đi quá xa
                    float maxDistance = 10f;
                    if (Vector3.Distance(villageCenter.position, explorePoint) > maxDistance)
                    {
                        explorePoint = villageCenter.position +
                            (explorePoint - villageCenter.position).normalized * maxDistance;
                    }

                    // ✅ // Debug: Log explore point details
                    // Debug.Log($"🎲 {gameObject.name}: Creating explore point");
                    // Debug.Log($"🏘️ VillageCenter: {villageCenter.position:F2}");
                    // Debug.Log($"📏 WanderRadius: {wanderRadius:F2}");
                    // Debug.Log($"🎯 Target Explore Point: {explorePoint:F2}");
                    // Debug.Log($"📏 Distance from VillageCenter: {Vector3.Distance(villageCenter.position, explorePoint):F2}");

                    bool reachedExplore = false;
                    moveRoutine = StartCoroutine(MoveToPosition(explorePoint, r => reachedExplore = r));
                    yield return moveRoutine;
                    yield return new WaitForSeconds(0.5f);
                }
                else
                {
                    // Nếu fail quá nhiều lần, kết thúc session
                    // Debug.Log($"🚫 {gameObject.name}: Quá nhiều lần thử thất bại, kết thúc FlowerHunting");
                    break;
                }
            }

            // ✅ Giảm delay để tìm kiếm nhanh hơn
            yield return new WaitForSeconds(0.2f);
        }

        // Debug.Log($"🏁 {gameObject.name}: FlowerHuntingRoutine kết thúc (Đã hái {flowersGathered}/{maxFlowersPerSession} hoa)");
    }




    IEnumerator LunchBreakRoutine()
    {
        currentState = NPCState.Resting;
        currentState = NPCState.MovingToTarget;

        // Quay về làng để ăn trưa
        bool reachedLunch = false;
        moveRoutine = StartCoroutine(MoveToPosition(villageCenter.position, r => reachedLunch = r));
        yield return moveRoutine;

        if (!reachedLunch)
        {
            Debug.LogError($"{name}: Failed to reach lunch spot");
        }


        // Đứng yên ăn
        transform.rotation = Quaternion.LookRotation(Vector3.forward);
        // ⚠️ Animation disabled
        // if (animator) animator.SetTrigger("Eat");
        yield return new WaitForSeconds(Random.Range(30f, 60f)); // 30-60 giây ăn trưa

        while (currentActivity == NPCActivity.LunchBreak)
        {
            yield return new WaitForSeconds(1f);
        }
    }

    IEnumerator ExploreVillageRoutine()
    {
        currentState = NPCState.Idle;

        while (currentActivity == NPCActivity.ExploreVillage)
        {
            // Lang thang ngẫu nhiên quanh làng
            Vector3 wanderPoint = villageCenter.position +
                new Vector3(Random.Range(-wanderRadius, wanderRadius), Random.Range(-wanderRadius, wanderRadius), 0f);

            currentState = NPCState.MovingToTarget;
            bool reachedWander = false;
            moveRoutine = StartCoroutine(MoveToPosition(wanderPoint, r => reachedWander = r));
            yield return moveRoutine;


            // Dừng lại khoảng thời gian ngắn
            currentState = NPCState.Idle;
            yield return new WaitForSeconds(Random.Range(2f, 5f));

            yield return new WaitForSeconds(1f);
        }
    }

    IEnumerator EveningRoutine()
    {
        currentState = NPCState.Resting;
        currentState = NPCState.MovingToTarget;

        // Quay về nhà/area trung tâm của làng
        bool reachedEvening = false;
        moveRoutine = StartCoroutine(MoveToPosition(villageCenter.position, r => reachedEvening = r));
        yield return moveRoutine;


        // Nấu ăn/công việc tối
        // ⚠️ Animation disabled
        // if (animator) animator.SetTrigger("Cook");
        yield return new WaitForSeconds(Random.Range(30f, 60f));

        while (currentActivity == NPCActivity.EveningRoutine)
        {
            yield return new WaitForSeconds(1f);
        }
    }

    IEnumerator SocialTimeRoutine()
    {
        currentState = NPCState.Socializing;

        // Tìm NPCs khác để giao tiếp
        NPCRoutineAI[] otherNPCs = FindObjectsOfType<NPCRoutineAI>()
            .Where(npc => npc != this && Vector3.Distance(transform.position, npc.transform.position) < 5f)
            .ToArray();

        if (otherNPCs.Length > 0)
        {
            // Đi đến gần NPC khác
            NPCRoutineAI targetNPC = otherNPCs[Random.Range(0, otherNPCs.Length)];
            bool reachedSocial = false;
            moveRoutine = StartCoroutine(MoveToPosition(targetNPC.transform.position + Vector3.back * 2f, r => reachedSocial = r));
            yield return moveRoutine;

            // Giao tiếp (xoay mặt về phía NPC khác)
            transform.LookAt(targetNPC.transform.position);
            // ⚠️ Animation disabled
            // if (animator) animator.SetTrigger("Talk");

            yield return new WaitForSeconds(Random.Range(60f, 120f)); // 1-2 phút giao tiếp
        }
        else
        {
            // Không có NPC nào, đi lang thang
            Vector3 socialSpot = villageCenter.position + new Vector3(Random.Range(-3f, 3f), 0, Random.Range(-3f, 3f));
            bool reachSocial2 = false;
            moveRoutine = StartCoroutine(MoveToPosition(socialSpot, r => reachSocial2 = r));
            yield return moveRoutine;
            yield return new WaitForSeconds(Random.Range(30f, 60f));
        }

        while (currentActivity == NPCActivity.SocialTime)
        {
            yield return new WaitForSeconds(1f);
        }
    }

    IEnumerator NightRoutine()
    {
        currentState = NPCState.MovingToTarget;

        // Chuẩn bị đi ngủ - di chuyển về khu vực gần nhà
        Vector3 prepArea = homeLocation.position + new Vector3(Random.Range(-3f, 3f), Random.Range(-3f, 3f), 0f);
        bool reachedNight = false;
        moveRoutine = StartCoroutine(MoveToPosition(prepArea, r => reachedNight = r));
        yield return moveRoutine;

        // Đứng yên/thức dậy
        transform.rotation = Quaternion.LookRotation(Vector3.forward);
        // ⚠️ Animation disabled
        // if (animator) animator.SetTrigger("Prepare");
        yield return new WaitForSeconds(Random.Range(30f, 60f));

        while (currentActivity == NPCActivity.NightRoutine)
        {
            yield return new WaitForSeconds(1f);
        }
    }

    // === MARKET TRADING ROUTINE ===

    IEnumerator MarketTradingRoutine()
    {
        Debug.Log($"🏪 {gameObject.name}: Starting MarketTradingRoutine");

        NPCTrader trader = GetComponent<NPCTrader>();
        if (trader == null)
        {
            Debug.LogWarning($"⚠️ {gameObject.name}: No NPCTrader component found!");
            yield break;
        }

        // -------------------------------
        // STEP 0 — Find Market & StandPoint
        // -------------------------------
        if (marketStallLocation == null)
        {
            Debug.Log($"🔍 {name}: Searching for Market in scene...");

            GameObject camp =
                GameObject.FindWithTag("Camp") ??
                GameObject.Find("Camp(Clone)") ??
                GameObject.Find("Camp");

            if (camp != null)
            {
                Transform market =
                    camp.transform.Find("Market_0") ??
                    camp.transform.Find("Market") ??
                    camp.transform.Find("FlowerMarket_0");

                if (market != null)
                {
                    // Tìm theo tag "StandPoint" trong children của market
                    Transform stand = null;
                    foreach (Transform child in market)
                    {
                        if (child.CompareTag("StandPoint"))
                        {
                            stand = child;
                            break;
                        }
                    }

                    if (stand != null)
                    {
                        // Force X = 0 relative to market
                        Vector3 correctedLocalPos = stand.localPosition;
                        correctedLocalPos.x = 0f;
                        stand.localPosition = correctedLocalPos;
                        
                        marketStallLocation = stand;
                        Debug.Log($"📍 {name}: Using StandPoint (by Tag) - World pos: {stand.position}, Local pos: {stand.localPosition}, Market at: {market.position}");
                    }
                    else
                    {
                        // Tạo vị trí BÊN CẠNH market (tránh collider)
                        GameObject standPointObj = new GameObject($"{market.name}_StandPoint_Auto");
                        standPointObj.transform.SetParent(camp.transform);
                        standPointObj.transform.position = market.position;

                        marketStallLocation = standPointObj.transform;
                        Debug.LogWarning($"⚠️ No child with tag 'StandPoint' found! Created at {standPointObj.transform.position}");
                    }
                }
                else
                {
                    Debug.LogError($"❌ {name}: Market not found under Camp!");
                    yield break;
                }
            }
            else
            {
                Debug.LogError($"❌ {name}: Camp not found in scene!");
                yield break;
            }
        }

        Vector3 stallPos = marketStallLocation.position;

        // -------------------------------
        // STEP 1 — Move STRAIGHT TO MARKET
        // -------------------------------
        Debug.Log($"🚶 {name}: Moving directly to MARKET → World: {stallPos}, Local to parent: {marketStallLocation.localPosition}");

        bool reached = false;
        moveRoutine = StartCoroutine(MoveToPosition(stallPos, r => reached = r));
        yield return moveRoutine;
        if (!reached)
        {
            Debug.LogError($"❌ {name}: FAILED to reach Market Stall");
            yield break;
        }

        Debug.Log($"🏪 {name}: REACHED Market Stall → shop open!");

        // -------------------------------
        // STEP 2 — Idle while trading
        // -------------------------------
        currentState = NPCState.Idle;
        Rigidbody2D rb = GetComponent<Rigidbody2D>();
        if (rb) rb.velocity = Vector2.zero;

        while (currentActivity == NPCActivity.MarketTrading)
        {
            yield return new WaitForSeconds(1f);
        }

        Debug.Log($"🏁 {name}: MarketTrading ended.");
    }


    // === FLOWER GATHERING LOGIC ===

    public IEnumerator ScanForFlowers()
    {
        while (true)
        {
            // Tìm tất cả game objects được coi là "hoa"
            foreach (GameObject flower in GameObject.FindGameObjectsWithTag("Flower"))
            {
                AddFlowerIfNotExists(flower);
            }

            // Cũng tìm theo prefab list
            foreach (GameObject obj in GameObject.FindObjectsOfType<GameObject>())
            {
                foreach (GameObject flowerPrefab in flowerPrefabs)
                {
                    if (obj.name.Contains(flowerPrefab.name))
                    {
                        AddFlowerIfNotExists(obj);
                        if (!obj.CompareTag("Flower"))
                            obj.tag = "Flower";
                        break;
                    }
                }
            }

            // Loại bỏ những hoa đã bị hái
            availableFlowers.RemoveAll(flower => flower.gameObject == null || !flower.isAvailable);

            yield return new WaitForSeconds(5f); // Quét mỗi 5 giây
        }
    }

    void AddFlowerIfNotExists(GameObject flowerObj)
    {
        if (!availableFlowers.Any(f => f.gameObject == flowerObj))
        {
            availableFlowers.Add(new FlowerObject(flowerObj));
        }
    }

    FlowerObject FindNearestAvailableFlower()
    {
        // ✅ Giảm log spam - chỉ khi cần
        if (currentState == NPCState.Idle)
        {
            if (Time.frameCount % 60 == 0) // Log mỗi 1 giây thay vì mỗi lần
            {
                // Debug.Log($"🔍 {gameObject.name}: Bắt đầu tìm hoa gần nhất...");
            }
        }

        // ✅ Kiểm tra FlowerManager
        if (FlowerManager.Instance == null && Application.isPlaying)
        {
            if (currentState == NPCState.Idle)
            {
                // Debug.LogWarning($"⚠️ {gameObject.name}: FlowerManager chưa sẵn sàng!");
            }
            return null;
        }

        // ✅ Tạo danh sách tất cả hoa có sẵn
        List<GameObject> allFlowers = new List<GameObject>();

        // Thêm hoa từ FlowerManager
        if (FlowerManager.Instance != null)
        {
            allFlowers.AddRange(FlowerManager.Instance.GetAllFlowerObjects());
        }

        // Thêm hoa theo tag
        GameObject[] taggedFlowers = GameObject.FindGameObjectsWithTag("Flower");
        foreach (var flower in taggedFlowers)
        {
            if (!allFlowers.Contains(flower))
                allFlowers.Add(flower);
        }

        // ✅ Lọc hoa còn tồn tại và active
        allFlowers.RemoveAll(f => f == null || !f.activeInHierarchy);

        if (currentState == NPCState.Idle)
        {
            // Debug.Log($"🌸 {gameObject.name}: Tìm thấy {allFlowers.Count} bông hoa trong scene");

            // ✅ // Debug: Check for flowers at extreme positions
            foreach (GameObject flower in allFlowers.Take(5))
            {
                Vector3 pos = flower.transform.position;
                float distFromCenter = Vector3.Distance(pos, Vector3.zero);
                // Debug.Log($"🌸 Flower '{flower.name}' at ({pos.x:F1}, {pos.y:F1}) - distance from center: {distFromCenter:F1}");

                if (distFromCenter > 50f)
                {
                    // Debug.LogError($"⚠️ FLOWER TOO FAR FROM CENTER! This may be causing NPC to run to border: {flower.name}");
                }
            }
        }

        if (allFlowers.Count == 0)
        {
            if (currentState == NPCState.Idle)
            {
                // Debug.Log($"❌ {gameObject.name}: Không tìm thấy bông hoa nào");
            }
            return null;
        }

        // ✅ Tìm hoa gần nhất với logging tối thiểu
        GameObject nearestFlower = null;
        float minDistance = float.MaxValue;
        int totalChecked = 0;

        foreach (GameObject flower in allFlowers)
        {
            float distance = Vector3.Distance(transform.position, flower.transform.position);
            totalChecked++;

            // ✅ Chỉ log top 3 hoa gần nhất để giảm spam
            if (distance < minDistance || totalChecked <= 3)
            {
                if (distance < minDistance)
                {
                    minDistance = distance;
                    nearestFlower = flower;
                }

                if (totalChecked <= 3)
                {
                    // Debug.Log($"📏 {gameObject.name}: Kiểm tra hoa '{flower.name}' ở khoảng cách {distance:F2}");
                }
            }
        }

        if (nearestFlower == null)
        {
            // Debug.Log($"❌ {gameObject.name}: Không xác định được hoa gần nhất");
            return null;
        }

        // ✅ Important log - always show this
        // Debug.Log($"🎯 {gameObject.name}: Tìm thấy hoa gần nhất '{nearestFlower.name}' ở gần nhất '{nearestFlower.name}' ở khoảng cách {minDistance:F2}");
        // Debug.Log($"🌸 Flower Position: {nearestFlower.transform.position:F2}");

        // ✅ Kiểm tra khoảng cách hợp lý để di chuyển
        float maxDistance = flowerDetectionRadius * 4f; // Tăng tầm tìm lên 12f
        if (minDistance > maxDistance)
        {
            // Debug.LogWarning($"⚠️ {gameObject.name}: Hoa '{nearestFlower.name}' quá xa ({minDistance:F2} > {maxDistance})");
            return null;
        }

        return new FlowerObject(nearestFlower);
    }



    IEnumerator GatherFlower(FlowerObject flower)
    {
        if (!flower.isAvailable)
        {
            // Debug.LogWarning($"⚠️ {gameObject.name}: Không thể hái hoa - hoa không available");
            yield break;
        }

        SpriteRenderer sr = GetComponent<SpriteRenderer>();
        if (sr != null)
            // Debug.Log($"🧍‍♂️ {gameObject.name}: SpriteRenderer enabled = {sr.enabled}, color={sr.color}");

            // Debug.Log($"🌸 {gameObject.name}: Bắt đầu hái hoa '{flower.gameObject.name}'");



            // ⚠️ Animation disabled - no animation parameters yet
            // if (animator != null)
            // {
            //     try { animator.SetTrigger("Gather"); }
            //     catch
            //     { 
            //         // Debug.LogWarning($"⚠️ {gameObject.name}: Trigger Gather fail"); 
            //     }
            // }

            yield return StartCoroutine(GatheringTimer(flower));

        // Kiểm tra nếu NPC bị ẩn sprite trong lúc hái
        //         if (sr != null && !sr.enabled)
        //     // Debug.LogError($"❌ {gameObject.name}: SpriteRenderer bị tắt trong lúc hái hoa!");
        // // else
        // //     // Debug.Log($"✅ {gameObject.name}: SpriteRenderer vẫn hiển thị bình thường.");

        // //                 // Debug.Log($"✅ {gameObject.name}: Hoàn thành việc hái hoa!");
    }


    IEnumerator GatheringTimer(FlowerObject flower)
    {
        float timer = 0f;
        float logInterval = 1f; // Log mỗi 1 giây
        float lastLogTime = 0f;
        Vector3 lockedPos = transform.position;

        // Debug.Log($"⏱️ {gameObject.name}: Bắt đầu đếm ngược hái hoa ({gatheringTime}s)");

        // Lock flower ngay khi bắt đầu gathering
        flower.isAvailable = false;

        // Đảm bảo flower vẫn tồn tại
        if (flower.gameObject == null)
        {
            // Debug.LogError($"❌ {gameObject.name}: Hoa đã bị destroy!");
            yield break;
        }

        while (timer < gatheringTime)
        {
            transform.position = lockedPos;

            float currentDistance = Vector3.Distance(transform.position, flower.position);
            if (currentDistance > flowerDetectionRadius * 2f)
            {
                // Debug.LogWarning($"⚠️ {gameObject.name}: NPC rời xa hoa ({currentDistance:F2}) — có thể rơi xuống?");
                // Debug.Log($"📍 LockedPos={lockedPos}, CurrentPos={transform.position}");
                yield break;
            }

            if (Mathf.Abs(transform.position.z) > 0.1f)
            {
                // Debug.LogError($"❌ {gameObject.name}: Z bị lệch khỏi mặt phẳng 2D ({transform.position.z:F3}) — NPC có thể biến mất khỏi camera!");
                transform.position = new Vector3(transform.position.x, transform.position.y, 0f);
            }

            timer += Time.deltaTime;
            yield return null;
        }


        // Hoàn thành hái - hoa biến mất
        if (flower.gameObject != null)
        {
            // Debug.Log($"🎉 {gameObject.name}: Hoàn thành hái hoa '{flower.gameObject.name}'!");

            // ⚠️ Animation disabled
            // if (animator)
            // {
            //     animator.SetTrigger("GatherComplete");
            // }
            yield return new WaitForSeconds(0.5f); // Giảm delay

            // ✅ Gọi FlowerManager để quản lý respawn
            if (FlowerManager.Instance != null)
            {
                // Debug.Log($"🔄 {gameObject.name}: Gửi hoa {flower.gameObject.name} cho FlowerManager để xử lý");
                StartCoroutine(DelayedRemoveFlower(flower.gameObject, 1.5f));

            }
            else
            {
                // Debug.LogWarning($"⚠️ {gameObject.name}: FlowerManager null, tự hủy hoa!");
                Destroy(flower.gameObject, 0.1f);
            }

            // Debug.Log($"🌸 NPC {gameObject.name} đã hái hoa thành công tại {flower.position}");
        }
        else
        {
            // Debug.LogError($"❌ {gameObject.name}: Hoa không tồn tại khi hoàn thành hái!");
        }
    }
    IEnumerator DelayedRemoveFlower(GameObject flower, float delay)
    {
        yield return new WaitForSeconds(delay);
        if (FlowerManager.Instance != null)
            FlowerManager.Instance.RemoveFlower(flower);
        else
            Destroy(flower);
    }



    // === MOVEMENT LOGIC ===

    public IEnumerator MoveToPosition(Vector3 targetPos, System.Action<bool> onComplete)
    {
        if (physicsLockedForMarket)
        {
            Debug.Log($"⛔ {name}: Movement blocked — market active");
            onComplete?.Invoke(false);
            yield break;
        }
        if (mapGenerator == null)
            mapGenerator = FindObjectOfType<MapGenerator>();

        targetPos.z = 0;
        Rigidbody2D rb = GetComponent<Rigidbody2D>();
        if (!rb)
        {
            onComplete?.Invoke(false);
            yield break;
        }

        Debug.Log($"🚶 {name}: MoveToPosition → {targetPos}");

        currentState = NPCState.MovingToTarget;
        currentTargetPosition = targetPos;

        // Convert to tile
        Vector2Int endTile = new(Mathf.RoundToInt(targetPos.x), Mathf.RoundToInt(targetPos.y));

        if (endTile.x < 0 || endTile.x >= mapGenerator.width ||
            endTile.y < 0 || endTile.y >= mapGenerator.height)
        {
            Debug.LogError($"❌ {name}: Target OUT OF MAP");
            onComplete?.Invoke(false);
            yield break;
        }

        // Find A* path
        List<Vector3> path = FindPath(transform.position, targetPos);

        if (path == null || path.Count == 0)
        {
            Debug.LogError($"❌ {name}: NO PATH FOUND");
            onComplete?.Invoke(false);
            yield break;
        }

        int index = 0;
        float timeout = 10f;
        float timer = 0f;
        int repathCount = 0;

        float stopDistance = 0.15f;
        float finalStop = 0.45f;

        int mask = LayerMask.GetMask("Obstacle", "Water");

        while (index < path.Count)
        {
            Vector3 next = path[index];
            next.z = 0;

            Vector2 dir = ((Vector2)next - rb.position);
            float dist = dir.magnitude;

            if (dist < stopDistance)
            {
                index++;
                continue;
            }

            dir.Normalize();

            // Obstacle check
            if (Physics2D.Raycast(rb.position, dir, 0.25f, mask))
            {
                repathCount++;
                if (repathCount > 4)
                {
                    Debug.LogError($"❌ {name}: Cannot repath → FAIL");
                    onComplete?.Invoke(false);
                    yield break;
                }

                path = FindPath(transform.position, targetPos);
                index = 0;
                yield return new WaitForSeconds(0.1f);
                continue;
            }

            // Move
            rb.MovePosition(rb.position + dir * moveSpeed * Time.fixedDeltaTime);

            float distToTarget = Vector3.Distance(rb.position, targetPos);
            if (distToTarget < finalStop)
            {
                Debug.Log($"🏁 {name}: Arrived target");
                currentState = NPCState.Idle;
                onComplete?.Invoke(true);
                yield break;
            }

            // Timeout removed - let NPC take as long as needed to reach destination
            // timer += Time.deltaTime;
            // if (timer >= timeout)
            // {
            //     Debug.LogError($"⏰ {name}: TIMEOUT");
            //     onComplete?.Invoke(false);
            //     yield break;
            // }

            yield return new WaitForFixedUpdate();
        }

        rb.velocity = Vector2.zero;
        currentState = NPCState.Idle;
        onComplete?.Invoke(true);
    }


    // === PAUSE/RESUME SYSTEM ===

    public void PauseCurrentActivity()
    {
        if (isPaused) return;
        isPaused = true;

        // Không StopAllCoroutines — chỉ tạm dừng di chuyển
        Rigidbody2D rb = GetComponent<Rigidbody2D>();
        if (rb != null)
        {
            rb.velocity = Vector2.zero;
            rb.angularVelocity = 0f;
        }

        currentState = NPCState.Idle;

        if (animator)
        {
            animator.SetBool("Walking", false);
            animator.SetBool("Idle", true);
        }
    }
    public void DisablePhysicsForMarket()
    {
        physicsLockedForMarket = true;
        if (moveRoutine != null)
        {
            StopCoroutine(moveRoutine);
            moveRoutine = null;
        }

        Rigidbody2D rb = GetComponent<Rigidbody2D>();
        if (rb != null)
        {
            rb.velocity = Vector2.zero;
            rb.simulated = false;
        }
    }

    public void EnablePhysicsAfterMarket()
    {
        physicsLockedForMarket = false;
        Rigidbody2D rb = GetComponent<Rigidbody2D>();
        if (rb != null)
        {
            rb.simulated = true;
        }
    }



    public void ResumeCurrentActivity()
    {
        if (!isPaused) return;
        isPaused = false;

        Debug.Log($"▶️ {gameObject.name}: Resuming activity - SimpleFlowerHunting will continue checking conditions");

        // SimpleFlowerHunting vẫn đang chạy, chỉ cần unpause
        // Coroutine sẽ tự tiếp tục từ vị trí đã pause
        // KHÔNG start coroutine mới để tránh duplicate
    }


    // === ACTIVITY MANAGEMENT ===

    IEnumerator StartActivity(NPCActivity activity)
    {
        // Gọi trực tiếp routine, không cần lồng StartCoroutine ở đây
        yield return StartCoroutine(StartActivityInternal(activity));
    }


    IEnumerator StartActivityInternal(NPCActivity activity)
    {
        switch (activity)
        {
            case NPCActivity.Sleep:
                yield return StartCoroutine(SleepRoutine());
                break;
            case NPCActivity.MorningRoutine:
                yield return StartCoroutine(MorningRoutine());
                break;
            case NPCActivity.FlowerHunting:
                yield return StartCoroutine(FlowerHuntingRoutine());
                break;
            case NPCActivity.MarketTrading:
                yield return StartCoroutine(MarketTradingRoutine());
                break;
            case NPCActivity.LunchBreak:
                yield return StartCoroutine(LunchBreakRoutine());
                break;
            case NPCActivity.ExploreVillage:
                yield return StartCoroutine(ExploreVillageRoutine());
                break;
            case NPCActivity.EveningRoutine:
                yield return StartCoroutine(EveningRoutine());
                break;
            case NPCActivity.SocialTime:
                yield return StartCoroutine(SocialTimeRoutine());
                break;
            case NPCActivity.NightRoutine:
                yield return StartCoroutine(NightRoutine());
                break;
        }
        yield break;
    }

    // === PAUSE/RESUME SYSTEM ===

    public string GetCurrentActivityName()
    {
        return currentActivity.ToString();
    }

    public float GetCurrentGameTime()
    {
        return useRealTimeManager && TimeManager.Instance != null ?
            TimeManager.Instance.GetCurrentHour() : currentGameTime;
    }

    // ✅ Method để kiểm tra có phải giờ hái hoa không
    public bool IsFlowerHuntingTime()
    {
        float currentHour = GetCurrentGameTime();
        return currentHour >= flowerHuntingStartHour && currentHour < flowerHuntingEndHour;
    }



    // ✅ Method để set thời gian thủ công (cho testing)
    public void SetCustomTime(float hour)
    {
        currentGameTime = hour;
        useRealTimeManager = false; // Tạm tắt TimeManager khi set thủ công
    }

    // ✅ Method để bật lại TimeManager
    public void UseTimeManager(bool use)
    {
        useRealTimeManager = use;
    }

    void OnDrawGizmosSelected()
    {
        // Vẽ vùng home
        if (homeLocation != null)
        {
            Gizmos.color = Color.blue;
            Gizmos.DrawWireSphere(homeLocation.position, 2f);
        }

        // Vẽ vùng làng
        if (villageCenter != null)
        {
            Gizmos.color = Color.green;
            Gizmos.DrawWireSphere(villageCenter.position, wanderRadius);
        }



        // Vẽ vùng phát hiện hoa
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, flowerDetectionRadius);

        // Vẽ đường đi đến mục tiêu
        if (currentState == NPCState.MovingToTarget || currentState == NPCState.GatheringFlower)
        {
            Gizmos.color = currentState == NPCState.GatheringFlower ? Color.magenta : Color.red;
            Gizmos.DrawLine(transform.position, currentTargetPosition);
        }
    }
    void ClampToMapBounds()
    {
        if (mapGenerator == null) return;

        Vector3 pos = transform.position;

        // Nếu tilemap của bạn là 1:1 thì để nguyên;
        // nếu mỗi tile = 0.5f hoặc 2f, nhân theo tỉ lệ scale.
        float maxX = mapGenerator.width;
        float maxY = mapGenerator.height;

        pos.x = Mathf.Clamp(pos.x, 0f, maxX);
        pos.y = Mathf.Clamp(pos.y, 0f, maxY);
        pos.z = 0f; // 2D giữ cố định Z

        transform.position = pos;
    }
    #region ===== PATHFINDING SUPPORT =====

    class Node
    {
        public Vector2Int pos;
        public float gCost, hCost;
        public Node parent;
        public float fCost => gCost + hCost;

        public Node(Vector2Int position) => pos = position;
    }

    List<Vector3> FindPath(Vector3 start, Vector3 end)
    {
        if (mapGenerator == null)
            mapGenerator = FindObjectOfType<MapGenerator>();

        Vector2Int startTile = new(Mathf.RoundToInt(start.x), Mathf.RoundToInt(start.y));
        Vector2Int endTile = new(Mathf.RoundToInt(end.x), Mathf.RoundToInt(end.y));

        List<Node> openList = new();
        HashSet<Vector2Int> closedSet = new();

        Node startNode = new(startTile);
        startNode.gCost = 0;
        startNode.hCost = Vector2Int.Distance(startTile, endTile);
        openList.Add(startNode);

        while (openList.Count > 0)
        {
            Node current = openList.OrderBy(n => n.fCost).First();

            if (current.pos == endTile)
                return ReconstructPath(current);

            openList.Remove(current);
            closedSet.Add(current.pos);

            foreach (Vector2Int neighbor in GetNeighbors(current.pos))
            {
                if (closedSet.Contains(neighbor)) continue;
                if (!IsWalkable(neighbor)) continue;

                float tentativeG = current.gCost + Vector2Int.Distance(current.pos, neighbor);

                Node neighborNode = openList.FirstOrDefault(n => n.pos == neighbor);
                if (neighborNode == null)
                {
                    neighborNode = new Node(neighbor);
                    neighborNode.parent = current;
                    neighborNode.gCost = tentativeG;
                    neighborNode.hCost = Vector2Int.Distance(neighbor, endTile);
                    openList.Add(neighborNode);
                }
                else if (tentativeG < neighborNode.gCost)
                {
                    neighborNode.parent = current;
                    neighborNode.gCost = tentativeG;
                }
            }
        }

        // Debug.LogWarning($"⚠️ {gameObject.name}: Không tìm được đường hợp lệ!");
        return new List<Vector3>();
    }

    List<Vector3> ReconstructPath(Node endNode)
    {
        List<Vector3> path = new();
        Node current = endNode;
        while (current != null)
        {
            path.Add(new Vector3(current.pos.x + 0.5f, current.pos.y + 0.5f, 0));
            current = current.parent;
        }
        path.Reverse();
        return path;
    }

    IEnumerable<Vector2Int> GetNeighbors(Vector2Int tile)
    {
        yield return tile + Vector2Int.up;
        yield return tile + Vector2Int.down;
        yield return tile + Vector2Int.left;
        yield return tile + Vector2Int.right;
    }

    bool IsWalkable(Vector2Int tile)
    {
        if (tile.x < 0 || tile.x >= mapGenerator.width ||
            tile.y < 0 || tile.y >= mapGenerator.height)
            return false;

        Vector3 worldPos = new(tile.x + 0.5f, tile.y + 0.5f, 0);
        Collider2D hit = Physics2D.OverlapCircle(worldPos, 0.25f, LayerMask.GetMask("Obstacle", "Water"));
        return hit == null;
    }
    #endregion

    // ===== FLOWER GATHERING INTEGRATION =====

    /// <summary>
    /// Called by NavActionHandler when player requests flower gathering
    /// </summary>
    public void PlayerMadeGatheringRequest()
    {
        Debug.Log($"🌸 {gameObject.name}: Player requested flower gathering!");
        playerMadeRequest = true;

        // Reset request after completion
        if (stopResetCoroutine != null)
        {
            StopCoroutine(stopResetCoroutine);
        }
        stopResetCoroutine = StartCoroutine(ResetPlayerRequest());
    }

    private Coroutine stopResetCoroutine;

    IEnumerator ResetPlayerRequest()
    {
        yield return new WaitForSeconds(30f);
        playerMadeRequest = false;
        Debug.Log($"🌸 {gameObject.name}: Reset player request (timeout)");
    }

    /// <summary>
    /// Check if NPC currently has player gathering request
    /// </summary>
    public bool HasPlayerRequest()
    {
        return playerMadeRequest;
    }

    /// <summary>
    /// Force reset player request (debug/external use)
    /// </summary>
    public void ForceResetPlayerRequest()
    {
        Debug.Log($"🔴 {gameObject.name}: Force resetting player request from {playerMadeRequest} to false");
        playerMadeRequest = false;
        if (stopResetCoroutine != null)
        {
            StopCoroutine(stopResetCoroutine);
            stopResetCoroutine = null;
        }
    }
}

