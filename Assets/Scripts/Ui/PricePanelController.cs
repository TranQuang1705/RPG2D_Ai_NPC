using UnityEngine;
using TMPro;
using System.Collections.Generic;

/// <summary>
/// Controller for PricePanel - displays total price when selecting coins
/// Shows accumulated coin value when trading
/// </summary>
public class PricePanelController : MonoBehaviour
{
    public static PricePanelController Instance { get; private set; }

    [Header("References")]
    [SerializeField] private GameObject pricePanel;
    [SerializeField] private TMP_Text priceNumberText;  // Tổng giá items trong cart
    [SerializeField] private TMP_Text payAmountText;    // Số tiền từ coins đã chọn
    [SerializeField] private TMP_Text changeText;       // Text "Need" hoặc "Change"
    [SerializeField] private TMP_Text changeNumberText; // Số tiền thừa/thiếu

    [Header("Auto Find")]
    [SerializeField] private bool autoFind = true;

    [Header("Audio")]
    [SerializeField] private AudioClip coinAddSound;     // Sound when adding coin
    [SerializeField] private AudioSource audioSource;     // Optional: assign or will create

    // Prices (in Obal)
    private int totalPrice = 0;      // Tổng giá items
    private int coinPayAmount = 0;   // Số tiền từ coins
    
    // Track selected coins (for payment)
    private Dictionary<string, int> selectedCoins = new Dictionary<string, int>();
    
    // Event when coin count changes
    public static event System.Action<int> OnCoinCountChanged;

    // Coin conversion values (matching EconomyManagement)
    private static readonly Dictionary<string, int> coinValues = new Dictionary<string, int>()
    {
        { "obal", 1 },
        { "varos", 10 },
        { "sylv", 100 },
        { "feron", 1000 },
        { "astryl", 1000 },
        { "aurum", 10000 }
    };

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    void Start()
    {
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
            if (pricePanel == null)
            {
                pricePanel = GameObject.Find("PricePanel");
                if (pricePanel == null)
                {
                    pricePanel = GameObject.Find("PricePanelUI");
                }
            }

            if (pricePanel != null)
            {
                // Auto-find PriceNumber text
                if (priceNumberText == null)
                {
                    priceNumberText = FindChildWithName(pricePanel.transform, "PriceNumber")?.GetComponent<TMP_Text>();
                    if (priceNumberText == null)
                    {
                        priceNumberText = FindChildWithName(pricePanel.transform, "TotalPrice")?.GetComponent<TMP_Text>();
                    }
                    if (priceNumberText == null)
                    {
                        priceNumberText = FindChildWithName(pricePanel.transform, "Price")?.GetComponent<TMP_Text>();
                    }
                }

                // Auto-find PayAmount text
                if (payAmountText == null)
                {
                    payAmountText = FindChildWithName(pricePanel.transform, "PayAmount")?.GetComponent<TMP_Text>();
                    if (payAmountText == null)
                    {
                        payAmountText = FindChildWithName(pricePanel.transform, "PayNumber")?.GetComponent<TMP_Text>();
                    }
                    if (payAmountText == null)
                    {
                        payAmountText = FindChildWithName(pricePanel.transform, "CoinAmount")?.GetComponent<TMP_Text>();
                    }
                }

                // Auto-find Change label text
                if (changeText == null)
                {
                    changeText = FindChildWithName(pricePanel.transform, "ChangeLabel")?.GetComponent<TMP_Text>();
                    if (changeText == null)
                    {
                        changeText = FindChildWithName(pricePanel.transform, "Change")?.GetComponent<TMP_Text>();
                    }
                    if (changeText == null)
                    {
                        changeText = FindChildWithName(pricePanel.transform, "ChangeText")?.GetComponent<TMP_Text>();
                    }
                }

                // Auto-find Change number text
                if (changeNumberText == null)
                {
                    changeNumberText = FindChildWithName(pricePanel.transform, "ChangeNumber")?.GetComponent<TMP_Text>();
                    if (changeNumberText == null)
                    {
                        changeNumberText = FindChildWithName(pricePanel.transform, "ChangeAmount")?.GetComponent<TMP_Text>();
                    }
                    if (changeNumberText == null)
                    {
                        changeNumberText = FindChildWithName(pricePanel.transform, "ChangeValue")?.GetComponent<TMP_Text>();
                    }
                }
            }
        }

