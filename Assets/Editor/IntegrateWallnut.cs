using UnityEngine;
using UnityEditor;

public class IntegrateWallnut
{
    [MenuItem("Tools/Integrate Wallnut to Characters")]
    public static void Integrate()
    {
        // 1. Tìm prefab Wallnut
        GameObject wallnutPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Wallnut.prefab");
        if (wallnutPrefab == null)
        {
            Debug.LogError("Không tìm thấy Wallnut.prefab, hãy chạy 'Create Wallnut Prefab' trước!");
            return;
        }

        // 2. Tìm hình ảnh cho Wallnut (tạm dùng một ảnh có sẵn trong thư mục nếu cần, hoặc bỏ trống)
        // Mình sẽ lấy tạm portrait của Peashooter hoặc một ảnh nào đó
        Sprite portrait = null;
        string[] guids = AssetDatabase.FindAssets("t:Sprite");
        foreach(var g in guids) {
            string path = AssetDatabase.GUIDToAssetPath(g);
            if (path.ToLower().Contains("potato") || path.ToLower().Contains("pumpkin")) {
                portrait = AssetDatabase.LoadAssetAtPath<Sprite>(path);
                break;
            }
        }
        
        // 3. Danh sách các nhân vật
        string[] chars = { "Rogue", "Mage", "Barbarian", "Knight" };
        
        foreach (string charName in chars)
        {
            string path = $"Assets/Prefabs/{charName}.prefab";
            GameObject charPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (charPrefab != null)
            {
                PlayerController pc = charPrefab.GetComponent<PlayerController>();
                if (pc != null)
                {
                    // Chuyển mảng thành List để dễ thao tác
                    var plantList = new System.Collections.Generic.List<PlantData>(pc.plants);
                    
                    // Kiểm tra xem đã có Wallnut chưa
                    bool hasWallnut = false;
                    foreach(var p in plantList) {
                        if (p.name == "Wallnut") hasWallnut = true;
                    }
                    
                    if (!hasWallnut)
                    {
                        PlantData wallnutData = new PlantData();
                        wallnutData.name = "Wallnut";
                        wallnutData.prefab = wallnutPrefab;
                        wallnutData.cost = 50;
                        wallnutData.cooldownTime = 20f;
                        wallnutData.portrait = portrait;
                        
                        plantList.Add(wallnutData);
                        pc.plants = plantList.ToArray();
                        
                        EditorUtility.SetDirty(charPrefab);
                        Debug.Log($"Đã thêm Wallnut vào {charName}.prefab");
                    }
                }
            }
        }
        
        AssetDatabase.SaveAssets();
        Debug.Log("Hoàn tất tích hợp Wallnut vào các nhân vật! Bạn hãy Play game ở Map_Cloudy để test.");
    }
}
