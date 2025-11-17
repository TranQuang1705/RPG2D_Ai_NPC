using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// NPC Quest Giver - Handles quest giving and completion for NPCs
/// Shows exclamation mark when NPC has available quests
/// Integrates with dialogue system to give/complete quests
/// </summary>
public class NPCQuestGiver : MonoBehaviour
{
    [Header("NPC Info")]
    [SerializeField] private int npcId = 1; // Database NPC ID
    
    [Header("Quest Indicators")]
    [SerializeField] private GameObject questAvailableIndicator; // Exclamation mark for available quest
    [SerializeField] private GameObject questCompleteIndicator; // Exclamation mark for quest ready to turn in
    [SerializeField] private float indicatorOffset = 1.5f;

    [Header("Random Quest Assignment")]
    [SerializeField] private bool canGiveRandomQuests = false;
    [SerializeField] private List<int> availableQuestPool = new List<int>(); // Quest IDs that can be randomly assigned

    private List<DatabaseQuest> availableQuests = new List<DatabaseQuest>();
    private List<QuestWithDetails> completableQuests = new List<QuestWithDetails>();
    private bool hasCheckedQuests = false;

    void Start()
    {
        // Create indicators if not assigned
        if (questAvailableIndicator == null)
        {
            CreateIndicator(ref questAvailableIndicator, "!", Color.yellow);
        }
        
        if (questCompleteIndicator == null)
        {
            CreateIndicator(ref questCompleteIndicator, "!", Color.green);
        }

        // Hide indicators initially
        if (questAvailableIndicator != null)
            questAvailableIndicator.SetActive(false);
        
        if (questCompleteIndicator != null)
            questCompleteIndicator.SetActive(false);

        // Subscribe to quest events
        QuestManager.OnQuestsLoaded += RefreshQuestStatus;
        QuestManager.OnQuestAccepted += OnQuestStatusChanged;
        QuestManager.OnQuestCompleted += OnQuestStatusChanged;
        QuestManager.OnQuestProgressUpdated += OnQuestProgressChanged;

        // Initial check
        Invoke(nameof(RefreshQuestStatus), 1f);
    }

    void OnDestroy()
    {
        // Unsubscribe
        QuestManager.OnQuestsLoaded -= RefreshQuestStatus;
        QuestManager.OnQuestAccepted -= OnQuestStatusChanged;
        QuestManager.OnQuestCompleted -= OnQuestStatusChanged;
        QuestManager.OnQuestProgressUpdated -= OnQuestProgressChanged;
    }

    void CreateIndicator(ref GameObject indicator, string text, Color color)
    {
        // Create a simple UI indicator
        GameObject canvas = GameObject.Find("Canvas");
        if (canvas == null)
        {
            Debug.LogWarning("⚠️ No Canvas found for quest indicator");
            return;
        }

        indicator = new GameObject($"{name}_QuestIndicator");
        indicator.transform.SetParent(canvas.transform);
        
        var canvasObj = indicator.AddComponent<Canvas>();
        canvasObj.renderMode = RenderMode.WorldSpace;
        
        var rectTransform = indicator.GetComponent<RectTransform>();
        rectTransform.sizeDelta = new Vector2(0.5f, 0.5f);
        
        var textObj = new GameObject("Text");
        textObj.transform.SetParent(indicator.transform);
        
        var textComponent = textObj.AddComponent<TMPro.TextMeshProUGUI>();
        textComponent.text = text;
        textComponent.fontSize = 24;
        textComponent.color = color;
        textComponent.alignment = TMPro.TextAlignmentOptions.Center;
        textComponent.fontStyle = TMPro.FontStyles.Bold;
        
        var textRect = textObj.GetComponent<RectTransform>();
        textRect.sizeDelta = new Vector2(0.5f, 0.5f);
        textRect.anchoredPosition = Vector2.zero;
    }

    void Update()
    {
        // Update indicator positions
        UpdateIndicatorPositions();
    }

    void UpdateIndicatorPositions()
    {
        Vector3 indicatorPos = transform.position + Vector3.up * indicatorOffset;

        if (questAvailableIndicator != null && questAvailableIndicator.activeSelf)
            questAvailableIndicator.transform.position = indicatorPos;

        if (questCompleteIndicator != null && questCompleteIndicator.activeSelf)
            questCompleteIndicator.transform.position = indicatorPos;
    }

    void OnQuestStatusChanged(int questId)
    {
        RefreshQuestStatus();
    }

    void OnQuestProgressChanged(int questId, int objectiveId, int count)
    {
        RefreshQuestStatus();
    }

    void RefreshQuestStatus()
    {
        if (QuestManager.Instance == null)
            return;

        hasCheckedQuests = true;

        // Get quests available from this NPC
        availableQuests = QuestManager.Instance.GetQuestsForNPC(npcId);

        // Get quests that can be turned in
        var activeQuests = QuestManager.Instance.GetActiveQuests();
        completableQuests = activeQuests.Where(q => CanTurnInQuest(q)).ToList();

        // Update indicators
        UpdateIndicators();

        Debug.Log($"🎯 NPC {npcId}: {availableQuests.Count} available quests, {completableQuests.Count} completable quests");
    }

