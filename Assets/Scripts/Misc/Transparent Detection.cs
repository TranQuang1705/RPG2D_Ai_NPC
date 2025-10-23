using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

public class TransparentDetection : MonoBehaviour
{
    [Range(0, 1)]
    [SerializeField] private float transparencyAmount = 0.8f;
    [SerializeField] private float fadeTime = .4f;

    private SpriteRenderer SpriteRenderer;
    private Tilemap tilemap;

    private void Awake()
    {
        SpriteRenderer = GetComponent<SpriteRenderer>();
        tilemap = GetComponent<Tilemap>();  
    }
    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.gameObject.GetComponent<PlayerController>())
        {
            if (SpriteRenderer)
            {
                StartCoroutine(FadeRoutine(SpriteRenderer, fadeTime, SpriteRenderer.color.a, transparencyAmount));
            }else if (tilemap)
            {
                StartCoroutine(FadeRoutine(tilemap, fadeTime, tilemap.color.a, transparencyAmount));    
            }
        }
    }
    private void OnTriggerExit2D(Collider2D other)
    {
        if (other.gameObject.GetComponent<PlayerController>())
        {
            if (SpriteRenderer)
            {
                StartCoroutine(FadeRoutine(SpriteRenderer, fadeTime, SpriteRenderer.color.a, 1f));
            }
            else if (tilemap)
            {
                StartCoroutine(FadeRoutine(tilemap, fadeTime, tilemap.color.a, 1f));
            }
        }
    }
    private IEnumerator FadeRoutine(SpriteRenderer SpriteRenderer, float fadeTime, float startValue, float targetTransparency)
    {
        float elapsedTime = 0;
        while(elapsedTime < fadeTime)
        {
            elapsedTime += Time.deltaTime;
            float Al = Mathf.Lerp(startValue, targetTransparency, elapsedTime / fadeTime);
            SpriteRenderer.color = new Color(SpriteRenderer.color.r, SpriteRenderer.color.g, SpriteRenderer.color.b, Al);
            yield return null;
        }
    }
    private IEnumerator FadeRoutine(Tilemap tilemap, float fadeTime, float startValue, float targetTransparency)
    {
        float elapsedTime = 0;
        while (elapsedTime < fadeTime)
        {
            elapsedTime += Time.deltaTime;
            float Al = Mathf.Lerp(startValue, targetTransparency, elapsedTime / fadeTime);
            tilemap.color = new Color(tilemap.color.r, tilemap.color.g, tilemap.color.b, Al);
            yield return null;
        }
    }
}
