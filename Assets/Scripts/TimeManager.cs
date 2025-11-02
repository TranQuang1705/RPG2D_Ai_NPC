using System.Collections;
using UnityEngine;
using UnityEngine.Rendering.Universal;

public enum Season { Spring, Summer, Autumn, Winter }

public class TimeManager : MonoBehaviour
{
    [Header("Time Settings")]
    public int minutesPerDay = 30; // 1 ngày trong game = 30 phút ngoài đời thực
    public float currentTime = 6f; // Bắt đầu vào lúc 6:00 sáng (6.0 = 6:00)

    [Header("Day/Night Settings")]
    [Range(6, 20)] public float dayStartHour = 6f;
    [Range(6, 20)] public float dayEndHour = 20f;

    [Header("Season Settings")]
    public int daysPerSeason = 30; // Mỗi mùa kéo dài 30 ngày trong game (tương đương 15h thực tế)
    public Season currentSeason = Season.Spring;

    [Header("Lighting")]
    public Light2D globalLight2D;
    public AnimationCurve lightIntensityCurve;

    [Header("Time Period Colors - 4 Phases")]
    public Color morningColor = new Color(1.0f, 0.8f, 0.6f, 1f); // 6-10, ánh sáng vàng ấm
    public Color noonColor = new Color(1.0f, 1.0f, 0.9f, 1f); // 10-14, trắng sáng
    public Color afternoonColor = new Color(1.0f, 0.8f, 0.7f, 1f); // 14-18, vàng cam
    public Color eveningColor = new Color(0.8f, 0.4f, 0.2f, 1f); // 18-22, đỏ cam tối
    public Color nightColor = new Color(0.1f, 0.1f, 0.3f, 1f); // 22-6, xanh đen tối

    [Header("Ambient Lighting")]
    public GameObject ambientEnvironment;
    public float nightTimeAmbientIntensity = 0.1f;
    public float eveningTimeAmbientIntensity = 0.3f;
    public AnimationCurve ambientIntensityCurve;

    [Header("Skybox/Environment")]
    public Material skyboxMaterial;
    public Color daySkyColor = new Color(0.5f, 0.7f, 1.0f, 1f);
    public Color eveningSkyColor = new Color(0.8f, 0.3f, 0.2f, 1f);
    public Color nightSkyColor = new Color(0.05f, 0.05f, 0.2f, 1f);

    [Header("Audio")]
    public AudioSource ambientAudio;
    public AudioClip dayAmbientSound;
    public AudioClip nightAmbientSound;

    [Header("Seasonal Icons - 8 Icons: Spring/Summer/Autumn/Winter x Day/Night")]
    public Sprite springDayIcon;
    public Sprite springNightIcon;
    public Sprite summerDayIcon;
    public Sprite summerNightIcon;
    public Sprite autumnDayIcon;
    public Sprite autumnNightIcon;
    public Sprite winterDayIcon;
    public Sprite winterNightIcon;

    [Header("Events")]
    public UnityEngine.Events.UnityEvent onDayStart;
    public UnityEngine.Events.UnityEvent onNightStart;
    public UnityEngine.Events.UnityEvent onHourChanged;
    public UnityEngine.Events.UnityEvent onSeasonChanged;

    private bool isDay = true;
    private int currentDay = 1;
    private int currentYear = 1;

