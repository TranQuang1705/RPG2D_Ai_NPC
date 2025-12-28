using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;
using System.Collections.Generic;

public class TextInputNpcBridge : MonoBehaviour
{
    [Header("Target NPC")]
    public NpcChatSpeaker npcSpeaker;
    public NPC npcComponent;

    [Header("UI References")]
    [Tooltip("TMP_InputField for text input (InputContent/InputText)")]
    public TMP_InputField inputField;

    [Tooltip("Optional: Send button")]
    public Button sendButton;

    [Tooltip("Optional: Output display (can be same as mic output)")]
    public TextMeshProUGUI outputDisplay;

    [Header("Settings")]
    [Tooltip("Minimum characters before sending")]
    public int minChars = 1;

    [Tooltip("Clear input after sending")]
    public bool clearAfterSend = true;

    [Tooltip("Wait until NPC finishes speaking before sending")]
    public bool waitUntilNpcFinished = true;

    [Tooltip("Send cooldown in seconds")]
    public float sendCooldown = 0.3f;

    [Tooltip("Display NPC reply in output")]
    public bool displayNpcReply = true;

    private float _lastSend = -999f;
    private readonly Queue<string> _queue = new Queue<string>();
    private string _lastSent;
    private const int MaxQueue = 50;
    private Coroutine _syncCoroutine;
    private string _lastNpcReply = "";
    [Header("Mic Control")]
    public FreeSpeechToTextToggle mic;
    void Start()
    {
        if (inputField == null)
        {
            inputField = GetComponentInChildren<TMP_InputField>();
        }

        if (inputField != null)
        {
            // Send on Enter key
            inputField.onSubmit.AddListener(OnInputSubmit);

            Debug.Log($"✅ TextInputNpcBridge: Input field connected");
        }
        else
        {
            Debug.LogError("❌ TextInputNpcBridge: No TMP_InputField found! Please assign in inspector.");
        }

        // Setup send button
        if (sendButton != null)
        {
            sendButton.onClick.AddListener(OnSendButtonClicked);
        }

        // Auto-detect NPC component
        if (npcSpeaker != null && npcComponent == null)
        {
            npcComponent = npcSpeaker.GetComponent<NPC>();
            if (npcComponent != null)
            {
                Debug.Log($"✅ TextInputNpcBridge: Auto-detected NPC component on {npcSpeaker.name}");
            }
        }
    }

    void OnEnable()
    {
        if (npcSpeaker != null)
        {
            npcSpeaker.OnSpeakStart += OnNpcStart;
            npcSpeaker.OnSpeakEnd += OnNpcEnd;
        }
    }

    void OnDisable()
    {
        if (npcSpeaker != null)
        {
            npcSpeaker.OnSpeakStart -= OnNpcStart;
            npcSpeaker.OnSpeakEnd -= OnNpcEnd;
        }
    }


    void Update()
    {
        TryFlushQueue();
    }


    private void OnInputSubmit(string text)
    {
        SendTextToNpc(text);

        if (inputField != null)
        {
            inputField.text = "";
            StartCoroutine(ReactivateInputField());
        }
    }


    private void OnSendButtonClicked()
    {
        if (inputField != null)
        {
            SendTextToNpc(inputField.text);

            if (clearAfterSend)
            {
                inputField.text = "";
            }

            inputField.Select();
            inputField.ActivateInputField();
        }
    }


