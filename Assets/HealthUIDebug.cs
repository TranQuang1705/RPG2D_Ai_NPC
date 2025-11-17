using UnityEngine;
using TMPro;

/// <summary>
/// Debug script to check Health UI and Level UI setup
/// Attach to any GameObject in scene to test
/// </summary>
public class HealthUIDebug : MonoBehaviour
{
    void Start()
    {
        Debug.Log("===== HEALTH UI DEBUG =====");
        
        // Check PlayerHealth
        var playerHealth = FindObjectOfType<PlayerHealth>();
        if (playerHealth == null)
        {
            Debug.LogError("❌ PlayerHealth NOT FOUND in scene!");
        }
        else
        {
            Debug.Log("✅ PlayerHealth found");
        }
        
        // Check HeartHealthUI
        var heartUI = FindObjectOfType<HeartHealthUI>();
        if (heartUI == null)
        {
            Debug.LogError("❌ HeartHealthUI NOT FOUND in scene!");
        }
        else
        {
            Debug.Log("✅ HeartHealthUI found");
        }
        
        // Check PlayerLevelUI
        var levelUI = FindObjectOfType<PlayerLevelUI>();
        if (levelUI == null)
        {
            Debug.LogError("❌ PlayerLevelUI NOT FOUND in scene!");
        }
        else
        {
            Debug.Log("✅ PlayerLevelUI found");
        }
        
        // Check if TextMeshPro exists in scene
        var tmpTexts = FindObjectsOfType<TextMeshProUGUI>();
        Debug.Log($"📝 Found {tmpTexts.Length} TextMeshPro components in scene");
        foreach (var tmp in tmpTexts)
        {
            Debug.Log($"  - {tmp.gameObject.name}: \"{tmp.text}\"");
        }
        
        Debug.Log("===== DEBUG COMPLETE =====");
    }
}
