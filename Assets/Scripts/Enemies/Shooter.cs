using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Shooter : MonoBehaviour, IEnemy
{
    [SerializeField] private GameObject bulletPrefab;
    [SerializeField] private float bulletMoveSpeed;
    [SerializeField] private int burstCount;
    [SerializeField] private int projectilesPerBurst;
    [SerializeField][Range(0, 359)] private float angleSpread;
    [SerializeField] private float startingDistance = 0.1f;
    [SerializeField] private float timeBetweenBursts;
    [SerializeField] private float restTime = 1f;
    [SerializeField] private bool stagger;
    [Tooltip("Stragger myst be enabled for oscillate to function properly.")]
    [SerializeField] private bool oscillate;

    [Header("Audio Settings")]
    [SerializeField] private AudioClip shootSound; 

    private bool isShooting = false;
    private Animator myAnimator;

    readonly int ATTACK_HASH = Animator.StringToHash("Attack");

    private void OnValidate()
    {
        if (oscillate) { stagger = true; }
        if (!oscillate) { stagger = false; }
        if (projectilesPerBurst < 1) { projectilesPerBurst = 1; }
        if (burstCount < 1) { burstCount = 1; }
        if (timeBetweenBursts < 0.1f){ timeBetweenBursts = 0.1f;  }
        if (restTime < 0.1f) { restTime = 0.1f; }
        if (startingDistance < 0.1f) { startingDistance = 0.1f; }
        if (angleSpread == 0) { projectilesPerBurst = 1; }
        if (bulletMoveSpeed <= 0) { bulletMoveSpeed = 0.1f; }
    }
    private void Awake()
    {
        myAnimator = GetComponent<Animator>();
    }
    public void Attack()
    {
        if (!isShooting)
        {
            myAnimator.SetTrigger(ATTACK_HASH);
            StartCoroutine(ShotRoutine());
        }
    }
    private IEnumerator ShotRoutine()
    {
        isShooting = true;

        float startAngle, currentAngle, angleStep, endAngle;
        float timeBetweenProjectiles = 0f;

        TargetConeOfInfluence(out startAngle, out currentAngle, out angleStep, out endAngle);

        if(stagger) { timeBetweenProjectiles += timeBetweenBursts / projectilesPerBurst; }

        for (int i = 0; i < burstCount; i++)
        {
            if (!oscillate)
            {
                TargetConeOfInfluence(out startAngle, out currentAngle, out angleStep, out endAngle);

            }
            if(oscillate && i % 2 != 1)
            {
                TargetConeOfInfluence(out startAngle, out currentAngle, out angleStep, out endAngle);
            }else if (oscillate)
            {
                currentAngle = endAngle;
                endAngle = startAngle;
                startAngle = currentAngle;
                angleStep *= -1;
            }
            if (shootSound != null)
            {
                AudioSource.PlayClipAtPoint(shootSound, transform.position);
            }
            for (int j = 0; j < projectilesPerBurst; j++)
            {
                Vector2 pos = FindBulletSpawnPos(currentAngle);
                GameObject newBullet = Instantiate(bulletPrefab, pos, Quaternion.identity);
                newBullet.transform.right = newBullet.transform.position - transform.position;


                if (newBullet.TryGetComponent(out ProjectTile projectile))
                {
                    projectile.UpdateMoveSpeed(bulletMoveSpeed);
                }
                currentAngle += angleStep;

                if(stagger) { yield return new WaitForSeconds(timeBetweenProjectiles); }
            }
            currentAngle = startAngle;

            if(!stagger) { yield return new WaitForSeconds(timeBetweenBursts); }    
        }
        myAnimator.SetTrigger(ATTACK_HASH);
        yield return new WaitForSeconds(restTime);
        isShooting = false;
    }

    private void TargetConeOfInfluence(out float startAngle, out float currentAngle, out float angleStep, out float endAngle)
    {
        Vector2 targetDirection = PlayerController.Instance.transform.position - transform.position;
        float targleAngle = Mathf.Atan2(targetDirection.y, targetDirection.x) * Mathf.Rad2Deg;
        startAngle = targleAngle;
        endAngle = targleAngle;
        currentAngle = targleAngle;
        float halfAngleSpeed = 0f;
        angleStep = 0;
        if(angleSpread != 0)
        {
            angleStep = angleSpread / (projectilesPerBurst - 1);
            halfAngleSpeed = angleSpread / 2f;
            startAngle = targleAngle - halfAngleSpeed;
            endAngle = targleAngle + halfAngleSpeed;
            currentAngle = startAngle;
        }
    }
    private Vector2 FindBulletSpawnPos(float currentAngle)
    {
        float x = transform.position.x + startingDistance * Mathf.Cos(currentAngle * Mathf.Deg2Rad);
        float y = transform.position.y + startingDistance * Mathf.Sin(currentAngle * Mathf.Deg2Rad);

        Vector2 pos  = new Vector2(x, y);
        return pos;
    }
}