    public static TimeManager Instance { get; private set; }

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            InitializeTime();
        }
        else
        {
            Destroy(gameObject);
        }
    }

    void InitializeTime()
    {
        currentTime = 6f; // Bắt đầu vào 6:00 sáng
        UpdateLighting();
    }

    void Update()
    {
        // Tăng thời gian theo tỷ lệ: 24 giờ = minutesPerDay phút thực
        float timeIncreaseRate = 24f / (minutesPerDay * 60f);
        currentTime += Time.deltaTime * timeIncreaseRate;

        // Reset sau khi qua đêm
        if (currentTime >= 24f)
        {
            currentTime -= 24f;
            currentDay++;

            // Kiểm tra chuyển mùa
            CheckSeasonChange();

            Debug.Log($"Bắt đầu ngày mới: Ngày {currentDay}");
        }

        // Kiểm tra chuyển ngày/đêm
        bool wasDay = isDay;
        isDay = currentTime >= dayStartHour && currentTime < dayEndHour;

        if (wasDay != isDay)
        {
            if (isDay)
            {
                onDayStart?.Invoke();
                if (ambientAudio && nightAmbientSound)
                    ambientAudio.Stop();
                if (ambientAudio && dayAmbientSound)
                    ambientAudio.Play();
            }
            else
            {
                onNightStart?.Invoke();
                if (ambientAudio && dayAmbientSound)
                    ambientAudio.Stop();
                if (ambientAudio && nightAmbientSound)
                    ambientAudio.Play();
            }
        }

        // Kiểm tra thay đổi giờ
        if (Mathf.Floor(currentTime) != Mathf.Floor(currentTime - Time.deltaTime * timeIncreaseRate))
        {
            onHourChanged?.Invoke();
        }

        UpdateLighting();
    }

    void UpdateLighting()
    {
        if (globalLight2D == null) return;

        float hours = currentTime;
        Color targetColor = nightColor;
        float targetIntensity = 0.2f;

        // Sáng (6–10)
        if (hours >= 6f && hours < 10f)
        {
            float t = (hours - 6f) / 4f;
            targetColor = Color.Lerp(morningColor, noonColor, t);
            targetIntensity = Mathf.Lerp(0.6f, 1.0f, t);
        }
        // Trưa (10–14)
        else if (hours >= 10f && hours < 14f)
        {
            targetColor = noonColor;
            targetIntensity = 1.0f;
        }
        // Chiều (14–18)
        else if (hours >= 14f && hours < 18f)
        {
            float t = (hours - 14f) / 4f;
            targetColor = Color.Lerp(noonColor, afternoonColor, t);
            targetIntensity = Mathf.Lerp(1.0f, 0.6f, t);
        }
        // Tối (18–22)
        else if (hours >= 18f && hours < 22f)
        {
            float t = (hours - 18f) / 4f;
            targetColor = Color.Lerp(afternoonColor, eveningColor, t);
            targetIntensity = Mathf.Lerp(0.6f, 0.3f, t);
        }
        // Đêm (22–6)
        else
        {
            float t = (hours >= 22f ? (hours - 22f) / 8f : (hours + 2f) / 8f);
            targetColor = Color.Lerp(eveningColor, nightColor, t);
            targetIntensity = Mathf.Lerp(0.3f, 0.15f, t);
        }

        // Làm mượt
        globalLight2D.intensity = Mathf.Lerp(globalLight2D.intensity, targetIntensity, Time.deltaTime * 2f);
        globalLight2D.color = Color.Lerp(globalLight2D.color, targetColor, Time.deltaTime * 2f);
    }


    void UpdateAmbientLighting(float lightIntensity)
    {
        if (ambientEnvironment == null) return;

        float ambientIntensity = ambientIntensityCurve.Evaluate(lightIntensity);

        // Find all renderers in environment and update their materials
        Renderer[] renderers = ambientEnvironment.GetComponentsInChildren<Renderer>();
        foreach (Renderer renderer in renderers)
        {
            foreach (Material material in renderer.materials)
            {
                if (material.HasProperty("_Color"))
                {
                    Color originalColor = material.GetColor("_Color");
                    float brightnessFactor = 0.3f + ambientIntensity * 0.7f; // Keep base 30% brightness
                    material.SetColor("_Color", originalColor * brightnessFactor);
                }
            }
        }
    }

    void UpdateEnvironmentLighting(float hours)
    {
        if (skyboxMaterial == null) return;

        Color skyColor;

        if (hours >= 6f && hours < 18f)
        {
            // Daytime - blue sky
            skyColor = daySkyColor;
        }
        else if (hours >= 18f && hours < 22f)
        {
            // Evening - orange/red sky
            skyColor = eveningSkyColor;
        }
        else
        {
            // Night - dark blue sky
            skyColor = nightSkyColor;
        }

        // Apply sky color
        if (RenderSettings.skybox != null)
        {
            RenderSettings.skybox.SetColor("_Tint", skyColor);
        }
        else if (skyboxMaterial != null)
        {
            skyboxMaterial.SetColor("_Tint", skyColor);
            RenderSettings.skybox = skyboxMaterial;
        }

        // Update ambient light color
        RenderSettings.ambientLight = skyColor * 0.3f; // Dim ambient light
        RenderSettings.reflectionIntensity = (hours >= 6f && hours < 20f) ? 1.0f : 0.1f;
    }

    float GetNormalizedTime()
    {
        // Chuyển thời gian 0-24 giờ thành 0-1
        return currentTime / 24f;
    }

    // Public methods để lấy thông tin thời gian
    public string GetCurrentTimeString()
    {
        int hours = Mathf.FloorToInt(currentTime);
        int minutes = Mathf.FloorToInt((currentTime - hours) * 60);
        return $"{hours:D2}:{minutes:D2}";
    }

    public int GetCurrentHour()
    {
        return Mathf.FloorToInt(currentTime);
    }

    public int GetCurrentDay()
    {
        return currentDay;
    }

    public Season GetCurrentSeason()
    {
        return currentSeason;
    }

    public int GetCurrentYear()
    {
        return currentYear;
    }

    public int GetDayInSeason()
    {
        int totalDays = (currentYear - 1) * 4 * daysPerSeason + currentDay;
        return (totalDays % (4 * daysPerSeason)) % daysPerSeason + 1;
    }

    void CheckSeasonChange()
    {
        int totalDaysPassed = (currentYear - 1) * 4 * daysPerSeason + currentDay;
        int seasonIndex = (totalDaysPassed - 1) / daysPerSeason % 4;
        Season newSeason = (Season)seasonIndex;

        if (newSeason != currentSeason)
        {
            currentSeason = newSeason;
            onSeasonChanged?.Invoke();
            Debug.Log($"Mùa mới: {GetSeasonName(currentSeason)}");
        }

        // Kiểm tra qua năm mới
        if (currentDay > 4 * daysPerSeason)
        {
            currentDay = 1;
            currentYear++;
        }
    }

    public string GetSeasonName(Season season)
    {
        switch (season)
        {
            case Season.Spring: return "Spring";
            case Season.Summer: return "Summer";
            case Season.Autumn: return "Autumn";
            case Season.Winter: return "Winter";
            default: return "Unknown";
        }
    }

    public bool IsDaytime()
    {
        return isDay;
    }

    public bool IsNighttime()
    {
        return !isDay;
    }

    public float GetDayProgress()
    {
        return currentTime / 24f;
    }

    public Sprite GetCurrentSeasonalIcon()
    {
        // Return appropriate icon based on current season and time of day
        if (isDay)
        {
            switch (currentSeason)
            {
                case Season.Spring:
                    return springDayIcon;
                case Season.Summer:
                    return summerDayIcon;
                case Season.Autumn:
                    return autumnDayIcon;
                case Season.Winter:
                    return winterDayIcon;
            }
        }
        else
        {
            switch (currentSeason)
            {
                case Season.Spring:
                    return springNightIcon;
                case Season.Summer:
                    return summerNightIcon;
                case Season.Autumn:
                    return autumnNightIcon;
                case Season.Winter:
                    return winterNightIcon;
            }
        }
        return null;
    }

    // Debug
    void OnGUI()
    {
        GUILayout.BeginArea(new Rect(10, 10, 300, 120));
        GUILayout.Label($"Year {currentYear} - {GetSeasonName(currentSeason)}");
        GUILayout.Label($"Day {GetDayInSeason()}/{daysPerSeason}");
        GUILayout.Label($"Time {GetCurrentTimeString()}");
        GUILayout.Label(isDay ? "Day" : "Night");
        GUILayout.EndArea();
    }
}