        // Subscribe to shopping cart events
        if (ShoppingCartManager.Instance != null)
        {
            ShoppingCartManager.Instance.OnItemAddedToCart += OnCartItemAdded;
            ShoppingCartManager.Instance.OnItemRemovedFromCart += OnCartItemRemoved;
            ShoppingCartManager.Instance.OnCartCleared += OnCartCleared;
        }
    }

    void OnDestroy()
    {
        // Unsubscribe from events
        if (ShoppingCartManager.Instance != null)
        {
            ShoppingCartManager.Instance.OnItemAddedToCart -= OnCartItemAdded;
            ShoppingCartManager.Instance.OnItemRemovedFromCart -= OnCartItemRemoved;
            ShoppingCartManager.Instance.OnCartCleared -= OnCartCleared;
        }

        if (Instance == this)
        {
            Instance = null;
        }
    }

    /// <summary>
    /// Add coin to pay amount
    /// </summary>
    public void AddCoin(string coinName, int amount = 1)
    {
        coinName = coinName.ToLower();

        if (!coinValues.ContainsKey(coinName))
        {
            Debug.LogWarning($"⚠️ [PricePanel] Unknown coin type: {coinName}");
            return;
        }

        // Track selected coins
        if (!selectedCoins.ContainsKey(coinName))
        {
            selectedCoins[coinName] = 0;
        }
        selectedCoins[coinName] += amount;

        int obalValue = coinValues[coinName] * amount;
        coinPayAmount += obalValue;

        Debug.Log($"💰 [PricePanel] Added {amount} {coinName} (+{obalValue} Obal). Pay: {coinPayAmount} Obal");

        UpdatePayAmountDisplay();
        
        // Play coin sound
        PlayCoinSound();
        
        // Trigger event with total coin count
        int totalCoins = 0;
        foreach (var count in selectedCoins.Values)
        {
            totalCoins += count;
        }
        OnCoinCountChanged?.Invoke(totalCoins);
    }

    /// <summary>
    /// Clear all
    /// </summary>
    public void ClearAll()
    {
        totalPrice = 0;
        coinPayAmount = 0;
        selectedCoins.Clear();
        UpdatePriceDisplay();
        UpdatePayAmountDisplay();
        
        // Trigger event with 0 coins
        OnCoinCountChanged?.Invoke(0);
        
        // Clear change texts
        if (changeText != null)
        {
            changeText.text = "";
        }
        if (changeNumberText != null)
        {
            changeNumberText.text = "";
        }
        
        Debug.Log("🧹 [PricePanel] Cleared all");
    }

    /// <summary>
    /// Clear only pay amount
    /// </summary>
    public void ClearPayAmount()
    {
        coinPayAmount = 0;
        selectedCoins.Clear();
        UpdatePayAmountDisplay();
        Debug.Log("🧹 [PricePanel] Cleared pay amount");
    }

    /// <summary>
    /// Update price display (items from cart)
    /// </summary>
    private void UpdatePriceDisplay()
    {
        if (priceNumberText != null)
        {
            priceNumberText.text = FormatPrice(totalPrice);
        }
        
        // Also update change display
        UpdateChangeDisplay();
    }

    /// <summary>
    /// Update pay amount display (coins selected)
    /// </summary>
    private void UpdatePayAmountDisplay()
    {
        if (payAmountText != null)
        {
            payAmountText.text = FormatPrice(coinPayAmount);
        }
        
        // Also update change display
        UpdateChangeDisplay();
    }

    /// <summary>
    /// Update change display (difference between pay and price)
    /// </summary>
    private void UpdateChangeDisplay()
    {
        int difference = coinPayAmount - totalPrice;

        if (difference < 0)
        {
            // Need more money
            if (changeText != null)
            {
                changeText.text = "Need";
            }
            if (changeNumberText != null)
            {
                changeNumberText.text = FormatPrice(Mathf.Abs(difference));
            }
        }
        else
        {
            // Change (có thể = 0 hoặc > 0)
            if (changeText != null)
            {
                changeText.text = "Change";
            }
            if (changeNumberText != null)
            {
                changeNumberText.text = FormatPrice(difference);
            }
        }
    }

    /// <summary>
    /// Format price with abbreviated notation (K, M, B)
    /// Examples: 1000 = 1K, 10000 = 10K, 1000000 = 1M, 1000000000 = 1B
    /// </summary>
    private string FormatPrice(int price)
    {
        if (price >= 1_000_000_000) // Billion
        {
            float value = price / 1_000_000_000f;
            return $"{value:0.##}B";
        }
        else if (price >= 1_000_000) // Million
        {
            float value = price / 1_000_000f;
            return $"{value:0.##}M";
        }
        else if (price >= 1_000) // Thousand
        {
            float value = price / 1_000f;
            return $"{value:0.##}K";
        }
        else
        {
            return $"{price}";
        }
    }

    /// <summary>
    /// Get total price in Obal (from cart items)
    /// </summary>
    public int GetTotalPrice()
    {
        return totalPrice;
    }

    /// <summary>
    /// Get pay amount in Obal (from coins)
    /// </summary>
    public int GetPayAmount()
    {
        return coinPayAmount;
    }

    /// <summary>
    /// Get change amount (difference between pay and price)
    /// Positive = change (thừa), Negative = need (thiếu)
    /// </summary>
    public int GetChangeAmount()
    {
        return coinPayAmount - totalPrice;
    }

    /// <summary>
    /// Check if payment is exact (change = 0)
    /// </summary>
    public bool IsPaymentExact()
    {
        return GetChangeAmount() == 0;
    }

    /// <summary>
    /// Check if payment is sufficient (change >= 0)
    /// </summary>
    public bool IsPaymentSufficient()
    {
        return GetChangeAmount() >= 0;
    }

    /// <summary>
    /// Get selected coins dictionary
    /// </summary>
    public Dictionary<string, int> GetSelectedCoins()
    {
        return new Dictionary<string, int>(selectedCoins);
    }

    /// <summary>
    /// Calculate and update price from shopping cart
    /// </summary>
    public void UpdateFromShoppingCart()
    {
        if (ShoppingCartManager.Instance == null) return;

        var cartItems = ShoppingCartManager.Instance.GetCartItems();
        
        if (cartItems.Count == 0)
        {
            totalPrice = 0;
            UpdatePriceDisplay();
            return;
        }

        // Calculate total price in Obal from cart
        int calculatedPrice = 0;
        
        foreach (var item in cartItems)
        {
            // Convert item price to Obal
            int itemPriceInObal = ConvertCoinTypeToObal(item.coinType, item.price);
            int itemTotal = itemPriceInObal * item.quantity;
            calculatedPrice += itemTotal;
            
            Debug.Log($"  💰 {item.itemName} x{item.quantity}: {item.price} {item.coinType} = {itemTotal} Obal");
        }

        // Update total price
        totalPrice = calculatedPrice;
        Debug.Log($"🛒 [PricePanel] Cart total: {totalPrice} Obal");
        
        UpdatePriceDisplay();
    }

    /// <summary>
    /// Convert coin type to Obal value
    /// </summary>
    private int ConvertCoinTypeToObal(string coinType, int amount)
    {
        coinType = coinType.ToLower();
        
        if (coinValues.ContainsKey(coinType))
        {
            return amount * coinValues[coinType];
        }
        
        // Default: assume it's already in Obal
        return amount;
    }

    /// <summary>
    /// Callback when item added to cart
    /// </summary>
    private void OnCartItemAdded(ShoppingCartItem item)
    {
        UpdateFromShoppingCart();
    }

    /// <summary>
    /// Callback when item removed from cart
    /// </summary>
    private void OnCartItemRemoved(ShoppingCartItem item)
    {
        UpdateFromShoppingCart();
    }

    /// <summary>
    /// Callback when cart cleared
    /// </summary>
    private void OnCartCleared()
    {
        totalPrice = 0;
        UpdatePriceDisplay();
    }

    /// <summary>
    /// Helper to find child by name recursively
    /// </summary>
    private Transform FindChildWithName(Transform parent, string name)
    {
        if (parent.name == name) return parent;

        Transform found = parent.Find(name);
        if (found != null) return found;

        foreach (Transform child in parent)
        {
            found = FindChildWithName(child, name);
            if (found != null) return found;
        }

        return null;
    }

    /// <summary>
    /// Play coin add sound
    /// </summary>
    private void PlayCoinSound()
    {
        if (coinAddSound == null)
        {
            return; // Silent if no sound assigned
        }

        if (audioSource != null)
        {
            audioSource.PlayOneShot(coinAddSound);
            Debug.Log($"🔊 [PricePanel] Playing coin sound: {coinAddSound.name}");
        }
        else
        {
            // Fallback: play at position
            AudioSource.PlayClipAtPoint(coinAddSound, Camera.main.transform.position);
            Debug.Log($"🔊 [PricePanel] Playing coin sound at camera position: {coinAddSound.name}");
        }
    }
}
