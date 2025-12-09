using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class Stamina : Singleton<Stamina>
{
    public int CurrentStamina {  get; private set; }

    [SerializeField] private Sprite fullStaminaImage, emptyStaminaImage;
    [SerializeField] private float timeBetweenStaminaRefesh = 5f;
    [SerializeField] private float timeAfterUseToStartRegen = 5f;

    private Transform staminaContainer;
    private int startingStamina = 3;
    private int maxStamina;
    private float lastStaminaUseTime;
    private Coroutine regenCoroutine;
    const string STAMINA_COINTAINER_TEXT = "StaminaContainer";

    protected override void Awake()
    {
        base.Awake();
        maxStamina = startingStamina;
        CurrentStamina = startingStamina;
    }
    private void Start()
    {
        staminaContainer = GameObject.Find(STAMINA_COINTAINER_TEXT).transform;
        StartAutoRegen();
    }

    public void UseStamina()
    {
        CurrentStamina--;
        lastStaminaUseTime = Time.time;
        UpdateStaminaImages();
        
        // Restart auto-regen timer
        if (regenCoroutine != null)
        {
            StopCoroutine(regenCoroutine);
        }
        regenCoroutine = StartCoroutine(AutoRegenAfterDelay());
    }

    public void RefreshStamina()
    {
        if(CurrentStamina < maxStamina)
        {
            CurrentStamina++;
        }
        UpdateStaminaImages();
    }
    
    private void StartAutoRegen()
    {
        if (regenCoroutine != null)
        {
            StopCoroutine(regenCoroutine);
        }
        regenCoroutine = StartCoroutine(AutoRegenAfterDelay());
    }
    
    private IEnumerator AutoRegenAfterDelay()
    {
        // Wait for delay after last use
        yield return new WaitForSeconds(timeAfterUseToStartRegen);
        
        // Then continuously regenerate
        while(CurrentStamina < maxStamina)
        {
            yield return new WaitForSeconds(timeBetweenStaminaRefesh);
            RefreshStamina();
        }
    }
    
    private IEnumerator RefreshStaminaRoutine()
    {
        while(true)
        {
            yield return new WaitForSeconds(timeBetweenStaminaRefesh);
            RefreshStamina();
        }
    }
    private void UpdateStaminaImages()
    {
        for(int i = 0; i < maxStamina; i++)
        {
            if (i <= CurrentStamina - 1)
            {
                staminaContainer.GetChild(i).GetComponent<Image>().sprite = fullStaminaImage;
            }
            else
            {
                staminaContainer.GetChild(i).GetComponent<Image>().sprite = emptyStaminaImage;
            }
        }
        if(CurrentStamina < maxStamina)
        {
            StopAllCoroutines();
            StartCoroutine(RefreshStaminaRoutine());
        }
    }
}
