using UnityEngine;
using UnityEngine.InputSystem; // hỗ trợ Keyboard.current; nếu không dùng có thể bỏ

public class InventoryToggle : MonoBehaviour
{
    [Header("Refs (kéo 3 cái này)")]
    public GameObject inventoryCanvas;   // panel túi
    public Rigidbody2D playerRb;         // rigidbody của nhân vật
    public GameObject activeWeapon;      // vũ khí đang cầm (child ở tay)


    // --- state nội bộ
    bool isOpen;
    bool weaponWasActive;
    RigidbodyConstraints2D origConstraints;

    void Awake()
    {
        if (inventoryCanvas) inventoryCanvas.SetActive(false);
        if (playerRb) origConstraints = playerRb.constraints;
    }

    void Update()
    {
        // Nhấn E hoặc Esc để toggle
        bool pressed =
            (Keyboard.current != null && (Keyboard.current.eKey.wasPressedThisFrame || Keyboard.current.escapeKey.wasPressedThisFrame))
            || Input.GetKeyDown(KeyCode.E) || Input.GetKeyDown(KeyCode.Escape);

        if (pressed) Toggle();
    }

    public void Toggle()
    {
        SetOpen(!isOpen);
    }

    public void Open()  => SetOpen(true);
    public void Close() => SetOpen(false);

    void SetOpen(bool open)
    {
        if (isOpen == open) return;
        isOpen = open;

        // UI bag
        if (inventoryCanvas) inventoryCanvas.SetActive(isOpen);

        // ✅ Đóng ItemDetailPanel khi đóng Inventory
        if (!isOpen && ItemDetailPanel.Instance != null)
        {
            ItemDetailPanel.Instance.Hide();
        }

        // ✅ Notify UIManager để disable/enable player controls
        if (UIManager.Instance != null)
        {
            if (isOpen)
                UIManager.Instance.OnPanelOpened();
            else
                UIManager.Instance.OnPanelClosed();
        }

        // Khóa/khôi phục chuyển động nhân vật (freeze rigidbody để chắc chắn)
        if (playerRb)
        {
            if (isOpen)
            {
                playerRb.velocity = Vector2.zero;
                playerRb.angularVelocity = 0f;
                playerRb.constraints = RigidbodyConstraints2D.FreezePosition | RigidbodyConstraints2D.FreezeRotation;
            }
            else
            {
                playerRb.constraints = origConstraints;
            }
        }

        // Khóa/khôi phục tấn công bằng cách bật/tắt vũ khí đang cầm (backup, UIManager sẽ handle chính)
        if (activeWeapon)
        {
            if (isOpen)
            {
                weaponWasActive = activeWeapon.activeSelf;
                activeWeapon.SetActive(false);
            }
            else
            {
                if (weaponWasActive) activeWeapon.SetActive(true);
            }
        }
    }

    void OnDisable()
    {
        // Phòng kẹt khi object bị disable lúc đang mở
        if (inventoryCanvas) inventoryCanvas.SetActive(false);
        if (playerRb) playerRb.constraints = origConstraints;
        if (activeWeapon) activeWeapon.SetActive(true);
        isOpen = false;
    }
}
