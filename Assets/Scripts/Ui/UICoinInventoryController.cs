using UnityEngine;
using System.Collections.Generic;
using Inventory.UI;

namespace Inventory.UI
{
    /// <summary>
    /// Controller for Coin Inventory UI
    /// Manages display of coins separate from regular items
    /// Similar structure to UIInventoryPanel but for coins
    /// Coins cannot be moved or dropped - display only
    /// </summary>
    public class UICoinInventoryController : MonoBehaviour
    {
        [Header("Build")]
        [SerializeField] private Transform slotsParent;
        [SerializeField] private UIInventoryCoins slotPrefab;
        [SerializeField] private bool rebuildOnAwake = true;

        private readonly List<UIInventoryCoins> uiSlots = new();
        private UIInventoryCoins selected;

        void Awake()
        {
            // Delay để chờ CoinInventorySystem khởi động
            if (rebuildOnAwake)
            {
                Invoke(nameof(BuildSlotsDelayed), 0.1f);
            }
        }

        void OnEnable()
        {
            if (CoinInventorySystem.Instance != null)
            {
                CoinInventorySystem.Instance.OnCoinInventoryChanged += RefreshAll;
                CoinInventorySystem.Instance.OnCoinSlotChanged += RefreshSlot;
            }
            
            // ⚠️ Subscribe to DatabaseCoinLoader event
            if (DatabaseCoinLoader.Instance != null)
            {
                DatabaseCoinLoader.OnPlayerCoinsLoaded += OnCoinsLoadedFromDatabase;
            }

            RefreshAll();
        }
        
        private void BuildSlotsDelayed()
        {
            BuildSlots();
        }

        void OnDisable()
        {
            if (CoinInventorySystem.Instance != null)
            {
                CoinInventorySystem.Instance.OnCoinInventoryChanged -= RefreshAll;
                CoinInventorySystem.Instance.OnCoinSlotChanged -= RefreshSlot;
            }
            
            // Unsubscribe from database loader
            if (DatabaseCoinLoader.Instance != null)
            {
                DatabaseCoinLoader.OnPlayerCoinsLoaded -= OnCoinsLoadedFromDatabase;
            }
        }
        
        /// <summary>
        /// Called when coins are loaded from database
        /// Force refresh all slots to display loaded coins
        /// </summary>
        private void OnCoinsLoadedFromDatabase()
        {
            RefreshAll();
        }

        [ContextMenu("Rebuild Slots")]
        public void BuildSlots()
        {
            ClearChildren(slotsParent);
            uiSlots.Clear();

            if (CoinInventorySystem.Instance == null)
            {
                Debug.LogError("[UICoinInventoryController] No CoinInventorySystem found!");
                Debug.LogError("   → Solution: Tạo GameObject 'CoinInventorySystem' trong Scene và add script CoinInventorySystem");
                return;
            }
            
            if (slotPrefab == null)
            {
                Debug.LogError("[UICoinInventoryController] Slot Prefab is NULL!");
                Debug.LogError("   → Solution: Gán prefab 'CoinSlots' vào field 'Slot Prefab' trong Inspector");
                return;
            }
            
            if (slotsParent == null)
            {
                Debug.LogError("[UICoinInventoryController] Slots Parent is NULL!");
                Debug.LogError("   → Solution: Gán Transform parent (Grid Layout Group) vào field 'Slots Parent'");
                return;
            }

            int capacity = CoinInventorySystem.Instance.Capacity;

            for (int i = 0; i < capacity; i++)
            {
                var slot = Instantiate(slotPrefab, slotsParent);
                slot.BindIndex(i);
                slot.Deselect();
                slot.OnCoinClicked += HandleCoinClicked;
                uiSlots.Add(slot);
            }

            Debug.Log($"✅ [UICoinInventoryController] Built {uiSlots.Count} coin UI slots");
            RefreshAll();
        }

        public void RefreshAll()
        {
            if (CoinInventorySystem.Instance == null)
            {
                for (int i = 0; i < uiSlots.Count; i++)
                    uiSlots[i].Render(null);
                return;
            }

            var slots = CoinInventorySystem.Instance.CoinSlots;
            int n = Mathf.Min(uiSlots.Count, slots.Count);

            for (int i = 0; i < n; i++)
            {
                uiSlots[i].Render(slots[i] as CoinSlot);
            }

            for (int i = n; i < uiSlots.Count; i++)
                uiSlots[i].Render(null);
        }

        private void RefreshSlot(int index)
        {
            if (index < 0 || index >= uiSlots.Count) return;
            if (CoinInventorySystem.Instance == null) return;

            var slots = CoinInventorySystem.Instance.CoinSlots;
            if (index < slots.Count)
            {
                uiSlots[index].Render(slots[index] as CoinSlot);
            }
        }

        private void HandleCoinClicked(UIInventoryCoins clicked)
        {
            if (selected && selected != clicked) selected.Deselect();
            selected = clicked;
            selected.Select();

            var slot = CoinInventorySystem.Instance?.CoinSlots.Count > clicked.Index
                ? CoinInventorySystem.Instance?.CoinSlots[clicked.Index] as CoinSlot
                : null;

            if (slot == null || slot.IsEmpty)
            {
                Debug.Log($"[UICoinInventory] Click slot {clicked.Index}: (empty)");
            }
            else
            {
                Debug.Log($"[UICoinInventory] Click slot {clicked.Index}: {slot.coin.coinName} x{slot.amount}");
            }
        }

        void ClearChildren(Transform parent)
        {
            if (!parent) return;
            if (Application.isPlaying)
            {
                foreach (Transform t in parent) Destroy(t.gameObject);
            }
            else
            {
                var list = new List<Transform>();
                foreach (Transform t in parent) list.Add(t);
                foreach (var t in list) DestroyImmediate(t.gameObject);
            }
        }
    }
}
