using UnityEngine;

[RequireComponent(typeof(Animation), typeof(SpriteRenderer))]
public class RandomTreeAnimation : MonoBehaviour
{
    public AnimationClip defaultClip;   // pamtree
    public AnimationClip flowerClip;    // flowerTree
    public AnimationClip appleClip;     // appleTree

    public Sprite defaultSprite;        // tree1
    public Sprite flowerSprite;         // tree1_var_3
    public Sprite appleSprite;          // tree_healelement_1

    private Animation anim;
    private SpriteRenderer sr;

    private void Awake()
    {
        anim = GetComponent<Animation>();
        sr   = GetComponent<SpriteRenderer>();

        // 1) Random chọn biến thể 0=Default,1=Flower,2=Apple
        int idx = Random.Range(0, 3);

        AnimationClip chosenClip = defaultClip;
        Sprite       chosenSpr  = defaultSprite;

        switch (idx)
        {
            case 1:
                chosenClip = flowerClip;
                chosenSpr  = flowerSprite;
                break;
            case 2:
                chosenClip = appleClip;
                chosenSpr  = appleSprite;
                break;
        }

        // 2) Gán sprite tĩnh ban đầu
        sr.sprite = chosenSpr;

        // 3) Gán clip và play đúng tên
        anim.clip = chosenClip;
        anim.Play(chosenClip.name);

        bool isPlaying = anim.IsPlaying(chosenClip.name);
        Debug.Log($"[RandomTree] Called Play on '{chosenClip.name}'. IsPlaying? {isPlaying}");
    }
}
