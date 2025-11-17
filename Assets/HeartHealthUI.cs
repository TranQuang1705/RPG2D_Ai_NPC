using UnityEngine;
using UnityEngine.UI;

public class HeartHealthUI : MonoBehaviour
{
    [Header("Heart Sprites")]
    [SerializeField] private Sprite heartFull;
    [SerializeField] private Sprite heartHalf;
    [SerializeField] private Sprite heartEmpty;

    [Header("Heart Container")]
    [SerializeField] private Transform heartsContainer;
    [SerializeField] private GameObject heartPrefab;

    private Image[] heartImages;
    private int maxHearts;

    public void InitHearts(int maxHealth)
    {
        maxHearts = Mathf.CeilToInt(maxHealth / 2f);
        
        foreach (Transform child in heartsContainer)
        {
            Destroy(child.gameObject);
        }

        heartImages = new Image[maxHearts];
        for (int i = 0; i < maxHearts; i++)
        {
            GameObject heartObj = Instantiate(heartPrefab, heartsContainer);
            heartImages[i] = heartObj.GetComponent<Image>();
            heartImages[i].sprite = heartFull;
            
            // Disable Layout Element to prevent auto-scaling
            var layoutElement = heartImages[i].GetComponent<LayoutElement>();
            if (layoutElement == null)
            {
                layoutElement = heartImages[i].gameObject.AddComponent<LayoutElement>();
            }
            layoutElement.ignoreLayout = false;
            layoutElement.preferredWidth = 64;  
            layoutElement.preferredHeight = 64;
        }

        Debug.Log("HeartHealthUI: Initialized " + maxHearts + " hearts for " + maxHealth + " HP");
    }

    public void UpdateHearts(int currentHealth, int maxHealth)
    {
        if (heartImages == null || heartImages.Length == 0)
        {
            Debug.LogWarning("HeartHealthUI: Hearts not initialized!");
            return;
        }

        for (int i = 0; i < heartImages.Length; i++)
        {
            int heartMinHP = i * 2;
            int heartMaxHP = heartMinHP + 2;

            if (currentHealth >= heartMaxHP)
            {
                heartImages[i].sprite = heartFull;
            }
            else if (currentHealth > heartMinHP)
            {
                heartImages[i].sprite = heartHalf;
            }
            else
            {
                heartImages[i].sprite = heartEmpty;
            }
        }
    }
}
