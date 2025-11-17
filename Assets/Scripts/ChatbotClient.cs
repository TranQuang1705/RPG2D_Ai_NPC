using UnityEngine;
using UnityEngine.Networking;
using System.Collections;
using System.Collections.Generic;

/// <summary>
/// Chatbot Client - Handles communication with ChatBox.py Flask server
/// Sends player messages and receives AI responses with actions
/// </summary>
public class ChatbotClient : MonoBehaviour
{
    public static ChatbotClient Instance { get; private set; }

    [Header("Chatbot Server Configuration")]
    [SerializeField] private string chatbotUrl = "http://127.0.0.1:5000/chat";
    [SerializeField] private string sessionId = "default";

    [Header("Current NPC")]
    private NPC currentNPC;

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

    /// <summary>
    /// Send message to chatbot and get response
    /// </summary>
    public void SendMessage(string message, NPC npc, string questContext = null, string npcContext = null)
    {
        if (string.IsNullOrEmpty(message))
        {
            Debug.LogWarning("⚠️ ChatbotClient: Empty message, skipping");
            return;
        }

        currentNPC = npc;
        StartCoroutine(SendMessageCoroutine(message, questContext, npcContext));
    }

    IEnumerator SendMessageCoroutine(string message, string questContext = null, string npcContext = null)
    {
        Debug.Log($"💬 ChatbotClient: Sending message: {message}");
        if (!string.IsNullOrEmpty(questContext))
            Debug.Log($"📜 ChatbotClient: Quest context: {questContext}");
        if (!string.IsNullOrEmpty(npcContext))
            Debug.Log($"🎭 ChatbotClient: NPC context: {npcContext}");

        // Create request payload
        var requestData = new ChatbotRequest
        {
            text = message,
            session_id = sessionId,
            quest_context = questContext,
            npc_context = npcContext
        };

        string jsonData = JsonUtility.ToJson(requestData);
        Debug.Log($"📤 ChatbotClient: JSON being sent: {jsonData}");

        using (UnityWebRequest req = new UnityWebRequest(chatbotUrl, "POST"))
        {
            byte[] bodyRaw = System.Text.Encoding.UTF8.GetBytes(jsonData);
            req.uploadHandler = new UploadHandlerRaw(bodyRaw);
            req.downloadHandler = new DownloadHandlerBuffer();
            req.SetRequestHeader("Content-Type", "application/json");

            yield return req.SendWebRequest();

            if (req.result == UnityWebRequest.Result.Success)
            {
                string responseJson = req.downloadHandler.text;
                Debug.Log($"✅ ChatbotClient: Response received: {responseJson}");

                // Parse response
                ChatbotResponse response = JsonUtility.FromJson<ChatbotResponse>(responseJson);

                if (response != null)
                {
                    HandleChatbotResponse(response);
                }
                else
                {
                    Debug.LogError("❌ ChatbotClient: Failed to parse response");
                }
            }
            else
            {
                Debug.LogError($"❌ ChatbotClient: Request failed: {req.error}");
            }
        }
    }

    void HandleChatbotResponse(ChatbotResponse response)
    {
        Debug.Log($"🤖 Chatbot Reply: {response.reply}");
        Debug.Log($"🎯 Intent: {response.intent}");
        Debug.Log($"🎮 Action: {response.action}");

        // Send reply text to NPC (for display or TTS)
        if (currentNPC != null)
        {
            currentNPC.SpeakResponse(response.reply);
        }

        // Handle action if any
        if (!string.IsNullOrEmpty(response.action) && response.action != "NONE")
        {
            HandleAction(response.action, response.parameters);
        }
    }

