using UnityEngine;
using TMPro;
using UnityEngine.UI;

namespace Inventory.UI
{
    /// <summary>
    /// Displays total coin value in Obal currency
    /// Auto-updates when coin inventory changes
    /// Shows formatted number with dots (1.000, 1.000.000)
    /// </summary>
    public class UICoinTotalDisplay : MonoBehaviour
    {
        [Header("UI References")]
        [SerializeField] private TMP_Text totalValueText;
        [SerializeField] private Image coinIcon;

        [Header("Display Settings")]
        [SerializeField] private string prefix = "";
        [SerializeField] private string suffix = " Obal";
        [SerializeField] private Color normalColor = Color.white;
        [SerializeField] private Color emptyColor = Color.gray;

        [Header("Animation (Optional)")]
        [SerializeField] private bool animateOnChange = true;
        [SerializeField] private float animationDuration = 0.3f;

        private int currentDisplayValue = 0;
        private int targetValue = 0;
        private float animationTime = 0f;

        void Start()
        {
            StartCoroutine(WaitForCoinSystemAndSubscribe());
        }

        void OnEnable()
        {
            
            // Nếu CoinInventorySystem đã có sẵn, subscribe ngay
            if (CoinInventorySystem.Instance != null)
            {
                SubscribeToEvents();
                UpdateDisplay();
            }
        }

        System.Collections.IEnumerator WaitForCoinSystemAndSubscribe()
        {
            // Đợi cho đến khi CoinInventorySystem sẵn sàng
            while (CoinInventorySystem.Instance == null)
            {
                Debug.Log("[UICoinTotalDisplay] Waiting for CoinInventorySystem...");
                yield return null;
            }

            Debug.Log("[UICoinTotalDisplay] CoinInventorySystem found! Subscribing...");
            SubscribeToEvents();
            UpdateDisplay();
        }

        void SubscribeToEvents()
        {
            if (CoinInventorySystem.Instance == null) return;

            // Unsubscribe trước để tránh duplicate
            CoinInventorySystem.Instance.OnCoinInventoryChanged -= OnCoinInventoryChanged;
            CoinInventorySystem.Instance.OnCoinSlotChanged -= OnCoinSlotChanged;

            // Subscribe mới
            CoinInventorySystem.Instance.OnCoinInventoryChanged += OnCoinInventoryChanged;
            CoinInventorySystem.Instance.OnCoinSlotChanged += OnCoinSlotChanged;
            
            Debug.Log("[UICoinTotalDisplay] Subscribed to CoinInventorySystem events");
        }

        void OnDisable()
        {
            UnsubscribeFromEvents();
        }

        void UnsubscribeFromEvents()
        {
            if (CoinInventorySystem.Instance != null)
            {
                CoinInventorySystem.Instance.OnCoinInventoryChanged -= OnCoinInventoryChanged;
                CoinInventorySystem.Instance.OnCoinSlotChanged -= OnCoinSlotChanged;
                Debug.Log("[UICoinTotalDisplay] Unsubscribed from CoinInventorySystem events");
            }
        }

        void Update()
        {
            if (animateOnChange && currentDisplayValue != targetValue)
            {
                AnimateValue();
            }
        }

        private void OnCoinInventoryChanged()
        {
            UpdateDisplay();
        }

        private void OnCoinSlotChanged(int slotIndex)
        {
            UpdateDisplay();
        }

        /// <summary>
        /// Update the display with current total value
        /// </summary>
        public void UpdateDisplay()
        {
            Debug.Log("[UICoinTotalDisplay] UpdateDisplay called");

            if (totalValueText == null)
            {
                Debug.LogError("[UICoinTotalDisplay] totalValueText is NULL! Please assign it in Inspector.");
                return;
            }

            if (CoinInventorySystem.Instance == null)
            {
                Debug.LogWarning("[UICoinTotalDisplay] CoinInventorySystem.Instance is NULL - showing 0");
                SetDisplayText("0", true);
                return;
            }

            targetValue = CoinInventorySystem.Instance.GetTotalCoinValueInObal();
            Debug.Log($"[UICoinTotalDisplay] Target value: {targetValue}");

            if (animateOnChange)
            {
                animationTime = 0f;
                Debug.Log("[UICoinTotalDisplay] Animation enabled - will animate to target");
            }
            else
            {
                currentDisplayValue = targetValue;
                string formatted = CoinInventorySystem.FormatNumberWithDots(currentDisplayValue);
                SetDisplayText(formatted, targetValue == 0);
                Debug.Log($"[UICoinTotalDisplay] Set text immediately to: {formatted}");
            }
        }

        /// <summary>
        /// Animate value change smoothly
        /// </summary>
        private void AnimateValue()
        {
            animationTime += Time.deltaTime;
            float t = Mathf.Clamp01(animationTime / animationDuration);

            currentDisplayValue = Mathf.RoundToInt(Mathf.Lerp(currentDisplayValue, targetValue, t));
            SetDisplayText(CoinInventorySystem.FormatNumberWithDots(currentDisplayValue), currentDisplayValue == 0);

            if (t >= 1f)
            {
                currentDisplayValue = targetValue;
            }
        }

        /// <summary>
        /// Set display text with formatting
        /// </summary>
        private void SetDisplayText(string formattedValue, bool isEmpty)
        {
            if (totalValueText == null)
            {
                Debug.LogError("[UICoinTotalDisplay] totalValueText is NULL in SetDisplayText!");
                return;
            }

            string finalText = $"{prefix}{formattedValue}{suffix}";
            totalValueText.text = finalText;
            totalValueText.color = isEmpty ? emptyColor : normalColor;
            
            Debug.Log($"[UICoinTotalDisplay] Text set to: '{finalText}' (isEmpty: {isEmpty})");
        }

        /// <summary>
        /// Set prefix text (e.g., "Total: ")
        /// </summary>
        public void SetPrefix(string newPrefix)
        {
            prefix = newPrefix;
            UpdateDisplay();
        }

        /// <summary>
        /// Set suffix text (e.g., " Gold", " Obal")
        /// </summary>
        public void SetSuffix(string newSuffix)
        {
            suffix = newSuffix;
            UpdateDisplay();
        }

        /// <summary>
        /// Get current total value
        /// </summary>
        public int GetTotalValue()
        {
            return CoinInventorySystem.Instance?.GetTotalCoinValueInObal() ?? 0;
        }

        /// <summary>
        /// Manual refresh (for testing or external calls)
        /// </summary>
        public void Refresh()
        {
            UpdateDisplay();
        }
    }
}
