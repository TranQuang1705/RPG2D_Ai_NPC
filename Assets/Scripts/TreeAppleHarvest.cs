using System.Collections;
using UnityEngine;


public class TreeAppleHarvest : MonoBehaviour
{
    [Header("Apple Drop Settings")]
    [SerializeField] private GameObject applePrefab; // Prefab quả táo
    [SerializeField] private int hitsToHarvest = 3; // Số lần đánh để thu hoạch
    [SerializeField] private int applesPerHarvest = 3; // Số táo rơi mỗi lần

    [Header("Drop Position Settings")]
    [SerializeField] private float dropRadius = 0.5f; // Bán kính rơi quanh cây
    [SerializeField] private Vector2 dropOffset = Vector2.zero; // Offset vị trí rơi

    [Header("Visual Effects")]
    [SerializeField] private GameObject harvestVFX; // Hiệu ứng khi thu hoạch
    [SerializeField] private AudioClip harvestSound; // Âm thanh thu hoạch

    [Header("Hit Animation")]
    [SerializeField] private GameObject hitAnimationObject; // Object chứa animation lá rơi
    [SerializeField] private float animationStopDelay = 0.5f; // Thời gian chờ sau khi ngưng chém để tắt animation

    private int currentHits = 0;
    private AudioSource audioSource;
    private TreeStateCycle treeStateCycle;
    private Coroutine stopAnimationCoroutine;

    void Awake()
    {
        audioSource = GetComponent<AudioSource>();
        if (audioSource == null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
        }
        treeStateCycle = GetComponent<TreeStateCycle>();

        // Tắt animation ban đầu
        if (hitAnimationObject != null)
        {
            hitAnimationObject.SetActive(false);
        }
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        // Kiểm tra xem có phải là damage source không
        if (other.gameObject.GetComponent<DamageSource>() || other.gameObject.GetComponent<ProjectTile>())
        {
            OnTreeHit();
        }
    }

    void OnTreeHit()
    {
        // Bật animation lá rơi khi bị chém (bất kể trạng thái nào)
        if (hitAnimationObject != null)
        {
            hitAnimationObject.SetActive(true);

            // Hủy coroutine cũ nếu đang chạy
            if (stopAnimationCoroutine != null)
            {
                StopCoroutine(stopAnimationCoroutine);
            }

            // Bắt đầu coroutine mới để tắt animation sau một khoảng thời gian
            stopAnimationCoroutine = StartCoroutine(StopAnimationAfterDelay());
        }

        // Chỉ tính hit và harvest khi cây ở trạng thái Apple
        if (treeStateCycle != null && treeStateCycle.GetCurrentState() == TreeStateCycle.TreeState.Apple)
        {
            currentHits++;
            Debug.Log($"[TreeHarvest] Hit {currentHits}/{hitsToHarvest} on Apple tree");

            // Khi đủ số lần đánh
            if (currentHits >= hitsToHarvest)
            {
                HarvestApples();
            }
        }
        else
        {
            Debug.Log($"[TreeHarvest] Tree hit but not in Apple state - animation plays, no harvest");
        }
    }

    void HarvestApples()
    {
        Debug.Log($"🍎 [TreeHarvest] Harvesting {applesPerHarvest} apples!");

        // Spawn táo
        if (applePrefab != null)
        {
            for (int i = 0; i < applesPerHarvest; i++)
            {
                SpawnApple();
            }
        }
        else
        {
            Debug.LogError("❌ [TreeHarvest] applePrefab is null! Assign apple prefab in Inspector.");
        }

        // Hiệu ứng
        if (harvestVFX != null)
        {
            Instantiate(harvestVFX, transform.position, Quaternion.identity);
        }

        if (harvestSound != null && audioSource != null)
        {
            audioSource.PlayOneShot(harvestSound);
        }

        if (treeStateCycle != null)
        {
            treeStateCycle.ForceState(TreeStateCycle.TreeState.Default);
        }

        currentHits = 0;

        if (hitAnimationObject != null)
        {
            hitAnimationObject.SetActive(false);
        }

        if (stopAnimationCoroutine != null)
        {
            StopCoroutine(stopAnimationCoroutine);
            stopAnimationCoroutine = null;
        }

        Debug.Log("🌳 [TreeHarvest] Tree reset to Default state");
    }

    private IEnumerator StopAnimationAfterDelay()
    {
        yield return new WaitForSeconds(animationStopDelay);

        if (hitAnimationObject != null)
        {
            hitAnimationObject.SetActive(false);
        }

        stopAnimationCoroutine = null;
    }


    void SpawnApple()
    {
        // Random vị trí rơi xung quanh cây
        Vector2 randomOffset = Random.insideUnitCircle * dropRadius;
        Vector3 spawnPosition = transform.position + (Vector3)dropOffset + (Vector3)randomOffset;

        GameObject apple = Instantiate(applePrefab, spawnPosition, Quaternion.identity);


        Debug.Log($"🍎 Spawned apple at {spawnPosition}");
    }

    // ✅ Reset hits (dùng khi cần reset thủ công)
    public void ResetHits()
    {
        currentHits = 0;
    }

    // Gizmos để visualize drop radius trong Editor
    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Vector3 center = transform.position + (Vector3)dropOffset;
        Gizmos.DrawWireSphere(center, dropRadius);
    }
}
