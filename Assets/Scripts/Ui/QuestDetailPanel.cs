using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;

/// <summary>
/// Quest Detail Panel - Hiển thị chi tiết nhiệm vụ: mô tả, mục tiêu, phần thưởng, tiến độ.
/// </summary>
public class QuestDetailPanel : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private Transform questListContainer;
    [SerializeField] private GameObject questItemPrefab;
    [SerializeField] private Button closeButton;
    [SerializeField] private ScrollRect scrollRect;

    [Header("Selected Quest Details")]
    [SerializeField] private GameObject detailPanel;
    [SerializeField] private TextMeshProUGUI questNameText;
    [SerializeField] private TextMeshProUGUI taskLevelText;
    [SerializeField] private TextMeshProUGUI detailText;
    [SerializeField] private Transform taskListContainer;
    [SerializeField] private GameObject taskItemPrefab;
    [SerializeField] private Image markImage;
    [SerializeField] private Sprite inProgressMark;
    [SerializeField] private Sprite completedMark;

    [Header("Reward UI")]
    [SerializeField] private TextMeshProUGUI rewardItemText;
    [SerializeField] private Image rewardItemImage;

    [Header("Currency & EXP Groups")]
    [SerializeField] private GameObject sylvGroup;
    [SerializeField] private TextMeshProUGUI sylvCountText;

    [SerializeField] private GameObject goldGroup;
    [SerializeField] private TextMeshProUGUI goldCountText;

    [SerializeField] private GameObject obalGroup;
    [SerializeField] private TextMeshProUGUI obalCountText;

    [SerializeField] private GameObject expGroup;
    [SerializeField] private TextMeshProUGUI expCountText;

    private List<GameObject> questItemInstances = new List<GameObject>();
    private List<GameObject> taskItemInstances = new List<GameObject>();
    private QuestWithDetails selectedQuest = null;

    // ================== LIFE CYCLE ==================
    void Start()
    {
        if (closeButton != null)
            closeButton.onClick.AddListener(ClosePanel);

        if (detailPanel != null)
            detailPanel.SetActive(false);

        RefreshQuestList();
    }

    // ================== QUEST LIST ==================
    public void RefreshQuestList()
    {
        if (QuestManager.Instance == null)
            return;

        ClearQuestItems();
        var activeQuests = QuestManager.Instance.GetActiveQuests();

        foreach (var questData in activeQuests)
            CreateQuestItem(questData);

        // ✅ Refresh selected quest details if a quest is selected
        if (selectedQuest != null)
        {
            // Find updated quest data
            var updatedQuest = activeQuests.Find(q => q.quest.quest_id == selectedQuest.quest.quest_id);
            if (updatedQuest != null)
            {
                selectedQuest = updatedQuest;
                DisplayQuestDetails(updatedQuest);
            }
        }

        Debug.Log($"📋 QuestDetailPanel: Displaying {activeQuests.Count} quests");
    }

    void ClearQuestItems()
    {
        foreach (var item in questItemInstances)
            if (item != null) Destroy(item);
        questItemInstances.Clear();
    }

    void CreateQuestItem(QuestWithDetails questData)
    {
        if (questListContainer == null || questItemPrefab == null)
            return;

        GameObject itemObj = Instantiate(questItemPrefab, questListContainer);
        questItemInstances.Add(itemObj);

        var nameText = itemObj.transform.Find("QuestName")?.GetComponent<TextMeshProUGUI>();
        if (nameText != null)
            nameText.text = questData.quest.quest_name;

        var levelText = itemObj.transform.Find("Level")?.GetComponent<TextMeshProUGUI>();
        if (levelText != null)
            levelText.text = $"Lv.{questData.quest.min_level}";

        var typeText = itemObj.transform.Find("Type")?.GetComponent<TextMeshProUGUI>();
        if (typeText != null)
        {
            string typeDisplay = questData.quest.quest_type.ToUpper();
            typeText.text = $"[{typeDisplay}]";
            switch (questData.quest.quest_type.ToLower())
            {
                case "main": typeText.color = Color.yellow; break;
                case "side": typeText.color = Color.cyan; break;
                case "daily": typeText.color = Color.green; break;
            }
        }

        var button = itemObj.GetComponent<Button>();
        if (button != null)
            button.onClick.AddListener(() => SelectQuest(questData));
    }

    // ================== QUEST DETAILS ==================
    void SelectQuest(QuestWithDetails questData)
    {
        selectedQuest = questData;
        DisplayQuestDetails(questData);
    }

    void DisplayQuestDetails(QuestWithDetails questData)
    {
        if (detailPanel == null) return;
        detailPanel.SetActive(true);

        questNameText.text = questData.quest.quest_name;
        taskLevelText.text = $"Level Requirement: {questData.quest.min_level}";
        detailText.text = questData.quest.description;

        DisplayTaskList(questData);
        DisplayRewards(questData);
        UpdateQuestMark(questData);
    }

    void DisplayTaskList(QuestWithDetails questData)
    {
        if (taskListContainer == null || taskItemPrefab == null)
            return;

        ClearTaskItems();

        foreach (var objective in questData.objectives)
        {
            GameObject taskObj = Instantiate(taskItemPrefab, taskListContainer);
            taskItemInstances.Add(taskObj);

            int currentCount = 0;
            var progress = questData.progress.Find(p => p.objective_id == objective.objective_id);
            if (progress != null)
                currentCount = progress.current_count;

            var taskText = taskObj.GetComponentInChildren<TextMeshProUGUI>();
            if (taskText != null)
            {
                string progressStr = $"[{currentCount}/{objective.quantity}]";
                taskText.text = $"{progressStr} {objective.description}";

                if (currentCount >= objective.quantity)
                {
                    taskText.color = Color.green;
                    taskText.fontStyle = FontStyles.Strikethrough;
                }
                else
                {
                    taskText.color = Color.white;
                    taskText.fontStyle = FontStyles.Normal;
                }
            }

            var checkIcon = taskObj.transform.Find("CheckIcon")?.GetComponent<Image>();
            if (checkIcon != null)
                checkIcon.enabled = currentCount >= objective.quantity;
        }
    }

    void ClearTaskItems()
    {
        foreach (var item in taskItemInstances)
            if (item != null) Destroy(item);
        taskItemInstances.Clear();
    }

    // ================== REWARDS ==================
    void DisplayRewards(QuestWithDetails questData)
    {
        HideAllRewardGroups();

        // ---- HIỂN THỊ ITEM ----
        if (rewardItemText != null && rewardItemImage != null)
        {
            rewardItemText.text = "";
            rewardItemImage.enabled = false;

            if (questData.quest.reward_item_id > 0 && DatabaseItemManager.Instance != null)
            {
                var item = DatabaseItemManager.Instance.GetDatabaseItem(questData.quest.reward_item_id);
                if (item != null)
                {
                    rewardItemText.text = item.item_name;
                    
                    Sprite itemSprite = Resources.Load<Sprite>(item.icon_path);
                    if (itemSprite != null)
                    {
                        rewardItemImage.sprite = itemSprite;
                        rewardItemImage.enabled = true;
                    }
                }
            }
        }

        // ---- TIỀN TỆ ----
        DisplayCurrencyRewards(questData.quest.reward_gold);

        // ---- EXP ----
        DisplayExpReward(questData.quest.reward_exp);
    }

    void DisplayCurrencyRewards(int totalGold)
    {
        if (totalGold <= 0)
        {
            HideCurrencyGroups();
            return;
        }

        int gold = totalGold / 10000;
        int silver = (totalGold % 10000) / 100;
        int copper = totalGold % 100;

        if (gold > 0 && goldGroup != null)
        {
            goldGroup.SetActive(true);
            goldCountText.text = gold.ToString();
        }

        if (silver > 0 && sylvGroup != null)
        {
            sylvGroup.SetActive(true);
            sylvCountText.text = silver.ToString();
        }

        if (copper > 0 && obalGroup != null)
        {
            obalGroup.SetActive(true);
            obalCountText.text = copper.ToString();
        }
    }

    void DisplayExpReward(int exp)
    {
        if (exp > 0 && expGroup != null)
        {
            expGroup.SetActive(true);
            expCountText.text = exp.ToString();
        }
        else if (expGroup != null)
        {
            expGroup.SetActive(false);
        }
    }

    void HideAllRewardGroups()
    {
        if (sylvGroup != null) sylvGroup.SetActive(false);
        if (goldGroup != null) goldGroup.SetActive(false);
        if (obalGroup != null) obalGroup.SetActive(false);
        if (expGroup != null) expGroup.SetActive(false);

        if (rewardItemImage != null) rewardItemImage.enabled = false;
        if (rewardItemText != null) rewardItemText.text = "";
    }

    void HideCurrencyGroups()
    {
        if (sylvGroup != null) sylvGroup.SetActive(false);
        if (goldGroup != null) goldGroup.SetActive(false);
        if (obalGroup != null) obalGroup.SetActive(false);
    }

    // ================== QUEST STATUS ==================
    void UpdateQuestMark(QuestWithDetails questData)
    {
        if (markImage == null) return;

        bool allCompleted = true;
        foreach (var objective in questData.objectives)
        {
            int currentCount = 0;
            var progress = questData.progress.Find(p => p.objective_id == objective.objective_id);
            if (progress != null)
                currentCount = progress.current_count;

            if (currentCount < objective.quantity)
            {
                allCompleted = false;
                break;
            }
        }

        markImage.sprite = allCompleted ? completedMark : inProgressMark;
        markImage.color = allCompleted ? Color.green : Color.yellow;
    }

    void ClosePanel()
    {
        // ✅ Gọi QuestPanel để close (QuestPanel sẽ notify UIManager)
        var questPanel = FindObjectOfType<QuestPanel>();
        if (questPanel != null)
        {
            questPanel.CloseQuestDetail();
        }
        else
        {
            // Fallback: nếu không tìm thấy QuestPanel, tự đóng
            gameObject.SetActive(false);
            selectedQuest = null;
            if (detailPanel != null)
                detailPanel.SetActive(false);
        }
    }

    // ================== HELPERS ==================
    public bool CanTurnInQuest(QuestWithDetails questData)
    {
        foreach (var objective in questData.objectives)
        {
            int currentCount = 0;
            var progress = questData.progress.Find(p => p.objective_id == objective.objective_id);
            if (progress != null)
                currentCount = progress.current_count;
            if (currentCount < objective.quantity)
                return false;
        }
        return true;
    }

    public QuestWithDetails GetSelectedQuest()
    {
        return selectedQuest;
    }
    void OnEnable()
    {
        Debug.Log("🔍 QuestDetailPanel: OnEnable called");
        
        // ✅ Notify UIManager khi panel được enable
        if (UIManager.Instance != null)
        {
            Debug.Log("📋 QuestDetailPanel: Calling UIManager.OnPanelOpened()");
            UIManager.Instance.OnPanelOpened();
        }
        else
        {
            Debug.LogError("❌ QuestDetailPanel: UIManager.Instance is NULL!");
        }

        QuestManager.OnQuestProgressUpdated += HandleQuestProgressUpdated;
    }

    void OnDisable()
    {
        Debug.Log("🔍 QuestDetailPanel: OnDisable called");
        
        // ✅ Notify UIManager khi panel được disable
        if (UIManager.Instance != null)
        {
            Debug.Log("📋 QuestDetailPanel: Calling UIManager.OnPanelClosed()");
            UIManager.Instance.OnPanelClosed();
        }

        QuestManager.OnQuestProgressUpdated -= HandleQuestProgressUpdated;
    }

private void HandleQuestProgressUpdated(int questId, int objectiveId, int newCount)
{
    Debug.Log($"🟢 QuestDetailPanel nhận event: Quest {questId} Objective {objectiveId} → {newCount}");

    if (selectedQuest != null && selectedQuest.quest.quest_id == questId)
    {
        // Lấy dữ liệu mới
        var refreshedQuest = QuestManager.Instance.GetActiveQuests()
            .Find(q => q.quest.quest_id == questId);

        if (refreshedQuest != null)
        {
            selectedQuest = refreshedQuest;
            DisplayTaskList(refreshedQuest);
            UpdateQuestMark(refreshedQuest);
            Debug.Log("✅ QuestDetailPanel: UI updated instantly after progress change!");
        }
    }
}

}
