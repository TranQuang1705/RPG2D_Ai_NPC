// using UnityEngine;
// using UnityEngine.InputSystem;

// /// <summary>
// /// Centralized ESC key handler với priority system
// /// Priority order:
// /// 1. Input Field focused → Do nothing (let user type)
// /// 2. Shop active → Close shop
// /// 3. Inventory active → Close inventory
// /// 4. Coin Inventory active → Close coin inventory
// /// 5. Quest Detail active → Close quest detail
// /// 6. Nothing open → Open Pause Menu
// /// 
// /// Note: Dialog sử dụng F key, được xử lý trực tiếp trong NPC.cs
// /// </summary>
// public class EscapeKeyManager : Singleton<EscapeKeyManager>
// {
//     [Header("References - Auto-find")]
//     private NPC activeNPC;
//     private UIShopController activeShop;
//     private InventoryToggle inventoryToggle;
//     private CoinInventoryToggle coinInventoryToggle;
//     private QuestPanel questPanel;

//     [Header("Debug")]
//     [SerializeField] private bool showDebugLogs = true;
    
//     [Header("Cooldown")]
//     [SerializeField] private float escCooldown = 0.2f; // Prevent spam
//     private float lastEscTime = -999f;

//     protected override void Awake()
//     {
//         base.Awake();
//     }

//     void Update()
//     {
//         // Check for ESC key press
//         bool escPressed = (Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame)
//                        || Input.GetKeyDown(KeyCode.Escape);

//         if (escPressed)
//         {
//             // Cooldown check to prevent spam
//             if (Time.unscaledTime - lastEscTime >= escCooldown)
//             {
//                 lastEscTime = Time.unscaledTime;
//                 HandleEscapeKey();
//             }
//             else
//             {
//                 if (showDebugLogs)
//                     Debug.Log($"⏱️ ESC cooldown active ({Time.unscaledTime - lastEscTime:F2}s < {escCooldown}s)");
//             }
//         }

//         // F key handling removed - NPC handles it directly now
//     }

//     void HandleEscapeKey()
//     {
//         if (showDebugLogs)
//             Debug.Log("🔑 EscapeKeyManager: ESC pressed, checking priority...");

//         // Priority 1: If typing in input field, do nothing
//         if (UIManager.Instance != null && UIManager.Instance.IsInputFieldFocused())
//         {
//             if (showDebugLogs)
//                 Debug.Log("⌨️ ESC ignored - typing in input field");
//             return;
//         }

//         // Priority 2: Shop active? Close it
//         if (TryCloseShop())
//         {
//             if (showDebugLogs)
//                 Debug.Log("✅ ESC → Closed shop");
//             return;
//         }

//         // Priority 3: Quest Detail active? Close it
//         if (TryCloseQuestDetail())
//         {
//             if (showDebugLogs)
//                 Debug.Log("✅ ESC → Closed quest detail");
//             return;
//         }

//         // Priority 4: Inventory active? Close it
//         if (TryCloseInventory())
//         {
//             if (showDebugLogs)
//                 Debug.Log("✅ ESC → Closed inventory");
//             return;
//         }

//         // Priority 5: Coin Inventory active? Close it
//         if (TryCloseCoinInventory())
//         {
//             if (showDebugLogs)
//                 Debug.Log("✅ ESC → Closed coin inventory");
//             return;
//         }

//         // Priority 6: Nothing open? Open Pause Menu
//         OpenPauseMenu();
//         if (showDebugLogs)
//             Debug.Log("✅ ESC → Opened pause menu");
//     }

//     void HandleFKey()
//     {
//         Debug.Log($"🔑 EscapeKeyManager: F pressed, checking for dialog... activeNPC={(activeNPC != null ? activeNPC.name : "NULL")}");

//         // F key only closes dialog
//         if (TryCloseDialog())
//         {
//             Debug.Log("✅ F → Closed dialog");
//         }
//         else
//         {
//             Debug.Log("ℹ️ F pressed but no dialog active");
//         }
//     }

