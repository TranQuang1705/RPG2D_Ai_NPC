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

    const string TOWN_TEXT = "Scene1";
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
            Destroy(ActiveWeapon.Instance.gameObject);
            currentHealth = 0;
            GetComponent<Animator>().SetTrigger(DEATH_HASH);
            StartCoroutine(DeathLoadSceneRoutine());
        }
    }
    private IEnumerator DeathLoadSceneRoutine()
    {
        yield return new WaitForSeconds(2f);
        Destroy(gameObject);
        SceneManager.LoadScene(TOWN_TEXT);
    }
    private IEnumerator DamageRecoveryRoutine()
    {
        yield return new WaitForSeconds(damageRecoveryTime);
        canTakeDamage = true;
    }
}
