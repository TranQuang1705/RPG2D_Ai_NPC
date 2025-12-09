using UnityEngine;

/// <summary>
/// Controller cho mũi tên ShoppingBag - đổi hướng dựa vào item được chọn
/// - Chọn item từ Shop → Mũi tên trái (<<<) - chuyển vào giỏ
/// - Chọn item từ Inventory → Mũi tên phải (>>>) - trả lại shop
/// </summary>
public class ShoppingBagArrowController : MonoBehaviour
{
    [Header("Arrow Prefabs")]
    [SerializeField] private GameObject leftArrowPrefab;  // <<< - Chuyển vào giỏ
    [SerializeField] private GameObject rightArrowPrefab; // >>> - Trả về shop

    [Header("Arrow Container")]
    [SerializeField] private Transform arrowContainer; // Parent để spawn arrow

    [Header("Auto Find")]
    [SerializeField] private bool autoFindArrows = true;

    // Current arrow
    private GameObject currentArrow;
    private ArrowDirection currentDirection = ArrowDirection.None;

    // Enum cho hướng
    public enum ArrowDirection
    {
        None,
        Left,  // Shop → Cart
        Right  // Cart → Shop
    }

    void Start()
    {
        // Auto-find arrow container
        if (arrowContainer == null)
        {
            arrowContainer = transform;
        }

        // Auto-find arrow prefabs if not assigned
        if (autoFindArrows)
        {
            if (leftArrowPrefab == null)
            {
                // Try to find DirectionL (Left arrow)
                Transform found = transform.Find("DirectionL");
                if (found != null)
                {
                    leftArrowPrefab = found.gameObject;
                }
            }

            if (rightArrowPrefab == null)
            {
                // Try to find Direction (Right arrow)
                Transform found = transform.Find("Direction");
                if (found != null)
                {
                    rightArrowPrefab = found.gameObject;
                }
            }
        }

        // Tắt raycast cho arrows để không chặn button clicks
        DisableRaycastForArrows();

        // Hide both arrows initially
        HideAllArrows();
    }

    /// <summary>
    /// Disable raycast for arrow images (để không chặn button clicks)
    /// </summary>
    private void DisableRaycastForArrows()
    {
        if (leftArrowPrefab != null)
        {
            var images = leftArrowPrefab.GetComponentsInChildren<UnityEngine.UI.Image>(true);
            foreach (var img in images)
            {
                img.raycastTarget = false;
            }
        }

        if (rightArrowPrefab != null)
        {
            var images = rightArrowPrefab.GetComponentsInChildren<UnityEngine.UI.Image>(true);
            foreach (var img in images)
            {
                img.raycastTarget = false;
            }
        }
    }

    /// <summary>
    /// Show left arrow (<<< - chuyển từ shop vào giỏ)
    /// </summary>
    public void ShowLeftArrow()
    {
        if (currentDirection == ArrowDirection.Left) return;

        HideAllArrows();

        if (leftArrowPrefab != null)
        {
            leftArrowPrefab.SetActive(true);
            currentArrow = leftArrowPrefab;
            currentDirection = ArrowDirection.Left;
        }
    }

    /// <summary>
    /// Show right arrow (>>> - trả từ giỏ về shop)
    /// </summary>
    public void ShowRightArrow()
    {
        if (currentDirection == ArrowDirection.Right) return;

        HideAllArrows();

        if (rightArrowPrefab != null)
        {
            rightArrowPrefab.SetActive(true);
            currentArrow = rightArrowPrefab;
            currentDirection = ArrowDirection.Right;
        }
    }

    /// <summary>
    /// Hide all arrows
    /// </summary>
    public void HideAllArrows()
    {
        if (leftArrowPrefab != null)
        {
            leftArrowPrefab.SetActive(false);
        }

        if (rightArrowPrefab != null)
        {
            rightArrowPrefab.SetActive(false);
        }

        currentArrow = null;
        currentDirection = ArrowDirection.None;
    }

    /// <summary>
    /// Get current arrow direction
    /// </summary>
    public ArrowDirection GetCurrentDirection()
    {
        return currentDirection;
    }

    /// <summary>
    /// Check if left arrow is showing
    /// </summary>
    public bool IsLeftArrowShowing()
    {
        return currentDirection == ArrowDirection.Left;
    }

    /// <summary>
    /// Check if right arrow is showing
    /// </summary>
    public bool IsRightArrowShowing()
    {
        return currentDirection == ArrowDirection.Right;
    }
}
