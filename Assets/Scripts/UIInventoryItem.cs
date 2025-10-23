using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.EventSystems;
using System;

namespace Inventory.UI
{
    public class UIInventoryItem : MonoBehaviour, IPointerClickHandler
    {
        [Header("Refs")]
        [SerializeField] private Image itemImage;    
        [SerializeField] private TMP_Text quantityTxt; 
        [SerializeField] private Image borderImage;  

        public event Action<UIInventoryItem> OnItemClicked;

        public int Index { get; private set; } = -1;   // slot index trong inventory

        void Awake()
        {
            Deselect();
            Clear();
        }

        public void BindIndex(int index) => Index = index;

        public void SetData(Sprite sprite, int quantity)
        {
            itemImage.gameObject.SetActive(true);
            itemImage.sprite = sprite;
            quantityTxt.text = quantity > 1 ? $"x{quantity}" : "";  // không hiện số nếu =1
        }

        public void Clear()
        {
            itemImage.gameObject.SetActive(false);
            quantityTxt.text = "";
        }

        public void Render(InventorySlotBag slot)   // gọi từ controller
        {
            if (slot == null || slot.IsEmpty)
            {
                Clear();
            }
            else
            {
                SetData(slot.item.icon, slot.quantity);
            }
        }

        public void Select()   => borderImage.enabled = true;
        public void Deselect() => borderImage.enabled = false;

        public void OnPointerClick(PointerEventData eventData)
        {
            OnItemClicked?.Invoke(this);
        }
    }
}
