using UnityEngine;

public class NPCTalkTrigger : MonoBehaviour
{
    private NPC parentNPC;

    void Start()
    {
        parentNPC = GetComponentInParent<NPC>();
        if (parentNPC == null)
            Debug.LogWarning($"⚠️ {name}: Không tìm thấy NPC cha!");
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            Debug.Log($"💬 {name}: Player bước vào vùng nói chuyện của {parentNPC?.name}");
            parentNPC?.TriggerDialogueEnter();
        }
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            Debug.Log($"👋 {name}: Player rời vùng nói chuyện của {parentNPC?.name}");
            parentNPC?.TriggerDialogueExit();
        }
    }
}
