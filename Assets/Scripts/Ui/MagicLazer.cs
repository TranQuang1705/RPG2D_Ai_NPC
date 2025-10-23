using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MagicLazer : MonoBehaviour
{
    [SerializeField] private float lazerGrowTime = 2f;

    private bool isGrowing = true;
    private float lazerRange;
    private SpriteRenderer spriteRenderer;
    private CapsuleCollider2D capsuleCollider2D;

    private void Awake()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
        capsuleCollider2D = GetComponent<CapsuleCollider2D>();
    }
    private void Start()
    {
        LazerFaceMouse();
    }
    private void OnTriggerEnter2D(Collider2D other)
    {
        if(other.gameObject.GetComponent<Indestructible>() && !other.isTrigger)
        {
            isGrowing = false;
        }
    }
    public void UpdateLazerRange(float lazerRange)
    {
        this.lazerRange = lazerRange;
        StartCoroutine(IncreaseLazerLengthRoutine());
    }
    private IEnumerator IncreaseLazerLengthRoutine()
    {
        float timePassed = 0f;
        while (spriteRenderer.size.x < lazerRange && isGrowing)
        {
            timePassed += Time.deltaTime;
            float linearT = timePassed / lazerGrowTime;

            spriteRenderer.size = new Vector2(Mathf.Lerp(1f, lazerRange, linearT), 1f);

            capsuleCollider2D.size = new Vector2(Mathf.Lerp(1f, lazerRange, linearT), capsuleCollider2D.size.y);
            capsuleCollider2D.offset = new Vector2((Mathf.Lerp(1f, lazerRange, linearT)) / 2, capsuleCollider2D.offset.y);

            yield return null;  

        }
        StartCoroutine(GetComponent<SpriteFade>().SlowFadeRoutine());
    }
    private void LazerFaceMouse()
    {
        Vector3 mousePosition = Input.mousePosition;
        mousePosition = Camera.main.ScreenToWorldPoint(mousePosition);
        Vector2 direction = transform.position - mousePosition;
        transform.right = -direction;
    }
}