    public void SendTextToNpc(string text)
    {
        if (!npcSpeaker)
        {
            Debug.LogWarning("⚠️ TextInputNpcBridge: No NPC speaker assigned!");
            return;
        }

        text = (text ?? "").Trim();


        if (text.Length < minChars)
        {
            Debug.Log($"📝 TextInputNpcBridge: Text too short ({text.Length} < {minChars})");
            return;
        }

        if (text == _lastSent)
        {
            Debug.Log("📝 TextInputNpcBridge: Duplicate text, ignoring");
            return;
        }

        bool inCooldown = (Time.unscaledTime - _lastSend < sendCooldown);
        bool npcBusy = waitUntilNpcFinished && npcSpeaker.IsSpeaking;

        if (inCooldown || npcBusy)
        {
            if (_queue.Count < MaxQueue)
            {
                _queue.Enqueue(text);
                Debug.Log($"📝 TextInputNpcBridge: Queued text (cooldown: {inCooldown}, npcBusy: {npcBusy})");

                if (outputDisplay != null)
                {
                    outputDisplay.text = $"[Queued] {text}";
                }
            }
            return;
        }

        SendNow(text);
        _lastSent = text;
    }

    private void SendNow(string text)
    {
        text = (text ?? "").Trim();
        if (text.Length < minChars) return;

        _lastSend = Time.unscaledTime;

        Debug.Log($"📝 TextInputNpcBridge: Sending to NPC: \"{text}\"");

        if (outputDisplay != null)
        {
            outputDisplay.text = $"You: {text}";
        }

        if (npcComponent != null)
        {
            npcComponent.Say(text);
        }
        else
        {
            npcSpeaker.SpeakFromText(text);
        }
    }

    private void OnNpcStart()
    {
        Debug.Log("📝 TextInputNpcBridge: NPC started speaking");

        _lastNpcReply = "";

        if (_syncCoroutine != null)
        {
            StopCoroutine(_syncCoroutine);
            _syncCoroutine = null;
        }

        if (displayNpcReply && npcSpeaker && npcSpeaker.subtitleTMP)
        {
            _syncCoroutine = StartCoroutine(SyncNpcReplyToOutput());
        }
        if (mic != null)
        {
            Debug.Log("🛑 NPC speaking → Stop mic");
            mic.StopListening();
        }

    }

    private void OnNpcEnd()
    {
        Debug.Log("📝 TextInputNpcBridge: NPC finished speaking");

        if (_syncCoroutine != null)
        {
            StopCoroutine(_syncCoroutine);
            _syncCoroutine = null;
        }
        if (mic != null)
        {
            Debug.Log("🎤 NPC done → Restart mic");
            StartCoroutine(RestartMic());
        }

        TryFlushQueue();
    }
    IEnumerator RestartMic()
    {
        yield return new WaitForSeconds(0.3f);
        mic.StartListening();
    }
    private IEnumerator SyncNpcReplyToOutput()
    {
        yield return new WaitForSeconds(0.1f);

        while (npcSpeaker && npcSpeaker.IsSpeaking)
        {
            if (npcSpeaker.subtitleTMP != null && outputDisplay != null)
            {
                string npcText = npcSpeaker.subtitleTMP.text;

                if (!string.IsNullOrEmpty(npcText) && npcText != _lastNpcReply)
                {
                    _lastNpcReply = npcText;
                    outputDisplay.text = npcText;
                }
            }

            yield return new WaitForSeconds(0.1f);
        }

        _syncCoroutine = null;
    }

    private void TryFlushQueue()
    {
        if (_queue.Count == 0) return;
        if (Time.unscaledTime - _lastSend < sendCooldown) return;
        if (waitUntilNpcFinished && npcSpeaker && npcSpeaker.IsSpeaking) return;

        var text = _queue.Dequeue();
        SendNow(text);
        _lastSent = text;

        Debug.Log($"📝 TextInputNpcBridge: Flushed from queue: \"{text}\"");
    }

    private IEnumerator ReactivateInputField()
    {
        yield return null;
        if (inputField != null && inputField.gameObject.activeInHierarchy)
        {
            inputField.ActivateInputField();
        }
    }

    public void SendText(string text)
    {
        SendTextToNpc(text);
    }

    public void UpdateOutputDisplay(string text)
    {
        if (outputDisplay != null)
        {
            outputDisplay.text = text;
        }
    }
}
