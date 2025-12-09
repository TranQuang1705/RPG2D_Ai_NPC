using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class EconomyManagement : Singleton<EconomyManagement>
{
    private TMP_Text goldText;
    private int totalObal = 0; // Renamed from totalCopper for clarity
    private int totalAstryl = 0; // ⭐ ASTRYL tracked separately - Wizard currency

    const string COIN_AMOUNT_TEXT = "Gold Amount";
    
    // Currency conversion rates (base unit = OBAL)
    const int OBAL_PER_VAROS = 10;
    const int OBAL_PER_SYLV = 100;
    const int OBAL_PER_FERON = 1000;
    const int OBAL_PER_ASTRYL = 1000;
    const int OBAL_PER_AURUM = 10000;

    public void UpdateCurrentGold()
    {
        AddAurum(1);
    }
    
    public void AddObal(int amount)
    {
        if (amount <= 0) return;
        
        totalObal += amount;
        UpdateGoldDisplay();
        LogCurrencyPickup($"+{amount} OBAL", amount);
    }
    
    public void AddVaros(int amount)
    {
        if (amount <= 0) return;
        
        int obalValue = amount * OBAL_PER_VAROS;
        totalObal += obalValue;
        UpdateGoldDisplay();
        LogCurrencyPickup($"+{amount} VAROS", obalValue);
    }
    
    public void AddSylv(int amount)
    {
        if (amount <= 0) return;
        
        int obalValue = amount * OBAL_PER_SYLV;
        totalObal += obalValue;
        UpdateGoldDisplay();
        LogCurrencyPickup($"+{amount} SYLV", obalValue);
    }
    
    public void AddFeron(int amount)
    {
        if (amount <= 0) return;
        
        int obalValue = amount * OBAL_PER_FERON;
        totalObal += obalValue;
        UpdateGoldDisplay();
        LogCurrencyPickup($"+{amount} FERON", obalValue);
    }
    
    public void AddAstryl(int amount)
    {
        if (amount <= 0) return;
        
        // ⭐ ASTRYL: Đồng phù thủy - tracked separately
        totalAstryl += amount;
        
        // ASTRYL KHÔNG được cộng vào totalObal (currency riêng biệt)
        // totalObal += amount * OBAL_PER_ASTRYL; // ❌ KHÔNG làm điều này
        
        UpdateGoldDisplay();
        LogWizardCurrencyPickup($"+{amount} ASTRYL (Wizard)", amount);
    }
    
    public void AddAurum(int amount)
    {
        if (amount <= 0) return;
        
        int obalValue = amount * OBAL_PER_AURUM;
        totalObal += obalValue;
        UpdateGoldDisplay();
        LogCurrencyPickup($"+{amount} AURUM", obalValue);
    }
    
    // Legacy methods for backward compatibility
    public void AddCopper(int amount) => AddObal(amount);
    public void AddSilver(int amount) => AddSylv(amount);
    public void AddGold(int amount) => AddAurum(amount);
    
    private void LogCurrencyPickup(string pickupMessage, int obalValueAdded)
    {
        Debug.Log($"💰 {pickupMessage} | Tổng tiền: {GetAurum()}Au {GetFeronOrAstryl()}Fe {GetSylv()}Sy {GetVaros()}Va {GetObal()}Ob (Total: {totalObal} OBAL)");
    }
    
    private void LogWizardCurrencyPickup(string pickupMessage, int astrylAmount)
    {
        Debug.Log($"🧙 {pickupMessage} | ASTRYL riêng: {totalAstryl} | Tổng tiền thường: {GetAurum()}Au {GetFeronOrAstryl()}Fe {GetSylv()}Sy {GetVaros()}Va {GetObal()}Ob");
    }
    
    private void UpdateGoldDisplay()
    {
        if (goldText == null)
        {
            goldText = GameObject.Find(COIN_AMOUNT_TEXT)?.GetComponent<TMP_Text>();
        }

        if (goldText != null)
        {
            // Display as AURUM (highest denomination)
            goldText.text = GetAurum().ToString("D3");
        }
    }
    
    // Get individual coin counts
    public int GetAurum()
    {
        return totalObal / OBAL_PER_AURUM;
    }
    
    public int GetFeronOrAstryl()
    {
        return (totalObal % OBAL_PER_AURUM) / OBAL_PER_FERON;
    }
    
    public int GetSylv()
    {
        return (totalObal % OBAL_PER_FERON) / OBAL_PER_SYLV;
    }
    
    public int GetVaros()
    {
        return (totalObal % OBAL_PER_SYLV) / OBAL_PER_VAROS;
    }
    
    public int GetObal()
    {
        return totalObal % OBAL_PER_VAROS;
    }
    
    public int GetTotalObalValue()
    {
        return totalObal;
    }
    
    // ⭐ WIZARD CURRENCY METHODS
    public int GetAstrylBalance()
    {
        return totalAstryl;
    }
    
    public bool HasEnoughAstryl(int amount)
    {
        return totalAstryl >= amount;
    }
    
    public bool SpendAstryl(int amount)
    {
        if (amount <= 0) return false;
        
        if (totalAstryl >= amount)
        {
            totalAstryl -= amount;
            Debug.Log($"🧙 Spent {amount} ASTRYL | Remaining: {totalAstryl}");
            return true;
        }
        
        Debug.LogWarning($"⛔ Not enough ASTRYL! Need {amount}, have {totalAstryl}");
        return false;
    }
    
    // Legacy methods for backward compatibility
    public int GetTotalGold() => GetAurum();
    public int GetSilver() => GetSylv();
    public int GetCopper() => GetObal();
    public int GetTotalCopperValue() => GetTotalObalValue();
    public int GetCurrentGold() => GetAurum();
}
