using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;

public class PickUpSpawner : MonoBehaviour
{
    [Header("Pickup Prefabs")]
    [SerializeField] private GameObject obalCoin;      // 1
    [SerializeField] private GameObject varosCoin;     // 10
    [SerializeField] private GameObject sylvCoin;      // 100
    [SerializeField] private GameObject feronCoin;     // 1000
    [SerializeField] private GameObject astrylCoin;    // 1000 (alternative)
    [SerializeField] private GameObject aurumCoin;     // 10000
    [SerializeField] private GameObject emberExp;      // 1 exp
    [SerializeField] private GameObject groveExp;      // 10 exp
    [SerializeField] private GameObject tideExp;       // 100 exp
    [SerializeField] private GameObject voidExp;       // 1000 exp
    [SerializeField] private GameObject radiantExp;    // 10000 exp
    [SerializeField] private GameObject bloodmoonExp;  // 100000 exp
    [SerializeField] private GameObject heath;

    [Header("Currency Drop Settings")]
    [SerializeField] private bool enableCurrencyDrop = true;
    [Tooltip("Giá trị OBAL tối thiểu khi rơi tiền")]
    [SerializeField] private int minObalValue = 1;
    [Tooltip("Giá trị OBAL tối đa khi rơi tiền. Ví dụ: 159 = 1 Sylv + 5 Varos + 9 Obal")]
    [SerializeField] private int maxObalValue = 200;
    
    [Header("Coin Type Restrictions")]
    [Tooltip("Cho phép rơi AURUM (cần giá trị >= 10000)")]
    [SerializeField] private bool allowAurumDrop = false;
    [Tooltip("Cho phép rơi FERON (cần giá trị >= 1000)")]
    [SerializeField] private bool allowFeronDrop = false;
    [Tooltip("⭐ ASTRYL: Đồng phù thủy - CHỈ drop trong Wizard Biome")]
    [SerializeField] private bool useAstrylInsteadOfFeron = false;
    [Tooltip("Cho phép rơi SYLV (cần giá trị >= 100)")]
    [SerializeField] private bool allowSylvDrop = true;
    [Tooltip("Cho phép rơi VAROS (cần giá trị >= 10)")]
    [SerializeField] private bool allowVarosDrop = true;
    [Tooltip("Cho phép rơi OBAL")]
    [SerializeField] private bool allowObalDrop = true;
    
    [Header("Special Zone Settings")]
    [Tooltip("⚠️ LOCK: Chỉ cho phép ASTRYL drop khi ở trong Wizard Biome. Hiện tại KHÓA cho đến khi có Wizard Biome.")]
    [SerializeField] private bool isInWizardBiome = false;
    [Tooltip("Tag của Wizard Biome để tự động detect (implement sau)")]
    [SerializeField] private string wizardBiomeTag = "WizardBiome";

    [Header("EXP Drop Settings")]
    [SerializeField] private bool enableExpDrop = true;
    [Tooltip("Giá trị EXP tối thiểu khi rơi")]
    [SerializeField] private int minExpValue = 1;
    [Tooltip("Giá trị EXP tối đa khi rơi. Ví dụ: 159 = 1 Tide + 5 Grove + 9 Ember")]
    [SerializeField] private int maxExpValue = 200;
    
    [Header("EXP Type Restrictions")]
    [Tooltip("Cho phép rơi BLOODMOON (cần giá trị >= 100000)")]
    [SerializeField] private bool allowBloodmoonDrop = false;
    [Tooltip("Cho phép rơi RADIANT (cần giá trị >= 10000)")]
    [SerializeField] private bool allowRadiantDrop = false;
    [Tooltip("Cho phép rơi VOID (cần giá trị >= 1000)")]
    [SerializeField] private bool allowVoidDrop = false;
    [Tooltip("Cho phép rơi TIDE (cần giá trị >= 100)")]
    [SerializeField] private bool allowTideDrop = true;
    [Tooltip("Cho phép rơi GROVE (cần giá trị >= 10)")]
    [SerializeField] private bool allowGroveDrop = true;
    [Tooltip("Cho phép rơi EMBER")]
    [SerializeField] private bool allowEmberDrop = true;
    
    [Header("Other Items Drop Settings")]
    [SerializeField] private bool enableHealthDrop = true;

