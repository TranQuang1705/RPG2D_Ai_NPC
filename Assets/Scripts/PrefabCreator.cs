// using UnityEngine;

// /// <summary>
// /// Tự động tạo các prefab cần thiết cho hệ thống farming
// /// </summary>
// public class PrefabCreator : MonoBehaviour
// {
//     [Header("Auto-create Prefabs")]
//     public bool createTilledSoilPrefab = true;
    
//     void Awake()
//     {
//         if (createTilledSoilPrefab)
//         {
//             CreateTilledSoilPrefab();
//         }
//     }
    
//     /// <summary>
//     /// Tạo prefab cho mảnh đất đã xới
//     /// </summary>
//     void CreateTilledSoilPrefab()
//     {
//         // Tạo GameObject mới và thêm component
//         GameObject tilledSoil = new GameObject("TilledSoil");
        
//         // Thêm SpriteRenderer
//         SpriteRenderer sr = tilledSoil.AddComponent<SpriteRenderer>();
        
//         // Thêm TilledSoil script
//         TilledSoil soilScript = tilledSoil.AddComponent<TilledSoil>();
        
//         // Thêm Collider2D
//         BoxCollider2D collider = tilledSoil.AddComponent<BoxCollider2D>();
//         collider.isTrigger = true;
//         collider.size = Vector2.one;
        
//         // Tạo sprite đơn giản bằng code nếu không có sprite sẵn
//         if (sr.sprite == null)
//         {
//             sr.sprite = CreateSimpleSoilSprite();
//         }
        
//         // Gán vào Resources folder để có thể gọi từ script khác
//         #if UNITY_EDITOR
//         // Trong Editor, lưu vào Resources folder
//         string resourcesPath = "Assets/Resources/Prefabs";
//         if (!System.IO.Directory.Exists(resourcesPath))
//         {
//             System.IO.Directory.CreateDirectory(resourcesPath);
//         }
        
//         string prefabPath = "Assets/Resources/Prefabs/TilledSoil.prefab";
        
//         // Chỉ tạo nếu chưa có
//         #if UNITY_EDITOR
//         if (!UnityEditor.AssetDatabase.AssetPathExistsGUID(UnityEditor.AssetDatabase.AssetPathToGUID(prefabPath)))
//         {
//             UnityEditor.PrefabUtility.SaveAsPrefabAsset(tilledSoil, prefabPath);
//             Debug.Log("Đã tạo TilledSoil prefab tại: " + prefabPath);
//         }
//         #endif
        
//         // Xóa GameObject trong scene
//         Destroy(tilledSoil);
//         #endif
//     }
    
//     /// <summary>
//     /// Tạo sprite đất đơn giản bằng code
//     /// </summary>
//     Sprite CreateSimpleSoilSprite()
//     {
//         // Tạo texture nhỏ 32x32
//         int textureSize = 32;
//         Texture2D texture = new Texture2D(textureSize, textureSize);
        
//         // Màu đất (nâu sáng)
//         Color soilColor = new Color(0.5f, 0.35f, 0.2f, 1f);
//         Color darkSoilColor = new Color(0.35f, 0.25f, 0.15f, 1f);
        
//         // Tạo pattern đất đơn giản
//         for (int x = 0; x < textureSize; x++)
//         {
//             for (int y = 0; y < textureSize; y++)
//             {
//                 // Tạo texture đất ngẫu nhiên
//                 float noise = Mathf.PerlinNoise(x * 0.1f, y * 0.1f);
//                 Color pixelColor = Color.Lerp(soilColor, darkSoilColor, noise);
//                 texture.SetPixel(x, y, pixelColor);
//             }
//         }
        
//         texture.Apply();
        
//         // Tạo sprite từ texture
//         return Sprite.Create(texture, new Rect(0, 0, textureSize, textureSize), new Vector2(0.5f, 0.5f), 32);
//     }
// }

// #if UNITY_EDITOR
// /// <summary>
// /// Menu item để tạo prefab nhanh
// /// </summary>
// public class FarmingPrefabCreator
// {
//     [UnityEditor.MenuItem("Tools/Farming/Create Tilled Soil Prefab")]
//     static void CreateTilledSoilPrefab()
//     {
//         GameObject creatorObj = new GameObject("PrefabCreator");
//         PrefabCreator creator = creatorObj.AddComponent<PrefabCreator>();
//         creator.createTilledSoilPrefab = true;
//         creator.Awake();
//         DestroyImmediate(creatorObj);
//         UnityEditor.AssetDatabase.Refresh();
//     }
// }
// #endif