//     // ==================== DIALOG ====================
//     bool TryCloseDialog()
//     {
//         Debug.Log($"🔍 TryCloseDialog called. activeNPC={(activeNPC != null ? activeNPC.name : "NULL")}");
        
//         // Check if we have a registered active NPC
//         if (activeNPC != null)
//         {
//             Debug.Log($"🔍 Found active NPC: {activeNPC.name}, closing dialog...");
            
//             NPC npcToClose = activeNPC;
//             activeNPC = null; // Clear BEFORE calling exit to prevent re-entry
            
//             Debug.Log($"🔍 Calling TriggerDialogueExit on {npcToClose.name}...");
//             npcToClose.TriggerDialogueExit();
            
//             Debug.Log("✅ Dialog closed successfully, returning true");
            
//             return true;
//         }

//         Debug.Log("🔍 No active NPC dialog found");
        
//         return false;
//     }

//     // ==================== SHOP ====================
//     bool TryCloseShop()
//     {
//         if (activeShop == null)
//         {
//             activeShop = FindObjectOfType<UIShopController>();
//         }

//         if (activeShop != null && activeShop.gameObject.activeSelf)
//         {
//             activeShop.CloseShopUI();
//             return true;
//         }

//         return false;
//     }

//     // ==================== QUEST DETAIL ====================
//     bool TryCloseQuestDetail()
//     {
//         if (questPanel == null)
//         {
//             questPanel = FindObjectOfType<QuestPanel>();
//         }

//         if (questPanel != null)
//         {
//             // Check if quest detail panel is open via QuestPanel's method
//             // We'll add a public method to QuestPanel to check this
//             QuestDetailPanel detailPanel = FindObjectOfType<QuestDetailPanel>();
//             if (detailPanel != null && detailPanel.gameObject.activeSelf)
//             {
//                 questPanel.CloseQuestDetail();
//                 return true;
//             }
//         }

//         return false;
//     }

//     // ==================== INVENTORY ====================
//     bool TryCloseInventory()
//     {
//         if (inventoryToggle == null)
//         {
//             inventoryToggle = FindObjectOfType<InventoryToggle>();
//         }

//         if (inventoryToggle != null && inventoryToggle.inventoryCanvas != null && inventoryToggle.inventoryCanvas.activeSelf)
//         {
//             inventoryToggle.Close();
//             return true;
//         }

//         return false;
//     }

//     // ==================== COIN INVENTORY ====================
//     bool TryCloseCoinInventory()
//     {
//         if (coinInventoryToggle == null)
//         {
//             coinInventoryToggle = FindObjectOfType<CoinInventoryToggle>();
//         }

//         if (coinInventoryToggle != null && coinInventoryToggle.IsOpen())
//         {
//             coinInventoryToggle.Close();
//             return true;
//         }

//         return false;
//     }

//     // ==================== PAUSE MENU ====================
//     void OpenPauseMenu()
//     {
//         // TODO: Implement pause menu
//         // For now, just toggle pause state
//         if (GamePause.IsPaused)
//         {
//             GamePause.SetPaused(false);
//             Debug.Log("⏸️ Game resumed");
//         }
//         else
//         {
//             GamePause.SetPaused(true);
//             Debug.Log("⏸️ Game paused");
//         }
//     }

//     // ==================== PUBLIC API ====================
//     public void RegisterActiveNPC(NPC npc)
//     {
//         activeNPC = npc;
//     }

//     public void UnregisterActiveNPC(NPC npc)
//     {
//         if (activeNPC == npc)
//             activeNPC = null;
//     }

//     public void RegisterActiveShop(UIShopController shop)
//     {
//         activeShop = shop;
//     }

//     public void UnregisterActiveShop(UIShopController shop)
//     {
//         if (activeShop == shop)
//             activeShop = null;
//     }
// }