    private void Start()
    {
        // TODO: Auto-detect Wizard Biome when system is ready
        // CheckIfInWizardBiome();
    }

    public void DropItems()
    {
        List<int> availableOptions = new List<int>();
        
        if (enableHealthDrop) availableOptions.Add(1);
        if (enableExpDrop) availableOptions.Add(2);
        if (enableCurrencyDrop) availableOptions.Add(3);
        
        if (availableOptions.Count == 0) return;
        
        int randomIndex = Random.Range(0, availableOptions.Count);
        int selectedOption = availableOptions[randomIndex];

        if(selectedOption == 1)
        {
            Instantiate(heath, transform.position, Quaternion.identity);
        }
        else if(selectedOption == 2)
        {
            DropExp();
        }
        else if (selectedOption == 3)
        {
            DropCurrency();
        }
    }
    
    // ⭐ WIZARD BIOME DETECTION - Implement khi có Biome system
    private void CheckIfInWizardBiome()
    {
        // TODO: Implement biome detection
        // Ví dụ: Check collider với tag "WizardBiome"
        // Hoặc check position với WizardBiomeManager
        
        Collider2D[] colliders = Physics2D.OverlapCircleAll(transform.position, 0.5f);
        foreach (var col in colliders)
        {
            if (col.CompareTag(wizardBiomeTag))
            {
                isInWizardBiome = true;
                Debug.Log($"✨ {gameObject.name} detected Wizard Biome! ASTRYL drop enabled.");
                return;
            }
        }
        isInWizardBiome = false;
    }
    
    // Public method để enable/disable Wizard Biome từ bên ngoài
    public void SetWizardBiomeMode(bool enabled)
    {
        isInWizardBiome = enabled;
        Debug.Log($"🧙 {gameObject.name}: Wizard Biome mode = {enabled}");
    }

