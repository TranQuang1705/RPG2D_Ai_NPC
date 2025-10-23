using UnityEngine;
using System.Collections.Generic;

public class MicNpcBridge : MonoBehaviour
{
    [Header("Target NPC")] 
    public NpcChatSpeaker npcSpeaker;

    [Header("Mic reference (FreeSpeechToTextToggle)")]
    public FreeSpeechToTextToggle mic; // gán từ Inspector

    [Header("Flow")]
    public int minChars = 3;
    public bool waitUntilNpcFinished = true;   // true để xếp hàng hợp lý
    public float sendCooldown = 0.3f;

    private float _lastSend = -999f;
    private readonly Queue<string> _queue = new Queue<string>();
    private string _lastSent;
    private const int MaxQueue = 50;

    void OnEnable()
    {
        FreeSpeechToTextToggle.OnFinalTranscript += OnMicFinal;
        if (npcSpeaker)
        {
            npcSpeaker.OnSpeakStart += OnNpcStart;
            npcSpeaker.OnSpeakEnd   += OnNpcEnd;
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

    private void OnNpcStart()
    {
        // TẮT MIC khi NPC bắt đầu nói để không thu tiếng TTS
        if (mic) mic.StopListening();
    }

    private void OnNpcEnd()
    {
        // NPC nói xong -> bật mic lại và xả hàng đợi
        if (mic) mic.StartListening();
        TryFlushQueue();
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
        npcSpeaker.SpeakFromText(t);
    }
}
