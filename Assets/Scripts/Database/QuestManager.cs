using UnityEngine;
using UnityEngine.Networking;
using System.Collections;
using System.Collections.Generic;
using System;
using System.Linq;

public class QuestManager : MonoBehaviour
{
    public static QuestManager Instance { get; private set; }

    [Header("API Configuration")]
    [SerializeField] private string apiBaseUrl = "http://127.0.0.1:5002";

    [Header("Current Player")]
    [SerializeField] private int currentPlayerId = 1;

    // Cache data
    private Dictionary<int, DatabaseQuest> allQuests = new Dictionary<int, DatabaseQuest>();
    private Dictionary<int, List<DatabaseQuestObjective>> questObjectives = new Dictionary<int, List<DatabaseQuestObjective>>();
    private Dictionary<int, DatabasePlayerQuest> playerQuests = new Dictionary<int, DatabasePlayerQuest>();
    private Dictionary<int, Dictionary<int, DatabaseQuestProgress>> questProgress = new Dictionary<int, Dictionary<int, DatabaseQuestProgress>>();
    private Dictionary<int, List<int>> npcQuests = new Dictionary<int, List<int>>();

    // Events
    public static event Action OnQuestsLoaded;
    public static event Action<int> OnQuestAccepted; // quest_id
    public static event Action<int> OnQuestCompleted; // quest_id
    public static event Action<int, int, int> OnQuestProgressUpdated; // quest_id, objective_id, current_count

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);
        Debug.Log($"⚙️ QuestManager Awake on {gameObject.name}");
    }

    void Start()
    {
        StartCoroutine(LoadAllQuestData());
    }

    IEnumerator LoadAllQuestData()
    {
        Debug.Log("🚀 QuestManager: Loading quest data from database...");

        // Load quests
        yield return StartCoroutine(FetchQuests());

        // Load quest objectives
        yield return StartCoroutine(FetchQuestObjectives());

        // Load player quests
        yield return StartCoroutine(FetchPlayerQuests(currentPlayerId));

        // Load quest progress
        yield return StartCoroutine(FetchQuestProgress(currentPlayerId));

        // Load NPC quests
        yield return StartCoroutine(FetchNPCQuests());

        Debug.Log($"✅ QuestManager: Loaded {allQuests.Count} quests, {playerQuests.Count} player quests");
        OnQuestsLoaded?.Invoke();
    }

    IEnumerator FetchQuests()
    {
        using (UnityWebRequest req = UnityWebRequest.Get($"{apiBaseUrl}/quests"))
        {
            yield return req.SendWebRequest();

            if (req.result == UnityWebRequest.Result.Success)
            {
                string json = req.downloadHandler.text;
                DatabaseQuestList questList = JsonUtility.FromJson<DatabaseQuestList>("{\"quests\":" + json + "}");

                allQuests.Clear();
                foreach (var quest in questList.quests)
                {
                    allQuests[quest.quest_id] = quest;
                }
                Debug.Log($"✅ Loaded {allQuests.Count} quests");
            }
            else
            {
                Debug.LogError($"❌ Failed to load quests: {req.error}");
            }
        }
    }

    IEnumerator FetchQuestObjectives()
    {
        using (UnityWebRequest req = UnityWebRequest.Get($"{apiBaseUrl}/quest_objectives"))
        {
            yield return req.SendWebRequest();

            if (req.result == UnityWebRequest.Result.Success)
            {
                string json = req.downloadHandler.text;
                DatabaseQuestObjectiveList objList = JsonUtility.FromJson<DatabaseQuestObjectiveList>("{\"objectives\":" + json + "}");

                questObjectives.Clear();
                foreach (var obj in objList.objectives)
                {
                    if (!questObjectives.ContainsKey(obj.quest_id))
                        questObjectives[obj.quest_id] = new List<DatabaseQuestObjective>();

                    questObjectives[obj.quest_id].Add(obj);
                }
                Debug.Log($"✅ Loaded quest objectives for {questObjectives.Count} quests");
            }
            else
            {
                Debug.LogError($"❌ Failed to load quest objectives: {req.error}");
            }
        }
    }

    IEnumerator FetchPlayerQuests(int playerId)
    {
        using (UnityWebRequest req = UnityWebRequest.Get($"{apiBaseUrl}/player_quests?player_id={playerId}"))
        {
            yield return req.SendWebRequest();

            if (req.result == UnityWebRequest.Result.Success)
            {
                string json = req.downloadHandler.text;
                DatabasePlayerQuestList pqList = JsonUtility.FromJson<DatabasePlayerQuestList>("{\"player_quests\":" + json + "}");

                playerQuests.Clear();
                foreach (var pq in pqList.player_quests)
                {
                    playerQuests[pq.quest_id] = pq;
                }
                Debug.Log($"✅ Loaded {playerQuests.Count} player quests");
            }
            else
            {
                Debug.LogError($"❌ Failed to load player quests: {req.error}");
            }
        }
    }

    IEnumerator FetchQuestProgress(int playerId)
    {
        using (UnityWebRequest req = UnityWebRequest.Get($"{apiBaseUrl}/quest_progress?player_id={playerId}"))
        {
            yield return req.SendWebRequest();

            if (req.result == UnityWebRequest.Result.Success)
            {
                string json = req.downloadHandler.text;
                DatabaseQuestProgressList progList = JsonUtility.FromJson<DatabaseQuestProgressList>("{\"progress\":" + json + "}");

                questProgress.Clear();
                foreach (var prog in progList.progress)
                {
                    if (!questProgress.ContainsKey(prog.quest_id))
                        questProgress[prog.quest_id] = new Dictionary<int, DatabaseQuestProgress>();

                    questProgress[prog.quest_id][prog.objective_id] = prog;
                }
                Debug.Log($"✅ Loaded quest progress for {questProgress.Count} quests");
            }
            else
            {
                Debug.LogError($"❌ Failed to load quest progress: {req.error}");
            }
        }
    }
    public void ApplyQuestProgressUpdate(int questId, int objectiveId, int newCount, int goal)
    {
        // 🔹 Cập nhật dữ liệu cache
        
        if (questProgress.ContainsKey(questId) && questProgress[questId].ContainsKey(objectiveId))
            questProgress[questId][objectiveId].current_count = newCount;

        Debug.Log($"🔄 Quest {questId} objective {objectiveId} updated to {newCount}/{goal}");

        // 🔹 Phát event để UI (QuestDetailPanel, QuestLog, ...) lắng nghe và cập nhật
        Debug.Log($"🔄 asdasd Quest {questId} objective {objectiveId} updated → {newCount}/{goal}");
        Debug.Log($"📢 asdasd Firing event OnQuestProgressUpdated");
        OnQuestProgressUpdated?.Invoke(questId, objectiveId, newCount);
    }


    IEnumerator FetchNPCQuests()
    {
        using (UnityWebRequest req = UnityWebRequest.Get($"{apiBaseUrl}/npc_quests"))
        {
            yield return req.SendWebRequest();

            if (req.result == UnityWebRequest.Result.Success)
            {
                string json = req.downloadHandler.text;
                DatabaseNPCQuestList npcQuestList = JsonUtility.FromJson<DatabaseNPCQuestList>("{\"npc_quests\":" + json + "}");

                npcQuests.Clear();
                foreach (var nq in npcQuestList.npc_quests)
                {
                    if (!npcQuests.ContainsKey(nq.npc_id))
                        npcQuests[nq.npc_id] = new List<int>();

                    npcQuests[nq.npc_id].Add(nq.quest_id);
                }
                Debug.Log($"✅ Loaded NPC quests for {npcQuests.Count} NPCs");
            }
            else
            {
                Debug.LogError($"❌ Failed to load NPC quests: {req.error}");
            }
        }
    }

    // Get all active quests for current player
    public List<QuestWithDetails> GetActiveQuests()
    {
        List<QuestWithDetails> activeQuests = new List<QuestWithDetails>();

        foreach (var pq in playerQuests.Values)
        {
            if (pq.status == "in_progress" && allQuests.ContainsKey(pq.quest_id))
            {
                var questData = new QuestWithDetails
                {
                    quest = allQuests[pq.quest_id],
                    playerQuest = pq,
                    objectives = questObjectives.ContainsKey(pq.quest_id) ? questObjectives[pq.quest_id] : new List<DatabaseQuestObjective>(),
                    progress = new List<DatabaseQuestProgress>()
                };

                if (questProgress.ContainsKey(pq.quest_id))
                {
                    questData.progress = questProgress[pq.quest_id].Values.ToList();
                }

                activeQuests.Add(questData);
            }
        }

        return activeQuests;
    }

    // Get quests available from an NPC
    public List<DatabaseQuest> GetQuestsForNPC(int npcId)
    {
        List<DatabaseQuest> quests = new List<DatabaseQuest>();

        if (npcQuests.ContainsKey(npcId))
        {
            foreach (int questId in npcQuests[npcId])
            {
                if (allQuests.ContainsKey(questId))
                {
                    // Only return quests that are not started or failed
                    if (!playerQuests.ContainsKey(questId) ||
                        playerQuests[questId].status == "not_started" ||
                        playerQuests[questId].status == "failed")
                    {
                        quests.Add(allQuests[questId]);
                    }
                }
            }
        }

        return quests;
    }

    // Accept a quest
    public void AcceptQuest(int questId)
    {
        StartCoroutine(AcceptQuestCoroutine(questId));
    }

    IEnumerator AcceptQuestCoroutine(int questId)
    {
        WWWForm form = new WWWForm();
        form.AddField("player_id", currentPlayerId);
        form.AddField("quest_id", questId);
        form.AddField("status", "in_progress");

        using (UnityWebRequest req = UnityWebRequest.Post($"{apiBaseUrl}/player_quests/accept", form))
        {
            yield return req.SendWebRequest();

            if (req.result == UnityWebRequest.Result.Success)
            {
                Debug.Log($"✅ Quest {questId} accepted!");

                // Refresh player quests
                yield return StartCoroutine(FetchPlayerQuests(currentPlayerId));
                yield return StartCoroutine(FetchQuestProgress(currentPlayerId));

                OnQuestAccepted?.Invoke(questId);
            }
            else
            {
                Debug.LogError($"❌ Failed to accept quest: {req.error}");
            }
        }
    }

    // Update quest progress
    public void UpdateQuestProgress(int questId, int objectiveId, int increment = 1)
    {

        StartCoroutine(UpdateQuestProgressCoroutine(questId, objectiveId, increment));
    }

    IEnumerator UpdateQuestProgressCoroutine(int questId, int objectiveId, int increment)
    {
        // Get current progress
        int currentCount = 0;
        if (questProgress.ContainsKey(questId) && questProgress[questId].ContainsKey(objectiveId))
        {
            currentCount = questProgress[questId][objectiveId].current_count;
        }

        int newCount = currentCount + increment;

        WWWForm form = new WWWForm();
        form.AddField("player_id", currentPlayerId);
        form.AddField("quest_id", questId);
        form.AddField("objective_id", objectiveId);
        form.AddField("current_count", newCount);

        using (UnityWebRequest req = UnityWebRequest.Post($"{apiBaseUrl}/quest_progress/update", form))
        {
            yield return req.SendWebRequest();

            if (req.result == UnityWebRequest.Result.Success)
            {
                Debug.Log($"✅ Quest progress updated: {questId} - Objective {objectiveId} -> {newCount}");

                // Update cache
                if (!questProgress.ContainsKey(questId))
                    questProgress[questId] = new Dictionary<int, DatabaseQuestProgress>();

                if (!questProgress[questId].ContainsKey(objectiveId))
                {
                    questProgress[questId][objectiveId] = new DatabaseQuestProgress
                    {
                        player_id = currentPlayerId,
                        quest_id = questId,
                        objective_id = objectiveId,
                        current_count = newCount
                    };
                }
                else
                {
                    questProgress[questId][objectiveId].current_count = newCount;
                }

                OnQuestProgressUpdated?.Invoke(questId, objectiveId, newCount);

                // Check if quest is completed
                CheckQuestCompletion(questId);
            }
            else
            {
                Debug.LogError($"❌ Failed to update quest progress: {req.error}");
            }
        }
    }

    // Check if all objectives are completed
    void CheckQuestCompletion(int questId)
    {
        if (!questObjectives.ContainsKey(questId) || !questProgress.ContainsKey(questId))
            return;

        bool allCompleted = true;
        foreach (var objective in questObjectives[questId])
        {
            if (!questProgress[questId].ContainsKey(objective.objective_id))
            {
                allCompleted = false;
                break;
            }

            int currentCount = questProgress[questId][objective.objective_id].current_count;
            if (currentCount < objective.quantity)
            {
                allCompleted = false;
                break;
            }
        }

        if (allCompleted)
        {
            Debug.Log($"🎉 Quest {questId} completed! Ready to turn in at NPC.");
            // Update player quest status to show ready for turn-in
            if (playerQuests.ContainsKey(questId))
            {
                playerQuests[questId].status = "ready_to_complete";
            }
            // Trigger event to update NPC indicators
            OnQuestProgressUpdated?.Invoke(questId, 0, 0);
        }
    }

    // Check if quest can be turned in (all objectives completed)
    public bool CanCompleteQuest(int questId)
    {
        if (!questObjectives.ContainsKey(questId) || !questProgress.ContainsKey(questId))
            return false;

        foreach (var objective in questObjectives[questId])
        {
            if (!questProgress[questId].ContainsKey(objective.objective_id))
                return false;

            int currentCount = questProgress[questId][objective.objective_id].current_count;
            if (currentCount < objective.quantity)
                return false;
        }

        return true;
    }

    // Complete quest and give rewards
    public void CompleteQuest(int questId)
    {
        StartCoroutine(CompleteQuestCoroutine(questId));
    }

    IEnumerator CompleteQuestCoroutine(int questId)
    {
        // ✅ STEP 1: Remove quest items from inventory BEFORE completing
        RemoveQuestItems(questId);

        WWWForm form = new WWWForm();
        form.AddField("player_id", currentPlayerId);
        form.AddField("quest_id", questId);

        using (UnityWebRequest req = UnityWebRequest.Post($"{apiBaseUrl}/player_quests/complete", form))
        {
            yield return req.SendWebRequest();

            if (req.result == UnityWebRequest.Result.Success)
            {
                Debug.Log($"✅ Quest {questId} turned in and rewards given!");

                // ✅ STEP 2: Give rewards (gold, exp, items)
                GiveQuestRewards(questId);

                // ✅ STEP 3: Refresh data to update UI
                yield return StartCoroutine(FetchPlayerQuests(currentPlayerId));
                yield return StartCoroutine(FetchQuestProgress(currentPlayerId));

                // ✅ STEP 4: Notify all listeners that quest is completed
                OnQuestCompleted?.Invoke(questId);
            }
            else
            {
                Debug.LogError($"❌ Failed to complete quest: {req.error}");
            }
        }
    }

    void GiveQuestRewards(int questId)
    {
        if (!allQuests.ContainsKey(questId))
        {
            Debug.LogError($"❌ Quest {questId} not found for rewards!");
            return;
        }

        var quest = allQuests[questId];

        Debug.Log($"🎁 Giving rewards for quest: {quest.quest_name}");

        // Give gold reward
        if (quest.reward_gold > 0 && EconomyManagement.Instance != null)
        {
            EconomyManagement.Instance.AddGold(quest.reward_gold);
            Debug.Log($"💰 Rewarded {quest.reward_gold} gold");
        }

        // Give EXP reward
        if (quest.reward_exp > 0)
        {
            // TODO: Integrate with player XP system when available
            Debug.Log($"⭐ Rewarded {quest.reward_exp} exp");
        }

        // Give item reward
        if (quest.reward_item_id > 0)
        {
            var rewardItem = DatabaseItemManager.Instance?.FindItemSO(quest.reward_item_id);
            if (rewardItem != null && InventorySystem.Instance != null)
            {
                InventorySystem.Instance.AddItem(rewardItem, 1);
                Debug.Log($"🎁 Rewarded item: {rewardItem.displayName}");
            }
            else
            {
                Debug.LogWarning($"⚠️ Reward item {quest.reward_item_id} not found or inventory not available");
            }
        }
    }
    
    /// <summary>
    /// Remove collected items from inventory when completing quest
    /// </summary>
    void RemoveQuestItems(int questId)
    {
        if (!questObjectives.ContainsKey(questId))
            return;

        if (InventorySystem.Instance == null)
        {
            Debug.LogWarning("⚠️ InventorySystem not available to remove quest items");
            return;
        }

        foreach (var objective in questObjectives[questId])
        {
            // Only remove items for "collect" type objectives
            if (objective.objective_type.ToLower() == "collect")
            {
                var item = DatabaseItemManager.Instance?.FindItemSO(objective.target_id);
                if (item != null)
                {
                    int removed = InventorySystem.Instance.Remove(item, objective.quantity);
                    Debug.Log($"📦 Removed {removed} x {item.displayName} from inventory for quest completion");
                }
                else
                {
                    Debug.LogWarning($"⚠️ Could not find item {objective.target_id} to remove from inventory");
                }
            }
        }
    }

    // Get quest by ID
    public DatabaseQuest GetQuest(int questId)
    {
        return allQuests.ContainsKey(questId) ? allQuests[questId] : null;
    }

    // Get quest objectives
    public List<DatabaseQuestObjective> GetQuestObjectives(int questId)
    {
        return questObjectives.ContainsKey(questId) ? questObjectives[questId] : new List<DatabaseQuestObjective>();
    }

    // Get quest progress
    public int GetObjectiveProgress(int questId, int objectiveId)
    {
        if (questProgress.ContainsKey(questId) && questProgress[questId].ContainsKey(objectiveId))
        {
            return questProgress[questId][objectiveId].current_count;
        }
        return 0;
    }

    public bool IsQuestCompleted(int questId)
    {
        return playerQuests.ContainsKey(questId) && playerQuests[questId].status == "completed";
    }

    public bool IsQuestInProgress(int questId)
    {
        return playerQuests.ContainsKey(questId) && playerQuests[questId].status == "in_progress";
    }

    // Refresh quest progress from database (called after external updates)
    public IEnumerator RefreshQuestProgressFromDB()
    {
        Debug.Log("🔄 Refreshing quest progress from database...");
        yield return StartCoroutine(FetchQuestProgress(currentPlayerId));

        // Trigger event to update UI
        OnQuestsLoaded?.Invoke();
        Debug.Log("✅ Quest progress refreshed and UI notified");
    }

    // ✅ NEW: Notify when item is picked up - updates quest progress via API
    public void NotifyItemPickup(int itemId, int amount)
    {
        Debug.Log($"🔔 QuestManager: Item {itemId} picked up x{amount}");
        StartCoroutine(UpdateItemQuestProgress(itemId, amount));
    }

    IEnumerator UpdateItemQuestProgress(int itemId, int amount)
    {
        Debug.Log($"🔍 [START] UpdateItemQuestProgress - itemId: {itemId}, amount: {amount}");

        string url = $"{apiBaseUrl}/update_progress";

        string jsonBody = "{\"player_id\":" + currentPlayerId + ",\"item_id\":" + itemId + ",\"amount\":" + amount + "}";

        Debug.Log($"🔍 [REQUEST] Sending to {url}");
        Debug.Log($"🔍 [REQUEST] Body: {jsonBody}");

        UnityWebRequest req = new UnityWebRequest(url, "POST");
        byte[] bodyRaw = System.Text.Encoding.UTF8.GetBytes(jsonBody);
        req.uploadHandler = new UploadHandlerRaw(bodyRaw);
        req.downloadHandler = new DownloadHandlerBuffer();
        req.SetRequestHeader("Content-Type", "application/json");
        req.timeout = 10;

        Debug.Log("🔍 [WAITING] Waiting for server response...");
        yield return req.SendWebRequest();
        Debug.Log("🔍 [RECEIVED] Server responded!");

        Debug.Log($"🔍 [RESPONSE] Request result: {req.result}");
        Debug.Log($"🔍 [RESPONSE] Response code: {req.responseCode}");

        if (req.result == UnityWebRequest.Result.Success)
        {
            string responseText = req.downloadHandler.text;
            Debug.Log($"✅ Quest progress API call successful");
            Debug.Log($"🔍 [DEBUG] Server response: {responseText}");

            try
            {
                Debug.Log("🔍 [DEBUG] Attempting to parse response...");
                var response = JsonUtility.FromJson<ItemQuestUpdateResponse>(responseText);
                Debug.Log($"🔍 [DEBUG] Parse successful. Response is null? {response == null}");
                
                if (response != null)
                {
                    Debug.Log($"🔍 [DEBUG] response.updated is null? {response.updated == null}");
                    Debug.Log($"🔍 [DEBUG] response.updated length: {response.updated?.Length ?? -1}");
                }

                if (response != null && response.updated != null && response.updated.Length > 0)
                {
                    Debug.Log($"🎉 Updated {response.updated.Length} quest objectives!");

                    // Cập nhật cache và trigger UI update
                    foreach (var update in response.updated)
                    {
                        Debug.Log($"🔄 Processing update: Quest {update.quest_id}, Objective {update.objective_id} = {update.new_count}/{update.goal}");
                        
                        // Cập nhật cache
                        ApplyQuestProgressUpdate(update.quest_id, update.objective_id, update.new_count, update.goal);
                    }

                    Debug.Log("✅ Quest UI should update now via events!");
                }
                else
                {
                    Debug.Log("ℹ️ No quest objectives were updated (item not related to any active quest)");
                }
            }
            catch (System.Exception e)
            {
                Debug.LogError($"⚠️ Could not parse quest progress response: {e.Message}");
                Debug.LogError($"⚠️ Response was: {responseText}");
            }
        }
        else
        {
            Debug.LogError($"❌ Failed to update quest progress");
            Debug.LogError($"❌ Error: {req.error}");
            Debug.LogError($"❌ Response code: {req.responseCode}");
            if (req.downloadHandler != null)
            {
                Debug.LogError($"❌ Response text: {req.downloadHandler.text}");
            }
        }
    }

    [System.Serializable]
    public class ItemQuestUpdateResponse
    {
        public string status;
        public UpdatedObjectiveData[] updated;
        public string message;
    }

    [System.Serializable]
    public class UpdatedObjectiveData
    {
        public int quest_id;
        public int objective_id;
        public int new_count;
        public int goal;
    }
}