    void UpdateIndicators()
    {
        // Priority: Show complete indicator if any quest can be turned in
        if (completableQuests.Count > 0)
        {
            if (questAvailableIndicator != null)
                questAvailableIndicator.SetActive(false);
            
            if (questCompleteIndicator != null)
                questCompleteIndicator.SetActive(true);
        }
        // Otherwise show available indicator if this NPC has quests to give
        else if (availableQuests.Count > 0)
        {
            if (questAvailableIndicator != null)
                questAvailableIndicator.SetActive(true);
            
            if (questCompleteIndicator != null)
                questCompleteIndicator.SetActive(false);
        }
        // Hide all indicators
        else
        {
            if (questAvailableIndicator != null)
                questAvailableIndicator.SetActive(false);
            
            if (questCompleteIndicator != null)
                questCompleteIndicator.SetActive(false);
        }
    }

    bool CanTurnInQuest(QuestWithDetails questData)
    {
        // Check if all objectives are completed
        foreach (var objective in questData.objectives)
        {
            int currentCount = QuestManager.Instance.GetObjectiveProgress(questData.quest.quest_id, objective.objective_id);
            if (currentCount < objective.quantity)
                return false;
        }
        return true;
    }

    // Called when player interacts with NPC
    public void OnPlayerInteract()
    {
        // Check if player can turn in any quests
        if (completableQuests.Count > 0)
        {
            // Turn in the first completable quest
            CompleteQuest(completableQuests[0].quest.quest_id);
        }
        // Check if this NPC has quests to give
        else if (availableQuests.Count > 0)
        {
            // Give the first available quest
            GiveQuest(availableQuests[0].quest_id);
        }
    }

    // Called when player asks "Do you need anything?" or similar trigger
    public void OnPlayerAskForQuest()
    {
        if (!hasCheckedQuests)
        {
            RefreshQuestStatus();
        }

        // If NPC has no predefined quest, assign a random one
        if (availableQuests.Count == 0 && canGiveRandomQuests && availableQuestPool.Count > 0)
        {
            AssignRandomQuest();
        }
        // Give available quest
        else if (availableQuests.Count > 0)
        {
            GiveQuest(availableQuests[0].quest_id);
        }
        else
        {
            Debug.Log($"💬 NPC {npcId}: I don't have any quests for you right now.");
        }
    }

    void AssignRandomQuest()
    {
        if (QuestManager.Instance == null)
            return;

        // Pick a random quest from the pool
        int randomIndex = Random.Range(0, availableQuestPool.Count);
        int randomQuestId = availableQuestPool[randomIndex];

        // Check if player already has this quest
        if (QuestManager.Instance.IsQuestInProgress(randomQuestId) || QuestManager.Instance.IsQuestCompleted(randomQuestId))
        {
            Debug.Log($"💬 NPC {npcId}: You already have or completed that quest.");
            return;
        }

        // Give the quest
        GiveQuest(randomQuestId);
    }

    void GiveQuest(int questId)
    {
        if (QuestManager.Instance == null)
        {
            Debug.LogError("❌ QuestManager instance not found!");
            return;
        }

        var quest = QuestManager.Instance.GetQuest(questId);
        if (quest == null)
        {
            Debug.LogError($"❌ Quest {questId} not found!");
            return;
        }

        Debug.Log($"📜 NPC {npcId} gives quest: {quest.quest_name}");
        Debug.Log($"📜 Quest Description: {quest.description}");
        Debug.Log($"📜 Quest will change status from 'not_started' to 'in_progress'");

        // Accept the quest (this will update status to 'in_progress' and trigger QuestPanel)
        QuestManager.Instance.AcceptQuest(questId);

        // Show quest accepted notification
        ShowQuestNotification($"Quest Accepted: {quest.quest_name}");
    }

    void CompleteQuest(int questId)
    {
        if (QuestManager.Instance == null)
            return;

        var quest = QuestManager.Instance.GetQuest(questId);
        if (quest == null)
        {
            Debug.LogError($"❌ Quest {questId} not found!");
            return;
        }

        // ✅ Verify quest can be completed (all objectives done)
        if (!QuestManager.Instance.CanCompleteQuest(questId))
        {
            Debug.LogWarning($"⚠️ Quest {quest.quest_name} cannot be completed yet - objectives not finished!");
            ShowQuestNotification($"Quest not ready: Please complete all objectives first.");
            return;
        }

        Debug.Log($"💬 NPC {npcId}: Thank you for completing '{quest.quest_name}'!");
        Debug.Log($"🎁 NPC {npcId}: Here are your rewards!");

        // Show thank you message
        ShowQuestNotification($"NPC: Thank you! Quest '{quest.quest_name}' completed!");

        QuestManager.Instance.CompleteQuest(questId);
    }

    void ShowQuestNotification(string message)
    {
        // TODO: Implement notification UI
        Debug.Log($"🎉 {message}");
    }

    // Public getters
    public bool HasAvailableQuests()
    {
        return availableQuests.Count > 0;
    }

    public bool HasCompletableQuests()
    {
        return completableQuests.Count > 0;
    }

    public int GetNPCId()
    {
        return npcId;
    }

    public void SetNPCId(int id)
    {
        npcId = id;
        RefreshQuestStatus();
    }

    /// <summary>
    /// Get the first available quest for chatbot context (doesn't accept the quest)
    /// </summary>
    public DatabaseQuest GetFirstAvailableQuest()
    {
        if (availableQuests.Count > 0)
            return availableQuests[0];
        return null;
    }

    /// <summary>
    /// Get quest objectives for a specific quest
    /// </summary>
    public List<DatabaseQuestObjective> GetQuestObjectives(int questId)
    {
        if (QuestManager.Instance != null)
            return QuestManager.Instance.GetQuestObjectives(questId);
        return null;
    }
}
