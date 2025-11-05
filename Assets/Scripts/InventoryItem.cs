using System;
using System.Collections.Generic;
using UnityEngine;

// ============================ INVENTORY SYSTEM ============================
public class InventorySystem : MonoBehaviour
{
    public static InventorySystem Instance { get; private set; }

    [Header("Config")]
    [Min(1)] public int capacity = 12;                    // số ô
    [SerializeField] private List<InventorySlotBag> slots = new();

    // UI có thể lắng nghe 2 loại sự kiện này để refresh
    public event Action OnInventoryChanged;
    public event Action<int> OnSlotChanged;               // truyền index slot

    public IReadOnlyList<InventorySlotBag> Slots => slots;
    public int Capacity => slots.Count;
    
    // Provide access to internal slots list for modification
    public List<InventorySlotBag> GetInternalSlots() => slots;
    
    // Public method to trigger inventory changed event
    public void NotifyInventoryChanged()
    {
        OnInventoryChanged?.Invoke();
    }

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;

        // Force override to 12 slots regardless of Inspector value
        capacity = 12;
        slots = new List<InventorySlotBag>(capacity);
        for (int i = 0; i < capacity; i++) slots.Add(new InventorySlotBag());
        
        Debug.Log($"[InventorySystem] Initialized with capacity {capacity} slots.");
    }

    // ---------- ADD ----------
    /// Thêm 'amount' item vào túi, trả về còn dư (không nhét được).
    public int AddItem(ItemSO item, int amount)
    {
        if (!item || amount <= 0) return amount;
        int remaining = amount;

        if (item.stackable)
        {
            // 1) Lấp đầy các stack có sẵn
            for (int i = 0; i < slots.Count && remaining > 0; i++)
            {
                if (slots[i].CanStackWith(item))
                {
                    int canAdd = Mathf.Min(item.maxStack - slots[i].quantity, remaining);
                    if (canAdd > 0)
                    {
                        slots[i].quantity += canAdd;
                        remaining -= canAdd;
                        OnSlotChanged?.Invoke(i);
                        Debug.Log($"[Inventory] Stack {item.displayName} +{canAdd} -> slot {i} ({slots[i].quantity}/{item.maxStack})");
                    }
                }
            }
            // 2) Đổ vào ô trống
            for (int i = 0; i < slots.Count && remaining > 0; i++)
            {
                if (slots[i].IsEmpty)
                {
                    int add = Mathf.Min(item.maxStack, remaining);
                    slots[i].item = item;
                    slots[i].quantity = add;
                    remaining -= add;
                    OnSlotChanged?.Invoke(i);
                    Debug.Log($"[Inventory] New stack {item.displayName} x{add} -> slot {i}");
                }
            }
        }
        else
        {
            // Không stack: mỗi ô 1 cái
            for (int i = 0; i < slots.Count && remaining > 0; i++)
            {
                if (slots[i].IsEmpty)
                {
                    slots[i].item = item;
                    slots[i].quantity = 1;
                    remaining -= 1;
                    OnSlotChanged?.Invoke(i);
                    Debug.Log($"[Inventory] Place non-stack {item.displayName} -> slot {i}");
                }
            }
        }

        if (remaining != amount) OnInventoryChanged?.Invoke();
        if (remaining > 0) Debug.LogWarning($"[Inventory] FULL: leftover {item.displayName} = {remaining}");
        return remaining;
    }

    public bool TryAdd(ItemSO item, int amount, out int leftover)
    {
        leftover = AddItem(item, amount);
        return leftover == 0;
    }

    // ---------- REMOVE ----------
    public int Remove(ItemSO item, int amount)
    {
        if (!item || amount <= 0) return 0;
        int removed = 0;
        for (int i = 0; i < slots.Count && removed < amount; i++)
        {
            if (slots[i].item == item && slots[i].quantity > 0)
            {
                int take = Mathf.Min(slots[i].quantity, amount - removed);
                slots[i].quantity -= take;
                removed += take;
                if (slots[i].quantity == 0) slots[i].Clear();
                OnSlotChanged?.Invoke(i);
            }
        }
        if (removed > 0) OnInventoryChanged?.Invoke();
        return removed;
    }

    public int CountOf(ItemSO item)
    {
        int sum = 0;
        foreach (var s in slots) if (s.item == item) sum += s.quantity;
        return sum;
    }

    // ---------- MOVE / MERGE / SWAP ----------
    /// Di chuyển từ slot A sang slot B.
    /// - Nếu B trống: move.
    /// - Nếu cùng item và stackable: merge tới maxStack.
    /// - Nếu khác loại: swap.
    public bool TryMove(int from, int to)
    {
        if (!IndexValid(from) || !IndexValid(to) || from == to) return false;
        var a = slots[from];
        var b = slots[to];
        if (a.IsEmpty) return false;

        bool changed = false;

        // Merge
        if (!b.IsEmpty && a.item == b.item && a.item.stackable)
        {
            int canAdd = a.item.maxStack - b.quantity;
            if (canAdd > 0)
            {
                int moved = Mathf.Min(canAdd, a.quantity);
                b.quantity += moved;
                a.quantity -= moved;
                if (a.quantity <= 0) a.Clear();
                changed = moved > 0;
            }
        }
        // Move
        else if (b.IsEmpty)
        {
            b.item = a.item;
            b.quantity = a.quantity;
            a.Clear();
            changed = true;
        }
        // Swap
        else
        {
            (slots[to], slots[from]) = (slots[from], slots[to]);
            changed = true;
        }

        if (changed)
        {
            OnSlotChanged?.Invoke(from);
            OnSlotChanged?.Invoke(to);
            OnInventoryChanged?.Invoke();
        }
        return changed;
    }

    public void ClearSlot(int index)
    {
        if (!IndexValid(index)) return;
        slots[index].Clear();
        OnSlotChanged?.Invoke(index);
        OnInventoryChanged?.Invoke();
    }

    bool IndexValid(int i) => i >= 0 && i < slots.Count;
}

[Serializable]
public class InventorySlotBag
{
    public ItemSO item;
    public int quantity;

    public bool IsEmpty => item == null || quantity <= 0;

    public void Clear() { item = null; quantity = 0; }

    public bool CanStackWith(ItemSO other)
        => !IsEmpty && item == other && item.stackable && quantity < item.maxStack;
}

// ============================ WORLD PICKUP ============================
// Gắn component này lên prefab rơi (Táo, Kiếm...). Kéo ItemSO + amount.
[RequireComponent(typeof(Collider2D))]
public class PickupItem : MonoBehaviour
{
    public ItemSO item;
    [Min(1)] public int amount = 1;
    public string playerTag = "Player";
    public bool destroyWhenPicked = true;   // true: nhặt hết thì Destroy

    void Reset()
    {
        var col = GetComponent<Collider2D>();
        col.isTrigger = true;
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (!other.CompareTag(playerTag)) return;

        if (InventorySystem.Instance == null)
        {
            Debug.LogError("[PickupItem] No InventorySystem in scene!");
            return;
        }

        int leftover = InventorySystem.Instance.AddItem(item, amount);
        int picked = amount - leftover;
        Debug.Log($"[PickupItem] Picked {item.displayName} x{picked} (leftover {leftover})");

        if (destroyWhenPicked && picked > 0 && leftover == 0) Destroy(gameObject);
        else if (picked > 0 && leftover > 0) amount = leftover;   // còn dư thì cập nhật
    }
}
