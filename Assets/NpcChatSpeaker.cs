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

    public bool IsSpeaking => npcAudio && npcAudio.isPlaying;
    public System.Action OnSpeakStart;
    public System.Action OnSpeakEnd;

    private int latestResponseId = 0;
    private Coroutine currentAudioCo;

    // NEW: session id ổn định cho NPC này
    [SerializeField] private string sessionId; 

    // NEW: liên kết với NavActionHandler
    private NavActionHandler navHandler;

    void Awake()
    {
        if (string.IsNullOrEmpty(sessionId))
        {
            sessionId = SystemInfo.deviceUniqueIdentifier + "_" + gameObject.name;
        }

        // Tự tìm NavActionHandler trong scene
        navHandler = FindObjectOfType<NavActionHandler>();
    }

    void Reset() { npcAudio = GetComponent<AudioSource>(); }

    public void SpeakFromText(string userText)
    {
        if (string.IsNullOrWhiteSpace(userText)) return;
        StartCoroutine(CoAskServer(userText));
    }

    public void StopSpeaking()
    {
        if (npcAudio && npcAudio.isPlaying) npcAudio.Stop();
        OnSpeakEnd?.Invoke();
    }

    private IEnumerator CoAskServer(string userText)
    {
        string payload = "{\"text\":\"" + EscapeJson(userText) + "\",\"session_id\":\"" + EscapeJson(sessionId) + "\"}";

        using (UnityWebRequest req = new UnityWebRequest(chatUrl, "POST"))
        {
            byte[] bodyRaw = System.Text.Encoding.UTF8.GetBytes(payload);
            req.uploadHandler = new UploadHandlerRaw(bodyRaw);
            req.downloadHandler = new DownloadHandlerBuffer();
            req.SetRequestHeader("Content-Type", "application/json");

            yield return req.SendWebRequest();

            if (req.result != UnityWebRequest.Result.Success)
            {
                Debug.LogError("[NPC] Chat request failed: " + req.error);
                OnSpeakEnd?.Invoke();
                yield break;
            }

            string json = req.downloadHandler.text;
            var br = JsonUtility.FromJson<BotReply>(json);
            if (br == null)
            {
                Debug.LogError("[NPC] Invalid JSON response!");
                OnSpeakEnd?.Invoke();
                yield break;
            }

            // 💬 Hiển thị câu trả lời
            if (!string.IsNullOrEmpty(br.reply))
            {
                if (subtitleTMP) subtitleTMP.text = $"Snow: {br.reply}";
                Debug.Log($"💬 NPC Reply: {br.reply}");
            }

            // ⚙️ NEW: Nếu Flask trả về action (NAVIGATE, COMBAT, SHOP...)
            if (!string.IsNullOrEmpty(br.action))
            {
                Debug.Log($"🧭 Server action received: {br.action}");
                if (navHandler == null) navHandler = FindObjectOfType<NavActionHandler>();
                if (navHandler != null)
                {
                    // Gọi trực tiếp vào NavActionHandler
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
                    Debug.LogWarning("⚠️ NavActionHandler not found in scene!");
                }
            }

            // 🔊 Phát âm thanh trả lời
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

                string absolute = EnsureAbsoluteUrl(br.audio_url);
                currentAudioCo = StartCoroutine(CoDownloadAndPlay(absolute, thisId));
            }
            else
            {
                OnSpeakEnd?.Invoke();
            }
        }
    }

    private IEnumerator CoDownloadAndPlay(string url, int id)
    {
        string finalUrl = url + ((url.Contains("?") ? "&" : "?") + "t=" + System.DateTime.UtcNow.Ticks);
        string urlWithoutQuery = url.Split('?')[0];

        using (UnityWebRequest uwr = new UnityWebRequest(finalUrl, UnityWebRequest.kHttpVerbGET))
        {
            uwr.downloadHandler = new DownloadHandlerAudioClip(urlWithoutQuery, AudioType.MPEG);
            uwr.SetRequestHeader("Cache-Control", "no-cache, no-store, must-revalidate");
            uwr.SetRequestHeader("Pragma", "no-cache");

            yield return uwr.SendWebRequest();

            if (id != latestResponseId) yield break;
            if (uwr.result != UnityWebRequest.Result.Success)
            {
                Debug.LogError($"[NPC] Audio download failed: {uwr.error}");
                OnSpeakEnd?.Invoke();
                yield break;
            }

            var clip = DownloadHandlerAudioClip.GetContent(uwr);
            if (!clip || !npcAudio)
            {
                Debug.LogWarning("[NPC] No valid audio clip or AudioSource missing.");
                OnSpeakEnd?.Invoke();
                yield break;
            }

            npcAudio.spatialBlend = 0f;
            npcAudio.volume = 1f;
            npcAudio.Stop();
            npcAudio.clip = clip;
            npcAudio.loop = false;

            OnSpeakStart?.Invoke();
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
