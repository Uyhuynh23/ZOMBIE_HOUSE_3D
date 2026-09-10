using UnityEngine;
using UnityEditor;

public class CreateWallnutPrefab
{
    [MenuItem("Tools/Create Wallnut Prefab")]
    public static void Create()
    {
        // 1. Tải model glb
        GameObject model = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Models/wallnut_-_pvz_garden_warfare.glb");
        if (model == null)
        {
            Debug.LogError("Không tìm thấy model Wallnut tại Assets/Models/wallnut_-_pvz_garden_warfare.glb");
            return;
        }

        // 2. Tạo một GameObject gốc
        GameObject root = new GameObject("Wallnut");

        // 3. Instantiate model làm con của root
        GameObject child = (GameObject)PrefabUtility.InstantiatePrefab(model);
        child.transform.SetParent(root.transform);
        child.transform.localPosition = Vector3.zero;
        child.transform.localRotation = Quaternion.identity;
        // Chỉnh scale cho phù hợp, dựa theo Sunflower thì Sunflower scale = 5.
        // Bạn có thể chỉnh sửa giá trị này nếu model wallnut to/nhỏ quá
        root.transform.localScale = new Vector3(5f, 5f, 5f);

        // 4. Thêm script WallnutLogic
        WallnutLogic logic = root.AddComponent<WallnutLogic>();
        logic.maxHealth = 4000; // Máu trâu (Sunflower là 100)

        // 5. Thêm CapsuleCollider (giống Sunflower)
        CapsuleCollider col = root.AddComponent<CapsuleCollider>();
        col.radius = 0.35f;
        col.height = 1f;
        col.center = new Vector3(0, 0.5f, 0);

        // 6. Lưu thành Prefab
        string localPath = "Assets/Prefabs/Wallnut.prefab";
        localPath = AssetDatabase.GenerateUniqueAssetPath(localPath);
        PrefabUtility.SaveAsPrefabAssetAndConnect(root, localPath, InteractionMode.UserAction);

        // 7. Xóa object trên scene
        Object.DestroyImmediate(root);

        Debug.Log("Đã tạo thành công prefab Wallnut tại " + localPath);
    }
}
