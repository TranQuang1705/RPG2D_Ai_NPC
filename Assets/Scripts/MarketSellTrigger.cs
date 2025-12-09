using UnityEngine;

/// <summary>
/// Triggers Trade UI when player approaches MarketSell
/// Attach to MarketSell object with a Trigger Collider2D
/// </summary>
[RequireComponent(typeof(Collider2D))]
public class MarketSellTrigger : MonoBehaviour
{
    [Header("UI References")]
    [Tooltip("The Trade Panel UI to show when player enters")]
    [SerializeField] private GameObject tradePanelUI;

    [Header("Market Info")]
    [Tooltip("Reference to the NPC trader at this market")]
    [SerializeField] private NPCTrader npcTrader;

    [Header("Settings")]
    [Tooltip("Auto-find Trade Panel by name if not assigned")]
    [SerializeField] private string[] tradePanelNames = { "TradeUI", "ItemTradePanel" };

    [Tooltip("Distance to show UI prompt")]
    [SerializeField] private float interactionDistance = 2f;

    [Tooltip("Show visual indicator when player is near")]
    [SerializeField] private bool showIndicator = true;

    [Header("Visual Feedback")]
    [SerializeField] private GameObject interactionIndicator;
    [SerializeField] private Sprite interactionIcon;
    [SerializeField] private float indicatorOffset = 1f;

    // State
    private bool playerInRange = false;
    private GameObject player;
    private Collider2D triggerCollider;
    private SpriteRenderer indicatorRenderer;

    void Start()
    {

        // Setup trigger collider
        triggerCollider = GetComponent<Collider2D>();
        if (triggerCollider == null)
        {
            triggerCollider = gameObject.AddComponent<BoxCollider2D>();
        }

        triggerCollider.isTrigger = true;

        // Add Rigidbody2D if missing (required for trigger detection)
        Rigidbody2D rb = GetComponent<Rigidbody2D>();
        if (rb == null)
        {
            rb = gameObject.AddComponent<Rigidbody2D>();
            rb.bodyType = RigidbodyType2D.Static;
            rb.gravityScale = 0;
        }

        // Auto-find Trade Panel UI if not assigned
        if (tradePanelUI == null)
        {
            
            // Method 1: Find by tag "TradeUI"
            try
            {
                GameObject taggedUI = GameObject.FindWithTag("TradeUI");
                if (taggedUI != null)
                {
                    tradePanelUI = taggedUI;

                }
            }
            catch
            {
                Debug.Log($"   Tag 'TradeUI' does not exist in project.");
            }
            
            if (tradePanelUI == null)
            {
                
                // Method 2: Find by name in scene (NOT prefab)
                foreach (string panelName in tradePanelNames)
                {
                    tradePanelUI = GameObject.Find(panelName);
                    if (tradePanelUI != null)
                    {

                        break;
                    }
                }
                
                if (tradePanelUI == null)
                {
                    // Method 3: Find in Canvas
                    Canvas[] canvases = FindObjectsOfType<Canvas>();

                    
                    foreach (var canvas in canvases)
                    {
                        
                        foreach (string panelName in tradePanelNames)
                        {
                            Transform found = canvas.transform.Find(panelName);
                            if (found != null)
                            {
                                tradePanelUI = found.gameObject;
                                break;
                            }
                        }
                        
                        if (tradePanelUI != null) break;
                        
                        // Deep search in canvas
                        foreach (string panelName in tradePanelNames)
                        {
                            Transform[] allChildren = canvas.GetComponentsInChildren<Transform>(true);
                            foreach (var child in allChildren)
                            {
                                if (child.name == panelName)
                                {
                                    tradePanelUI = child.gameObject;

                                    break;
                                }
                            }
                            if (tradePanelUI != null) break;
                        }
                        
                        if (tradePanelUI != null) break;
                    }
                }
            }
            

        }
        else
        {
            // Check if assigned object is a prefab or scene object
            bool isPrefab = tradePanelUI.scene.name == null || tradePanelUI.scene.name == "";

            
            if (isPrefab)
            {

                string prefabName = tradePanelUI.name;
                tradePanelUI = null;
                
                // Try to find in scene

            }
        }

        // Auto-find NPC Trader if not assigned
        if (npcTrader == null)
        {
            npcTrader = GetComponentInParent<NPCTrader>();
            if (npcTrader == null)
            {
                npcTrader = FindObjectOfType<NPCTrader>();
            }

        }

        // Hide Trade Panel initially
        if (tradePanelUI != null)
        {
            tradePanelUI.SetActive(false);
        }

        // Create interaction indicator
        if (showIndicator && interactionIndicator == null)
        {
            CreateInteractionIndicator();
        }

        if (interactionIndicator != null)
        {
            interactionIndicator.SetActive(false);
        }

        

    }

