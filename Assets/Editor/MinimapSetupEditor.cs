using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;

public class MinimapSetupEditor : EditorWindow
{
    [MenuItem("Tools/Setup Minimap System")]
    public static void SetupMinimap()
    {
        // 1. Modify Zombie Prefab
        string zombiePrefabPath = "Assets/Prefabs/Zombie.prefab";
        GameObject zombiePrefab = AssetDatabase.LoadAssetAtPath<GameObject>(zombiePrefabPath);
        if (zombiePrefab != null)
        {
            GameObject prefabRoot = PrefabUtility.InstantiatePrefab(zombiePrefab) as GameObject;
            
            // Check if it already has an indicator
            Transform existingIndicator = prefabRoot.transform.Find("MinimapIndicator");
            if (existingIndicator == null)
            {
                GameObject indicator = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                indicator.name = "MinimapIndicator";
                indicator.transform.SetParent(prefabRoot.transform);
                indicator.transform.localPosition = new Vector3(0, 4f, 0); // Above the zombie
                indicator.transform.localScale = new Vector3(3f, 3f, 3f); // Make it large enough to see
                
                // Remove collider
                DestroyImmediate(indicator.GetComponent<Collider>());
                
                // Set Layer to 6 (LocationMarker)
                indicator.layer = 6;
                
                // Create a red material
                string matPath = "Assets/Minimap/ZombieIndicatorMat.mat";
                Material redMat = AssetDatabase.LoadAssetAtPath<Material>(matPath);
                if (redMat == null)
                {
                    if (!AssetDatabase.IsValidFolder("Assets/Minimap"))
                    {
                        AssetDatabase.CreateFolder("Assets", "Minimap");
                    }
                    redMat = new Material(Shader.Find("Universal Render Pipeline/Lit"));
                    if (redMat.shader == null) redMat = new Material(Shader.Find("Standard")); // fallback
                    redMat.color = Color.red;
                    AssetDatabase.CreateAsset(redMat, matPath);
                }
                
                indicator.GetComponent<MeshRenderer>().sharedMaterial = redMat;
                
                PrefabUtility.SaveAsPrefabAsset(prefabRoot, zombiePrefabPath);
                Debug.Log("Added MinimapIndicator to Zombie.prefab");
            }
            DestroyImmediate(prefabRoot);
        }
        else
        {
            Debug.LogError("Zombie prefab not found!");
        }

        // 2. Setup Minimap Prefab from Kha_Minimap
        string khaScenePath = "Assets/Scenes/GameScenes/Kha_Minimap.unity";
        if (System.IO.File.Exists(khaScenePath))
        {
            Scene currentScene = EditorSceneManager.GetActiveScene();
            string currentScenePath = currentScene.path;
            
            Scene khaScene = EditorSceneManager.OpenScene(khaScenePath, OpenSceneMode.Single);
            
            // Find the root Minimap object in Kha_Minimap scene
            GameObject minimapRoot = GameObject.Find("Minimap");
            
            if (minimapRoot != null)
            {
                // Clone the entire Minimap rig
                GameObject rootClone = Instantiate(minimapRoot);
                rootClone.name = "MinimapSystem";
                
                // Find the camera inside it (it's named "Camera")
                Transform camTrans = rootClone.transform.Find("Camera");
                if (camTrans != null)
                {
                    camTrans.name = "MiniMapCamera";
                }
                
                string prefabPath = "Assets/Prefabs/MinimapSystem.prefab";
                PrefabUtility.SaveAsPrefabAsset(rootClone, prefabPath);
                Debug.Log("Created MinimapSystem.prefab successfully!");
                
                DestroyImmediate(rootClone);
            }
            else
            {
                Debug.LogError("Could not find 'Minimap' root object in Kha_Minimap scene.");
            }
            
            // Return to MapZombieIntegration and integrate
            string targetScenePath = "Assets/Scenes/TestScenes/MapZombieIntegration.unity";
            if (System.IO.File.Exists(targetScenePath))
            {
                EditorSceneManager.OpenScene(targetScenePath, OpenSceneMode.Single);
                
                // Replace existing MinimapSystem if present
                GameObject existingMinimapSystem = GameObject.Find("MinimapSystem");
                if (existingMinimapSystem != null)
                {
                    DestroyImmediate(existingMinimapSystem);
                }
                
                GameObject minimapSystemPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/MinimapSystem.prefab");
                if (minimapSystemPrefab != null)
                {
                    GameObject instance = PrefabUtility.InstantiatePrefab(minimapSystemPrefab) as GameObject;
                    
                    // Setup Target for MinimapFollow
                    GameObject knight = GameObject.Find("Knight");
                    if (knight != null)
                    {
                        Transform camTrans = instance.transform.Find("MiniMapCamera");
                        if (camTrans != null)
                        {
                            var followComponent = camTrans.GetComponent("MinimapFollow");
                            if (followComponent != null)
                            {
                                SerializedObject so = new SerializedObject(followComponent);
                                so.FindProperty("target").objectReferenceValue = knight.transform;
                                so.ApplyModifiedProperties();
                            }
                        }
                    }
                    
                    EditorSceneManager.SaveScene(EditorSceneManager.GetActiveScene());
                    Debug.Log("Integrated MinimapSystem into MapZombieIntegration scene!");
                }
            }
            else
            {
                EditorSceneManager.OpenScene(currentScenePath, OpenSceneMode.Single);
            }
        }
        else
        {
            Debug.LogError("Kha_Minimap scene not found!");
        }
    }
}
