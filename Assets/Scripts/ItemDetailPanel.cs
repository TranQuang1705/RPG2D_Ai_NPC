using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Quản lý panel hiển thị chi tiết item bên phải inventory
/// Hiển thị: Hình ảnh, Tên, Chi tiết (mô tả, giá bán, loại, trọng lượng)
/// </summary>
public class ItemDetailPanel : MonoBehaviour
{
    public static ItemDetailPanel Instance { get; private set; }

    [Header("UI References")]
    [SerializeField] private GameObject panelRoot;
    [SerializeField] private Image itemImage;
    [SerializeField] private TMP_Text itemNameText;
    [SerializeField] private TMP_Text itemDetailText;

    [Header("Optional - Individual Detail Fields")]
    [SerializeField] private TMP_Text descriptionText;
    [SerializeField] private TMP_Text priceText;
    [SerializeField] private TMP_Text typeText;
    [SerializeField] private TMP_Text weightText;
    [SerializeField] private TMP_Text rarityText;

    [Header("Settings")]
    [SerializeField] private bool hideOnStart = true;
    [SerializeField] private bool useIndividualFields = false;

    private ItemSO currentItem;

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;

        if (hideOnStart)
            Hide();
    }

    public void ShowDetails(ItemSO item)
    {
        if (item == null)
        {
            Hide();
            return;
        }

        currentItem = item;
        if (panelRoot != null)
            panelRoot.SetActive(true);

        // Hình ảnh
        if (itemImage != null)
        {
            if (item.icon != null)
            {
                itemImage.sprite = item.icon;
                itemImage.enabled = true;
            }
            else
                itemImage.enabled = false;
        }

        // Tên
        if (itemNameText != null)
            itemNameText.text = item.displayName;

        // Hiển thị chi tiết
        if (useIndividualFields)
            UpdateIndividualFields(item);
        else
            UpdateCombinedDetail(item);
    }

    private void UpdateCombinedDetail(ItemSO item)
    {
        if (itemDetailText == null) return;

        string details = "";
        if (!string.IsNullOrEmpty(item.description))
            details += $"{item.description}\n\n";

        details += "<b>Detail:</b>\n";
        details += $"• Price: {FormatCurrencyText(item.value)}\n";
        details += $"• Category: <color=#87CEEB>{GetItemTypeDisplay(item.itemType)}</color>\n";
        details += $"• Weight: {item.weight:F2} kg\n";
        details += $"• Rarity: {GetRarityDisplay(item.rarity)}\n";

        itemDetailText.text = details;
    }

    private void UpdateIndividualFields(ItemSO item)
    {
        if (descriptionText != null)
            descriptionText.text = item.description;

        if (priceText != null)
            priceText.text = FormatCurrencyText(item.value);

        if (typeText != null)
            typeText.text = GetItemTypeDisplay(item.itemType);

        if (weightText != null)
            weightText.text = $"{item.weight:F2} kg";

        if (rarityText != null)
            rarityText.text = GetRarityDisplay(item.rarity);
    }

    private string FormatCurrencyText(int value)
    {
        if (value >= 10000)
        {
            float aurum = value / 10000f;
            return $"{aurum:0.##} Aurum";
        }
        else if (value >= 100)
        {
            float sylv = value / 100f;
            return $"{sylv:0.##} Sylv";
        }
        else
        {
            return $"{value} Obal";
        }
    }

    public void Hide()
    {
        currentItem = null;
        if (panelRoot != null)
            panelRoot.SetActive(false);
    }

    private string GetItemTypeDisplay(string itemType)
    {
        return itemType.ToLower() switch
        {
            "weapon" => "Weapon",
            "armor" => "Armor",
            "food" => "Food",
            "potion" => "Potion",
            "material" => "Material",
            "quest" => "Quest",
            "misc" => "Misc",
            "flower" => "Flower",
            "seed" => "Seed",
            "tool" => "Tool",
            _ => itemType
        };
    }

    private string GetRarityDisplay(string rarity)
    {
        return rarity.ToLower() switch
        {
            "common" => "<color=#FFFFFF>Common</color>",
            "uncommon" => "<color=#1EFF00>Uncommon</color>",
            "rare" => "<color=#0070DD>Rare</color>",
            "epic" => "<color=#A335EE>Epic</color>",
            "legendary" => "<color=#FF8000>Legendary</color>",
            _ => rarity
        };
    }
}
