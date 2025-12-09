using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class MicNpcBridge : MonoBehaviour
{
    [Header("Target NPC")] 
    public NpcChatSpeaker npcSpeaker;
    public NPC npcComponent; // Reference to NPC component for quest context

    [Header("Mic reference (FreeSpeechToTextToggle)")]
    public FreeSpeechToTextToggle mic; // gán từ Inspector

    [Header("Flow")]
    public int minChars = 3;
    public bool waitUntilNpcFinished = true;   // true để xếp hàng hợp lý
    public float sendCooldown = 0.3f;
    
    [Header("Display NPC Reply")]
    public bool displayNpcReply = true; // Hiển thị reply của NPC lên mic output

    private float _lastSend = -999f;
    private readonly Queue<string> _queue = new Queue<string>();
    private string _lastSent;
    private const int MaxQueue = 50;
    private string _lastNpcReply = "";

    void OnEnable()
    {
        FreeSpeechToTextToggle.OnFinalTranscript += OnMicFinal;
        if (npcSpeaker)
        {
            npcSpeaker.OnSpeakStart += OnNpcStart;
            npcSpeaker.OnSpeakEnd   += OnNpcEnd;
            
            // Auto-detect NPC component if not assigned
            if (npcComponent == null)
            {
                npcComponent = npcSpeaker.GetComponent<NPC>();
                if (npcComponent != null)
                {
                    Debug.Log($"✅ MicNpcBridge: Auto-detected NPC component on {npcSpeaker.name}");
                }
            }
        }
    }

    void OnDisable()
    {
        FreeSpeechToTextToggle.OnFinalTranscript -= OnMicFinal;
        if (npcSpeaker)
        {
            npcSpeaker.OnSpeakStart -= OnNpcStart;
            npcSpeaker.OnSpeakEnd   -= OnNpcEnd;
        }
    }

    private void OnMicFinal(string text)
    {
        if (!npcSpeaker) return;
        var t = (text ?? "").Trim();
        if (t.Length < minChars) return;
        if (t == _lastSent) return; // chống lặp 1-1

        bool inCooldown = (Time.unscaledTime - _lastSend < sendCooldown);
        bool npcBusy = waitUntilNpcFinished && npcSpeaker.IsSpeaking;

        if (inCooldown || npcBusy)
        {
            if (_queue.Count < MaxQueue) _queue.Enqueue(t); // tránh phình vô hạn
            return;
        }

        SendNow(t);
        _lastSent = t;
    }

    private Coroutine _syncCoroutine;
    
    private void OnNpcStart()
    {
        // TẮT MIC khi NPC bắt đầu nói để không thu tiếng TTS
        if (mic) mic.StopListening();
        
        // Reset last reply để update lại text mới
        _lastNpcReply = "";
        
        // Stop coroutine cũ nếu đang chạy
        if (_syncCoroutine != null)
        {
            StopCoroutine(_syncCoroutine);
            _syncCoroutine = null;
        }
        
        // Hiển thị reply của NPC lên mic output
        if (displayNpcReply && mic && npcSpeaker && npcSpeaker.subtitleTMP)
        {
            _syncCoroutine = StartCoroutine(SyncNpcReplyToMic());
        }
    }

    private void OnNpcEnd()
    {
        // Stop sync coroutine
        if (_syncCoroutine != null)
        {
            StopCoroutine(_syncCoroutine);
            _syncCoroutine = null;
        }
        
        // NPC nói xong -> bật mic lại và xả hàng đợi
        if (mic) mic.StartListening();
        TryFlushQueue();
    }
    
    private IEnumerator SyncNpcReplyToMic()
    {
        yield return new WaitForSeconds(0.1f);
        
        int updateCount = 0;
        while (npcSpeaker && npcSpeaker.IsSpeaking)
        {
            if (npcSpeaker.subtitleTMP != null)
            {
                string npcText = npcSpeaker.subtitleTMP.text;
                
                // Update nếu text khác với trước đó hoặc là lần đầu tiên
                if (!string.IsNullOrEmpty(npcText) && npcText != _lastNpcReply)
                {
                    _lastNpcReply = npcText;
                    if (mic) mic.UpdateDisplayText(npcText);
                    updateCount++;
                }
            }
            
            yield return new WaitForSeconds(0.1f);
        }
        
        _syncCoroutine = null;
    }

    void Update() => TryFlushQueue();

    void TryFlushQueue()
    {
        if (_queue.Count == 0) return;
        if (Time.unscaledTime - _lastSend < sendCooldown) return;
        if (waitUntilNpcFinished && npcSpeaker && npcSpeaker.IsSpeaking) return;

        var t = _queue.Dequeue();
        SendNow(t);
        _lastSent = t;
    }

    private void SendNow(string t)
    {
        t = (t ?? "").Trim();
        if (t.Length < minChars) return;

        _lastSend = Time.unscaledTime;
        
        if (npcComponent != null)
        {
            npcComponent.Say(t);
        }
        else
        {
            npcSpeaker.SpeakFromText(t);
        }
    }
}
