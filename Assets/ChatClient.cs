using UnityEngine;
using UnityEngine.Networking;
using System.Collections;

public class ChatClient : MonoBehaviour
{
    [Header("References")]
    public NavActionHandler navHandler;

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.V))
        {
            StartCoroutine(SendChat("where is the village?"));
        }
    }

    IEnumerator SendChat(string text)
    {
        if (navHandler == null)
        {
            yield break;
        }

        string json = "{\"text\": \"" + text + "\", \"session_id\": \"player1\"}";
        string url = "http://127.0.0.1:5000/chat";

        UnityWebRequest req = new UnityWebRequest(url, "POST");
        byte[] bodyRaw = System.Text.Encoding.UTF8.GetBytes(json);
        req.uploadHandler = new UploadHandlerRaw(bodyRaw);
        req.downloadHandler = new DownloadHandlerBuffer();
        req.SetRequestHeader("Content-Type", "application/json");

        yield return req.SendWebRequest();

        if (req.result == UnityWebRequest.Result.Success)
        {
            string raw = req.downloadHandler.text;

            try
            {
                ServerResponse resp = JsonUtility.FromJson<ServerResponse>(raw);
                if (resp != null)
                {
                    navHandler.HandleServerAction(resp);
                }
                else
                {
                    Debug.LogError("⚠️ Failed to parse ServerResponse!");
                }
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"❌ JSON Parse Error: {ex.Message}");
            }
        }
        else
        {
            Debug.LogError($"❌ Chat request failed: {req.error}");
        }
    }
}
