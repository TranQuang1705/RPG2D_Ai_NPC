using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Main Quest Panel UI - Shows icon when player has active quests
/// Clicking opens the QuestDetail panel
/// </summary>
public class QuestPanel : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private GameObject questDetailPanel;
    [SerializeField] private Button questPanelButton;
    [SerializeField] private TextMeshProUGUI questCountText;
    [SerializeField] private Image questIcon;

    [Header("Visual Settings")]
    [SerializeField] private Sprite defaultQuestIcon;
    [SerializeField] private Color hasQuestColor = Color.yellow;
    [SerializeField] private Color noQuestColor = Color.gray;

    private bool isDetailPanelOpen = false;
    private int activeQuestCount = 0;

    void Start()
    {
        Debug.Log("🔥🔥🔥 QuestPanel: START() CALLED 🔥🔥🔥");
        
        // Initialize
        if (questDetailPanel != null)
        {
            questDetailPanel.SetActive(false);
            Debug.Log("✅ QuestPanel: questDetailPanel assigned");
        }
        else
        {
            Debug.LogError("❌ QuestPanel: questDetailPanel is NULL in Inspector!");
        }

        // Setup button click
        if (questPanelButton != null)
        {
            questPanelButton.onClick.AddListener(ToggleQuestDetail);
            Debug.Log($"✅ QuestPanel: Button listener added. Button interactable: {questPanelButton.interactable}");
        }
        else
        {
            Debug.LogError("❌ QuestPanel: questPanelButton is NULL in Inspector!");
        }

        // Hide panel initially
        gameObject.SetActive(false);
        Debug.Log("⚠️ QuestPanel: Panel hidden initially (will show when quests are active)");

        // Subscribe to quest events
        QuestManager.OnQuestsLoaded += RefreshQuestPanel;
        QuestManager.OnQuestAccepted += OnQuestUpdate;
        QuestManager.OnQuestCompleted += OnQuestUpdate;
        QuestManager.OnQuestProgressUpdated += OnProgressUpdate;

        Debug.Log("✅ QuestPanel: Subscribed to QuestManager events");

        // Wait for QuestManager to load
        if (QuestManager.Instance != null)
        {
            Debug.Log("✅ QuestPanel: QuestManager found, will refresh in 0.5s");
            Invoke(nameof(RefreshQuestPanel), 0.5f);
        }
        else
        {
            Debug.LogError("❌ QuestPanel: QuestManager.Instance is NULL!");
        }
    }

    void OnDestroy()
    {
        // Unsubscribe from events
        QuestManager.OnQuestsLoaded -= RefreshQuestPanel;
        QuestManager.OnQuestAccepted -= OnQuestUpdate;
        QuestManager.OnQuestCompleted -= OnQuestUpdate;
        QuestManager.OnQuestProgressUpdated -= OnProgressUpdate;
    }

    void OnQuestUpdate(int questId)
    {
        Debug.Log($"🔔🔔🔔 QuestPanel: OnQuestUpdate CALLED for quest {questId} 🔔🔔🔔");
        RefreshQuestPanel();
        
        // ✅ Auto-open quest detail panel when a new quest is accepted
        if (!isDetailPanelOpen && activeQuestCount > 0)
        {
            Debug.Log($"📜 QuestPanel: New quest accepted! Auto-opening quest detail panel");
            OpenQuestDetail();
        }
        else
        {
            Debug.Log($"⚠️ QuestPanel: Not opening detail. isOpen={isDetailPanelOpen}, count={activeQuestCount}");
        }
    }

    void OnProgressUpdate(int questId, int objectiveId, int count)
    {
        // ✅ Always refresh quest panel count
        RefreshQuestPanel();
        
        // Update detail panel if it's open
        if (isDetailPanelOpen && questDetailPanel != null)
        {
            var detailScript = questDetailPanel.GetComponent<QuestDetailPanel>();
            if (detailScript != null)
                detailScript.RefreshQuestList();
        }
    }

    void RefreshQuestPanel()
    {
        Debug.Log("🔄 QuestPanel: RefreshQuestPanel() CALLED");
        
        if (QuestManager.Instance == null)
        {
            Debug.LogError("❌ QuestPanel: QuestManager.Instance is NULL in RefreshQuestPanel!");
            return;
        }

        // Get active quests
        var activeQuests = QuestManager.Instance.GetActiveQuests();
        activeQuestCount = activeQuests.Count;

        Debug.Log($"📋 QuestPanel: Found {activeQuestCount} active quests");

        // Show/hide panel based on quest count
        if (activeQuestCount > 0)
        {
            gameObject.SetActive(true);
            Debug.Log("✅ QuestPanel: Panel ACTIVATED (showing quest icon)");
            UpdateVisuals();
        }
        else
        {
            gameObject.SetActive(false);
            Debug.Log("⚠️ QuestPanel: Panel HIDDEN (no active quests)");
            if (isDetailPanelOpen && questDetailPanel != null)
            {
                questDetailPanel.SetActive(false);
                isDetailPanelOpen = false;
            }
        }
    }

    void UpdateVisuals()
    {
        // Update quest count text
        if (questCountText != null)
            questCountText.text = activeQuestCount.ToString();

        // Update icon color
        if (questIcon != null)
        {
            questIcon.sprite = defaultQuestIcon;
            questIcon.color = activeQuestCount > 0 ? hasQuestColor : noQuestColor;
        }

        // ✅ Check button state
        if (questPanelButton != null)
        {
            Debug.Log($"🔘 QuestPanel UpdateVisuals: Button exists, interactable = {questPanelButton.interactable}");
        }
        else
        {
            Debug.LogError("❌ QuestPanel UpdateVisuals: Button is NULL!");
        }
    }

    void ToggleQuestDetail()
    {
        Debug.Log("🔥🔥🔥 ToggleQuestDetail() CALLED! 🔥🔥🔥");
        
        if (questDetailPanel == null)
        {
            Debug.LogWarning("⚠️ QuestPanel: questDetailPanel is null!");
            return;
        }

        isDetailPanelOpen = !isDetailPanelOpen;
        
        // ✅ QuestDetailPanel's OnEnable/OnDisable sẽ tự động notify UIManager
        // Không cần gọi UIManager ở đây để tránh duplicate
        questDetailPanel.SetActive(isDetailPanelOpen);

        Debug.Log($"🔍 QuestPanel: ToggleQuestDetail called, isOpen = {isDetailPanelOpen}");

        if (isDetailPanelOpen)
        {
            // ✅ Force refresh quest data from QuestManager before displaying
            RefreshQuestPanel();
            
            // Refresh the detail panel
            var detailScript = questDetailPanel.GetComponent<QuestDetailPanel>();
            if (detailScript != null)
                detailScript.RefreshQuestList();
        }

        Debug.Log($"📋 QuestPanel: Detail panel {(isDetailPanelOpen ? "opened" : "closed")}");
    }

    // Public method to open quest detail directly
    public void OpenQuestDetail()
    {
        if (!isDetailPanelOpen && questDetailPanel != null)
        {
            ToggleQuestDetail();
        }
    }

    // Public method to close quest detail
    public void CloseQuestDetail()
    {
        if (isDetailPanelOpen && questDetailPanel != null)
        {
            ToggleQuestDetail();
        }
    }
}
