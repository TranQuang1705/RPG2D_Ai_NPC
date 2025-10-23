using UnityEngine;
using System.Collections;

public class FireflyFade : MonoBehaviour
{
    public Transform player;
    public float fadeDistance = 2.5f;   // Khoảng cách mà khi player tới gần thì fade
    public float fadeSpeed = 2f;        // Tốc độ mờ dần
    private ParticleSystem ps;
    private bool isFading = false;

    void Start()
    {
        ps = GetComponent<ParticleSystem>();
    }

    void Update()
    {
        if (player == null || ps == null) return;

        float dist = Vector3.Distance(player.position, transform.position);

        // Khi người chơi lại gần
        if (dist < fadeDistance && !isFading)
        {
            StartCoroutine(FadeAndDestroy());
            isFading = true;
        }
    }

    private IEnumerator FadeAndDestroy()
    {
        var main = ps.main;
        Color startColor = main.startColor.color;
        float alpha = 1f;

        while (alpha > 0f)
        {
            alpha -= Time.deltaTime * fadeSpeed;
            Color c = startColor;
            c.a = alpha;
            main.startColor = c;
            yield return null;
        }

        Destroy(gameObject);
    }
}
