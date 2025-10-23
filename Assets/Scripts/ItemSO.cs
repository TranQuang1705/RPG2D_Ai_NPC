using UnityEngine;

[CreateAssetMenu(menuName = "Items/ItemSO", fileName = "NewItem")]
public class ItemSO : ScriptableObject
{
    [Header("Info")]
    public string id;               // ví dụ: "apple", "sword_iron"
    public string displayName;      // ví dụ: "Táo", "Kiếm sắt"
    public Sprite icon;

    [Header("Stacking")]
    public bool stackable = true;   // Táo = true, Kiếm = false
    [Min(1)] public int maxStack = 15; // Táo 15; nếu stackable=false thì để 1

    [Header("Optional")]
    public GameObject worldPrefab;  // prefab rơi ngoài map (nếu cần)
}
