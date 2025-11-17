using System.Collections;
using UnityEngine;

public class ItemMagnet : MonoBehaviour
{
    [Header("Magnet Settings")]
    [SerializeField] private float pickUpDistance = 5f;
    [SerializeField] private float accelerationRate = 0.2f;
    [SerializeField] private float initialMoveSpeed = 3f;

    [Header("Spawn Animation")]
    [SerializeField] private AnimationCurve spawnAnimCurve = AnimationCurve.EaseInOut(0, 0, 0, 0);
    [SerializeField] private float spawnHeight = 1.5f;
    [SerializeField] private float spawnDuration = 1f;
    [SerializeField] private bool useSpawnAnimation = false;

    private Vector3 moveDir;
    private Rigidbody2D rb;
    private float currentMoveSpeed;
    private bool canBeAttracted = false;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        currentMoveSpeed = 0f;
    }

    private void Start()
    {
        if (useSpawnAnimation)
        {
            StartCoroutine(SpawnAnimationRoutine());
        }
        else
        {
            canBeAttracted = true;
        }
    }

    private void Update()
    {
        if (!canBeAttracted || PlayerController.Instance == null) return;

        Vector3 playerPos = PlayerController.Instance.transform.position;
        float distanceToPlayer = Vector3.Distance(transform.position, playerPos);

        if (distanceToPlayer < pickUpDistance)
        {
            moveDir = (playerPos - transform.position).normalized;
            currentMoveSpeed += accelerationRate;
        }
        else
        {
            moveDir = Vector3.zero;
            currentMoveSpeed = initialMoveSpeed;
        }
    }

    private void FixedUpdate()
    {
        if (!canBeAttracted) return;

        rb.velocity = moveDir * currentMoveSpeed * Time.fixedDeltaTime;
    }

    private IEnumerator SpawnAnimationRoutine()
    {
        canBeAttracted = false;

        Vector2 startPoint = transform.position;
        float timePassed = 0f;

        while (timePassed < spawnDuration)
        {
            timePassed += Time.deltaTime;
            float linearT = timePassed / spawnDuration;
            float heightT = spawnAnimCurve.Evaluate(linearT);
            float height = Mathf.Lerp(0f, spawnHeight, heightT);

            transform.position = startPoint + new Vector2(0f, height);
            yield return null;
        }

        transform.position = startPoint;
        canBeAttracted = true;
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, pickUpDistance);
    }
}