    private void DropCurrency()
    {
        // Random total OBAL value
        int totalObalValue = Random.Range(minObalValue, maxObalValue + 1);
        
        // Currency conversion constants
        const int OBAL_PER_AURUM = 10000;
        const int OBAL_PER_FERON = 1000;
        const int OBAL_PER_ASTRYL = 1000;
        const int OBAL_PER_SYLV = 100;
        const int OBAL_PER_VAROS = 10;
        
        // Calculate how many of each coin type
        int aurumCount = totalObalValue / OBAL_PER_AURUM;
        int remainingAfterAurum = totalObalValue % OBAL_PER_AURUM;
        
        int feronAstrylCount = remainingAfterAurum / OBAL_PER_FERON;
        int remainingAfterFeron = remainingAfterAurum % OBAL_PER_FERON;
        
        int sylvCount = remainingAfterFeron / OBAL_PER_SYLV;
        int remainingAfterSylv = remainingAfterFeron % OBAL_PER_SYLV;
        
        int varosCount = remainingAfterSylv / OBAL_PER_VAROS;
        int obalCount = remainingAfterSylv % OBAL_PER_VAROS;
        
        Debug.Log($"🎲 {gameObject.name} rolling {totalObalValue} OBAL (Range: {minObalValue}-{maxObalValue}) | Calculated: {aurumCount}Au {feronAstrylCount}Fe {sylvCount}Sy {varosCount}Va {obalCount}Ob");
        
        // Apply restrictions and convert disallowed coins to lower denominations
        if (!allowAurumDrop && aurumCount > 0)
        {
            remainingAfterAurum += aurumCount * OBAL_PER_AURUM;
            feronAstrylCount = remainingAfterAurum / OBAL_PER_FERON;
            remainingAfterFeron = remainingAfterAurum % OBAL_PER_FERON;
            sylvCount = remainingAfterFeron / OBAL_PER_SYLV;
            remainingAfterSylv = remainingAfterFeron % OBAL_PER_SYLV;
            varosCount = remainingAfterSylv / OBAL_PER_VAROS;
            obalCount = remainingAfterSylv % OBAL_PER_VAROS;
            aurumCount = 0;
        }
        
        if (!allowFeronDrop && feronAstrylCount > 0)
        {
            remainingAfterFeron += feronAstrylCount * OBAL_PER_FERON;
            sylvCount = remainingAfterFeron / OBAL_PER_SYLV;
            remainingAfterSylv = remainingAfterFeron % OBAL_PER_SYLV;
            varosCount = remainingAfterSylv / OBAL_PER_VAROS;
            obalCount = remainingAfterSylv % OBAL_PER_VAROS;
            feronAstrylCount = 0;
        }
        
        if (!allowSylvDrop && sylvCount > 0)
        {
            remainingAfterSylv += sylvCount * OBAL_PER_SYLV;
            varosCount = remainingAfterSylv / OBAL_PER_VAROS;
            obalCount = remainingAfterSylv % OBAL_PER_VAROS;
            sylvCount = 0;
        }
        
        if (!allowVarosDrop && varosCount > 0)
        {
            obalCount += varosCount * OBAL_PER_VAROS;
            varosCount = 0;
        }
        
        if (!allowObalDrop && obalCount > 0)
        {
            obalCount = 0;
            Debug.LogWarning("⚠️ OBAL coins disabled but value requires OBAL coins. Remaining value lost.");
        }
        
        // Spawn AURUM coins
        if (aurumCount > 0 && aurumCoin != null && allowAurumDrop)
        {
            for (int i = 0; i < aurumCount; i++)
            {
                GameObject coin = Instantiate(aurumCoin, transform.position, Quaternion.identity);
                PickUp pickupComponent = coin.GetComponent<PickUp>();
                if (pickupComponent != null)
                {
                    pickupComponent.SetCurrencyAmount(1);
                }
            }
        }
        
        // Spawn FERON or ASTRYL coins
        if (feronAstrylCount > 0 && allowFeronDrop)
        {
            // ⭐ ASTRYL CHỈ drop trong Wizard Biome
            bool canDropAstryl = useAstrylInsteadOfFeron && isInWizardBiome;
            
            if (useAstrylInsteadOfFeron && !isInWizardBiome)
            {
                Debug.LogWarning($"⛔ {gameObject.name}: ASTRYL drop bị chặn! Cần isInWizardBiome = true. Đổi sang FERON.");
                canDropAstryl = false; // Force drop FERON instead
            }
            
            GameObject coinPrefab = canDropAstryl ? astrylCoin : feronCoin;
            if (coinPrefab != null)
            {
                for (int i = 0; i < feronAstrylCount; i++)
                {
                    GameObject coin = Instantiate(coinPrefab, transform.position, Quaternion.identity);
                    PickUp pickupComponent = coin.GetComponent<PickUp>();
                    if (pickupComponent != null)
                    {
                        pickupComponent.SetCurrencyAmount(1);
                    }
                }
            }
        }
        
        // Spawn SYLV coins
        if (sylvCount > 0 && sylvCoin != null && allowSylvDrop)
        {
            for (int i = 0; i < sylvCount; i++)
            {
                GameObject coin = Instantiate(sylvCoin, transform.position, Quaternion.identity);
                PickUp pickupComponent = coin.GetComponent<PickUp>();
                if (pickupComponent != null)
                {
                    pickupComponent.SetCurrencyAmount(1);
                }
            }
        }
        
        // Spawn VAROS coins
        if (varosCount > 0 && varosCoin != null && allowVarosDrop)
        {
            for (int i = 0; i < varosCount; i++)
            {
                GameObject coin = Instantiate(varosCoin, transform.position, Quaternion.identity);
                PickUp pickupComponent = coin.GetComponent<PickUp>();
                if (pickupComponent != null)
                {
                    pickupComponent.SetCurrencyAmount(1);
                }
            }
        }
        
        // Spawn OBAL coins
        if (obalCount > 0 && obalCoin != null && allowObalDrop)
        {
            for (int i = 0; i < obalCount; i++)
            {
                GameObject coin = Instantiate(obalCoin, transform.position, Quaternion.identity);
                PickUp pickupComponent = coin.GetComponent<PickUp>();
                if (pickupComponent != null)
                {
                    pickupComponent.SetCurrencyAmount(1);
                }
            }
        }
        
        // Count what was actually spawned
        int actualAurum = (aurumCount > 0 && allowAurumDrop && aurumCoin != null) ? aurumCount : 0;
        int actualFeronAstryl = (feronAstrylCount > 0 && allowFeronDrop && (feronCoin != null || astrylCoin != null)) ? feronAstrylCount : 0;
        int actualSylv = (sylvCount > 0 && allowSylvDrop && sylvCoin != null) ? sylvCount : 0;
        int actualVaros = (varosCount > 0 && allowVarosDrop && varosCoin != null) ? varosCount : 0;
        int actualObal = (obalCount > 0 && allowObalDrop && obalCoin != null) ? obalCount : 0;
        
        Debug.Log($"💰 {gameObject.name} SPAWNED: {actualAurum}Au {actualFeronAstryl}Fe {actualSylv}Sy {actualVaros}Va {actualObal}Ob | Total: {totalObalValue} OBAL");
    }
    
