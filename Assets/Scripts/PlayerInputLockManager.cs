using UnityEngine;

/// <summary>
/// Manager to lock/unlock player controls during NPC dialog or UI interactions
/// Prevents player from moving/attacking while typing or talking to NPCs
/// </summary>
public class PlayerInputLockManager : MonoBehaviour
{
    private static PlayerInputLockManager _instance;
    public static PlayerInputLockManager Instance
    {
        get
        {
            if (_instance == null)
            {
                _instance = FindObjectOfType<PlayerInputLockManager>();
                if (_instance == null)
                {
                    GameObject go = new GameObject("PlayerInputLockManager");
                    _instance = go.AddComponent<PlayerInputLockManager>();
                    DontDestroyOnLoad(go);
                }
            }
            return _instance;
        }
    }

    private int lockCount = 0; // Track multiple lock sources
    private bool isLocked = false;

    void Awake()
    {
        if (_instance == null)
        {
            _instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else if (_instance != this)
        {
            Destroy(gameObject);
        }
    }

    /// <summary>
    /// Lock player input (movement, attack, dash)
    /// Can be called multiple times - uses reference counting
    /// </summary>
    public void LockPlayerInput()
    {
        lockCount++;
        
        if (!isLocked)
        {
            isLocked = true;
            ApplyLock();
            Debug.Log($"🔒 Player input LOCKED (count: {lockCount})");
        }
    }

    /// <summary>
    /// Unlock player input
    /// Only unlocks when all lock requests are released
    /// </summary>
    public void UnlockPlayerInput()
    {
        lockCount--;
        
        if (lockCount < 0) lockCount = 0; // Safety check
        
        if (lockCount == 0 && isLocked)
        {
            isLocked = false;
            ReleaseLock();
            Debug.Log($"🔓 Player input UNLOCKED");
        }
    }

    /// <summary>
    /// Force unlock regardless of lock count (use carefully!)
    /// </summary>
    public void ForceUnlock()
    {
        lockCount = 0;
        isLocked = false;
        ReleaseLock();
        Debug.Log($"🔓 Player input FORCE UNLOCKED");
    }

    /// <summary>
    /// Check if player input is currently locked
    /// </summary>
    public bool IsLocked()
    {
        return isLocked;
    }

    private void ApplyLock()
    {
        // Lock movement
        if (PlayerController.Instance != null)
        {
            PlayerController.Instance.SetCanMove(false);
            PlayerController.Instance.SetCanDash(false);
        }

        // Lock attack
        if (ActiveWeapon.Instance != null)
        {
            ActiveWeapon.Instance.SetCanAttack(false);
        }

        // Hide cursor or change to UI cursor
        Cursor.visible = true;
        Cursor.lockState = CursorLockMode.None;
    }

    private void ReleaseLock()
    {
        // Unlock movement
        if (PlayerController.Instance != null)
        {
            PlayerController.Instance.SetCanMove(true);
            PlayerController.Instance.SetCanDash(true);
        }

        // Unlock attack
        if (ActiveWeapon.Instance != null)
        {
            ActiveWeapon.Instance.SetCanAttack(true);
        }

        // Restore game cursor state (adjust based on your game's needs)
        // Cursor.visible = false; // Uncomment if you want to hide cursor in game
        // Cursor.lockState = CursorLockMode.Locked; // Uncomment if you want to lock cursor
    }

    /// <summary>
    /// Get current lock count (for debugging)
    /// </summary>
    public int GetLockCount()
    {
        return lockCount;
    }
}
