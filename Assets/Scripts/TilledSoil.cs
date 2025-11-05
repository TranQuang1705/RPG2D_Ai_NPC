using UnityEngine;

/// <summary>
/// Script cho mảnh đất đã được xới - có thể trồng cây lên trên
/// </summary>
public class TilledSoil : MonoBehaviour
{
    [Header("Soil Settings")]
    public bool isWatered = false;
    public bool hasPlant = false;
    
    [Header("Visual Settings")]
    public Sprite drySoilSprite;    // Đất khô
    public Sprite wateredSoilSprite; // Đất tưới nước
    
    private SpriteRenderer spriteRenderer;
    private float waterDryTime = 60f; // Thời gian để đất khô (60 giây)
    
    void Awake()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
        if (spriteRenderer == null)
        {
            spriteRenderer = gameObject.AddComponent<SpriteRenderer>();
        }
        
        // Set default sprite
        if (drySoilSprite != null)
            spriteRenderer.sprite = drySoilSprite;
    }
    
    /// <summary>
    /// Tưới nước cho đất
    /// </summary>
    public void WaterSoil()
    {
        isWatered = true;
        
        // Change sprite to watered version
        if (spriteRenderer != null && wateredSoilSprite != null)
            spriteRenderer.sprite = wateredSoilSprite;
            
        // Tắt collider khi đất được tưới để cây có thể trồng lên
        Collider2D collider = GetComponent<Collider2D>();
        if (collider != null)
            collider.enabled = false;
            
        // Sau một thời gian đất sẽ khô lại
        Invoke("DrySoil", waterDryTime);
    }
    
    /// <summary>
    /// Đất khô lại
    /// </summary>
    private void DrySoil()
    {
        isWatered = false;
        
        // Change sprite back to dry version
        if (spriteRenderer != null && drySoilSprite != null)
            spriteRenderer.sprite = drySoilSprite;
            
        // Bật lại collider
        Collider2D collider = GetComponent<Collider2D>();
        if (collider != null)
            collider.enabled = true;
    }
    
    /// <summary>
    /// Kiểm tra có thể trồng cây không
    /// </summary>
    public bool CanPlant()
    {
        return isWatered && !hasPlant;
    }
    
    /// <summary>
    /// Ghi đã có cây được trồng
    /// </summary>
    public void SetPlanted(GameObject plant)
    {
        hasPlant = true;
        plant.transform.parent = transform;
    }
    
    void OnDrawGizmos()
    {
        // Vẽ vùng đất có thể trồng cây
        Gizmos.color = isWatered ? Color.blue : new Color(0.5f, 0.3f, 0.1f, 0.5f);
        Gizmos.DrawCube(transform.position, Vector3.one * 0.8f);
    }
}
