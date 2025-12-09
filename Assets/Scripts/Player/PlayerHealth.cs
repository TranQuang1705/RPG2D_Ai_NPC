using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
public class PlayerHealth : Singleton<PlayerHealth>
{

    public bool isDead {  get; private set; }


    [SerializeField] private int maxHealth = 10; // 10 HP = 5 hearts
    [SerializeField] private float knockBackThrustAmount = 10f;
    [SerializeField] private float damageRecoveryTime = 1f;
    [SerializeField] private HeartHealthUI heartHealthUI;

    private int currentHealth;
    private bool canTakeDamage = true;
    private KnockBack knockBack;
    private Flash flash;

    const string TOWN_TEXT = "MainGameScene";  // Changed to MainGameScene
    readonly int DEATH_HASH = Animator.StringToHash("Death");

    protected override void Awake()
    {
        base.Awake();
        flash = GetComponent<Flash>();
        knockBack = GetComponent<KnockBack>();
    }
    private void Start()
    {
        isDead = false;
        currentHealth = maxHealth;
        
        if (heartHealthUI != null)
        {
            heartHealthUI.InitHearts(maxHealth);
            heartHealthUI.UpdateHearts(currentHealth, maxHealth);
        }
        else
        {
            Debug.LogError("❌ PlayerHealth: HeartHealthUI not assigned!");
        }
    }
    private void OnCollisionStay2D(Collision2D other)
    {
        EnemyAI enemy = other.gameObject.GetComponent<EnemyAI>();
        if (enemy)
        {
            TakeDamage(1, other.transform);
        }
    }
    public void HealPlayer()
    {
        if(currentHealth < maxHealth)
        {
            currentHealth += 1;
            if (heartHealthUI != null)
            {
                heartHealthUI.UpdateHearts(currentHealth, maxHealth);
            }
            Debug.Log($"💚 Player healed: {currentHealth}/{maxHealth} HP");
        }
    }
    public void TakeDamage(int damageAmount, Transform hitTransform)
    {
        if(!canTakeDamage) { return; }
        ScreenShake.Instance.ShakeScreen();
        knockBack.GetKnockBack(hitTransform, knockBackThrustAmount);
        StartCoroutine(flash.FlashRoutine());
        canTakeDamage = false;
        currentHealth -= damageAmount;
        StartCoroutine(DamageRecoveryRoutine());
        
        if (heartHealthUI != null)
        {
            heartHealthUI.UpdateHearts(currentHealth, maxHealth);
        }
        
        Debug.Log($"💔 Player took {damageAmount} damage: {currentHealth}/{maxHealth} HP");
        CheckPlayerDeath();
    }
    private void CheckPlayerDeath()
    {
        if ((currentHealth <= 0 && !isDead)){
            isDead = true;
            
            // ✅ Only destroy current weapon, NOT the ActiveWeapon GameObject itself
            if (ActiveWeapon.Instance != null && ActiveWeapon.Instance.CurrentActiveWeapon != null)
            {
                Destroy(ActiveWeapon.Instance.CurrentActiveWeapon.gameObject);
                ActiveWeapon.Instance.WeaponNull(); // Clear reference
                Debug.Log("🗡️ Current weapon destroyed on death");
            }
            
            currentHealth = 0;
            GetComponent<Animator>().SetTrigger(DEATH_HASH);
            StartCoroutine(DeathLoadSceneRoutine());
        }
    }
    private IEnumerator DeathLoadSceneRoutine()
    {
        yield return new WaitForSeconds(2f);
        
        // Respawn at map center instead of reloading scene
        RespawnAtMapCenter();
        
        // ✅ Small delay to ensure animator is fully reset before re-equipping weapon
        yield return new WaitForSeconds(0.1f);
        
        // Re-enable player controls
        PlayerController playerController = GetComponent<PlayerController>();
        if (playerController != null)
        {
            // Player controller will handle movement again
            Debug.Log("✅ Player controls re-enabled");
        }
    }
    
    private void RespawnAtMapCenter()
    {
        // Find MapGenerator to get map center
        MapGenerator mapGen = FindObjectOfType<MapGenerator>();
        Vector3 spawnPos;
        
        if (mapGen != null)
        {
            // Calculate center of map
            float centerX = mapGen.width / 2f;
            float centerY = mapGen.height / 2f;
            spawnPos = new Vector3(centerX, centerY, 0f);
            Debug.Log($"📍 Respawning at map center: ({centerX}, {centerY})");
        }
        else
        {
            // Fallback to default center (80x48 map)
            spawnPos = new Vector3(40f, 24f, 0f);
            Debug.LogWarning("⚠️ MapGenerator not found! Using default center (40, 24)");
        }
        
        // Teleport player to spawn position
        transform.position = spawnPos;
        
        // Reset player state
        currentHealth = maxHealth;
        isDead = false;
        canTakeDamage = true;
        
        // Reset health UI
        if (heartHealthUI != null)
        {
            heartHealthUI.UpdateHearts(currentHealth, maxHealth);
        }
        
        // ✅ FIX ANIMATOR - Force exit death state
        Animator animator = GetComponent<Animator>();
        if (animator != null)
        {
            // Method 1: Rebind animator to reset all states
            animator.Rebind();
            animator.Update(0f);
            
            // Method 2: Force play Idle animation
            animator.Play("Idle", 0, 0f);
            
            // Reset movement parameters
            animator.SetFloat("moveX", 0f);
            animator.SetFloat("moveY", -1f); // Facing down
            
            Debug.Log("✅ Animator reset to Idle state");
        }
        
        // ✅ FIX WEAPON - Recreate weapon using ActiveInventory
        if (ActiveInventory.Instance != null && ActiveWeapon.Instance != null)
        {
            ActiveInventory.Instance.EquipStartingWeapon();
            Debug.Log("✅ Starting weapon re-equipped");
        }
        else
        {
            if (ActiveInventory.Instance == null)
                Debug.LogWarning("⚠️ ActiveInventory not found, weapon not re-equipped");
            if (ActiveWeapon.Instance == null)
                Debug.LogWarning("⚠️ ActiveWeapon not found, weapon not re-equipped");
        }
        
        // Reset velocity
        Rigidbody2D rb = GetComponent<Rigidbody2D>();
        if (rb != null)
        {
            rb.velocity = Vector2.zero;
        }
        
        Debug.Log($"✅ Player respawned at {spawnPos} with full health and weapon");
    }
    private IEnumerator DamageRecoveryRoutine()
    {
        yield return new WaitForSeconds(damageRecoveryTime);
        canTakeDamage = true;
    }
}
