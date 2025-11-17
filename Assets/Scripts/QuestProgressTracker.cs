using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Quest Progress Tracker - Helper component to track quest progress
/// Listens for game events (item collected, enemy killed, etc.) and updates quest progress
/// </summary>
public class QuestProgressTracker : MonoBehaviour
{
    public static QuestProgressTracker Instance { get; private set; }

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    void Start()
    {
        // Subscribe to game events
        // TODO: Replace these with your actual game events
        Debug.Log("🎯 QuestProgressTracker initialized");
    }

    // Called when player collects an item
    public void OnItemCollected(int itemId, int quantity = 1)
    {
        if (QuestManager.Instance == null)
            return;

        Debug.Log($"📦 Item collected: {itemId} x{quantity}");

        // Check all active quests for collect objectives
        var activeQuests = QuestManager.Instance.GetActiveQuests();
        
        foreach (var questData in activeQuests)
        {
            foreach (var objective in questData.objectives)
            {
                // Check if this is a collect objective matching the item
                if (objective.objective_type == "collect" && objective.target_id == itemId)
                {
                    // Update progress
                    int currentProgress = QuestManager.Instance.GetObjectiveProgress(questData.quest.quest_id, objective.objective_id);
                    
                    if (currentProgress < objective.quantity)
                    {
                        QuestManager.Instance.UpdateQuestProgress(questData.quest.quest_id, objective.objective_id, quantity);
                        Debug.Log($"✅ Quest progress updated: {questData.quest.quest_name} - {objective.description}");
                    }
                }
            }
        }
    }

    // Called when player kills an enemy
    public void OnEnemyKilled(string enemyName)
    {
        if (QuestManager.Instance == null)
            return;

        Debug.Log($"⚔️ Enemy killed: {enemyName}");

        // Check all active quests for kill objectives
        var activeQuests = QuestManager.Instance.GetActiveQuests();
        
        foreach (var questData in activeQuests)
        {
            foreach (var objective in questData.objectives)
            {
                // Check if this is a kill objective matching the enemy
                if (objective.objective_type == "kill" && objective.target_name.ToLower() == enemyName.ToLower())
                {
                    // Update progress
                    int currentProgress = QuestManager.Instance.GetObjectiveProgress(questData.quest.quest_id, objective.objective_id);
                    
                    if (currentProgress < objective.quantity)
                    {
                        QuestManager.Instance.UpdateQuestProgress(questData.quest.quest_id, objective.objective_id, 1);
                        Debug.Log($"✅ Quest progress updated: {questData.quest.quest_name} - {objective.description}");
                    }
                }
            }
        }
    }

    // Called when player talks to an NPC
    public void OnNPCTalk(int npcId)
    {
        if (QuestManager.Instance == null)
            return;

        Debug.Log($"💬 Talked to NPC: {npcId}");

        // Check all active quests for talk objectives
        var activeQuests = QuestManager.Instance.GetActiveQuests();
        
        foreach (var questData in activeQuests)
        {
            foreach (var objective in questData.objectives)
            {
                // Check if this is a talk objective matching the NPC
                if (objective.objective_type == "talk" && objective.target_id == npcId)
                {
                    // Update progress (talk objectives are usually just 1)
                    int currentProgress = QuestManager.Instance.GetObjectiveProgress(questData.quest.quest_id, objective.objective_id);
                    
                    if (currentProgress < objective.quantity)
                    {
                        QuestManager.Instance.UpdateQuestProgress(questData.quest.quest_id, objective.objective_id, 1);
                        Debug.Log($"✅ Quest progress updated: {questData.quest.quest_name} - {objective.description}");
                    }
                }
            }
        }
    }

    // Called when player reaches a location
    public void OnLocationReached(string locationName)
    {
        if (QuestManager.Instance == null)
            return;

        Debug.Log($"📍 Location reached: {locationName}");

        // Check all active quests for reach objectives
        var activeQuests = QuestManager.Instance.GetActiveQuests();
        
        foreach (var questData in activeQuests)
        {
            foreach (var objective in questData.objectives)
            {
                // Check if this is a reach objective matching the location
                if (objective.objective_type == "reach" && objective.target_name.ToLower() == locationName.ToLower())
                {
                    // Update progress (reach objectives are usually just 1)
                    int currentProgress = QuestManager.Instance.GetObjectiveProgress(questData.quest.quest_id, objective.objective_id);
                    
                    if (currentProgress < objective.quantity)
                    {
                        QuestManager.Instance.UpdateQuestProgress(questData.quest.quest_id, objective.objective_id, 1);
                        Debug.Log($"✅ Quest progress updated: {questData.quest.quest_name} - {objective.description}");
                    }
                }
            }
        }
    }

    // Generic method to update quest progress by objective type and target
    public void UpdateQuestObjective(string objectiveType, int targetId, string targetName, int increment = 1)
    {
        if (QuestManager.Instance == null)
            return;

        var activeQuests = QuestManager.Instance.GetActiveQuests();
        
        foreach (var questData in activeQuests)
        {
            foreach (var objective in questData.objectives)
            {
                bool matches = objective.objective_type == objectiveType;
                
                if (targetId > 0)
                    matches = matches && objective.target_id == targetId;
                
                if (!string.IsNullOrEmpty(targetName))
                    matches = matches && objective.target_name.ToLower() == targetName.ToLower();

                if (matches)
                {
                    int currentProgress = QuestManager.Instance.GetObjectiveProgress(questData.quest.quest_id, objective.objective_id);
                    
                    if (currentProgress < objective.quantity)
                    {
                        QuestManager.Instance.UpdateQuestProgress(questData.quest.quest_id, objective.objective_id, increment);
                        Debug.Log($"✅ Quest progress updated: {questData.quest.quest_name} - {objective.description}");
                    }
                }
            }
        }
    }
}
