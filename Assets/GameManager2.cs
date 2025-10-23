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
}
