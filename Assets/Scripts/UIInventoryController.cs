using System.Collections.Generic;
using UnityEngine;

namespace Inventory.UI
{
    public class UIInventoryPanel : MonoBehaviour
    {
        [Header("Build")]
        [SerializeField] private Transform slotsParent;     // Grid/Content
        [SerializeField] private UIInventoryItem slotPrefab;
        [SerializeField] private bool rebuildOnAwake = true;

        [Header("Slot Count Control")]
        [Tooltip("Bật để dùng số ô của InventorySystem; tắt để nhập bằng tay.")]
        [SerializeField] private bool useInventoryCapacity = true;

        [Tooltip("Số ô tạo ra khi không dùng InventorySystem")]
        [Min(1)] [SerializeField] private int manualSlotCount = 24;

        private readonly List<UIInventoryItem> uiSlots = new();
        private UIInventoryItem selected;

        void Awake()
        {
            if (rebuildOnAwake) BuildSlots();
        }

        void OnEnable()
        {
            if (InventorySystem.Instance != null)
                InventorySystem.Instance.OnInventoryChanged += RefreshAll;

            RefreshAll();
        }

        void OnDisable()
        {
            if (InventorySystem.Instance != null)
                InventorySystem.Instance.OnInventoryChanged -= RefreshAll;
        }

        [ContextMenu("Rebuild Slots")]
        public void BuildSlots()
        {
            ClearChildren(slotsParent);
            uiSlots.Clear();

            int capacity = GetTargetCapacity();

            for (int i = 0; i < capacity; i++)
            {
                var slot = Instantiate(slotPrefab, slotsParent);
                slot.BindIndex(i);
                slot.Deselect();
                slot.OnItemClicked += HandleItemClicked;
                uiSlots.Add(slot);
            }

            // Sau khi build xong thì render theo dữ liệu có sẵn (nếu có)
            RefreshAll();
        }

        public void RefreshAll()
        {
            if (InventorySystem.Instance == null)
            {
                // Không có InventorySystem -> chỉ xóa/clear hình ảnh các slot
                for (int i = 0; i < uiSlots.Count; i++)
                    uiSlots[i].Render(null);
                return;
            }

            var data = InventorySystem.Instance.Slots;
            int n = Mathf.Min(uiSlots.Count, data.Count);

            for (int i = 0; i < n; i++)
                uiSlots[i].Render(data[i]);

            // Nếu UI có nhiều slot hơn dữ liệu, phần dư để trống
            for (int i = n; i < uiSlots.Count; i++)
                uiSlots[i].Render(null);
        }

        private void HandleItemClicked(UIInventoryItem clicked)
        {
            if (selected && selected != clicked) selected.Deselect();
            selected = clicked;
            selected.Select();

            var slot = InventorySystem.Instance?.Slots.Count > clicked.Index
                ? InventorySystem.Instance?.Slots[clicked.Index]
                : null;

            if (slot == null || slot.IsEmpty)
            {
                Debug.Log($"[UI] Click slot {clicked.Index}: (empty)");
                // Ẩn panel chi tiết khi click vào slot trống
                if (ItemDetailPanel.Instance != null)
                    ItemDetailPanel.Instance.Hide();
            }
            else
            {
                Debug.Log($"[UI] Click slot {clicked.Index}: {slot.item.displayName} x{slot.quantity}");
                // Hiển thị panel chi tiết khi click vào item
                if (ItemDetailPanel.Instance != null)
                    ItemDetailPanel.Instance.ShowDetails(slot.item);
            }
        }

        int GetTargetCapacity()
        {
            if (useInventoryCapacity && InventorySystem.Instance != null)
                return InventorySystem.Instance.Slots.Count;
            return Mathf.Max(1, manualSlotCount);
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
                // Trong Edit mode cần DestroyImmediate để không để lại object giả
                var list = new List<Transform>();
                foreach (Transform t in parent) list.Add(t);
                foreach (var t in list) DestroyImmediate(t.gameObject);
            }
        }

        // Khi đang chạy mà bạn đổi giá trị Manual Slot Count → bấm nút ContextMenu hoặc gọi hàm này
        public void SetManualSlotCount(int count, bool rebuild = true)
        {
            manualSlotCount = Mathf.Max(1, count);
            if (rebuild) BuildSlots();
        }

        public void SetUseInventoryCapacity(bool use, bool rebuild = true)
        {
            useInventoryCapacity = use;
            if (rebuild) BuildSlots();
        }
    }
}
