using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerController : Singleton<PlayerController>
{
    public bool FacingLeft { get { return facingLeft; } }

    [SerializeField] private float moveSpeed = 1f;
    [SerializeField] private float dashSpeed = 4f;
    [SerializeField] private TrailRenderer mytrailRenderer;
    [SerializeField] private Transform weaponCollider;
    [SerializeField] private AudioClip footstepSound;
    [SerializeField] private AudioClip dashSound;
    [SerializeField] private float footstepInterval = 0.4f; // thời gian giữa 2 bước chân
    private float footstepTimer = 0f;

    private PlayerControls playerControls;
    private Vector2 movement;
    private Rigidbody2D rb;
    private KnockBack knockBack;
    private Animator myAnimator;
    private SpriteRenderer mySpriteRenderer;
    private float startingMoveSpeed;
    private AudioSource audioSource;

    private bool facingLeft = false;
    private bool isDashing = false;
    private bool canDash = true;
    private bool canMove = true;
    protected override void Awake()
    {
        base.Awake();
        playerControls = new PlayerControls();
        rb = GetComponent<Rigidbody2D>();
        myAnimator = GetComponent<Animator>();
        mySpriteRenderer = GetComponent<SpriteRenderer>();
        knockBack = GetComponent<KnockBack>();

        audioSource = GetComponent<AudioSource>();
        if (audioSource == null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
        }
    }
    private void Start()
    {
        playerControls.Combat.Dash.performed += _ => Dash();

        startingMoveSpeed = moveSpeed;
        ActiveInventory.Instance.EquipStartingWeapon();
    }

    private void OnEnable()
    {
        playerControls.Enable();
    }
    private void OnDisable()
    {
        playerControls.Disable();
    }
    private void Update()
    {
        if (canMove) { PlayerInput(); }
    }
    private void FixedUpdate()
    {
        if (canMove)
        {
            AdjustPlayerFacingDirection();
            Move();
        }
    }
    public Transform GetWeaponCollider()
    {
        return weaponCollider;
    }
    private void PlayerInput()
    {
        movement = playerControls.Movement.Move.ReadValue<Vector2>();
        myAnimator.SetFloat("moveX", movement.x);
        myAnimator.SetFloat("moveY", movement.y);
    }
    private void Move()
    {
        if (knockBack.GettingKnockBack || PlayerHealth.Instance.isDead)
            return;

        // Nếu có chuyển động thực sự
        if (movement.sqrMagnitude > 0.01f)
        {
            footstepTimer -= Time.fixedDeltaTime;
            if (footstepTimer <= 0f)
            {
                PlayFootstepSound();
                footstepTimer = footstepInterval; // reset timer
            }

            rb.MovePosition(rb.position + movement * (moveSpeed * Time.fixedDeltaTime));
        }
        else
        {
            // Không di chuyển → reset bộ đếm để phát lại khi di chuyển tiếp
            footstepTimer = 0f;
            audioSource.Stop();
        }
    }

    private void PlayFootstepSound()
    {
        if (footstepSound != null)
        {
            audioSource.PlayOneShot(footstepSound);
        }
    }

    private void AdjustPlayerFacingDirection()
    {
        Vector3 mousePos = Input.mousePosition;
        Vector3 playerScreenPoint = Camera.main.WorldToScreenPoint(transform.position);

        if (mousePos.x < playerScreenPoint.x)
        {
            mySpriteRenderer.flipX = true;
            facingLeft = true;
        }
        else
        {
            mySpriteRenderer.flipX = false;
            facingLeft = false;
        }
    }
    private void Dash()
    {
        if (!isDashing && Stamina.Instance.CurrentStamina > 0)
        {
            Stamina.Instance.UseStamina();
            isDashing = true;
            moveSpeed *= dashSpeed;
            mytrailRenderer.emitting = true;
            PlayDashSound();
            StartCoroutine(EndDashRoutine());
        }
    }
    private IEnumerator EndDashRoutine()
    {
        float dashTime = .2f;
        float dashCD = .25f;
        yield return new WaitForSeconds(dashTime);
        moveSpeed = startingMoveSpeed;
        mytrailRenderer.emitting = false;
        yield return new WaitForSeconds(dashCD);
        isDashing = false;
    }
    public void SetCanMove(bool value)
    {
        canMove = value;
    }

    public void SetCanDash(bool value)
    {
        canDash = value;
    }
    private void PlayDashSound()
    {
        if (dashSound != null)
        {
            audioSource.PlayOneShot(dashSound);
        }
    }
}
