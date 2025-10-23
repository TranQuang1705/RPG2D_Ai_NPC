using UnityEngine;

public class NPC : MonoBehaviour
{
    [Header("UI")]
    [SerializeField] private GameObject dialoguePanel;

    [Header("Chat (optional)")]
    [SerializeField] private NpcChatSpeaker chatSpeaker; // gắn component này nếu muốn NPC nói

    private void Start()
    {
        if (dialoguePanel != null)
            dialoguePanel.SetActive(false); // start hidden
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Player") && dialoguePanel != null)
        {
            dialoguePanel.SetActive(true); // show when near
        }
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (other.CompareTag("Player") && dialoguePanel != null)
        {
            dialoguePanel.SetActive(false); // hide when leaving
        }
    }

    /// <summary>
    /// Gọi hàm này từ script mic/voice để NPC gửi câu hỏi lên Flask và nói ra câu trả lời.
    /// Không thay đổi logic panel.
    /// </summary>
    public void Say(string userText)
    {
        if (!string.IsNullOrWhiteSpace(userText) && chatSpeaker != null)
        {
            chatSpeaker.SpeakFromText(userText);
        }
    }
}
