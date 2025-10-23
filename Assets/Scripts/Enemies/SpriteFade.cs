using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SpriteFade : MonoBehaviour
{
    [SerializeField] private float fadeTime = .4f;
    private SpriteRenderer spriteFadeRenderer;

    private void Awake()
    {
        spriteFadeRenderer = GetComponent<SpriteRenderer>();
    }
    public IEnumerator SlowFadeRoutine()
    {
        float elapsedTime = 0;
        float startValue = spriteFadeRenderer.color.a;

        while(elapsedTime < fadeTime)
        {
            elapsedTime += Time.deltaTime;
            float newAl = Mathf.Lerp(startValue, 0f, elapsedTime / fadeTime);
            spriteFadeRenderer.color = new Color(spriteFadeRenderer.color.r, spriteFadeRenderer.color.g, spriteFadeRenderer.color.b, newAl);
            yield return null;
        }
        Destroy(gameObject);
    }
}
