using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

[RequireComponent(typeof(Collider2D))]
public class RoofZone : MonoBehaviour
{
    [Header("Fade settings")]
    [Range(0,1f)] public float transparencyAmount = 0.7f;
    public float fadeTime = 0.3f;

    [Header("Target renderers (mái)")]
    public List<SpriteRenderer> roofSprites = new List<SpriteRenderer>();
    public Tilemap roofTilemap; // để trống nếu không dùng tilemap

    [Header("Sorting for player")]
    public int playerOrderOffset = -50; 
    // âm: đẩy player ra phía sau (bị mái che), dương: ngược lại

    // cache
    private Dictionary<SpriteRenderer, float> _origAlphaSR = new Dictionary<SpriteRenderer, float>();
    private float _origAlphaTilemap = 1f;

    // track nhiều collider cùng lúc (nếu có)
    private int _insideCount = 0;

    private void Awake()
    {
        // Lưu alpha gốc
        foreach (var sr in roofSprites)
            if (sr) _origAlphaSR[sr] = sr.color.a;

        if (roofTilemap) _origAlphaTilemap = roofTilemap.color.a;

        // đảm bảo là trigger
        var col = GetComponent<Collider2D>();
        col.isTrigger = true;
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!IsPlayer(other)) return;

        _insideCount++;
        if (_insideCount == 1) // chỉ chạy 1 lần khi người đầu tiên bước vào
        {
            // Fade mái
            foreach (var sr in roofSprites) if (sr)
                StartCoroutine(FadeSprite(sr, fadeTime, sr.color.a, transparencyAmount));
            if (roofTilemap)
                StartCoroutine(FadeTilemap(roofTilemap, fadeTime, roofTilemap.color.a, transparencyAmount));
        }

        // Đẩy player vào sau mái bằng order offset
        var playerSR = other.GetComponent<SpriteRenderer>();
        if (playerSR)
        {
            // Lưu order gốc vào PlayerState (đính kèm lên player để khỏi mất)
            var state = other.GetComponent<PlayerSortState>();
            if (!state) state = other.gameObject.AddComponent<PlayerSortState>();
            state.BaseOrder = playerSR.sortingOrder;

            playerSR.sortingOrder = state.BaseOrder + playerOrderOffset;
        }
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (!IsPlayer(other)) return;

        _insideCount = Mathf.Max(0, _insideCount - 1);

        if (_insideCount == 0)
        {
            // Khôi phục alpha mái
            foreach (var sr in roofSprites) if (sr && _origAlphaSR.TryGetValue(sr, out var a0))
                StartCoroutine(FadeSprite(sr, fadeTime, sr.color.a, a0));
            if (roofTilemap)
                StartCoroutine(FadeTilemap(roofTilemap, fadeTime, roofTilemap.color.a, _origAlphaTilemap));
        }

        // Trả order player về gốc
        var playerSR = other.GetComponent<SpriteRenderer>();
        var state = other.GetComponent<PlayerSortState>();
        if (playerSR && state)
        {
            playerSR.sortingOrder = state.BaseOrder;
        }
    }

    private bool IsPlayer(Collider2D other)
    {
        // Tuỳ dự án: dùng component PlayerController hoặc Tag = "Player"
        return other.GetComponent<PlayerController>() != null
            || other.CompareTag("Player");
    }

    private IEnumerator FadeSprite(SpriteRenderer sr, float time, float from, float to)
    {
        float t = 0f;
        var c = sr.color;
        while (t < time)
        {
            t += Time.deltaTime;
            float a = Mathf.Lerp(from, to, t / time);
            sr.color = new Color(c.r, c.g, c.b, a);
            yield return null;
        }
        sr.color = new Color(c.r, c.g, c.b, to);
    }

    private IEnumerator FadeTilemap(Tilemap tm, float time, float from, float to)
    {
        float t = 0f;
        var c = tm.color;
        while (t < time)
        {
            t += Time.deltaTime;
            float a = Mathf.Lerp(from, to, t / time);
            tm.color = new Color(c.r, c.g, c.b, a);
            yield return null;
        }
        tm.color = new Color(c.r, c.g, c.b, to);
    }
}

// Component phụ để nhớ sorting gốc của player
public class PlayerSortState : MonoBehaviour
{
    public int BaseOrder;
}
