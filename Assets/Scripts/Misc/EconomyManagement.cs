using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class EconomyManagement : Singleton<EconomyManagement>
{
    private TMP_Text goldText;
    private int currentGold = 0;

    const string COIN_AMOUNT_TEXT = "Gold Amount";

    public void UpdateCurrentGold()
    {
        currentGold += 1;

        if (goldText == null)
        {
            goldText = GameObject.Find(COIN_AMOUNT_TEXT).GetComponent<TMP_Text>();
        }

        goldText.text = currentGold.ToString("D3");
    }
    
    public void AddGold(int amount)
    {
        if (amount <= 0) return;
        
        currentGold += amount;

        if (goldText == null)
        {
            goldText = GameObject.Find(COIN_AMOUNT_TEXT)?.GetComponent<TMP_Text>();
        }

        if (goldText != null)
        {
            goldText.text = currentGold.ToString("D3");
        }
        
        Debug.Log($"💰 Added {amount} gold. Total: {currentGold}");
    }
    
    public int GetCurrentGold()
    {
        return currentGold;
    }
}