    private void DropExp()
    {
        // Random total EXP value
        int totalExpValue = Random.Range(minExpValue, maxExpValue + 1);
        
        // EXP conversion constants
        const int EXP_PER_BLOODMOON = 100000;
        const int EXP_PER_RADIANT = 10000;
        const int EXP_PER_VOID = 1000;
        const int EXP_PER_TIDE = 100;
        const int EXP_PER_GROVE = 10;
        
        // Calculate how many of each exp type
        int bloodmoonCount = totalExpValue / EXP_PER_BLOODMOON;
        int remainingAfterBloodmoon = totalExpValue % EXP_PER_BLOODMOON;
        
        int radiantCount = remainingAfterBloodmoon / EXP_PER_RADIANT;
        int remainingAfterRadiant = remainingAfterBloodmoon % EXP_PER_RADIANT;
        
        int voidCount = remainingAfterRadiant / EXP_PER_VOID;
        int remainingAfterVoid = remainingAfterRadiant % EXP_PER_VOID;
        
        int tideCount = remainingAfterVoid / EXP_PER_TIDE;
        int remainingAfterTide = remainingAfterVoid % EXP_PER_TIDE;
        
        int groveCount = remainingAfterTide / EXP_PER_GROVE;
        int emberCount = remainingAfterTide % EXP_PER_GROVE;
        
        Debug.Log($"🎲 {gameObject.name} rolling {totalExpValue} EXP (Range: {minExpValue}-{maxExpValue}) | Calculated: {bloodmoonCount}Bl {radiantCount}Ra {voidCount}Vo {tideCount}Ti {groveCount}Gr {emberCount}Em");
        
        // Apply restrictions and convert disallowed exp to lower denominations
        if (!allowBloodmoonDrop && bloodmoonCount > 0)
        {
            remainingAfterBloodmoon += bloodmoonCount * EXP_PER_BLOODMOON;
            radiantCount = remainingAfterBloodmoon / EXP_PER_RADIANT;
            remainingAfterRadiant = remainingAfterBloodmoon % EXP_PER_RADIANT;
            voidCount = remainingAfterRadiant / EXP_PER_VOID;
            remainingAfterVoid = remainingAfterRadiant % EXP_PER_VOID;
            tideCount = remainingAfterVoid / EXP_PER_TIDE;
            remainingAfterTide = remainingAfterVoid % EXP_PER_TIDE;
            groveCount = remainingAfterTide / EXP_PER_GROVE;
            emberCount = remainingAfterTide % EXP_PER_GROVE;
            bloodmoonCount = 0;
        }
        
        if (!allowRadiantDrop && radiantCount > 0)
        {
            remainingAfterRadiant += radiantCount * EXP_PER_RADIANT;
            voidCount = remainingAfterRadiant / EXP_PER_VOID;
            remainingAfterVoid = remainingAfterRadiant % EXP_PER_VOID;
            tideCount = remainingAfterVoid / EXP_PER_TIDE;
            remainingAfterTide = remainingAfterVoid % EXP_PER_TIDE;
            groveCount = remainingAfterTide / EXP_PER_GROVE;
            emberCount = remainingAfterTide % EXP_PER_GROVE;
            radiantCount = 0;
        }
        
        if (!allowVoidDrop && voidCount > 0)
        {
            remainingAfterVoid += voidCount * EXP_PER_VOID;
            tideCount = remainingAfterVoid / EXP_PER_TIDE;
            remainingAfterTide = remainingAfterVoid % EXP_PER_TIDE;
            groveCount = remainingAfterTide / EXP_PER_GROVE;
            emberCount = remainingAfterTide % EXP_PER_GROVE;
            voidCount = 0;
        }
        
        if (!allowTideDrop && tideCount > 0)
        {
            remainingAfterTide += tideCount * EXP_PER_TIDE;
            groveCount = remainingAfterTide / EXP_PER_GROVE;
            emberCount = remainingAfterTide % EXP_PER_GROVE;
            tideCount = 0;
        }
        
        if (!allowGroveDrop && groveCount > 0)
        {
            emberCount += groveCount * EXP_PER_GROVE;
            groveCount = 0;
        }
        
        if (!allowEmberDrop && emberCount > 0)
        {
            emberCount = 0;
            Debug.LogWarning("⚠️ EMBER exp disabled but value requires EMBER exp. Remaining value lost.");
        }
        
        // Spawn BLOODMOON exp
        if (bloodmoonCount > 0 && bloodmoonExp != null && allowBloodmoonDrop)
        {
            for (int i = 0; i < bloodmoonCount; i++)
            {
                GameObject exp = Instantiate(bloodmoonExp, transform.position, Quaternion.identity);
                PickUp pickupComponent = exp.GetComponent<PickUp>();
                if (pickupComponent != null)
                {
                    pickupComponent.SetExpAmount(EXP_PER_BLOODMOON);
                }
            }
        }
        
        // Spawn RADIANT exp
        if (radiantCount > 0 && radiantExp != null && allowRadiantDrop)
        {
            for (int i = 0; i < radiantCount; i++)
            {
                GameObject exp = Instantiate(radiantExp, transform.position, Quaternion.identity);
                PickUp pickupComponent = exp.GetComponent<PickUp>();
                if (pickupComponent != null)
                {
                    pickupComponent.SetExpAmount(EXP_PER_RADIANT);
                }
            }
        }
        
        // Spawn VOID exp
        if (voidCount > 0 && voidExp != null && allowVoidDrop)
        {
            for (int i = 0; i < voidCount; i++)
            {
                GameObject exp = Instantiate(voidExp, transform.position, Quaternion.identity);
                PickUp pickupComponent = exp.GetComponent<PickUp>();
                if (pickupComponent != null)
                {
                    pickupComponent.SetExpAmount(EXP_PER_VOID);
                }
            }
        }
        
        // Spawn TIDE exp
        if (tideCount > 0 && tideExp != null && allowTideDrop)
        {
            for (int i = 0; i < tideCount; i++)
            {
                GameObject exp = Instantiate(tideExp, transform.position, Quaternion.identity);
                PickUp pickupComponent = exp.GetComponent<PickUp>();
                if (pickupComponent != null)
                {
                    pickupComponent.SetExpAmount(EXP_PER_TIDE);
                }
            }
        }
        
        // Spawn GROVE exp
        if (groveCount > 0 && groveExp != null && allowGroveDrop)
        {
            for (int i = 0; i < groveCount; i++)
            {
                GameObject exp = Instantiate(groveExp, transform.position, Quaternion.identity);
                PickUp pickupComponent = exp.GetComponent<PickUp>();
                if (pickupComponent != null)
                {
                    pickupComponent.SetExpAmount(EXP_PER_GROVE);
                }
            }
        }
        
        // Spawn EMBER exp
        if (emberCount > 0 && emberExp != null && allowEmberDrop)
        {
            for (int i = 0; i < emberCount; i++)
            {
                GameObject exp = Instantiate(emberExp, transform.position, Quaternion.identity);
                PickUp pickupComponent = exp.GetComponent<PickUp>();
                if (pickupComponent != null)
                {
                    pickupComponent.SetExpAmount(1);
                }
            }
        }
        
        // Count what was actually spawned
        int actualBloodmoon = (bloodmoonCount > 0 && allowBloodmoonDrop && bloodmoonExp != null) ? bloodmoonCount : 0;
        int actualRadiant = (radiantCount > 0 && allowRadiantDrop && radiantExp != null) ? radiantCount : 0;
        int actualVoid = (voidCount > 0 && allowVoidDrop && voidExp != null) ? voidCount : 0;
        int actualTide = (tideCount > 0 && allowTideDrop && tideExp != null) ? tideCount : 0;
        int actualGrove = (groveCount > 0 && allowGroveDrop && groveExp != null) ? groveCount : 0;
        int actualEmber = (emberCount > 0 && allowEmberDrop && emberExp != null) ? emberCount : 0;
        
        Debug.Log($"💎 {gameObject.name} SPAWNED EXP: {actualBloodmoon}Bl {actualRadiant}Ra {actualVoid}Vo {actualTide}Ti {actualGrove}Gr {actualEmber}Em | Total: {totalExpValue} EXP");
    }
}
