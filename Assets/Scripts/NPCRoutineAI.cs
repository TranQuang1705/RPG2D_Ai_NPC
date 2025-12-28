using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Linq;


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
    public Transform homeLocation;
    public Transform villageCenter;
    public float wanderRadius = 10f;

    [Header("Flower Gathering")]
    public List<GameObject> flowerPrefabs;
    public float flowerDetectionRadius = 5f;
    public float gatheringTime = 3f;
    public LayerMask flowerLayer;

    [Header("Movement Settings")]
    public float moveSpeed = 3.5f;
    public float detectionRadius = 5f;
    [Header("Movement Direction")]
    public bool useOnlyHorizontalMovement = false;

    [Header("Time Settings")]
    public float dayDurationInMinutes = 24f;
    [Header("References")]
    public MapGenerator mapGenerator;

    private List<FlowerObject> availableFlowers = new List<FlowerObject>();
    private FlowerObject currentTargetFlower;
    private Vector3 currentTargetPosition;
    private Animator animator;
    private float currentGameTime = 6f;
    private bool playerMadeRequest = false;

    [Header("Time-based Flower Hunting")]
    public bool useRealTimeManager = true;
    public float flowerHuntingStartHour = 14f;
    public float flowerHuntingEndHour = 16f;
    private Coroutine activityCoroutine;
    private Coroutine gatheringCoroutine;

    [Header("Market Trading (for traders)")]
    public bool isTrader = false;
    public float marketOpenHour = 8f;
    public float marketCloseHour = 12f;
    public Transform marketStallLocation;

    private bool isPaused = false;
    private NPCActivity pausedActivity;
    private IEnumerator pausedCoroutine;

    public static NPCRoutineAI Instance;
    private Coroutine moveRoutine;
    private bool physicsLockedForMarket = false;
    public bool requestGoHome = false;
    private int lastCheckedHour = -1;




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
        StartCoroutine(ScanForFlowers());
    }


    void LateUpdate()
    {
        if (TimeManager.Instance == null) return;

        int currentHour = Mathf.FloorToInt(TimeManager.Instance.GetCurrentHour());

        if (currentHour != lastCheckedHour)
        {
            Debug.Log($"⏰ {name} adsadadasd: Hour changed to {currentHour}h");
            lastCheckedHour = currentHour;

            NPCActivity prev = currentActivity;
            UpdateCurrentActivity();
            Debug.Log($"⏰ {name} adsadadasd : CurrentActivity updated to {currentActivity}");
            if (currentActivity != prev)
                SwitchActivity(currentActivity);
        }

        if (currentState != NPCState.MovingToTarget &&
            currentState != NPCState.GatheringFlower)
            ClampToMapBounds();
    }


    void FixedUpdate()
    {
        return;
    }

    void InitializeFlowerHunter()
    {
        wanderRadius = 15f;
        flowerDetectionRadius = 3f;

        if (villageCenter == null)
        {
            villageCenter = transform;
        }


    }
    void SwitchActivity(NPCActivity newActivity)
    {
        if (activityCoroutine != null)
            StopCoroutine(activityCoroutine);

        Debug.Log($"🔁 {name}: Switching activity → {newActivity}");

        activityCoroutine = StartCoroutine(StartActivity(newActivity));
    }










    // ✅ TIME-BASED ROUTINE - CHỈ HÁI HOA 15:00-18:00


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

    public void UpdateCurrentActivity()
    {
        float hour = useRealTimeManager && TimeManager.Instance != null
            ? TimeManager.Instance.GetCurrentHour()
            : currentGameTime;
        Debug.Log($"⏰asdasdadad {name}: UpdateCurrentActivity called at {hour:F2}h");
        if (Time.frameCount % 60 == 0)
        {
            Debug.Log($"🕒 NPC {name}: Giờ hiện tại {hour:F1}h → Activity={currentActivity}");
        }

        if (isTrader && hour >= marketOpenHour && hour < marketCloseHour)
        {
            currentActivity = NPCActivity.MarketTrading;
        }
        else if (isTrader && hour >= marketCloseHour && hour < 13f)
        {
            currentActivity = NPCActivity.LunchBreak;
        }
        else if (hour >= flowerHuntingStartHour && hour < flowerHuntingEndHour)
        {
            currentActivity = NPCActivity.FlowerHunting;
        }
        else
        {

            currentActivity = NPCActivity.LunchBreak;
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

        int flowersGathered = 0;
        int maxFlowersPerSession = 3;
        int failedAttempts = 0;

        Debug.Log($"🌸 {name}: Start FlowerHuntingRoutine");

        while (currentActivity == NPCActivity.FlowerHunting &&
               flowersGathered < maxFlowersPerSession)
        {
            FlowerObject flower = FindNearestAvailableFlower();

            if (flower == null)
            {
                failedAttempts++;
                Debug.Log($"❌ {name}: No flower found (attempt {failedAttempts})");

                yield return new WaitForSeconds(2f);
                continue;
            }

            failedAttempts = 0;
            currentTargetFlower = flower;

            currentState = NPCState.MovingToTarget;
            bool reached = false;

            Debug.Log($"🚶 {name}: Moving to flower {flower.gameObject.name}");

            yield return StartCoroutine(
                MoveToPosition(flower.gameObject.transform.position, r => reached = r)
            );

            if (!reached || flower.gameObject == null || !flower.isAvailable)
            {
                Debug.LogWarning($"⚠️ {name}: Failed to reach flower");
                currentTargetFlower = null;
                yield return new WaitForSeconds(0.5f);
                continue;
            }

            currentState = NPCState.GatheringFlower;

            bool gathered = false;
            yield return StartCoroutine(
                GatherFlower(flower, success => gathered = success)
            );

            if (gathered)
            {
                flowersGathered++;
                Debug.Log($"🌼 {name}: Gathered flower {flowersGathered}/{maxFlowersPerSession}");

                NPC npc = GetComponent<NPC>();
                if (npc != null)
                    npc.OnFlowerGathered(flower.gameObject);
            }
            else
            {
                Debug.LogWarning($"⚠️ {name}: Gather failed");
            }

            currentTargetFlower = null;
            currentState = NPCState.Idle;

            yield return new WaitForSeconds(0.3f);
        }

        Debug.Log($"🏁 {name}: FlowerHuntingRoutine END (gathered {flowersGathered})");
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
                        Vector3 correctedLocalPos = stand.localPosition;
                        correctedLocalPos.x = 0f;
                        stand.localPosition = correctedLocalPos;

                        marketStallLocation = stand;
                        Debug.Log($"📍 {name}: Using StandPoint (by Tag) - World pos: {stand.position}, Local pos: {stand.localPosition}, Market at: {market.position}");
                    }
                    else
                    {
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

        currentState = NPCState.Idle;
        Rigidbody2D rb = GetComponent<Rigidbody2D>();
        if (rb) rb.velocity = Vector2.zero;

        while (true)
        {
            float hour = TimeManager.Instance.GetCurrentHour();
            if (hour >= marketCloseHour)
            {

                MarketTrigger trigger = FindObjectOfType<MarketTrigger>();
                if (trigger != null)
                {

                    trigger.ShowNPC(gameObject);
                    trigger.ResetMarketToNormal();

                }
                currentState = NPCState.Idle;
                yield break;
            }
            yield return new WaitForSeconds(1f);
        }


    }



    public IEnumerator ScanForFlowers()
    {
        while (true)
        {
            GameObject[] flowers = GameObject.FindGameObjectsWithTag("Flower");

            foreach (GameObject flower in flowers)
            {
                if (flower == null) continue;

                if (!availableFlowers.Any(f => f.gameObject == flower))
                {
                    availableFlowers.Add(new FlowerObject(flower));
                    Debug.Log($"🌱 {name}: Added flower {flower.name}");
                }
            }

            // ❗ CHỈ remove hoa bị destroy
            availableFlowers.RemoveAll(f => f.gameObject == null);

            yield return new WaitForSeconds(5f);
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
        FlowerObject nearest = null;
        float minDistance = float.MaxValue;

        foreach (FlowerObject flower in availableFlowers)
        {
            if (flower.gameObject == null) continue;
            if (!flower.isAvailable) continue;

            Vector3 flowerPos = flower.gameObject.transform.position;
            flowerPos.z = 0f;

            float dist = Vector3.Distance(transform.position, flowerPos);

            if (dist < minDistance && dist <= flowerDetectionRadius * 4f)
            {
                minDistance = dist;
                nearest = flower;
            }
        }

        if (nearest == null)
            Debug.Log($"❌ {name}: No available flower found");

        return nearest;
    }




    IEnumerator GatherFlower(FlowerObject flower, System.Action<bool> onDone)
    {
        if (flower == null || flower.gameObject == null || !flower.isAvailable)
        {
            onDone?.Invoke(false);
            yield break;
        }

        bool success = false;

        yield return StartCoroutine(
            GatheringTimer(flower, r => success = r)
        );

        onDone?.Invoke(success);
    }



    IEnumerator GatheringTimer(FlowerObject flower, System.Action<bool> onDone)
    {
        float timer = 0f;

        if (flower == null || flower.gameObject == null)
        {
            onDone?.Invoke(false);
            yield break;
        }

        // 🔒 LOCK HOA KHI BẮT ĐẦU
        flower.isAvailable = false;

        Vector3 lockedPos = transform.position;
        GameObject flowerGO = flower.gameObject; // cache reference

        while (timer < gatheringTime)
        {
            // ❗ HOA BỊ DESTROY GIỮA CHỪNG
            if (flowerGO == null)
            {
                onDone?.Invoke(false);
                yield break;
            }

            // Giữ NPC đứng yên
            transform.position = lockedPos;

            float dist = Vector3.Distance(
                transform.position,
                flowerGO.transform.position
            );

            // ❗ NPC bị đẩy ra xa
            if (dist > flowerDetectionRadius * 2f)
            {
                flower.isAvailable = true; // UNLOCK
                onDone?.Invoke(false);
                yield break;
            }

            timer += Time.deltaTime;
            yield return null;
        }

        // ✅ HÁI HOA THÀNH CÔNG
        if (flowerGO != null)
        {
            // Xóa khỏi list TRƯỚC
            availableFlowers.RemoveAll(f => f.gameObject == flowerGO);

            // Gửi FlowerManager xử lý
            if (FlowerManager.Instance != null)
                FlowerManager.Instance.RemoveFlower(flowerGO);
            else
                Destroy(flowerGO);

            flower.gameObject = null;
            onDone?.Invoke(true);
            yield break;
        }

        onDone?.Invoke(false);
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
        float hour = TimeManager.Instance.GetCurrentHour();
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
    public void ForceGoHome()
    {
        Debug.Log($"🏠 {name}: ForceGoHome() called");

        if (homeLocation == null)
        {
            Debug.LogWarning($"⚠️ {name}: homeLocation is NULL → using current position");
            homeLocation = transform;
        }

        if (moveRoutine != null)
            StopCoroutine(moveRoutine);
    }

    IEnumerator GoHomeRoutine()
    {
        bool reached = false;

        yield return MoveToPosition(villageCenter.position, r => reached = r);
        Debug.Log($"🏠 {name}: GoHomeRoutine completed");
        if (reached)
            Debug.Log($"🏡 {name}: Arrived home, switching to Idle");
        else
            Debug.LogWarning($"⚠️ {name}: Could not reach home, forcing Idle anyway");

        currentState = NPCState.Idle;
    }

    IEnumerator WanderAroundCamp()
    {
        EnablePhysicsAfterMarket();

        Transform campCenter = marketStallLocation.parent;
        if (campCenter == null)
        {
            Debug.LogWarning($"⚠️ {name}: Camp center is NULL, cannot wander around camp");
            yield break;
        }

        float radius = 4.5f;
        float minStep = 1.2f;
        float maxStep = 2.2f;
        float minWait = 1.0f;
        float maxWait = 2.5f;

        Debug.Log($"🚶 {name}: Start optimized wander around camp...");

        while (true)
        {
            float randomAngle = Random.Range(0f, 360f) * Mathf.Deg2Rad;
            float stepSize = Random.Range(minStep, maxStep);

            Vector3 dir = new Vector3(Mathf.Cos(randomAngle), Mathf.Sin(randomAngle), 0f);
            Vector3 target = transform.position + dir * stepSize;

            if (Vector3.Distance(target, transform.position) < 0.5f)
                continue;

            Vector3 centerOffset = target - campCenter.position;
            if (centerOffset.magnitude > radius)
                target = campCenter.position + centerOffset.normalized * radius;

            bool reached = false;
            yield return StartCoroutine(MoveToPosition(target, r => reached = r));

            if (!reached)
            {
                yield return new WaitForSeconds(0.5f);
                continue;
            }

            if (animator != null)
            {
                animator.SetBool("Walking", false);
                animator.SetBool("Idle", true);
            }

            yield return new WaitForSeconds(Random.Range(minWait, maxWait));
        }
    }
    public void RequestGoHome()
    {
        requestGoHome = true;
    }

}

