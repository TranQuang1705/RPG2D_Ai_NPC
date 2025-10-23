using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
public class UiFade : Singleton<UiFade>
{
    [SerializeField] private Image fadeScreen;
    [SerializeField] private float fadeSpeed = 1f;

    private IEnumerator fadeRoutine;

    public void FadeToBack()
    {
        if(fadeRoutine != null)
        {
            StopCoroutine(fadeRoutine);
        }
        fadeRoutine = FadeRoutine(1);
        StartCoroutine(fadeRoutine);
    }
    public void FadeToClear()
    {
        if (fadeRoutine != null)
        {
            StopCoroutine(fadeRoutine);
        }

        fadeRoutine = FadeRoutine(0);
        StartCoroutine(fadeRoutine);
    }
    public IEnumerator FadeRoutine(float targetAl)
    {
        while (!Mathf.Approximately(fadeScreen.color.a, targetAl))
        {
            float Al = Mathf.MoveTowards(fadeScreen.color.a, targetAl, fadeSpeed * Time.deltaTime);
            fadeScreen.color = new Color(fadeScreen.color.r, fadeScreen.color.g, fadeScreen.color.b, Al);
            yield return null;
        }
    }
}
