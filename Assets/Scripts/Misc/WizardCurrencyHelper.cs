using UnityEngine;

/// <summary>
/// Helper script để quản lý ASTRYL currency - Đồng tiền đặc biệt của Wizard
/// ASTRYL chỉ có thể nhặt/dùng bởi những nhân vật có tag "Wizard"
/// </summary>
public class WizardCurrencyHelper : MonoBehaviour
{
    private static bool isWizardBiomeUnlocked = false;
    
    /// <summary>
    /// Kiểm tra xem object có quyền dùng ASTRYL không (cần tag "Wizard")
    /// </summary>
    public static bool CanUseAstryl(GameObject obj)
    {
        if (obj == null) return false;
        return obj.CompareTag("Wizard");
    }
    
    /// <summary>
    /// Kiểm tra xem có đang ở trong Wizard Biome không
    /// TODO: Implement khi có Biome system
    /// </summary>
    public static bool IsInWizardBiome()
    {
        return isWizardBiomeUnlocked;
    }
    
    /// <summary>
    /// Unlock Wizard Biome - gọi khi player vào vùng Wizard lần đầu
    /// </summary>
    public static void UnlockWizardBiome()
    {
        if (!isWizardBiomeUnlocked)
        {
            isWizardBiomeUnlocked = true;
            Debug.Log("✨🧙 WIZARD BIOME UNLOCKED! ASTRYL currency is now available.");
        }
    }
    
    /// <summary>
    /// Lock Wizard Biome - dùng để test hoặc khi rời khỏi vùng
    /// </summary>
    public static void LockWizardBiome()
    {
        isWizardBiomeUnlocked = false;
        Debug.Log("🔒 Wizard Biome locked. ASTRYL drops disabled.");
    }
    
    /// <summary>
    /// Kiểm tra có đủ điều kiện trao đổi với Wizard NPC không
    /// </summary>
    public static bool CanTradeWithWizard(GameObject player, GameObject wizard)
    {
        // Player hoặc Wizard phải có tag "Wizard"
        bool playerIsWizard = player != null && player.CompareTag("Wizard");
        bool npcIsWizard = wizard != null && wizard.CompareTag("Wizard");
        
        if (!npcIsWizard)
        {
            Debug.LogWarning("⛔ This NPC is not a Wizard! Cannot use ASTRYL.");
            return false;
        }
        
        // Cả player lẫn NPC đều phải là wizard
        // HOẶC có thể cho phép non-wizard player trade với wizard NPC
        // Tùy game design của bạn
        
        return true; // Cho phép trade nếu NPC là wizard
    }
}
