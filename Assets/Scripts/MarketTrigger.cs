using UnityEngine;
[RequireComponent(typeof(Collider2D))]
public class MarketTrigger : MonoBehaviour
{
    [Header("Market Objects")]
    [Tooltip("The market building sprite/object to hide when NPC enters")]
    [SerializeField] private GameObject marketBuilding;

    [Tooltip("The market sell UI/object to show when NPC enters")]
    [SerializeField] private GameObject marketSellUI;

    [Header("Settings")]
    [Tooltip("Only NPCs with NPCTrader component can trigger")]
    [SerializeField] private bool requireTraderComponent = true;

    [Tooltip("Layer mask for NPC detection")]
    [SerializeField] private LayerMask npcLayer;
    private GameObject currentNPCInMarket = null;
    private Collider2D triggerCollider;

    void Start()
    {
        triggerCollider = GetComponent<Collider2D>();
        if (triggerCollider == null)
        {
            triggerCollider = gameObject.AddComponent<BoxCollider2D>();
        }
        bool wasTrigger = triggerCollider.isTrigger;
        triggerCollider.isTrigger = true;
        if (marketBuilding == null)
        {
            SpriteRenderer spriteRenderer = GetComponentInChildren<SpriteRenderer>();
            if (spriteRenderer != null)
            {
                marketBuilding = spriteRenderer.gameObject;
            }
        }
        if (marketSellUI == null)
        {

            Transform marketSell = transform.Find("MarketSell");
            if (marketSell == null)
            {
                if (transform.parent != null)
                {
                    marketSell = transform.parent.Find("MarketSell");
                }
            }
            if (marketSell != null)
            {
                marketSellUI = marketSell.gameObject;
            }

        }
        if (marketSellUI != null)
        {
            bool wasActive = marketSellUI.activeSelf;
            marketSellUI.SetActive(false);
        }
        if (marketBuilding != null)
        {
            bool wasActive = marketBuilding.activeSelf;
            marketBuilding.SetActive(true);
        }


        Rigidbody2D rb = GetComponent<Rigidbody2D>();
        if (rb != null)
        {
            Debug.Log($"   - Has Rigidbody2D: BodyType={rb.bodyType}, Simulated={rb.simulated}");
        }
        else
        {
            rb = gameObject.AddComponent<Rigidbody2D>();
            rb.bodyType = RigidbodyType2D.Static;
            rb.gravityScale = 0;
        }
    }

    void Update()
    {
        Collider2D[] hits = Physics2D.OverlapBoxAll(transform.position, triggerCollider.bounds.size, 0f);

        foreach (var col in hits)
        {
            if (col.gameObject == gameObject) continue;

            if (IsNPC(col.gameObject))
            {
                if (currentNPCInMarket == null)
                {
                    OnTriggerEnter2D(col);
                }
            }
        }
    }


    void OnTriggerEnter2D(Collider2D other)
    {
        if (!IsNPC(other.gameObject))
            return;

        NPCTrader trader = other.GetComponent<NPCTrader>();
        if (trader == null)
        {
            Debug.Log($"🚫 [MarketTrigger] {other.name} entered but is NOT a trader");
            return;
        }

        if (!trader.IsMarketHours())
        {
            Debug.Log($"⏭ [MarketTrigger] {other.name} entered but market is CLOSED");
            return;
        }

        NPCRoutineAI ai = other.GetComponent<NPCRoutineAI>();
        if (ai != null)
        {
            ai.DisablePhysicsForMarket();
            Debug.Log($"🧤 Disabled Rigidbody2D for {other.name} while trading");
        }

        currentNPCInMarket = other.gameObject;

        if (marketBuilding != null)
            marketBuilding.SetActive(false);
        else
            Debug.LogWarning($"⚠️ [MarketTrigger] marketBuilding is NULL!");

        HideNPC(other.gameObject);

        if (marketSellUI != null)
            marketSellUI.SetActive(true);
        else
            Debug.LogWarning($"⚠️ [MarketTrigger] marketSellUI is NULL!");
    }


    void OnTriggerExit2D(Collider2D other)
    {

        if (other.gameObject != currentNPCInMarket)
        {
            return;
        }
        Rigidbody2D rb = other.GetComponent<Rigidbody2D>();
        if (rb != null)
        {
            NPCRoutineAI ai = other.GetComponent<NPCRoutineAI>();
            if (ai != null) ai.EnablePhysicsAfterMarket();
        }

        if (marketBuilding != null)
        {
            bool wasActive = marketBuilding.activeSelf;
            marketBuilding.SetActive(true);
            Debug.Log($"👁️ [MarketTrigger] Market Building: '{marketBuilding.name}' | Before: {wasActive} → After: {marketBuilding.activeSelf}");
        }
        else
        {
            Debug.LogWarning($"⚠️ [MarketTrigger] marketBuilding is NULL! Cannot show.");
        }

        if (marketSellUI != null)
        {
            bool wasActive = marketSellUI.activeSelf;
            marketSellUI.SetActive(false);
            Debug.Log($"🚫 [MarketTrigger] Market Sell UI: '{marketSellUI.name}' | Before: {wasActive} → After: {marketSellUI.activeSelf}");
        }
        else
        {
            Debug.LogWarning($"⚠️ [MarketTrigger] marketSellUI is NULL! Cannot hide.");
        }

        currentNPCInMarket = null;

    }

    bool IsNPC(GameObject obj)
    {
        if (obj.CompareTag("NPC"))
            return true;

        if (obj.GetComponent<NPC>() != null)
            return true;

        if (npcLayer != 0)
        {
            if (((1 << obj.layer) & npcLayer) != 0)
                return true;
        }

        return false;
    }


    public void HideNPC(GameObject npc)
    {
        if (npc == null) return;


        foreach (var r in npc.GetComponentsInChildren<SpriteRenderer>(true))
            r.enabled = false;

        Rigidbody2D rb = npc.GetComponent<Rigidbody2D>();
        if (rb != null)
        {
            rb.velocity = Vector2.zero;
            rb.angularVelocity = 0f;
        }
    }


    public void ShowNPC(GameObject npc)
    {
        if (npc == null) return;

        Debug.Log($"👁️ ShowNPC: {npc.name}");

        // 1. Re-enable sprite renderers
        foreach (var r in npc.GetComponentsInChildren<SpriteRenderer>(true))
            r.enabled = true;

        // 2. Re-enable physics
        Rigidbody2D rb = npc.GetComponent<Rigidbody2D>();
        if (rb != null)
        {
            rb.simulated = true;
            rb.constraints = RigidbodyConstraints2D.FreezeRotation;
        }

        // 3. Ensure AI scripts are active
        var ai = npc.GetComponent<NPCRoutineAI>();
        if (ai != null) ai.enabled = true;

        var trader = npc.GetComponent<NPCTrader>();
        if (trader != null) trader.enabled = true;

        Debug.Log("✅ NPC fully visible and AI restored.");
    }



    void OnDrawGizmosSelected()
    {
        Collider2D col = GetComponent<Collider2D>();
        if (col != null)
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireCube(transform.position, col.bounds.size);
        }
    }
    public void ResetTriggerState()
    {
        currentNPCInMarket = null;
    }
    public void ResetMarketToNormal()
    {
        Debug.Log("🔄 Reset Market to NORMAL stall");

        if (marketSellUI != null)
            marketSellUI.SetActive(false);
        Debug.Log("Tắt UI bán hàng");

        if (marketBuilding != null)
            marketBuilding.SetActive(true);
        Debug.Log("Bật lại stall bình thường");
        currentNPCInMarket = null;
    }


}