    void CreateInteractionIndicator()
    {
        interactionIndicator = new GameObject($"{name}_Indicator");
        interactionIndicator.transform.SetParent(transform);
        interactionIndicator.transform.localPosition = Vector3.up * indicatorOffset;

        indicatorRenderer = interactionIndicator.AddComponent<SpriteRenderer>();
        indicatorRenderer.sortingOrder = 100;

        // Load icon
        if (interactionIcon != null)
        {
            indicatorRenderer.sprite = interactionIcon;
        }
        else
        {
            // Try to load from Resources
            Sprite icon = Resources.Load<Sprite>("Icons/trade_icon");
            if (icon != null)
            {
                indicatorRenderer.sprite = icon;
            }
            else
            {
                // Fallback: colored circle
                indicatorRenderer.color = new Color(1f, 0.8f, 0f, 0.8f); // Yellow-gold
            }
        }

    }

    void Update()
    {
        // Update indicator position
        if (interactionIndicator != null && interactionIndicator.activeSelf)
        {
            Vector3 indicatorPos = transform.position + Vector3.up * indicatorOffset;
            interactionIndicator.transform.position = indicatorPos;

            // Optional: Bob animation
            float bob = Mathf.Sin(Time.time * 3f) * 0.1f;
            interactionIndicator.transform.position += Vector3.up * bob;
        }

        // Check for player interaction input
        if (playerInRange && player != null)
        {
            // Check if player presses interaction key (E or F)
            if (Input.GetKeyDown(KeyCode.E) || Input.GetKeyDown(KeyCode.F))
            {
                OpenTradeUI();
            }
        }

        // Debug: Manual trigger check (runs every second)
        if (Time.frameCount % 60 == 0)
        {
            CheckNearbyPlayers();
        }
    }

    void CheckNearbyPlayers()
    {
        Collider2D[] hits = Physics2D.OverlapBoxAll(transform.position, triggerCollider.bounds.size, 0f);
        
        bool foundPlayer = false;
        foreach (var col in hits)
        {
            if (col.gameObject == gameObject) continue;

            if (IsPlayer(col.gameObject))
            {
                foundPlayer = true;
                if (!playerInRange)
                {

                    OnTriggerEnter2D(col);
                }
            }
        }

        if (!foundPlayer && playerInRange)
        {
            Debug.Log($"⚠️ [MarketSellTrigger] Player left but OnTriggerExit2D was NOT called!");
        }
    }

    void OnTriggerEnter2D(Collider2D other)
    {
   

        // Check if it's the player
        if (!IsPlayer(other.gameObject))
        {
            Debug.Log($"❌ [MarketSellTrigger] {other.name} is not player, ignoring");
            return;
        }


        playerInRange = true;
        player = other.gameObject;

        // Show interaction indicator
        if (interactionIndicator != null)
        {
            interactionIndicator.SetActive(true);
        }

        // Auto-open Trade UI (optional - can remove if you want manual interaction)
        OpenTradeUI();
    }

    void OnTriggerExit2D(Collider2D other)
    {
        

        // Check if it's the player
        if (!IsPlayer(other.gameObject))
        {
            Debug.Log($"❌ [MarketSellTrigger] {other.name} is not player, ignoring exit");
            return;
        }


        playerInRange = false;
        player = null;

        // Hide interaction indicator
        if (interactionIndicator != null)
        {
            interactionIndicator.SetActive(false);
        }

        // Close Trade UI
        CloseTradeUI();
    }

    /// <summary>
    /// Check if GameObject is the player
    /// </summary>
    bool IsPlayer(GameObject obj)
    {
        // Method 1: Check tag
        if (obj.CompareTag("Player"))
            return true;

        // Method 2: Check for PlayerController component
        if (obj.GetComponent<PlayerController>() != null)
            return true;

        // Method 3: Check for PlayerHealth component
        if (obj.GetComponent<PlayerHealth>() != null)
            return true;

        // Method 4: Check name
        if (obj.name.ToLower().Contains("player"))
            return true;

        return false;
    }

    /// <summary>
    /// Open Trade UI Panel
    /// </summary>
    public void OpenTradeUI()
    {
        if (tradePanelUI == null)
        {
            return;
        }

        
        tradePanelUI.SetActive(true);
        

    }


    public void CloseTradeUI()
    {
        if (tradePanelUI == null)
        {
            return;
        }

        tradePanelUI.SetActive(false);
    }


    public void OnInteract()
    {
        if (playerInRange)
        {
            OpenTradeUI();
        }
    }

    void OnDrawGizmosSelected()
    {
        // Draw trigger area
        Collider2D col = GetComponent<Collider2D>();
        if (col != null)
        {
            Gizmos.color = new Color(0f, 1f, 0.5f, 0.3f); // Green
            Gizmos.DrawCube(transform.position, col.bounds.size);
            
            Gizmos.color = Color.green;
            Gizmos.DrawWireCube(transform.position, col.bounds.size);
        }
        else
        {
            // Draw default interaction distance
            Gizmos.color = Color.green;
            Gizmos.DrawWireSphere(transform.position, interactionDistance);
        }
    }

    /// <summary>
    /// Get full hierarchy path of GameObject
    /// </summary>
    string GetGameObjectPath(GameObject obj)
    {
        if (obj == null) return "NULL";
        
        string path = obj.name;
        Transform current = obj.transform.parent;
        
        while (current != null)
        {
            path = current.name + "/" + path;
            current = current.parent;
        }
        
        return path;
    }
}
