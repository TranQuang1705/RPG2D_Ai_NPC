using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// UI hiển thị thông tin thời gian game (ngày/đêm, giờ, ngày thứ)
/// </summary>
public class TimeUI : MonoBehaviour
{
    [Header("UI Elements")]
    public TextMeshProUGUI timeText;
    public TextMeshProUGUI dayText;
    public TextMeshProUGUI yearSeasonText;
    public TextMeshProUGUI periodText;
    public Image dayNightIcon;
    
    [Header("Visual Settings")]
    [SerializeField] private bool useSeasonalIcons = true;
    public Sprite dayIcon;
    public Sprite nightIcon;
    public Color dayColor = Color.white;
    public Color nightColor = Color.gray;
    
    [Header("Season Colors")]
    public Color springColor = new Color(1f, 0.7f, 0.8f, 1f); // Cherry blossom pink
    public Color summerColor = Color.yellow;
    public Color autumnColor = new Color(1f, 0.6f, 0f, 1f);
    public Color winterColor = new Color(0.8f, 0.9f, 1f, 1f);
    
    [Header("Update Settings")]
    public bool updateTimeEverySecond = true;
    public float updateInterval = 1f;
    
    private float lastUpdateTime;
    
    void Start()
    {
        // Cập nhật UI ban đầu
        UpdateTimeDisplay();
    }
    
    void Update()
    {
        if (updateTimeEverySecond && Time.time - lastUpdateTime >= updateInterval)
        {
            UpdateTimeDisplay();
            lastUpdateTime = Time.time;
        }
    }
    
    void UpdateTimeDisplay()
    {
        if (TimeManager.Instance == null) return;
        
        // Lấy thông tin từ TimeManager
        string currentTime = TimeManager.Instance.GetCurrentTimeString();
        int currentDay = TimeManager.Instance.GetCurrentDay();
        int dayInSeason = TimeManager.Instance.GetDayInSeason();
        int currentYear = TimeManager.Instance.GetCurrentYear();
        Season currentSeason = TimeManager.Instance.GetCurrentSeason();
        bool isDaytime = TimeManager.Instance.IsDaytime();
        
        // Lấy màu theo mùa
        Color seasonColor = GetSeasonColor(currentSeason);
        
        // Cập nhật text
        if (timeText != null)
            timeText.text = currentTime;
            
        if (dayText != null)
            dayText.text = $"{currentSeason}";
            
        if (yearSeasonText != null)
            yearSeasonText.text = $"Day {dayInSeason} - Year {currentYear}";
            
        if (periodText != null)
            periodText.text = isDaytime ? "Day" : "Night";
            
        // Cập nhật icon và màu sắc
        if (dayNightIcon != null)
        {
            // Use seasonal icons if available and enabled
            if (useSeasonalIcons && TimeManager.Instance != null)
            {
                Sprite seasonalIcon = TimeManager.Instance.GetCurrentSeasonalIcon();
                if (seasonalIcon != null)
                {
                    dayNightIcon.sprite = seasonalIcon;
                }
                else
                {
                    // Fallback to basic day/night icons
                    dayNightIcon.sprite = isDaytime ? dayIcon : nightIcon;
                }
            }
            else
            {
                // Use basic day/night icons
                dayNightIcon.sprite = isDaytime ? dayIcon : nightIcon;
            }
            dayNightIcon.color = isDaytime ? dayColor : nightColor;
        }
        
        // Cập nhật màu cho text theo thời gian và mùa
        if (timeText != null)
            timeText.color = isDaytime ? dayColor : nightColor;
            
        if (dayText != null)
            dayText.color = Color.Lerp(isDaytime ? dayColor : nightColor, seasonColor, 0.5f);
            
        if (yearSeasonText != null)
            yearSeasonText.color = seasonColor;
            
        if (periodText != null)
            periodText.color = isDaytime ? dayColor : nightColor;
    }
    
    // Public method để cập nhật thủ công khi cần
    [ContextMenu("Force Update Time Display")]
    public void ForceUpdateTimeDisplay()
    {
        UpdateTimeDisplay();
    }
    
    // Đăng ký sự kiện để cập nhật khi thời gian thay đổi
    Color GetSeasonColor(Season season)
    {
        switch (season)
        {
            case Season.Spring: return springColor;
            case Season.Summer: return summerColor;
            case Season.Autumn: return autumnColor;
            case Season.Winter: return winterColor;
            default: return Color.white;
        }
    }
    

    
    void OnEnable()
    {
        if (TimeManager.Instance != null)
        {
            TimeManager.Instance.onHourChanged.AddListener(UpdateTimeDisplay);
            TimeManager.Instance.onDayStart.AddListener(UpdateTimeDisplay);
            TimeManager.Instance.onNightStart.AddListener(UpdateTimeDisplay);
            TimeManager.Instance.onSeasonChanged.AddListener(UpdateTimeDisplay);
        }
    }
    
    void OnDisable()
    {
        if (TimeManager.Instance != null)
        {
            TimeManager.Instance.onHourChanged.RemoveListener(UpdateTimeDisplay);
            TimeManager.Instance.onDayStart.RemoveListener(UpdateTimeDisplay);
            TimeManager.Instance.onNightStart.RemoveListener(UpdateTimeDisplay);
            TimeManager.Instance.onSeasonChanged.RemoveListener(UpdateTimeDisplay);
        }
    }
}
