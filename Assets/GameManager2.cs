using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class GameManager : MonoBehaviour
{
    [Header("UI Elements")]
    public GameObject mainMenuUI; 
    private bool isMenuActive = false;

    [Header("Scene Management")]
    public int targetSceneIndex = 4;  
    public float delayBeforeSwitch = 5f; 
    
    [Header("Time System")]
    public TimeManager timeManager;

    void Start()
    {
        // Tìm TimeManager nếu chưa được assign
        if (timeManager == null)
        {
            timeManager = FindObjectOfType<TimeManager>();
        }
        
        // Nếu không có TimeManager, tạo mới
        if (timeManager == null)
        {
            GameObject timeManagerObj = new GameObject("TimeManager");
            timeManager = timeManagerObj.AddComponent<TimeManager>();
            Debug.Log("Đã tạo TimeManager mới");
        }
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            isMenuActive = !isMenuActive;
            mainMenuUI.SetActive(isMenuActive);

            Time.timeScale = isMenuActive ? 0f : 1f;
        }
    }
    
    public void StartSceneSwitch()
    {
        StartCoroutine(DelaySceneSwitchRoutine());
    }

    private IEnumerator DelaySceneSwitchRoutine()
    {
        yield return new WaitForSecondsRealtime(delayBeforeSwitch);
        Time.timeScale = 1f;
        SceneManager.LoadScene(targetSceneIndex);
    }
    
    // Public methods để truy cập TimeManager
    public string GetCurrentTimeString()
    {
        return timeManager ? timeManager.GetCurrentTimeString() : "00:00";
    }
    
    public int GetCurrentDay()
    {
        return timeManager ? timeManager.GetCurrentDay() : 1;
    }
    
    public bool IsDaytime()
    {
        return timeManager ? timeManager.IsDaytime() : true;
    }
    
    public bool IsNighttime()
    {
        return timeManager ? timeManager.IsNighttime() : false;
    }
}
