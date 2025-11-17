using UnityEngine;

[RequireComponent(typeof(SpriteRenderer))]
public class RandomTreeSprite : MonoBehaviour
{
    public enum TreeState
    {
        Default = 0,
        Flower = 1,
        Apple = 2
    }

    public Sprite defaultSprite;   // tree1
    public Sprite flowerSprite;    // tree1_var_3
    public Sprite appleSprite;     // tree_healelement_1

    private SpriteRenderer sr;

    public TreeState CurrentState { get; private set; }

    private void Awake()
    {
        sr = GetComponent<SpriteRenderer>();

        int idx = Random.Range(0, 3);
        CurrentState = (TreeState)idx;

        ApplyState(CurrentState);
    }

    public void SetState(TreeState newState)
    {
        CurrentState = newState;
        ApplyState(newState);
    }

    private void ApplyState(TreeState state)
    {
        Sprite chosenSprite = defaultSprite;
        switch (state)
        {
            case TreeState.Flower:
                chosenSprite = flowerSprite;
                break;
            case TreeState.Apple:
                chosenSprite = appleSprite;
                break;
        }
        sr.sprite = chosenSprite;
    }
}
