using UnityEngine;
using UnityEngine.EventSystems;

public class SlideTabUI : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    private RectTransform rect;

    public float hiddenX = -30f;
    public float shownX = 10f;
    public float speed = 10f;

    private Vector2 targetPos;

    void Start()
    {
        rect = GetComponent<RectTransform>();

        targetPos = new Vector2(hiddenX, rect.anchoredPosition.y);
        rect.anchoredPosition = targetPos;

        Debug.Log("[SlideTabUI] Start: hidden=" + targetPos);
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        Debug.Log("[SlideTabUI] ENTER FRAME");
        targetPos = new Vector2(shownX, rect.anchoredPosition.y);
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        Debug.Log("[SlideTabUI] EXIT FRAME");
        targetPos = new Vector2(hiddenX, rect.anchoredPosition.y);
    }

    void Update()
    {
        rect.anchoredPosition = Vector2.Lerp(
            rect.anchoredPosition,
            targetPos,
            Time.deltaTime * speed
        );
    }
}
