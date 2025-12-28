using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;

/// <summary>
/// Toggle Coin Inventory Panel (simpler than regular inventory)
/// Open: Button click only (from SlideTabUI icon)
/// Close: E key or Esc key or button click again
/// Does NOT freeze player - can view coins while playing
/// </summary>
public class CoinInventoryToggle : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private GameObject coinInventoryPanel;
    [SerializeField] private Button toggleButton;

    [Header("Input")]
    [SerializeField] private bool allowEKeyToClose = true;
    [SerializeField] private bool allowEscKeyToClose = true;

    private bool isOpen = false;

    void Awake()
    {
        if (coinInventoryPanel) 
            coinInventoryPanel.SetActive(false);

        if (toggleButton)
            toggleButton.onClick.AddListener(Toggle);
    }

    void OnDestroy()
    {
        if (toggleButton)
            toggleButton.onClick.RemoveListener(Toggle);
    }

    void Update()
    {
        // Chỉ cho phép đóng bằng phím E, KHÔNG mở (ESC handled by EscapeKeyManager)
        if (isOpen)
        {
            bool pressedClose = false;

            if (allowEKeyToClose)
            {
                pressedClose = (Keyboard.current != null && Keyboard.current.eKey.wasPressedThisFrame) 
                            || Input.GetKeyDown(KeyCode.E);
            }

            if (pressedClose)
            {
                Close();
            }
        }
    }

    /// <summary>
    /// Toggle: Mở nếu đang đóng, đóng nếu đang mở (for button)
    /// </summary>
    public void Toggle()
    {
        SetOpen(!isOpen);
    }

    /// <summary>
    /// Mở panel (only via button)
    /// </summary>
    public void Open() => SetOpen(true);
    
    /// <summary>
    /// Đóng panel (via button or key)
    /// </summary>
    public void Close() => SetOpen(false);

    void SetOpen(bool open)
    {
        if (isOpen == open) return;
        isOpen = open;

        if (coinInventoryPanel)
            coinInventoryPanel.SetActive(isOpen);

        Debug.Log($"[CoinInventoryToggle] Coin bag {(isOpen ? "opened" : "closed")}");
    }

    void OnDisable()
    {
        if (coinInventoryPanel) 
            coinInventoryPanel.SetActive(false);
        isOpen = false;
    }

    /// <summary>
    /// Check if panel is currently open
    /// </summary>
    public bool IsOpen() => isOpen;
}