    void HandleAction(string action, ChatbotParameters parameters)
    {
        Debug.Log($"🎮 ChatbotClient: Executing action '{action}'");

        // Convert parameters to dictionary for NPC handler
        Dictionary<string, object> paramDict = new Dictionary<string, object>();

        if (parameters != null)
        {
            if (!string.IsNullOrEmpty(parameters.target))
                paramDict["target"] = parameters.target;
            
            if (!string.IsNullOrEmpty(parameters.target_label))
                paramDict["target_label"] = parameters.target_label;
            
            if (!string.IsNullOrEmpty(parameters.shop_id))
                paramDict["shop_id"] = parameters.shop_id;
            
            if (!string.IsNullOrEmpty(parameters.name))
                paramDict["name"] = parameters.name;
            
            if (!string.IsNullOrEmpty(parameters.trigger))
                paramDict["trigger"] = parameters.trigger;
            
            paramDict["open_quest_panel"] = parameters.open_quest_panel;
        }

        // Let NPC handle the action
        if (currentNPC != null)
        {
            currentNPC.HandleChatbotAction(action, paramDict);
        }

        // Global action handlers (not NPC-specific)
        switch (action)
        {
            case "SHOW_QUEST_STATUS":
                OpenQuestPanel();
                break;

            case "NAVIGATE":
                if (paramDict.ContainsKey("target"))
                {
                    string target = paramDict["target"].ToString();
                    Debug.Log($"🗺️ Navigate to: {target}");
                    // TODO: Implement navigation marker
                }
                break;

            case "OPEN_SHOP":
                if (paramDict.ContainsKey("shop_id"))
                {
                    string shopId = paramDict["shop_id"].ToString();
                    Debug.Log($"🏪 Opening shop: {shopId}");
                    // TODO: Implement shop opening
                }
                break;

            case "START_COMBAT":
                Debug.Log($"⚔️ Starting combat...");
                // TODO: Implement combat start
                break;

            case "ANIM":
                if (paramDict.ContainsKey("name") && currentNPC != null)
                {
                    string animName = paramDict["name"].ToString();
                    Debug.Log($"🎭 Playing animation: {animName}");
                    // TODO: Play animation on NPC
                }
                break;
        }
    }

    void OpenQuestPanel()
    {
        var questPanel = FindObjectOfType<QuestPanel>();
        if (questPanel != null)
        {
            questPanel.OpenQuestDetail();
            Debug.Log("📋 Quest panel opened");
        }
        else
        {
            Debug.LogWarning("⚠️ QuestPanel not found in scene");
        }
    }

    /// <summary>
    /// Reset chatbot conversation history
    /// </summary>
    public void ResetConversation()
    {
        StartCoroutine(ResetConversationCoroutine());
    }

    IEnumerator ResetConversationCoroutine()
    {
        string resetUrl = "http://127.0.0.1:5000/reset";
        
        var requestData = new ChatbotRequest
        {
            session_id = sessionId
        };

        string jsonData = JsonUtility.ToJson(requestData);

        using (UnityWebRequest req = new UnityWebRequest(resetUrl, "POST"))
        {
            byte[] bodyRaw = System.Text.Encoding.UTF8.GetBytes(jsonData);
            req.uploadHandler = new UploadHandlerRaw(bodyRaw);
            req.downloadHandler = new DownloadHandlerBuffer();
            req.SetRequestHeader("Content-Type", "application/json");

            yield return req.SendWebRequest();

            if (req.result == UnityWebRequest.Result.Success)
            {
                Debug.Log("✅ Chatbot conversation reset");
            }
            else
            {
                Debug.LogError($"❌ Failed to reset conversation: {req.error}");
            }
        }
    }

    /// <summary>
    /// Set current NPC for conversation context
    /// </summary>
    public void SetCurrentNPC(NPC npc)
    {
        currentNPC = npc;
    }
}

// ========== DATA CLASSES ==========

[System.Serializable]
public class ChatbotRequest
{
    public string text;
    public string session_id;
    public string quest_context; // Quest details for contextual conversation
    public string npc_context; // NPC activity/state info
}

[System.Serializable]
public class ChatbotResponse
{
    public string reply;
    public string audio_url;
    public string intent;
    public string action;
    public ChatbotParameters parameters;

    // Alternative field name that might be returned
    public ChatbotParameters @params;

    // Getter that checks both field names
    public ChatbotParameters parameters_safe
    {
        get { return parameters ?? @params; }
    }
}

[System.Serializable]
public class ChatbotParameters
{
    public string target;
    public string target_label;
    public string shop_id;
    public string name;
    public string trigger;
    public bool open_quest_panel;
}
