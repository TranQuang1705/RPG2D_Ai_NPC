using UnityEngine;
using UnityEngine.Networking;
using TMPro;
using System.Collections;

[RequireComponent(typeof(AudioSource))]
public class NpcChatSpeaker : MonoBehaviour
{
    [Header("Server")]
    public string chatUrl = "http://127.0.0.1:5000/chat";

    [Header("NPC UI & Audio")]
    public TextMeshProUGUI subtitleTMP;
    public AudioSource npcAudio;
    public bool interruptPrevious = true;

    [Header("Typing Animation")]
    [Tooltip("Enable typing animation effect")]
    public bool useTypingAnimation = true;
    [Tooltip("Characters per second for typing effect (auto-synced with audio if available)")]
    public float typingSpeed = 30f;
    
    [Header("Text Overflow")]
    [Tooltip("Auto clear text when it overflows the box")]
    public bool autoHandleOverflow = true;

    public bool IsSpeaking => npcAudio && npcAudio.isPlaying;
    public System.Action OnSpeakStart;
    public System.Action OnSpeakEnd;

    private int latestResponseId = 0;
    private Coroutine currentAudioCo;
    private Coroutine currentTypingCo;

    [SerializeField] private string sessionId; 

    private NavActionHandler navHandler;
    
    private NPC npcComponent;

    void Awake()
    {
        if (string.IsNullOrEmpty(sessionId))
        {
            sessionId = SystemInfo.deviceUniqueIdentifier + "_" + gameObject.name;
        }

        navHandler = FindObjectOfType<NavActionHandler>();
        
        npcComponent = GetComponent<NPC>();
    }

    void Reset() { npcAudio = GetComponent<AudioSource>(); }

    public void SpeakFromText(string userText, string questContext = null, string npcContext = null)
    {
        if (string.IsNullOrWhiteSpace(userText)) return;
        StartCoroutine(CoAskServer(userText, questContext, npcContext));
    }

    public void StopSpeaking()
    {
        if (npcAudio && npcAudio.isPlaying) npcAudio.Stop();
        OnSpeakEnd?.Invoke();
    }

    private IEnumerator CoAskServer(string userText, string questContext = null, string npcContext = null)
    {
        string payload = "{\"text\":\"" + EscapeJson(userText) + 
                         "\",\"session_id\":\"" + EscapeJson(sessionId) + "\"";
        
        if (!string.IsNullOrEmpty(questContext))
        {
            payload += ",\"quest_context\":\"" + EscapeJson(questContext) + "\"";
        }
        
        if (!string.IsNullOrEmpty(npcContext))
        {
            payload += ",\"npc_context\":\"" + EscapeJson(npcContext) + "\"";
        }
        
        payload += "}";
        
        using (UnityWebRequest req = new UnityWebRequest(chatUrl, "POST"))
        {
            byte[] bodyRaw = System.Text.Encoding.UTF8.GetBytes(payload);
            req.uploadHandler = new UploadHandlerRaw(bodyRaw);
            req.downloadHandler = new DownloadHandlerBuffer();
            req.SetRequestHeader("Content-Type", "application/json");

            yield return req.SendWebRequest();

            if (req.result != UnityWebRequest.Result.Success)
            {
                OnSpeakEnd?.Invoke();
                yield break;
            }

            string json = req.downloadHandler.text;
            var br = JsonUtility.FromJson<BotReply>(json);
            if (br == null)
            {
                OnSpeakEnd?.Invoke();
                yield break;
            }

            string replyText = br.reply;

            if (!string.IsNullOrEmpty(br.action))
            {
                
                bool isNpcAction = IsNpcSpecificAction(br.action);
                
                if (isNpcAction && npcComponent != null)
                {
                    var parameters = new System.Collections.Generic.Dictionary<string, object>();
                    
                    if (br.@params != null)
                    {
                        if (!string.IsNullOrEmpty(br.@params.target))
                            parameters["target"] = br.@params.target;
                        if (!string.IsNullOrEmpty(br.@params.target_label))
                            parameters["target_label"] = br.@params.target_label;
                        if (!string.IsNullOrEmpty(br.@params.location))
                            parameters["location"] = br.@params.location;
                        if (!string.IsNullOrEmpty(br.@params.item))
                            parameters["item"] = br.@params.item;
                    }
                    
                    npcComponent.HandleChatbotAction(br.action, parameters);
                }
                else
                {
                    if (navHandler == null) navHandler = FindObjectOfType<NavActionHandler>();
                    if (navHandler != null)
                    {
                        navHandler.HandleServerAction(new ServerResponse
                        {
                            action = br.action,
                            intent = br.intent,
                            reply = br.reply,
                            @params = br.@params
                        });
                    }
                    else
                    {
                        Debug.LogWarning(" NavActionHandler not found for global action!");
                    }
                }
            }

            if (!string.IsNullOrEmpty(br.audio_url))
            {
                latestResponseId++;
                int thisId = latestResponseId;

                if (interruptPrevious && currentAudioCo != null)
                {
                    StopCoroutine(currentAudioCo);
                    currentAudioCo = null;
                    if (npcAudio && npcAudio.isPlaying) npcAudio.Stop();
                    OnSpeakEnd?.Invoke();
                }
                
                if (currentTypingCo != null)
                {
                    StopCoroutine(currentTypingCo);
                    currentTypingCo = null;
                }

                string absolute = EnsureAbsoluteUrl(br.audio_url);
                currentAudioCo = StartCoroutine(CoDownloadAndPlay(absolute, thisId, replyText));
            }
            else
            {
                if (!string.IsNullOrEmpty(replyText) && subtitleTMP != null)
                {
                    string emotion;
                    string cleanText = ExtractEmotionMarkers(replyText, out emotion);
                    subtitleTMP.text = $"{cleanText}";
                }
                OnSpeakEnd?.Invoke();
            }
        }
    }

