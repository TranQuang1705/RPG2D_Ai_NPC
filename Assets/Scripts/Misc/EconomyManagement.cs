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
}
