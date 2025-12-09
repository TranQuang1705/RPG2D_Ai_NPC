using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Manages player's coin inventory - separate from regular items
/// Maps to player_coins table in database
/// </summary>
public class CoinInventorySystem : MonoBehaviour
{
    public static CoinInventorySystem Instance { get; private set; }

    [Header("Config")]
    [Min(1)] public int capacity = 6;  // 6 types of coins: Obal, Varos, Sylv, Feron, Astryl, Aurum
    [SerializeField] private List<CoinSlot> coinSlots = new();

    public event Action OnCoinInventoryChanged;
    public event Action<int> OnCoinSlotChanged;

    public IReadOnlyList<CoinSlot> CoinSlots => coinSlots;
    public int Capacity => coinSlots.Count;

    void Awake()
    {
        if (Instance != null && Instance != this) 
        { 
            Destroy(gameObject); 
            return; 
        }
        Instance = this;

        // Initialize coin slots (one for each coin type)
        capacity = 6;  // Fixed: 6 coin types
        coinSlots = new List<CoinSlot>(capacity);
        for (int i = 0; i < capacity; i++) 
        {
            coinSlots.Add(new CoinSlot());
        }
        

    }

    public int AddCoin(CoinSO coin, int amount)
    {
        if (coin == null || amount <= 0) return amount;

        // Find existing slot with this coin type
        int slotIndex = FindSlotWithCoin(coin);
        
        if (slotIndex >= 0)
        {
            // Add to existing slot
            coinSlots[slotIndex].amount += amount;
            OnCoinSlotChanged?.Invoke(slotIndex);
            Debug.Log($"[CoinInventory] Added {amount} {coin.coinName} to slot {slotIndex}. Total: {coinSlots[slotIndex].amount}");
        }
        else
        {
            // Find empty slot
            slotIndex = FindEmptySlot();
            if (slotIndex >= 0)
            {
                coinSlots[slotIndex].coin = coin;
                coinSlots[slotIndex].amount = amount;
                OnCoinSlotChanged?.Invoke(slotIndex);
                Debug.Log($"[CoinInventory] New coin {coin.coinName} x{amount} in slot {slotIndex}");
            }
            else
            {
                Debug.LogWarning($"[CoinInventory] No empty slot for {coin.coinName}!");
                return amount;  // Could not add
            }
        }

        OnCoinInventoryChanged?.Invoke();
        
        // Also update EconomyManagement for currency tracking
        UpdateEconomyManagement(coin, amount);
        
        return 0;  // Successfully added all
    }

    /// <summary>
    /// Remove coins from inventory
    /// Returns amount actually removed
    /// </summary>
    public int RemoveCoin(CoinSO coin, int amount)
    {
        if (coin == null || amount <= 0) return 0;

        int slotIndex = FindSlotWithCoin(coin);
        if (slotIndex < 0) return 0;  // Coin not found

        int removed = Mathf.Min(coinSlots[slotIndex].amount, amount);
        coinSlots[slotIndex].amount -= removed;

        if (coinSlots[slotIndex].amount <= 0)
        {
            coinSlots[slotIndex].Clear();
        }

        OnCoinSlotChanged?.Invoke(slotIndex);
        OnCoinInventoryChanged?.Invoke();

        Debug.Log($"[CoinInventory] Removed {removed} {coin.coinName}");
        return removed;
    }

    /// <summary>
    /// Get total amount of a specific coin type
    /// </summary>
    public int CountCoin(CoinSO coin)
    {
        int slotIndex = FindSlotWithCoin(coin);
        return slotIndex >= 0 ? coinSlots[slotIndex].amount : 0;
    }

    /// <summary>
    /// Check if player has enough of a specific coin
    /// </summary>
    public bool HasEnough(CoinSO coin, int amount)
    {
        return CountCoin(coin) >= amount;
    }

    private int FindSlotWithCoin(CoinSO coin)
    {
        for (int i = 0; i < coinSlots.Count; i++)
        {
            if (!coinSlots[i].IsEmpty && coinSlots[i].coin == coin)
                return i;
        }
        return -1;
    }

    private int FindEmptySlot()
    {
        for (int i = 0; i < coinSlots.Count; i++)
        {
            if (coinSlots[i].IsEmpty)
                return i;
        }
        return -1;
    }

    /// <summary>
    /// Update the EconomyManagement system when coins are added
    /// </summary>
    private void UpdateEconomyManagement(CoinSO coin, int amount)
    {
        if (EconomyManagement.Instance == null) return;

        switch (coin.coinName.ToLower())
        {
            case "obal":
                EconomyManagement.Instance.AddObal(amount);
                break;
            case "varos":
                EconomyManagement.Instance.AddVaros(amount);
                break;
            case "sylv":
                EconomyManagement.Instance.AddSylv(amount);
                break;
            case "feron":
                EconomyManagement.Instance.AddFeron(amount);
                break;
            case "astryl":
                EconomyManagement.Instance.AddAstryl(amount);
                break;
            case "aurum":
                EconomyManagement.Instance.AddAurum(amount);
                break;
        }
    }

    public void ClearSlot(int index)
    {
        if (index < 0 || index >= coinSlots.Count) return;
        
        coinSlots[index].Clear();
        OnCoinSlotChanged?.Invoke(index);
        OnCoinInventoryChanged?.Invoke();
    }

    /// <summary>
    /// Calculate total value of all coins in Obal (base currency)
    /// Example: 10 Obal (1) + 1 Varos (10) + 1 Sylv (100) = 120 Obal
    /// </summary>
    public int GetTotalCoinValueInObal()
    {
        int totalValue = 0;

        Debug.Log($"[CoinInventorySystem] Calculating total value from {coinSlots.Count} slots");

        foreach (var slot in coinSlots)
        {
            if (!slot.IsEmpty && slot.coin != null)
            {
                int slotValue = slot.coin.coinValue * slot.amount;
                totalValue += slotValue;
                Debug.Log($"  → {slot.coin.coinName}: {slot.amount} x {slot.coin.coinValue} = {slotValue}");
            }
            else
            {
                Debug.Log($"  → Empty slot");
            }
        }

        return totalValue;
    }


    public string GetFormattedTotalValue()
    {
        int totalValue = GetTotalCoinValueInObal();
        return FormatNumberWithDots(totalValue);
    }

    /// <summary>
    /// Format number with dots as thousand separators
    /// </summary>
    public static string FormatNumberWithDots(int number)
    {
        return number.ToString("N0", new System.Globalization.CultureInfo("de-DE"));
    }
}


[Serializable]
public class CoinSlot
{
    public CoinSO coin;
    public int amount;

    public bool IsEmpty => coin == null || amount <= 0;

    public void Clear() 
    { 
        coin = null; 
        amount = 0; 
    }
}
