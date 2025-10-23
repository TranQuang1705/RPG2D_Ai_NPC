using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PickUp : MonoBehaviour
{
    private enum PickUpType
    {
        GoldCoin,
        Stamina,
        Health,
    }
    [SerializeField] private PickUpType type;
    [SerializeField] private float pickUpDistance = 5f;
    [SerializeField] private float accelartionRate = .2f;
    [SerializeField] private float moveSpeed = 3f;
    [SerializeField] private AnimationCurve animCurve;
    [SerializeField] private float heighty = 1.5f;
    [SerializeField] private float popDuration = 1f;

    private Vector3 moveDir;
    private Rigidbody2D rb;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
    }
    private void Start()
    {
        StartCoroutine(AnimCurveSpawnRoutine());
    }

    private void Update()
    {
        Vector3 playerPos = PlayerController.Instance.transform.position;
        if(Vector3.Distance(transform.position, playerPos) < pickUpDistance )
        {
            moveDir = (playerPos - transform.position).normalized;
            moveSpeed += accelartionRate;
        }
        else
        {
            moveDir = Vector3.zero;
            moveSpeed = 0;
        }

    }
    private void FixedUpdate()
    {
        rb.velocity = moveDir * moveSpeed * Time.deltaTime;
    }
    private void OnTriggerStay2D(Collider2D other)
    {
        if (other.gameObject.GetComponent<PlayerController>())
        {
            DectectPickupType();
            Destroy(gameObject);
        }
    }

    private IEnumerator AnimCurveSpawnRoutine()
    {

        Vector2 startPoint = transform.position;
        float ramdomX = transform.position.x + Random.Range(-2f, 2f);
        float ramdomY = transform.position.y + Random.Range(-1f, 1f);
        Vector2 endPoint = new Vector2(ramdomX, ramdomY);
        float timePassed = 0f;
        while(timePassed < popDuration)
        {
            timePassed += Time.deltaTime;
            float linearT = timePassed / popDuration;
            float heightT = animCurve.Evaluate(linearT);
            float height = Mathf.Lerp(0f, heighty, heightT);
            transform.position = Vector2.Lerp(startPoint, endPoint, linearT) + new Vector2(0f, height);
            yield return null;
        }
    }
    private void DectectPickupType()
    {
        switch (type)
        {
            case PickUpType.GoldCoin:
                EconomyManagement.Instance.UpdateCurrentGold();
                
                break;

            case PickUpType.Stamina:
                Stamina.Instance.RefreshStamina();
                break;

            case PickUpType.Health:
                PlayerHealth.Instance.HealhPlayer();
                Debug.Log("Health picked up!");
                break;

            default:
                Debug.LogWarning("Unknown pickup type!");
                break;
        }
    }

}
