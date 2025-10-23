using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
public class BossHealth : MonoBehaviour
{
    [SerializeField] private GameObject targetEnemy; 
    [SerializeField] private Slider healthSlider;    
    [SerializeField] private int startingHealth = 3;
    [SerializeField] private GameObject deathVFXPrefab;
    [SerializeField] private float knockBackThrust = 15f;
    [SerializeField] private float delayBeforeSceneChange = 5f;
    [SerializeField] private GameObject bossNameObject;

    const string HEALTH_SLIDER_TEXT = "HealthSlider";

    private int currentHealth;
    private KnockBack knockBack;
    private Flash flash;


    private void Awake()
    {
        flash = GetComponent<Flash>();
        knockBack = GetComponent<KnockBack>();
        currentHealth = startingHealth;


        UpdateHealthSlider();
    }

    public void TakeDamage(int damage)
    {


        currentHealth -= damage;
        knockBack.GetKnockBack(PlayerController.Instance.transform, knockBackThrust);
        StartCoroutine(flash.FlashRoutine());
        StartCoroutine(CheckDetectDeathRoutine());
        UpdateHealthSlider();
    }

    private IEnumerator CheckDetectDeathRoutine()
    {
        yield return new WaitForSeconds(flash.GetRestoreMatTime());
        DetectDeath();
    }

    public void DetectDeath()
    {
        if (currentHealth <= 0)
        {
            Instantiate(deathVFXPrefab, transform.position, Quaternion.identity);
            GetComponent<PickUpSpawner>().DropItems();
            Destroy(targetEnemy);
            if (healthSlider != null)
            {
                Destroy(healthSlider.gameObject);
                Destroy(bossNameObject);
            }
            GameObject.FindObjectOfType<GameManager>().StartSceneSwitch();
        }
    }
    private void UpdateHealthSlider()
    {
        if (healthSlider == null)
        {
            healthSlider = GameObject.Find(HEALTH_SLIDER_TEXT).GetComponent<Slider>();
        }
        healthSlider.maxValue = startingHealth;
        healthSlider.value = currentHealth;
    }
}
