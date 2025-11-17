using System;
using System.IO;
using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using HuggingFace.API;   // <- keep this; your plugin defines HuggingFaceAPI here

public class SpeechRecognitionTest : MonoBehaviour
{
    [Header("UI")]
    [SerializeField] private Button startButton;
    [SerializeField] private Button stopButton;
    [SerializeField] private TextMeshProUGUI outputText;

    [Header("Hugging Face (only used if your wrapper requires it)")]
    [Tooltip("Your Hugging Face access token (hf_...). Some wrappers don't need it here.")]
    [SerializeField] private string hfApiToken = "hf_XXXXXXXXXXXXXXXXXXXXXXXX";
    [Tooltip("Model id only if your wrapper supports overriding (e.g., openai/whisper-tiny).")]
    [SerializeField] private string modelId = "openai/whisper-tiny";

    [Header("Audio")]
    [Tooltip("Target sample rate for STT engines. 16 kHz is safe for Whisper.")]
    [SerializeField] private int targetSampleRate = 16000;
    [Tooltip("Max seconds per utterance.")]
    [SerializeField] private int recordSeconds = 10;

    private AudioClip clip;
    private string micDevice;
    private bool recording;

    // Callback system for NPC integration
    public System.Action<string> OnSpeechResult;
    public System.Action OnRecordingStarted;
    public System.Action OnRecordingStopped;

    void Start()
    {
        if (startButton) startButton.onClick.AddListener(StartRecording);
        if (stopButton) stopButton.onClick.AddListener(StopRecording);
    }

    void Update()
    {
        if (!recording || clip == null) return;

        int pos = Microphone.GetPosition(micDevice);
        if (pos > 0 && pos >= clip.samples)
            StopRecording();
    }

    public void StartRecording()
    {
        if (Microphone.devices.Length == 0)
        {
            outputText.text = "No microphone found.";
            return;
        }
        micDevice = Microphone.devices[0];
        clip = Microphone.Start(micDevice, false, recordSeconds, targetSampleRate);
        recording = true;
        outputText.text = "Listening...";

        OnRecordingStarted?.Invoke();
    }

    public void StopRecording()
    {
        if (!recording || clip == null) return;

        int position = Microphone.GetPosition(micDevice);
        Microphone.End(micDevice);
        recording = false;

        OnRecordingStopped?.Invoke();

        if (position <= 0)
        {
            outputText.text = "No audio captured.";
            return;
        }

        // fetch recorded portion
        int channels = clip.channels;
        float[] interleaved = new float[position * channels];
        clip.GetData(interleaved, 0);

        // downmix to mono
        float[] mono = (channels == 1) ? interleaved : DownmixToMono(interleaved, channels);

        // 16-bit PCM WAV @ targetSampleRate
        byte[] wav = EncodeAsWav16BitPcm(mono, targetSampleRate, 1);

        outputText.text = "Transcribing...";
        SendRecording(wav);
    }

    public void SendRecording(byte[] wav)
    {
        HuggingFaceAPI.AutomaticSpeechRecognition(
            wav,
            response =>
            {
                Debug.Log($"📝 Kết quả transcribe: \"{response}\"");
                outputText.text = string.IsNullOrEmpty(response) ? "(empty)" : response;
                OnSpeechResult?.Invoke(response); // callback cho NPC
            },
            error =>
            {
                Debug.LogError($"❌ Lỗi ASR: {error}");
                outputText.text = "ASR error: " + error;
                OnSpeechResult?.Invoke(null);
            }
        );
    }


    // ----- helpers -----

    private float[] DownmixToMono(float[] interleaved, int channels)
    {
        int frames = interleaved.Length / channels;
        float[] mono = new float[frames];
        for (int i = 0; i < frames; i++)
        {
            float sum = 0f;
            for (int c = 0; c < channels; c++) sum += interleaved[i * channels + c];
            mono[i] = sum / channels;
        }
        return mono;
    }

    private byte[] EncodeAsWav16BitPcm(float[] samples, int frequency, int channels)
    {
        using var ms = new MemoryStream(44 + samples.Length * 2);
        using var bw = new BinaryWriter(ms);

        int byteRate = frequency * channels * 2;
        short blockAlign = (short)(channels * 2);
        short bitsPerSample = 16;

        // RIFF
        bw.Write(Encoding.ASCII.GetBytes("RIFF"));
        bw.Write(36 + samples.Length * 2);
        bw.Write(Encoding.ASCII.GetBytes("WAVE"));

        // fmt 
        bw.Write(Encoding.ASCII.GetBytes("fmt "));
        bw.Write(16);
        bw.Write((short)1);                 // PCM
        bw.Write((short)channels);
        bw.Write(frequency);
        bw.Write(byteRate);
        bw.Write(blockAlign);
        bw.Write(bitsPerSample);

        // data
        bw.Write(Encoding.ASCII.GetBytes("data"));
        bw.Write(samples.Length * 2);
        for (int i = 0; i < samples.Length; i++)
        {
            float f = Mathf.Clamp(samples[i], -1f, 1f);
            short s = (short)Mathf.RoundToInt(f * short.MaxValue);
            bw.Write(s);
        }

        return ms.ToArray();
    }
}
