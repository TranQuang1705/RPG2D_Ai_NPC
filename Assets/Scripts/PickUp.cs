using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PickUp : MonoBehaviour
{
    private enum PickUpType
    {
        ObalCoin,      // 1 copper
        VarosCoin,     // 10 copper
        SylvCoin,      // 100 copper
        FeronCoin,     // 1000 copper
        AstrylCoin,    // 1000 copper (alternative)
        AurumCoin,     // 10000 copper
        EmberExp,      // 1 exp
        GroveExp,      // 10 exp
        TideExp,       // 100 exp
        VoidExp,       // 1000 exp
        RadiantExp,    // 10000 exp
        BloodmoonExp,  // 100000 exp
        Health,
    }
    [SerializeField] private PickUpType type;
    [SerializeField] private int currencyAmount = 1;
    [SerializeField] private int expAmount = 1;
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
            case PickUpType.ObalCoin:
                EconomyManagement.Instance.AddObal(currencyAmount);
                AddToCoinInventory("Obal", currencyAmount);
                break;

            case PickUpType.VarosCoin:
                EconomyManagement.Instance.AddVaros(currencyAmount);
                AddToCoinInventory("Varos", currencyAmount);
                break;

            case PickUpType.SylvCoin:
                EconomyManagement.Instance.AddSylv(currencyAmount);
                AddToCoinInventory("Sylv", currencyAmount);
                break;

            case PickUpType.FeronCoin:
                EconomyManagement.Instance.AddFeron(currencyAmount);
                AddToCoinInventory("Feron", currencyAmount);
                break;

            case PickUpType.AstrylCoin:
                // ⭐ ASTRYL: Đồng tiền phù thủy - chỉ những NPC/Player có tag "Wizard" mới nhặt được
                if (PlayerController.Instance != null && PlayerController.Instance.CompareTag("Wizard"))
                {
                    EconomyManagement.Instance.AddAstryl(currencyAmount);
                    AddToCoinInventory("Astryl", currencyAmount);
                    Debug.Log("✨ Wizard currency (ASTRYL) picked up!");
                }
                else
                {
                    Debug.LogWarning("⛔ ASTRYL coin requires Wizard tag! This coin is for wizards only.");
                    // Không destroy coin, để coin nằm đó (chỉ wizard mới nhặt được)
                    return; // Exit without destroying the coin
                }
                break;

            case PickUpType.AurumCoin:
                EconomyManagement.Instance.AddAurum(currencyAmount);
                AddToCoinInventory("Aurum", currencyAmount);
                break;

            case PickUpType.EmberExp:
                AddExp(expAmount);
                break;

            case PickUpType.GroveExp:
                AddExp(expAmount);
                break;

            case PickUpType.TideExp:
                AddExp(expAmount);
                break;

            case PickUpType.VoidExp:
                AddExp(expAmount);
                break;

            case PickUpType.RadiantExp:
                AddExp(expAmount);
                break;

            case PickUpType.BloodmoonExp:
                AddExp(expAmount);
                break;

            case PickUpType.Health:
                PlayerHealth.Instance.HealPlayer();
                Debug.Log("Health picked up!");
                break;

            default:
                Debug.LogWarning("Unknown pickup type!");
                break;
        }
    }
    
    private void AddToCoinInventory(string coinName, int amount)
    {
        // Thêm coin vào CoinInventorySystem (UI display)
        if (CoinInventorySystem.Instance != null)
        {
            // Lấy CoinSO từ DatabaseCoinLoader (runtime creation)
            CoinSO coinSO = null;
            
            if (DatabaseCoinLoader.Instance != null)
            {
                coinSO = DatabaseCoinLoader.Instance.GetCoinSOByName(coinName);
            }
            
            // Fallback: Load từ Resources nếu DatabaseCoinLoader chưa ready
            if (coinSO == null)
            {
                coinSO = Resources.Load<CoinSO>($"Coins/{coinName}");
            }
            
            if (coinSO != null)
            {
                CoinInventorySystem.Instance.AddCoin(coinSO, amount);
                
                // ⭐ Cập nhật thẳng vào database
                StartCoroutine(SaveCoinToDatabase(coinName, amount));
            }
            else
            {
                Debug.LogWarning($"⚠️ Could not find CoinSO for: {coinName}");
            }
        }
    }
    
    /// <summary>
    /// Save picked up coin to database immediately
    /// </summary>
    private IEnumerator SaveCoinToDatabase(string coinName, int amount)
    {
        if (DatabaseCoinLoader.Instance == null)
        {
            Debug.LogWarning("⚠️ DatabaseCoinLoader not found - coin not saved to database");
            yield break;
        }

        // Get coin_id from database
        int coinId = DatabaseCoinLoader.Instance.GetCoinIdByName(coinName);
        if (coinId <= 0)
        {
            Debug.LogError($"❌ Coin ID not found for: {coinName}");
            yield break;
        }

        // Get player ID (default: 1, hoặc lấy từ PlayerController nếu có)
        int playerId = 1; // TODO: Get from PlayerController or GameManager

        // Call API to add coins
        string apiUrl = "http://127.0.0.1:5002/player_coins/add";
        
        WWWForm form = new WWWForm();
        form.AddField("player_id", playerId);
        form.AddField("coin_id", coinId);
        form.AddField("amount", amount);

        using (UnityEngine.Networking.UnityWebRequest req = UnityEngine.Networking.UnityWebRequest.Post(apiUrl, form))
        {
            yield return req.SendWebRequest();

            if (req.result == UnityEngine.Networking.UnityWebRequest.Result.Success)
            {
                Debug.Log($"💾 Saved to database: {amount}x {coinName} for player {playerId}");
            }
            else
            {
                Debug.LogError($"❌ Failed to save coin to database: {req.error}");
            }
        }
    }
    
    public void SetCurrencyAmount(int amount)
    {
        currencyAmount = amount;
    }
    
    public void SetExpAmount(int amount)
    {
        expAmount = amount;
    }
    
    private void AddExp(int amount)
    {
        if (PlayerLevelSystem.Instance != null)
        {
            PlayerLevelSystem.Instance.AddExp(amount);
            Debug.Log($"💎 Picked up {amount} EXP!");
        }
        else
        {
            Debug.LogWarning("⚠️ PlayerLevelSystem not found!");
        }
    }

}
