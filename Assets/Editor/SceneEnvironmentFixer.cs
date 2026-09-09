using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;

public class SceneEnvironmentFixer : EditorWindow
{
    [MenuItem("Tools/Apply Scene Fixes")]
    public static void ApplyFixes()
    {
        Debug.Log("Starting Scene Fixes...");

        // 1. Fix the House
        GameObject house = GameObject.Find("LowpolyBakersHouse");
        if (house == null) house = GameObject.Find("House");
        if (house != null)
        {
            // Scale x2
            house.transform.localScale = new Vector3(2f, 2f, 2f);
            
            // Add colliders to children if they have meshes
            MeshFilter[] meshFilters = house.GetComponentsInChildren<MeshFilter>();
            foreach (var mf in meshFilters)
            {
                if (mf.gameObject.GetComponent<Collider>() == null)
                {
                    mf.gameObject.AddComponent<MeshCollider>();
                }
            }
            Debug.Log("House scaled and colliders added.");
        }
        else
        {
            Debug.LogWarning("House object not found in the scene.");
        }

        // 2. Setup Player
        GameObject existingKnight = GameObject.Find("Knight_Player");
        if (existingKnight == null) existingKnight = GameObject.Find("Knight");
        
        if (existingKnight == null)
        {
            GameObject knightPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Knight.prefab");
            if (knightPrefab != null)
            {
                existingKnight = PrefabUtility.InstantiatePrefab(knightPrefab) as GameObject;
                existingKnight.name = "Knight";
                Debug.Log("Instantiated Knight prefab into the scene.");
            }
        }
        
        if (existingKnight != null)
        {
            // Move outside the house (assuming house is around 0,0,0, move to Z = -15 or X = -15)
            existingKnight.transform.position = new Vector3(0, 0, -20f);
            Debug.Log("Moved Knight outside the house to: " + existingKnight.transform.position);
        }

        // 3. Scale Zombies and Plants
        ScalePrefab("Assets/Prefabs/Zombie.prefab", 0.5f);
        ScalePrefab("Assets/Prefabs/Spider.prefab", 0.5f); // Optional, if they count as zombies/enemies
        ScalePrefab("Assets/Prefabs/PlantableSquare.prefab", 0.5f); 
        // Note: If you have other specific plant prefabs (like Peashooter, etc.), they should be scaled similarly.

        // 4. Decorate Environment
        DecorateScene();

        EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
        Debug.Log("Scene fixes applied successfully!");
    }

    private static void ScalePrefab(string path, float scale)
    {
        GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
        if (prefab != null)
        {
            GameObject instance = PrefabUtility.InstantiatePrefab(prefab) as GameObject;
            instance.transform.localScale = new Vector3(scale, scale, scale);
            PrefabUtility.SaveAsPrefabAsset(instance, path);
            DestroyImmediate(instance);
            Debug.Log($"Scaled {prefab.name} to {scale}");
        }
    }

    private static void DecorateScene()
    {
        GameObject envParent = GameObject.Find("Environment_Foliage");
        if (envParent != null) DestroyImmediate(envParent);
        
        envParent = new GameObject("Environment_Foliage");

        string[] naturePrefabs = new string[]
        {
            "Assets/SimpleNaturePack/Prefabs/Tree_01.prefab",
            "Assets/SimpleNaturePack/Prefabs/Tree_02.prefab",
            "Assets/SimpleNaturePack/Prefabs/Tree_03.prefab",
            "Assets/SimpleNaturePack/Prefabs/Rock_01.prefab",
            "Assets/SimpleNaturePack/Prefabs/Rock_03.prefab",
            "Assets/SimpleNaturePack/Prefabs/Bush_01.prefab",
            "Assets/SimpleNaturePack/Prefabs/Grass_01.prefab",
            "Assets/SimpleNaturePack/Prefabs/Mushroom_01.prefab"
        };

        // Determine Theme Color based on Scene Name
        Scene currentScene = EditorSceneManager.GetActiveScene();
        Color themeColor = Color.white;
        string sceneName = currentScene.name.ToLower();
        
        if (sceneName.Contains("cloudy"))
        {
            themeColor = new Color(1.0f, 0.5f, 0.1f); // Orange tint
        }
        else if (sceneName.Contains("night"))
        {
            themeColor = new Color(0.4f, 0.5f, 0.8f); // Blueish/dark tint
        }

        int numObjects = 150;
        int placedCount = 0;
        int maxAttempts = 500;
        
        for (int i = 0; i < maxAttempts && placedCount < numObjects; i++)
        {
            Vector2 randomCircle = Random.insideUnitCircle.normalized * Random.Range(15f, 60f);
            Vector3 rayStart = new Vector3(randomCircle.x, 200f, randomCircle.y);
            Vector3 pos = new Vector3(randomCircle.x, 0, randomCircle.y);
            
            bool validPosition = false;

            // Try Raycast first to find the ground and avoid the house
            if (Physics.Raycast(rayStart, Vector3.down, out RaycastHit hit, 300f))
            {
                if (hit.collider.gameObject.name.Contains("House") || hit.collider.gameObject.name.Contains("Knight"))
                {
                    continue; // Skip if we hit the house or player
                }
                pos = hit.point;
                validPosition = true;
            }
            // Fallback to Terrain height if Raycast misses (e.g. ground has no collider)
            else if (Terrain.activeTerrain != null)
            {
                pos.y = Terrain.activeTerrain.SampleHeight(pos);
                validPosition = true;
            }

            if (!validPosition) continue;

            string prefabPath = naturePrefabs[Random.Range(0, naturePrefabs.Length)];
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
            if (prefab != null)
            {
                GameObject instance = PrefabUtility.InstantiatePrefab(prefab) as GameObject;
                instance.transform.SetParent(envParent.transform);
                instance.transform.position = pos;
                instance.transform.rotation = Quaternion.Euler(0, Random.Range(0, 360f), 0);
                
                float randomScale = Random.Range(0.8f, 1.5f);
                instance.transform.localScale = new Vector3(randomScale, randomScale, randomScale);

                // Apply Theme Color if not white
                if (themeColor != Color.white)
                {
                    MeshRenderer[] renderers = instance.GetComponentsInChildren<MeshRenderer>();
                    foreach (var r in renderers)
                    {
                        MaterialPropertyBlock block = new MaterialPropertyBlock();
                        r.GetPropertyBlock(block);
                        block.SetColor("_BaseColor", themeColor); // URP
                        block.SetColor("_Color", themeColor);     // Standard
                        r.SetPropertyBlock(block);
                    }
                }
                
                placedCount++;
            }
        }
        
        Debug.Log($"Generated {placedCount} random foliage objects around the house.");
    }
}