    private IEnumerator CoDownloadAndPlay(string url, int id, string replyText)
    {
        string finalUrl = url + ((url.Contains("?") ? "&" : "?") + "t=" + System.DateTime.UtcNow.Ticks);
        string urlWithoutQuery = url.Split('?')[0];

        using (UnityWebRequest uwr = new UnityWebRequest(finalUrl, UnityWebRequest.kHttpVerbGET))
        {
            AudioType audioType = AudioType.MPEG; 
            if (urlWithoutQuery.EndsWith(".wav")) audioType = AudioType.WAV;
            else if (urlWithoutQuery.EndsWith(".ogg")) audioType = AudioType.OGGVORBIS;
            
            uwr.downloadHandler = new DownloadHandlerAudioClip(urlWithoutQuery, audioType);
            uwr.SetRequestHeader("Cache-Control", "no-cache, no-store, must-revalidate");
            uwr.SetRequestHeader("Pragma", "no-cache");

            yield return uwr.SendWebRequest();

            if (id != latestResponseId) yield break;
            if (uwr.result != UnityWebRequest.Result.Success)
            {
                OnSpeakEnd?.Invoke();
                yield break;
            }

            var clip = DownloadHandlerAudioClip.GetContent(uwr);
            if (!clip || !npcAudio)
            {
                OnSpeakEnd?.Invoke();
                yield break;
            }

            npcAudio.spatialBlend = 0f;
            npcAudio.volume = 1f;
            npcAudio.Stop();
            npcAudio.clip = clip;
            npcAudio.loop = false;

            OnSpeakStart?.Invoke();
            
            // Start typing animation synchronized with audio
            if (!string.IsNullOrEmpty(replyText))
            {
                currentTypingCo = StartCoroutine(TypeText(replyText, clip.length));
            }
            
            npcAudio.Play();

            try
            {
                float timeout = Mathf.Max(clip.length + 0.3f, 0.6f);
                float t = 0f;
                while (t < timeout && id == latestResponseId && npcAudio.isPlaying)
                {
                    t += Time.unscaledDeltaTime;
                    yield return null;
                }
            }
            finally
            {
                if (npcAudio.clip == clip) npcAudio.clip = null;
#if UNITY_2020_1_OR_NEWER
                if (clip) Destroy(clip);
#endif
                OnSpeakEnd?.Invoke();
            }
        }
    }

    private string EnsureAbsoluteUrl(string u)
    {
        if (string.IsNullOrEmpty(u)) return u;
        if (u.StartsWith("http")) return u;
        return "http://127.0.0.1:5000" + (u.StartsWith("/") ? "" : "/") + u;
    }

    private string EscapeJson(string s)
    {
        return s.Replace("\\", "\\\\").Replace("\"", "\\\"")
                .Replace("\n", "\\n").Replace("\r", "\\r");
    }

