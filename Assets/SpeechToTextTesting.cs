using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.Windows.Speech;
using UnityEngine.Networking;
using System.Collections;
using System.Text;
using System;
    
public class FreeSpeechToTextToggle : MonoBehaviour
{
    [Header("UI References")]
    public TextMeshProUGUI outputTMP;     // nơi hiển thị text mic
    public Button toggleButton;           // nút Start/Stop
    public TextMeshProUGUI buttonLabel;   // label nút

    [Header("Optional: gửi thẳng lên bot (KHÔNG khuyến nghị khi dùng MicNpcBridge)")]
    public bool forwardToBot = false;     // hãy để false nếu đã dùng MicNpcBridge
    [SerializeField] private string flaskUrl = "http://127.0.0.1:5000/chat";

    // 🔔 Event bắn ra khi có kết quả cuối từ mic
    public static event Action<string> OnFinalTranscript;

    private DictationRecognizer dictationRecognizer;
    private bool isListening = false;

    void Start()
    {
        dictationRecognizer = new DictationRecognizer();

        dictationRecognizer.DictationHypothesis += (text) =>
        {
            if (outputTMP) outputTMP.text = text;
        };

        dictationRecognizer.DictationResult += (text, confidence) =>
        {
            if (outputTMP) outputTMP.text = text;
            OnFinalTranscript?.Invoke(text);             // chỉ phát khi có kết quả CUỐI
            if (forwardToBot) StartCoroutine(AskLocalBot(text)); // KHÔNG dùng nếu bridge đã lo gửi
        };

        dictationRecognizer.DictationError += (error, hresult) =>
        {
            if (outputTMP) outputTMP.text = $"Lỗi: {error} (HResult: {hresult})";
            StopListening();
        };

        if (toggleButton) toggleButton.onClick.AddListener(ToggleListening);
        if (buttonLabel) buttonLabel.text = "Start";
    }

    void ToggleListening()
    {
        if (isListening) StopListening();
        else StartListening();
    }

    // ĐÃ CHUYỂN THÀNH public để Bridge gọi
    public void StartListening()
    {
        if (dictationRecognizer.Status != SpeechSystemStatus.Running)
            dictationRecognizer.Start();

        isListening = true;
        if (buttonLabel) buttonLabel.text = "Stop";
        if (outputTMP) outputTMP.text = "Đang lắng nghe...";
    }

    // ĐÃ CHUYỂN THÀNH public để Bridge gọi
    public void StopListening()
    {
        if (dictationRecognizer.Status == SpeechSystemStatus.Running)
            dictationRecognizer.Stop();

        isListening = false;
        if (buttonLabel) buttonLabel.text = "Start";
        if (outputTMP) outputTMP.text = "Đã dừng nghe.";
    }

    void OnDestroy()
    {
        if (dictationRecognizer != null)
        {
            if (dictationRecognizer.Status == SpeechSystemStatus.Running)
                dictationRecognizer.Stop();
            dictationRecognizer.Dispose();
        }
    }

    // ====== Gửi text tới Flask server (nếu forwardToBot = true) ======
    [System.Serializable] public class ChatPayload { public string text; }
    [System.Serializable] public class BotReply { public string reply; public string audio_url; public string error; }
    [System.Serializable] public class Message { public string role; public string content; }
    [System.Serializable] public class Choice { public int index; public Message message; public string finish_reason; }
    [System.Serializable] public class OllamaResponse { public string id; public string model; public Choice[] choices; }

    IEnumerator AskLocalBot(string userInput)
    {
        var payload = new ChatPayload { text = userInput ?? "" };
        string jsonBody = JsonUtility.ToJson(payload);

        using (UnityWebRequest req = new UnityWebRequest(flaskUrl, "POST"))
        {
            byte[] bodyRaw = Encoding.UTF8.GetBytes(jsonBody);
            req.uploadHandler = new UploadHandlerRaw(bodyRaw);
            req.downloadHandler = new DownloadHandlerBuffer();
            req.SetRequestHeader("Content-Type", "application/json");
            req.SetRequestHeader("Accept", "application/json");
            req.timeout = 15;

            yield return req.SendWebRequest();

            if (req.result == UnityWebRequest.Result.Success)
            {
                string result = req.downloadHandler.text;
                Debug.Log("🔵 Raw JSON từ Flask/Ollama:\n" + result);

                BotReply simple = null;
                try { simple = JsonUtility.FromJson<BotReply>(result); } catch {}

                if (simple != null && !string.IsNullOrEmpty(simple.reply))
                {
                    if (outputTMP) outputTMP.text = simple.reply;
                    Debug.Log("🟢 NPC Reply (reply): " + simple.reply);
                    yield break;
                }

                // Fallback kiểu OpenAI/Ollama
                OllamaResponse responseObj = null;
                try { responseObj = JsonUtility.FromJson<OllamaResponse>(result); } catch {}

                if (responseObj != null &&
                    responseObj.choices != null &&
                    responseObj.choices.Length > 0 &&
                    responseObj.choices[0].message != null)
                {
                    string reply = responseObj.choices[0].message.content.Trim();
                    if (outputTMP) outputTMP.text = reply;
                    Debug.Log("🟢 NPC Reply (choices[0].message.content): " + reply);
                }
                else
                {
                    if (outputTMP) outputTMP.text = "⚠️ Không tìm thấy reply.";
                    Debug.LogWarning("⚠️ JSON không có field 'reply' hoặc 'choices[0].message.content'.");
                }
            }
            else
            {
                if (outputTMP) outputTMP.text = "❌ HTTP " + req.responseCode + " - " + req.error;
                Debug.LogError($"HTTP {req.responseCode}: {req.error}\nBody: {req.downloadHandler.text}");
            }
        }
    }
}
