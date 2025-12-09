using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.EventSystems;
using System;

namespace Inventory.UI
{
    /// <summary>
    /// UI component for displaying coins in coin inventory
    /// Similar structure to UIInventoryItem but specifically for coins
    /// Coins CANNOT be dropped or moved to regular inventory
    /// When TradeUI is open, clicking coins adds their value to PayAmount
    /// </summary>
    public class UIInventoryCoins : MonoBehaviour, IPointerClickHandler
    {
        [Header("Refs")]
        [SerializeField] private Image coinImage;    
        [SerializeField] private TMP_Text amountTxt; 
        [SerializeField] private Image borderImage;  

        public event Action<UIInventoryCoins> OnCoinClicked;

        public int Index { get; private set; } = -1;
        private CoinSlot currentSlot; // Store current slot data

        void Awake()
        {
            Deselect();
            Clear();
        }

        public void BindIndex(int index) => Index = index;

        public void SetData(Sprite sprite, int amount)
        {
            coinImage.gameObject.SetActive(true);
            coinImage.sprite = sprite;
            amountTxt.text = amount > 1 ? $"x{amount}" : "";
        }

        public void Clear()
        {
            coinImage.gameObject.SetActive(false);
            amountTxt.text = "";
            currentSlot = null;
        }

        public void Render(CoinSlot slot)
        {
            currentSlot = slot;
            
            if (slot == null || slot.IsEmpty)
            {
                Clear();
            }
            else
            {
                SetData(slot.coin.RuntimeIcon, slot.amount);
            }
        }

        public void Select()   => borderImage.enabled = true;
        public void Deselect() => borderImage.enabled = false;

        public void OnPointerClick(PointerEventData eventData)
        {
            // Normal coin click event
            OnCoinClicked?.Invoke(this);

            // Check if TradeUI is open - add coin to pay amount
            if (IsTradeUIOpen() && currentSlot != null && !currentSlot.IsEmpty)
            {
                if (PricePanelController.Instance != null)
                {
                    PricePanelController.Instance.AddCoin(currentSlot.coin.coinName, 1);
                    Debug.Log($"💰 [CoinSlot] Added {currentSlot.coin.coinName} to pay amount (value: {currentSlot.coin.coinValue})");
                    
                    // Visual feedback
                    StartCoroutine(FlashBorder());
                }
            }
        }

        /// <summary>
        /// Check if TradeUI is open
        /// </summary>
        private bool IsTradeUIOpen()
        {
            GameObject tradeUI = GameObject.FindWithTag("TradeUI");
            if (tradeUI == null) tradeUI = GameObject.Find("TradeUI");
            return tradeUI != null && tradeUI.activeInHierarchy;
        }

        /// <summary>
        /// Flash border as visual feedback
        /// </summary>
        private System.Collections.IEnumerator FlashBorder()
        {
            if (borderImage == null) yield break;

            bool wasEnabled = borderImage.enabled;
            
            // Flash 3 times
            for (int i = 0; i < 3; i++)
            {
                borderImage.enabled = true;
                yield return new WaitForSeconds(0.1f);
                borderImage.enabled = false;
                yield return new WaitForSeconds(0.1f);
            }

            // Restore original state
            borderImage.enabled = wasEnabled;
        }

        /// <summary>
        /// Get current coin data
        /// </summary>
        public CoinSlot GetCoinSlot()
        {
            return currentSlot;
        }
    }
}
