using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class SceneChanger : MonoBehaviour
{
    private float delayTime = 22f; 

    void Start()
    {
        StartCoroutine(ChangeSceneAfterDelay());
    }

    private IEnumerator ChangeSceneAfterDelay()
    {
        yield return new WaitForSeconds(delayTime);

        SceneManager.LoadScene(0);
    }
}