    /// <summary>
    /// Extract emotion markers from text (e.g., **happy**, **nervous**)
    /// Also removes any content between * marks (actions/emotions)
    /// Returns the emotion found (or null) and the clean text without markers
    /// </summary>
    private string ExtractEmotionMarkers(string text, out string emotion)
    {
        emotion = null;
        if (string.IsNullOrEmpty(text)) return text;

        // Find emotion markers like **happy**, **nervous**, etc.
        var match = System.Text.RegularExpressions.Regex.Match(text, @"\*\*(\w+)\*\*");
        if (match.Success)
        {
            emotion = match.Groups[1].Value;
        }

        // Remove all content between ** markers (emotions)
        string cleanText = System.Text.RegularExpressions.Regex.Replace(text, @"\*\*[^*]+\*\*", "");
        
        // Remove all content between single * markers (actions/descriptions)
        cleanText = System.Text.RegularExpressions.Regex.Replace(cleanText, @"\*[^*]+\*", "");
        
        // Clean up extra spaces and trim
        cleanText = System.Text.RegularExpressions.Regex.Replace(cleanText, @"\s+", " ");
        return cleanText.Trim();
    }

    /// <summary>
    /// Display text with typing animation effect synchronized with audio length
    /// </summary>
    private IEnumerator TypeText(string fullText, float audioDuration)
    {
        if (subtitleTMP == null) yield break;

        // Extract and remove emotion markers
        string emotion;
        string displayText = ExtractEmotionMarkers(fullText, out emotion);
        
        if (!string.IsNullOrEmpty(emotion))
        {
            Debug.Log($"[NPC Emotion] Detected: {emotion} (will be used for avatar in future)");
            // TODO: In future, trigger emotion avatar based on 'emotion' variable
        }

        // Add NPC name prefix
        displayText = $"{displayText}";

        if (!useTypingAnimation || audioDuration <= 0)
        {
            // No animation - show all text immediately
            subtitleTMP.text = displayText;
            yield break;
        }

        // Calculate typing speed based on audio duration for perfect sync
        float charsPerSecond = displayText.Length / audioDuration;
        
        // Clamp to reasonable range (not too fast or slow)
        charsPerSecond = Mathf.Clamp(charsPerSecond, 10f, 50f);

        subtitleTMP.text = "";
        int startIndex = 0; // Track where we start typing from after overflow
        
        for (int i = 1; i <= displayText.Length; i++)
        {
            // Get substring from current start index
            int length = i - startIndex;
            if (length > displayText.Length - startIndex)
                length = displayText.Length - startIndex;
                
            string currentText = displayText.Substring(startIndex, length);
            subtitleTMP.text = currentText;
            
            // Check for overflow after updating text (but not on the last character)
            if (autoHandleOverflow && i < displayText.Length && IsTextOverflowing())
            {
                Debug.Log($"[NPC Text] Overflow detected at position {i}! Clearing text and continuing from here.");
                // Clear the text and restart from current position
                subtitleTMP.text = "";
                startIndex = i; // Next iteration will start from here
            }
            
            // Wait based on typing speed
            yield return new WaitForSeconds(1f / charsPerSecond);
        }
    }

    /// <summary>
    /// Check if TextMeshPro text is overflowing the bounds
    /// </summary>
    private bool IsTextOverflowing()
    {
        if (subtitleTMP == null) return false;

        // Force TextMeshPro to update its mesh info
        subtitleTMP.ForceMeshUpdate();

        // Check if text is being truncated (overflow)
        return subtitleTMP.isTextOverflowing;
    }
    
    /// <summary>
    /// Phân loại action: NPC-specific hay global
    /// </summary>
    private bool IsNpcSpecificAction(string action)
    {
        switch (action)
        {
            // NPC-specific actions
            case "GATHER_FLOWER":
            case "ASK_FOR_QUEST":
            case "QUEST_DIALOGUE":
            case "ACCEPT_QUEST_CONFIRM":
            case "COMPLETE_QUEST":
            case "SHOW_QUEST_STATUS":
            case "ANIM":
                return true;
            
            // Global actions (handled by NavActionHandler)
            case "NAVIGATE":
            case "START_COMBAT":
            case "OPEN_SHOP":
            case "NONE":
            default:
                return false;
        }
    }

    // 🔹 Model phản hồi mở rộng đầy đủ
    [System.Serializable]
    private class BotReply
    {
        public string reply;
        public string audio_url;
        public string action;
        public string intent;
        public ResponseParams @params;
        public string error;
    }
}
