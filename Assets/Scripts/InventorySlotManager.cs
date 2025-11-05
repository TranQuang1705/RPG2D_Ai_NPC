using UnityEngine;
using System.Collections.Generic;
using System.Linq;

// Quản lý logic tìm slot trống và sắp xếp items trong inventory
public class InventorySlotManager : MonoBehaviour
{
    public static InventorySlotManager Instance { get; private set; }
    
    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }
    
    /// <summary>
    /// Tìm slot trống tiếp theo
    /// </summary>
    /// <returns>Index của slot trống, hoặc -1 nếu không có</returns>
    public int FindEmptySlot()
    {
        if (InventorySystem.Instance == null) return -1;
        
        var slots = InventorySystem.Instance.GetInternalSlots();
        
        for (int i = 0; i < slots.Count; i++)
        {
            if (slots[i].IsEmpty)
            {
                return i;
            }
        }
        
        return -1; // Không có slot trống
    }
    
    /// <summary>
    /// Tìm slot có thể stack với item này
    /// </summary>
    /// <param name="item">Item cần stack</param>
    /// <param name="amount">Số lượng cần thêm</param>
    /// <returns>Tuple (slotIndex, canStackAmount)</returns>
    public (int slotIndex, int canStackAmount) FindStackableSlot(ItemSO item, int amount)
    {
        if (!item.stackable || InventorySystem.Instance == null) 
            return (-1, 0);
        
        var slots = InventorySystem.Instance.GetInternalSlots();
        
        // Ưu tiên stack vào slot cùng loại có số lượng ít nhất trước
        var stackableSlots = new List<(int index, int availableSpace)>();
        
        for (int i = 0; i < slots.Count; i++)
        {
            var slot = slots[i];
            if (slot.CanStackWith(item))
            {
                int availableSpace = item.maxStack - slot.quantity;
                if (availableSpace > 0)
                {
                    stackableSlots.Add((i, availableSpace));
                }
            }
        }
        
        // Sắp xếp theo availableSpace tăng dần để ưu tiên lấp đầy các stack gần đầy
        stackableSlots = stackableSlots.OrderBy(x => x.availableSpace).ToList();
        
        foreach (var (index, availableSpace) in stackableSlots)
        {
            if (availableSpace >= amount)
            {
                return (index, amount); // Có thể stack hết vào slot này
            }
        }
        
        // Không có slot nào đủ, trả về slot có nhiều chỗ trống nhất
        if (stackableSlots.Any())
        {
            var bestSlot = stackableSlots.OrderByDescending(x => x.availableSpace).First();
            return (bestSlot.index, bestSlot.availableSpace);
        }
        
        return (-1, 0); // Không thể stack
    }
    
    /// <summary>
    /// Tìm vị trí tối ưu để thêm item
    /// </summary>
    /// <param name="item">Item cần thêm</param>
    /// <param name="amount">Số lượng cần thêm</param>
    /// <returns>Danh sách các positions để thêm item</returns>
    public List<(int slotIndex, int addAmount)> GetOptimalPositions(ItemSO item, int amount)
    {
        var result = new List<(int, int)>();
        int remaining = amount;
        
        if (item.stackable && remaining > 0)
        {
            // Thử stack vào các slot có sẵn
            while (remaining > 0)
            {
                var (slotIndex, canStack) = FindStackableSlot(item, remaining);
                
                if (slotIndex == -1 || canStack <= 0) break;
                
                result.Add((slotIndex, canStack));
                remaining -= canStack;
            }
        }
        
        // Thêm vào các slot trống cho số lượng còn lại
        while (remaining > 0)
        {
            int emptySlot = FindEmptySlot();
            if (emptySlot == -1) break; // Inventory full
            
            // Không stackable: chỉ thêm 1 item mỗi slot
            if (!item.stackable)
            {
                result.Add((emptySlot, 1));
                remaining -= 1;
            }
            else
            {
                // Stackable: thêm tối đa maxStack vào mỗi slot
                int addAmount = Mathf.Min(item.maxStack, remaining);
                result.Add((emptySlot, addAmount));
                remaining -= addAmount;
            }
        }
        
        return result;
    }
    
    /// <summary>
    /// Sắp xếp inventory theo loại item (nhóm các loại item giống nhau lại gần nhau)
    /// </summary>
    public void SortInventory()
    {
        if (InventorySystem.Instance == null) return;
        
        var slots = InventorySystem.Instance.GetInternalSlots();
        var itemGroups = new Dictionary<string, List<(int index, InventorySlotBag slot)>>();
        
        // Nhóm items theo loại
        for (int i = 0; i < slots.Count; i++)
        {
            var slot = slots[i];
            if (!slot.IsEmpty)
            {
                string groupKey = GetItemGroupKey(slot.item);
                
                if (!itemGroups.ContainsKey(groupKey))
                {
                    itemGroups[groupKey] = new List<(int, InventorySlotBag)>();
                }
                
                itemGroups[groupKey].Add((i, slot));
            }
        }
        
        // Sắp xếp các nhóm theo thứ tự ưu tiên
        var groupOrder = new List<string> { "weapon", "armor", "food", "material", "misc" };
        var sortedGroups = itemGroups.OrderBy(kvp => 
        {
            int index = groupOrder.IndexOf(kvp.Key);
            return index == -1 ? groupOrder.Count : index;
        }).ToList();
        
        // Sắp xếp lại các slot trong inventory
        int currentSlot = 0;
        foreach (var group in sortedGroups)
        {
            foreach (var (originalIndex, slot) in group.Value.OrderBy(x => x.slot.item.displayName))
            {
                if (currentSlot < slots.Count)
                {
                    // Di chuyển item đến vị trí mới
                    if (originalIndex != currentSlot)
                    {
                        slots[currentSlot] = slot;
                        slots[originalIndex] = new InventorySlotBag(); // Clear old slot
                    }
                    currentSlot++;
                }
            }
        }
        
        // Clear các slot còn lại
        for (int i = currentSlot; i < slots.Count; i++)
        {
            if (!slots[i].IsEmpty)
            {
                slots[i] = new InventorySlotBag();
            }
        }
        
        // Notify UI update
        InventorySystem.Instance.NotifyInventoryChanged();
    }
    
    /// <summary>
    /// GOM các đồ đồng loại lại với nhau (merge stacks)
    /// </summary>
    public void ConsolidateStacks()
    {
        if (InventorySystem.Instance == null) return;
        
        var slots = InventorySystem.Instance.GetInternalSlots();
        
        // Đọc qua các slot và merge
        for (int i = 0; i < slots.Count; i++)
        {
            var slotA = slots[i];
            if (slotA.IsEmpty || !slotA.item.stackable) continue;
            
            // Tìm các slot khác có cùng item
            for (int j = i + 1; j < slots.Count; j++)
            {
                var slotB = slots[j];
                if (slotB.IsEmpty || slotA.item != slotB.item) continue;
                
                // Merge vào slot A
                int canMerge = Mathf.Min(slotA.item.maxStack - slotA.quantity, slotB.quantity);
                if (canMerge > 0)
                {
                    slotA.quantity += canMerge;
                    slotB.quantity -= canMerge;
                    
                    if (slotB.quantity <= 0)
                    {
                        slotB.Clear();
                    }
                }
            }
        }
        
        // Sắp xếp lại để loại bỏ các slot trống giữa các vật phẩm
        CompactInventory();
        
        InventorySystem.Instance.NotifyInventoryChanged();
    }
    
    /// <summary>
    /// Nén inventory - lấp đầy các slot trỗng giữa các vật phẩm
    /// </summary>
    public void CompactInventory()
    {
        if (InventorySystem.Instance == null) return;
        
        var slots = InventorySystem.Instance.GetInternalSlots();
        var occupiedSlots = new List<InventorySlotBag>();
        
        // Thu thập các slot có vật phẩm
        foreach (var slot in slots)
        {
            if (!slot.IsEmpty)
            {
                occupiedSlots.Add(slot);
            }
        }
        
        // Clear toàn bộ inventory
        for (int i = 0; i < slots.Count; i++)
        {
            slots[i] = new InventorySlotBag();
        }
        
        // Đặt lại các vật phẩm từ đầu
        for (int i = 0; i < occupiedSlots.Count && i < slots.Count; i++)
        {
            slots[i] = occupiedSlots[i];
        }
    }
    
    /// <summary>
    /// Lấy key để nhóm items theo loại
    /// </summary>
    private string GetItemGroupKey(ItemSO item)
    {
        if (item == null) return "misc";
        
        // Determine group based on item type or name patterns
        switch (item.itemType.ToLower())
        {
            case "weapon":
            case "sword":
            case "bow":
                return "weapon";
                
            case "armor":
            case "helmet":
            case "chestplate":
                return "armor";
                
            case "food":
            case "potion":
                return "food";
                
            case "material":
            case "ore":
            case "wood":
                return "material";
                
            default:
                return "misc";
        }
    }
    
    /// <summary>
    /// Lấy tổng số slot trống
    /// </summary>
    public int GetEmptySlotCount()
    {
        if (InventorySystem.Instance == null) return 0;
        
        var slots = InventorySystem.Instance.GetInternalSlots();
        return slots.Count(slot => slot.IsEmpty);
    }
    
    /// <summary>
    /// Kiểm tra inventory có đủ chỗ cho item này không
    /// </summary>
    public bool CanAddItem(ItemSO item, int amount)
    {
        if (InventorySystem.Instance == null) return false;
        
        if (!item.stackable)
        {
            return GetEmptySlotCount() >= amount;
        }
        
        // Đếm tổng số chỗ trống cho stackable items
        int totalSpace = 0;
        var slots = InventorySystem.Instance.GetInternalSlots();
        
        foreach (var slot in slots)
        {
            if (slot.IsEmpty)
            {
                totalSpace += item.maxStack;
            }
            else if (slot.CanStackWith(item))
            {
                totalSpace += item.maxStack - slot.quantity;
            }
        }
        
        return totalSpace >= amount;
    }
    
    /// <summary>
    /// Lấy thống kê inventory: số lượng các loại item khác nhau
    /// </summary>
    public Dictionary<string, int> GetItemCountByType()
    {
        var stats = new Dictionary<string, int>();
        
        if (InventorySystem.Instance == null) return stats;
        
        var slots = InventorySystem.Instance.GetInternalSlots();
        foreach (var slot in slots)
        {
            if (!slot.IsEmpty)
            {
                string key = GetItemGroupKey(slot.item);
                if (!stats.ContainsKey(key))
                {
                    stats[key] = 0;
                }
                stats[key] += slot.quantity;
            }
        }
        
        return stats;
    }
}
