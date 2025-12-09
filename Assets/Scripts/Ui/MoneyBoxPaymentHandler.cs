using UnityEngine;
using UnityEngine.UI;
using System;
using System.Collections;

/// <summary>
/// Handler for MoneyBox payment button
/// Processes payment when clicked - checks if Change = 0 and deducts coins
/// </summary>
[RequireComponent(typeof(Button))]
public class MoneyBoxPaymentHandler : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private GameObject tradeUI;
    [SerializeField] private GameObject notificationPanel;
    [SerializeField] private TMPro.TMP_Text notificationText;
    [SerializeField] private UnityEngine.UI.Image notificationIcon;

    [Header("Icons")]
    [SerializeField] private Sprite successIcon;
    [SerializeField] private Sprite failIcon;

    [Header("Auto Find")]
    [SerializeField] private bool autoFind = true;

    [Header("Settings")]
    [SerializeField] private float notificationDuration = 1f;

    [Header("Visual States - Money Box")]
    [SerializeField] private GameObject moneyBox1;     // State 1: few coins
    [SerializeField] private GameObject moneyBox2;     // State 2: medium coins
    [SerializeField] private GameObject moneyBox3;     // State 3: many coins

    [Header("Audio")]
    [SerializeField] private AudioClip paymentSuccessSound; // Sound when payment succeeds
    [SerializeField] private AudioClip paymentFailSound;    // Sound when payment fails
    [SerializeField] private AudioSource audioSource;       // Optional: assign or will create

    private Button button;
    
    // Track coins selected for visual state
    private int coinsSelected = 0;

    void Start()
    {
        button = GetComponent<Button>();
        button.onClick.AddListener(OnPaymentClicked);
        
        // Initialize visual state (all hidden)
        UpdateMoneyBoxVisualState(0);
        
        // Subscribe to coin count changes
        PricePanelController.OnCoinCountChanged += OnCoinCountChanged;
        
        // Setup audio source
        if (audioSource == null)
        {
            audioSource = GetComponent<AudioSource>();
            if (audioSource == null)
            {
                audioSource = gameObject.AddComponent<AudioSource>();
                audioSource.playOnAwake = false;
            }
        }

        if (autoFind)
        {
            // Auto-find TradeUI
            if (tradeUI == null)
            {
                tradeUI = GameObject.FindWithTag("TradeUI");
                if (tradeUI == null)
                {
                    tradeUI = GameObject.Find("TradeUI");
                }
            }

            // Auto-find notification panel
            if (notificationPanel == null)
            {
                notificationPanel = GameObject.Find("NotificationPanel");
                if (notificationPanel == null)
                {
                    notificationPanel = GameObject.Find("Notification");
                }
            }

            // Auto-find notification text
            if (notificationText == null && notificationPanel != null)
            {
                notificationText = notificationPanel.GetComponentInChildren<TMPro.TMP_Text>();
            }

            // Auto-find notification icon
            if (notificationIcon == null && notificationPanel != null)
            {
                // Try to find Image with name "Icon" or "NotificationIcon"
                Transform iconTransform = notificationPanel.transform.Find("Icon");
                if (iconTransform == null)
                {
                    iconTransform = notificationPanel.transform.Find("NotificationIcon");
                }
                if (iconTransform != null)
                {
                    notificationIcon = iconTransform.GetComponent<UnityEngine.UI.Image>();
                }
            }
        }
    }

    /// <summary>
    /// Handle MoneyBox click - process payment
    /// </summary>
    private void OnPaymentClicked()
    {
        Debug.Log("💳 [MoneyBox] Payment button clicked");

        if (PricePanelController.Instance == null)
        {
            Debug.LogError("❌ [MoneyBox] PricePanelController not found!");
            return;
        }

        // Check if payment is sufficient
        int changeAmount = PricePanelController.Instance.GetChangeAmount();
        
        if (changeAmount < 0)
        {
            // Not enough money
            Debug.LogWarning($"⚠️ [MoneyBox] Not enough money! Need {Mathf.Abs(changeAmount)} more Obal");
            PlaySound(paymentFailSound);
            ShowNotification("Not enough money!", false);
            return;
        }

        // Process payment (accept exact or overpayment)
        ProcessPayment();
    }

    /// <summary>
    /// Process the payment - deduct coins and finalize transaction
    /// </summary>
    private void ProcessPayment()
    {
        Debug.Log("💰 [MoneyBox] Processing payment...");
        Debug.Log($"   Price: {PricePanelController.Instance.GetTotalPrice()} Obal");
        Debug.Log($"   Pay: {PricePanelController.Instance.GetPayAmount()} Obal");
        Debug.Log($"   Change: {PricePanelController.Instance.GetChangeAmount()} Obal");

        // Get selected coins and change amount
        var selectedCoins = PricePanelController.Instance.GetSelectedCoins();
        int changeAmount = PricePanelController.Instance.GetChangeAmount();

        if (selectedCoins.Count == 0)
        {
            Debug.LogWarning("⚠️ [MoneyBox] No coins selected!");
            PlaySound(paymentFailSound);
            ShowNotification("No coins selected!", false);
            return;
        }

        // Deduct coins from player's inventory
        bool success = DeductCoins(selectedCoins);

        if (!success)
        {
            Debug.LogError("❌ [MoneyBox] Failed to deduct coins!");
            PlaySound(paymentFailSound);
            ShowNotification("Payment failed!", false);
            return;
        }

        // Return change if overpaid
        if (changeAmount > 0)
        {
            Debug.Log($"💵 [MoneyBox] Returning change: {changeAmount} Obal");
            ReturnChange(changeAmount);
        }

        // Keep items in inventory (they are already added)
        // Mark cart as paid so items won't be removed when shop closes
        if (ShoppingCartManager.Instance != null)
        {
            ShoppingCartManager.Instance.MarkAsPaid();
            ShoppingCartManager.Instance.ClearCart();
        }

        // Play success sound
        PlaySound(paymentSuccessSound);
        
        // Show success notification
        string message = changeAmount > 0 
            ? $"Trade Success! Change: {FormatObal(changeAmount)}" 
            : "Trade Success!";
        ShowNotification(message, true);

        // Clear price panel
        if (PricePanelController.Instance != null)
        {
            PricePanelController.Instance.ClearAll();
        }

        // Close TradeUI after notification
        StartCoroutine(CloseTradeUIAfterDelay(notificationDuration));

        Debug.Log("✅ [MoneyBox] Payment successful!");
    }

    /// <summary>
    /// Return change to player by converting Obal to coins
    /// </summary>
    private void ReturnChange(int obalAmount)
    {
        if (CoinInventorySystem.Instance == null)
        {
            Debug.LogError("❌ [MoneyBox] CoinInventorySystem not found!");
            return;
        }

        // Convert Obal to coins (largest first)
        var changeCoins = ConvertObalToCoins(obalAmount);

        foreach (var coinPair in changeCoins)
        {
            string coinName = coinPair.Key;
            int amount = coinPair.Value;

            if (amount <= 0) continue;

            // Load CoinSO
            CoinSO coinSO = LoadCoinSO(coinName);

            if (coinSO != null)
            {
                // Add coins back to player
                CoinInventorySystem.Instance.AddCoin(coinSO, amount);
                Debug.Log($"💰 [MoneyBox] Returned {amount} {coinName} as change");
            }
            else
            {
                Debug.LogWarning($"⚠️ [MoneyBox] Could not load CoinSO for {coinName} change");
            }
        }
    }

    /// <summary>
    /// Convert Obal amount to coins (largest denominations first)
    /// Example: 1150 Obal → 1 Feron, 1 Sylv, 5 Varos
    /// </summary>
    private System.Collections.Generic.Dictionary<string, int> ConvertObalToCoins(int obalAmount)
    {
        var result = new System.Collections.Generic.Dictionary<string, int>();

        // Coin values in descending order
        var coinTypes = new[]
        {
            ("aurum", 10000),
            ("feron", 1000),
            ("sylv", 100),
            ("varos", 10),
            ("obal", 1)
        };

        int remaining = obalAmount;

        foreach (var (coinName, value) in coinTypes)
        {
            if (remaining >= value)
            {
                int count = remaining / value;
                result[coinName] = count;
                remaining %= value;
                Debug.Log($"  💵 Change breakdown: {count} {coinName} ({count * value} Obal)");
            }
        }

        return result;
    }

    /// <summary>
    /// Deduct coins from player's coin inventory
    /// </summary>
    private bool DeductCoins(System.Collections.Generic.Dictionary<string, int> coins)
    {
        if (CoinInventorySystem.Instance == null)
        {
            Debug.LogError("❌ [MoneyBox] CoinInventorySystem not found!");
            return false;
        }

        Debug.Log($"💸 [MoneyBox] Attempting to deduct {coins.Count} coin types:");
        foreach (var c in coins)
        {
            Debug.Log($"  → {c.Key}: {c.Value}");
        }

        // Find CoinSO for each coin type and deduct
        foreach (var coinPair in coins)
        {
            string coinName = coinPair.Key;
            int amount = coinPair.Value;

            Debug.Log($"🔍 [MoneyBox] Processing {coinName} x{amount}...");

            // Load CoinSO
            CoinSO coinSO = LoadCoinSO(coinName);

            if (coinSO == null)
            {
                Debug.LogError($"❌ [MoneyBox] Could not find CoinSO for '{coinName}'");
                Debug.LogError($"   → Make sure CoinSO exists in Resources/Coins/{coinName}.asset");
                return false;
            }

            // Check how many player has
            int currentAmount = CoinInventorySystem.Instance.CountCoin(coinSO);
            Debug.Log($"📊 [MoneyBox] Player has {currentAmount} {coinName}, need {amount}");

            // Check if player has enough
            if (currentAmount < amount)
            {
                Debug.LogError($"❌ [MoneyBox] Not enough {coinName}! Have {currentAmount}, need {amount}");
                return false;
            }

            // Deduct coins
            int removed = CoinInventorySystem.Instance.RemoveCoin(coinSO, amount);
            
            if (removed != amount)
            {
                Debug.LogError($"❌ [MoneyBox] Failed to remove {coinName}! Removed {removed}/{amount}");
                return false;
            }

            Debug.Log($"✅ [MoneyBox] Successfully deducted {amount} {coinName}");
        }

        Debug.Log("✅ [MoneyBox] All coins deducted successfully");
        return true;
    }

    /// <summary>
    /// Load CoinSO by finding it in CoinInventorySystem
    /// </summary>
    private CoinSO LoadCoinSO(string coinName)
    {
        Debug.Log($"📦 [MoneyBox] Loading CoinSO for '{coinName}'...");

        if (CoinInventorySystem.Instance == null)
        {
            Debug.LogError("❌ [MoneyBox] CoinInventorySystem not found!");
            return null;
        }

        // Get all coin slots and find matching coin
        var coinSlots = CoinInventorySystem.Instance.CoinSlots;
        
        foreach (var slot in coinSlots)
        {
            if (slot.IsEmpty || slot.coin == null) continue;

            // Case-insensitive comparison
            if (slot.coin.coinName.Equals(coinName, StringComparison.OrdinalIgnoreCase))
            {
                Debug.Log($"✅ [MoneyBox] Found CoinSO: {slot.coin.coinName} (value: {slot.coin.coinValue})");
                return slot.coin;
            }
        }

        // If not found in inventory, try to get from DatabaseCoinLoader
        if (DatabaseCoinLoader.Instance != null)
        {
            CoinSO coin = DatabaseCoinLoader.Instance.GetCoinSOByName(coinName);
            if (coin != null)
            {
                Debug.Log($"✅ [MoneyBox] Found CoinSO from DatabaseCoinLoader: {coin.coinName}");
                return coin;
            }
        }

        Debug.LogError($"❌ [MoneyBox] Could not find CoinSO for '{coinName}'");
        Debug.LogError($"   → Coin not found in CoinInventorySystem or DatabaseCoinLoader");
        Debug.LogError($"   → Make sure coins are loaded from database first");
        return null;
    }

    /// <summary>
    /// Show notification message with icon
    /// </summary>
    /// <param name="message">Notification message</param>
    /// <param name="isSuccess">True = success icon (✓), False = fail icon (✗)</param>
    private void ShowNotification(string message, bool isSuccess)
    {
        if (notificationPanel != null)
        {
            // Update text
            if (notificationText != null)
            {
                notificationText.text = message;
            }

            // Update icon
            if (notificationIcon != null)
            {
                Sprite iconToShow = isSuccess ? successIcon : failIcon;
                if (iconToShow != null)
                {
                    notificationIcon.sprite = iconToShow;
                    notificationIcon.enabled = true;
                }
                else
                {
                    notificationIcon.enabled = false;
                }
            }

            notificationPanel.SetActive(true);
            StartCoroutine(HideNotificationAfterDelay(notificationDuration));
        }
        else
        {
            string emoji = isSuccess ? "✅" : "❌";
            Debug.Log($"{emoji} [MoneyBox] Notification: {message}");
        }
    }

    /// <summary>
    /// Hide notification after delay
    /// </summary>
    private IEnumerator HideNotificationAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay);
        
        if (notificationPanel != null)
        {
            notificationPanel.SetActive(false);
        }
    }

    /// <summary>
    /// Close TradeUI after delay
    /// </summary>
    private IEnumerator CloseTradeUIAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay);
        
        if (tradeUI != null)
        {
            tradeUI.SetActive(false);
        }
    }

    /// <summary>
    /// Format Obal amount with K/M/B notation
    /// </summary>
    private string FormatObal(int obalAmount)
    {
        if (obalAmount >= 1_000_000_000)
        {
            return $"{(obalAmount / 1_000_000_000f):0.##}B";
        }
        else if (obalAmount >= 1_000_000)
        {
            return $"{(obalAmount / 1_000_000f):0.##}M";
        }
        else if (obalAmount >= 1_000)
        {
            return $"{(obalAmount / 1_000f):0.##}K";
        }
        else
        {
            return obalAmount.ToString();
        }
    }

    /// <summary>
    /// Handle coin count changed event from PricePanelController
    /// </summary>
    private void OnCoinCountChanged(int coinCount)
    {
        coinsSelected = coinCount;
        UpdateMoneyBoxVisualState(coinCount);
    }

    /// <summary>
    /// Update visual state of money box based on coin count
    /// </summary>
    /// <param name="count">Number of coins selected</param>
    private void UpdateMoneyBoxVisualState(int count)
    {
        Debug.Log($"💰 [MoneyBox] Updating visual state for {count} coins");

        // State 0: All hidden (no coins)
        // State 1: MoneyBox1 visible (1-2 coins)
        // State 2: MoneyBox1 + MoneyBox2 visible (3-5 coins)
        // State 3: All visible (6+ coins)

        if (moneyBox1 != null)
        {
            moneyBox1.SetActive(count >= 1);
        }

        if (moneyBox2 != null)
        {
            moneyBox2.SetActive(count >= 3);
        }

        if (moneyBox3 != null)
        {
            moneyBox3.SetActive(count >= 6);
        }

        Debug.Log($"  State: MoneyBox1={count >= 1}, MoneyBox2={count >= 3}, MoneyBox3={count >= 6}");
    }

    /// <summary>
    /// Play audio clip
    /// </summary>
    private void PlaySound(AudioClip clip)
    {
        if (clip == null)
        {
            return; // Silent if no sound assigned
        }

        if (audioSource != null)
        {
            audioSource.PlayOneShot(clip);
            Debug.Log($"🔊 [MoneyBox] Playing sound: {clip.name}");
        }
        else
        {
            // Fallback: play at position
            AudioSource.PlayClipAtPoint(clip, Camera.main.transform.position);
            Debug.Log($"🔊 [MoneyBox] Playing sound at camera position: {clip.name}");
        }
    }

    void OnDestroy()
    {
        if (button != null)
        {
            button.onClick.RemoveListener(OnPaymentClicked);
        }
        
        // Unsubscribe from events
        PricePanelController.OnCoinCountChanged -= OnCoinCountChanged;
    }
}
