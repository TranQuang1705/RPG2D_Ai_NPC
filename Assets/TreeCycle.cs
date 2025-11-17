using UnityEngine;
using System.Collections;

[RequireComponent(typeof(SpriteRenderer))]
public class TreeStateCycle : MonoBehaviour
{
    public enum TreeState
    {
        Default = 0,
        Flower = 1,
        Apple = 2
    }

    [Header("Sprites cho từng state")]
    public Sprite defaultSprite;
    public Sprite flowerSprite;
    public Sprite appleSprite;

    [Header("Thời gian (giây) cho mỗi state")]
    public float stateDuration = 3f;

    private SpriteRenderer sr;
    private TreeState currentState;

    private void Awake()
    {
        sr = GetComponent<SpriteRenderer>();
    }

    private void Start()
    {
        // Random trạng thái khởi đầu ngay lập tức
        currentState = (TreeState)Random.Range(0, 3);
        // Debug.Log($"[TreeStateCycle] State ban đầu: {currentState}");

        // Hiển thị sprite cho state đó
        ApplyState(currentState);

        // Bắt đầu chu kỳ tiếp theo
        StartCoroutine(CycleRoutine());
    }

    private IEnumerator CycleRoutine()
    {
        while (true)
        {
            yield return new WaitForSeconds(stateDuration);

            // Chuyển tuần tự: Default→Flower→Apple→Default...
            currentState = (TreeState)(((int)currentState + 1) % 3);
            // Debug.Log($"[TreeStateCycle] Chuyển sang: {currentState}");

            ApplyState(currentState);
        }
    }

    private void ApplyState(TreeState st)
    {
        switch (st)
        {
            case TreeState.Flower:
                sr.sprite = flowerSprite;
                break;
            case TreeState.Apple:
                sr.sprite = appleSprite;
                break;
            default:
                sr.sprite = defaultSprite;
                break;
        }
    }
    public TreeState GetCurrentState()
    {
        return currentState;
    }

    public void ForceState(TreeState newState)
    {
        currentState = newState;
        ApplyState(currentState);
    }
    
}
