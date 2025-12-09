using UnityEngine;

/// <summary>
/// Handles NPC market interaction - hides market building and NPC, shows market sell UI
/// Attach this to Market building with a Trigger Collider2D
/// </summary>
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

    // State tracking
    private GameObject currentNPCInMarket = null;
    private Collider2D triggerCollider;

    void Start()
    {
        Debug.Log($"🔧 [MarketTrigger] Start() called on {name}");

        // Get or add trigger collider
        triggerCollider = GetComponent<Collider2D>();
        if (triggerCollider == null)
        {
            triggerCollider = gameObject.AddComponent<BoxCollider2D>();
            Debug.LogWarning($"⚠️ [MarketTrigger] No Collider2D found on {name}, added BoxCollider2D");
        }
        else
        {
            Debug.Log($"✅ [MarketTrigger] Found Collider2D: {triggerCollider.GetType().Name} on {name}");
        }

        // Ensure it's a trigger
        bool wasTrigger = triggerCollider.isTrigger;
        triggerCollider.isTrigger = true;
        Debug.Log($"🔧 [MarketTrigger] Collider isTrigger: {wasTrigger} → {triggerCollider.isTrigger}");

        // Auto-find market building if not assigned (use this GameObject)
        Debug.Log($"🔍 [MarketTrigger] Looking for marketBuilding...");
        if (marketBuilding == null)
        {
            Debug.Log($"   marketBuilding not assigned in Inspector, searching...");

            // Look for child sprite renderer or use self
            SpriteRenderer spriteRenderer = GetComponentInChildren<SpriteRenderer>();
            if (spriteRenderer != null)
            {
                marketBuilding = spriteRenderer.gameObject;
                Debug.Log($"✅ [MarketTrigger] Auto-assigned marketBuilding: {marketBuilding.name} (found SpriteRenderer in children)");
            }
            else
            {
                Debug.LogWarning($"⚠️ [MarketTrigger] No market building assigned and no SpriteRenderer found in {name}!");
            }
        }
        else
        {
            Debug.Log($"✅ [MarketTrigger] marketBuilding already assigned: {marketBuilding.name}");
        }

        // Auto-find marketSellUI if not assigned
        Debug.Log($"🔍 [MarketTrigger] Looking for marketSellUI...");
        if (marketSellUI == null)
        {
            Debug.Log($"   marketSellUI not assigned in Inspector, searching...");

            // Try to find MarketSell child or sibling
            Transform marketSell = transform.Find("MarketSell");
            if (marketSell == null)
            {
                Debug.Log($"   'MarketSell' not found as child of {name}");

                // Look in parent
                if (transform.parent != null)
                {
                    Debug.Log($"   Searching in parent: {transform.parent.name}");
                    marketSell = transform.parent.Find("MarketSell");
                    if (marketSell != null)
                    {
                        Debug.Log($"   Found 'MarketSell' in parent!");
                    }
                }
                else
                {
                    Debug.Log($"   No parent to search in");
                }
            }
            else
            {
                Debug.Log($"   Found 'MarketSell' as child of {name}");
            }

            if (marketSell != null)
            {
                marketSellUI = marketSell.gameObject;
                Debug.Log($"✅ [MarketTrigger] Auto-assigned marketSellUI: {marketSellUI.name}");
            }
            else
            {
                Debug.LogWarning($"⚠️ [MarketTrigger] marketSellUI not found! Please assign it manually in Inspector.");
            }
        }
        else
        {
            Debug.Log($"✅ [MarketTrigger] marketSellUI already assigned: {marketSellUI.name}");
        }

        // Hide market sell UI initially
        Debug.Log($"🔧 [MarketTrigger] Setting initial states...");
        if (marketSellUI != null)
        {
            bool wasActive = marketSellUI.activeSelf;
            marketSellUI.SetActive(false);
            Debug.Log($"   marketSellUI '{marketSellUI.name}': {wasActive} → {marketSellUI.activeSelf}");
        }

        // Show market building initially
        if (marketBuilding != null)
        {
            bool wasActive = marketBuilding.activeSelf;
            marketBuilding.SetActive(true);
            Debug.Log($"   marketBuilding '{marketBuilding.name}': {wasActive} → {marketBuilding.activeSelf}");
        }

        Debug.Log($"🏪✅ [MarketTrigger] Initialization completed on {name}!");
        Debug.Log($"   - marketBuilding: {(marketBuilding != null ? marketBuilding.name : "NULL")}");
        Debug.Log($"   - marketSellUI: {(marketSellUI != null ? marketSellUI.name : "NULL")}");
        Debug.Log($"   - requireTraderComponent: {requireTraderComponent}");
        Debug.Log($"   - Collider isTrigger: {triggerCollider.isTrigger}");
        Debug.Log($"   - This GameObject Layer: {gameObject.layer}");
        Debug.Log($"   - This GameObject Tag: {gameObject.tag}");

        // Check for Rigidbody2D
        Rigidbody2D rb = GetComponent<Rigidbody2D>();
        if (rb != null)
        {
            Debug.Log($"   - Has Rigidbody2D: BodyType={rb.bodyType}, Simulated={rb.simulated}");
        }
        else
        {
            Debug.LogWarning($"⚠️⚠️⚠️ [MarketTrigger] NO RIGIDBODY2D! Adding Static Rigidbody2D to fix trigger detection...");

            // Auto-add Static Rigidbody2D
            rb = gameObject.AddComponent<Rigidbody2D>();
            rb.bodyType = RigidbodyType2D.Static; // Static = không di chuyển
            rb.gravityScale = 0;

            Debug.Log($"✅ [MarketTrigger] Added Static Rigidbody2D automatically!");
        }
    }

    void Update()
    {
        Collider2D[] hits = Physics2D.OverlapBoxAll(transform.position, triggerCollider.bounds.size, 0f);

        foreach (var col in hits)
        {
            if (col.gameObject == gameObject) continue; // bỏ chính nó

            if (IsNPC(col.gameObject))
            {
                if (currentNPCInMarket == null)
                {
                    Debug.Log($"⚠️ NPC đang đứng sẵn trong trigger → gọi OnTriggerEnter2D thủ công: {col.name}");
                    OnTriggerEnter2D(col);
                }
            }
        }
    }


    void OnTriggerEnter2D(Collider2D other)
    {
        Debug.Log($"🔔🔔🔔 [MarketTrigger] OnTriggerEnter2D CALLED! Object: {other.name}, Layer: {other.gameObject.layer}, Tag: {other.tag}");

        // Check if it's an NPC
        if (!IsNPC(other.gameObject))
        {
            Debug.Log($"❌ [MarketTrigger] {other.name} is NOT an NPC, ignoring");
            return;
        }
        Rigidbody2D rb = other.GetComponent<Rigidbody2D>();
        if (rb != null)
        {
            NPCRoutineAI ai = other.GetComponent<NPCRoutineAI>();
            if (ai != null) ai.DisablePhysicsForMarket();
            Debug.Log($"🧤 Disabled Rigidbody2D for {other.name} while trading");
        }
        Debug.Log($"✅ [MarketTrigger] {other.name} is an NPC!");

        // Check if NPC has trader component (optional)
        if (requireTraderComponent)
        {
            NPCTrader trader = other.GetComponent<NPCTrader>();
            if (trader == null)
            {
                Debug.Log($"🚫 [MarketTrigger] {other.name} entered but is NOT a trader (no NPCTrader component)");
                return;
            }
            Debug.Log($"✅ [MarketTrigger] {other.name} has NPCTrader component");
        }

        Debug.Log($"🏪 [MarketTrigger] NPC {other.name} entered market trigger!");

        // Store current NPC
        currentNPCInMarket = other.gameObject;
        Debug.Log($"📝 [MarketTrigger] Stored currentNPCInMarket: {currentNPCInMarket.name}");

        // Hide market building
        if (marketBuilding != null)
        {
            bool wasActive = marketBuilding.activeSelf;
            marketBuilding.SetActive(false);
            Debug.Log($"👻 [MarketTrigger] Market Building: '{marketBuilding.name}' | Before: {wasActive} → After: {marketBuilding.activeSelf}");
        }
        else
        {
            Debug.LogWarning($"⚠️ [MarketTrigger] marketBuilding is NULL! Cannot hide.");
        }

        // Hide NPC
        HideNPC(other.gameObject);

        // Show market sell UI
        if (marketSellUI != null)
        {
            bool wasActive = marketSellUI.activeSelf;
            marketSellUI.SetActive(true);
            Debug.Log($"🛒 [MarketTrigger] Market Sell UI: '{marketSellUI.name}' | Before: {wasActive} → After: {marketSellUI.activeSelf}");
        }
        else
        {
            Debug.LogWarning($"⚠️ [MarketTrigger] marketSellUI is NULL! Cannot show.");
        }

        Debug.Log($"✅ [MarketTrigger] OnTriggerEnter2D completed for {other.name}");
    }

    void OnTriggerExit2D(Collider2D other)
    {
        Debug.Log($"🔔 [MarketTrigger] OnTriggerExit2D called by: {other.name}");

        // Only process if this is the NPC that was in market
        if (other.gameObject != currentNPCInMarket)
        {
            Debug.Log($"❌ [MarketTrigger] {other.name} is NOT the current NPC in market (current: {(currentNPCInMarket != null ? currentNPCInMarket.name : "NULL")}), ignoring exit");
            return;
        }
        Rigidbody2D rb = other.GetComponent<Rigidbody2D>();
        if (rb != null)
        {
            NPCRoutineAI ai = other.GetComponent<NPCRoutineAI>();
            if (ai != null) ai.EnablePhysicsAfterMarket();
            Debug.Log($"💡 Re-enabled Rigidbody2D for {other.name} after leaving market");
        }
        Debug.Log($"👋 [MarketTrigger] NPC {other.name} left market trigger");

        // Show market building
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

        // Show NPC
        ShowNPC(other.gameObject);

        // Hide market sell UI
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
        Debug.Log($"📝 [MarketTrigger] Cleared currentNPCInMarket");

        Debug.Log($"✅ [MarketTrigger] OnTriggerExit2D completed for {other.name}");
    }

    /// <summary>
    /// Check if GameObject is an NPC
    /// </summary>
    bool IsNPC(GameObject obj)
    {
        // Method 1: Check tag
        if (obj.CompareTag("NPC"))
            return true;

        // Method 2: Check for NPC component
        if (obj.GetComponent<NPC>() != null)
            return true;

        // Method 3: Check layer
        if (npcLayer != 0)
        {
            if (((1 << obj.layer) & npcLayer) != 0)
                return true;
        }

        return false;
    }

    /// <summary>
    /// Hide NPC sprite and collider
    /// </summary>
    void HideNPC(GameObject npc)
    {
        if (npc == null)
        {
            Debug.LogWarning($"⚠️ [MarketTrigger] HideNPC called with NULL npc!");
            return;
        }

        Debug.Log($"👻 [MarketTrigger] Hiding NPC: {npc.name}");

        // Hide sprite renderers
        SpriteRenderer[] sprites = npc.GetComponentsInChildren<SpriteRenderer>();
        Debug.Log($"   Found {sprites.Length} SpriteRenderer(s) in {npc.name}");

        int hiddenCount = 0;
        foreach (var sprite in sprites)
        {
            bool wasEnabled = sprite.enabled;
            sprite.enabled = false;
            Debug.Log($"   - Sprite '{sprite.gameObject.name}': {wasEnabled} → {sprite.enabled}");
            if (wasEnabled) hiddenCount++;
        }
        Debug.Log($"   Total sprites hidden: {hiddenCount}/{sprites.Length}");

        // Optionally disable colliders to prevent interaction
        Collider2D[] colliders = npc.GetComponents<Collider2D>();
        Debug.Log($"   Found {colliders.Length} Collider2D(s) in {npc.name}");

        int disabledCount = 0;
        foreach (var col in colliders)
        {
            if (!col.isTrigger) // Keep triggers active
            {
                bool wasEnabled = col.enabled;
                col.enabled = false;
                Debug.Log($"   - Collider '{col.GetType().Name}': {wasEnabled} → {col.enabled}");
                if (wasEnabled) disabledCount++;
            }
            else
            {
                Debug.Log($"   - Collider '{col.GetType().Name}': SKIPPED (is trigger)");
            }
        }
        Debug.Log($"   Total colliders disabled: {disabledCount}/{colliders.Length}");

        Debug.Log($"✅ [MarketTrigger] NPC {npc.name} hidden successfully");
    }

    /// <summary>
    /// Show NPC sprite and collider
    /// </summary>
    void ShowNPC(GameObject npc)
    {
        if (npc == null)
        {
            Debug.LogWarning($"⚠️ [MarketTrigger] ShowNPC called with NULL npc!");
            return;
        }

        Debug.Log($"👁️ [MarketTrigger] Showing NPC: {npc.name}");

        // Show sprite renderers
        SpriteRenderer[] sprites = npc.GetComponentsInChildren<SpriteRenderer>();
        Debug.Log($"   Found {sprites.Length} SpriteRenderer(s) in {npc.name}");

        int shownCount = 0;
        foreach (var sprite in sprites)
        {
            bool wasEnabled = sprite.enabled;
            sprite.enabled = true;
            Debug.Log($"   - Sprite '{sprite.gameObject.name}': {wasEnabled} → {sprite.enabled}");
            if (!wasEnabled) shownCount++;
        }
        Debug.Log($"   Total sprites shown: {shownCount}/{sprites.Length}");

        // Re-enable colliders
        Collider2D[] colliders = npc.GetComponents<Collider2D>();
        Debug.Log($"   Found {colliders.Length} Collider2D(s) in {npc.name}");

        int enabledCount = 0;
        foreach (var col in colliders)
        {
            bool wasEnabled = col.enabled;
            col.enabled = true;
            Debug.Log($"   - Collider '{col.GetType().Name}': {wasEnabled} → {col.enabled}");
            if (!wasEnabled) enabledCount++;
        }
        Debug.Log($"   Total colliders enabled: {enabledCount}/{colliders.Length}");

        Debug.Log($"✅ [MarketTrigger] NPC {npc.name} shown successfully");
    }

    void OnDrawGizmosSelected()
    {
        // Draw trigger area
        Collider2D col = GetComponent<Collider2D>();
        if (col != null)
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireCube(transform.position, col.bounds.size);
        }
    }
}
