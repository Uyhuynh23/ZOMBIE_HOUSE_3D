using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

public class GameMechanicsSetup
{
    [MenuItem("Tools/Setup New Plants & UI")]
    public static void Setup()
    {
        CreateSunPrefab();
        SetupSunflowerPrefab();
        SetupSnowPeaPrefab();
        SetupScene();
        Debug.Log("Successfully setup all new Mechanics and UI!");
    }

    static void CreateSunPrefab()
    {
        string path = "Assets/Prefabs/Sun.prefab";
        if (AssetDatabase.LoadAssetAtPath<GameObject>(path) != null) return;

        GameObject sun = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        sun.name = "Sun";
        sun.transform.localScale = Vector3.one * 0.5f;
        
        // Material
        Shader shader = Shader.Find("Universal Render Pipeline/Lit");
        if (shader == null) shader = Shader.Find("Standard");
        Material mat = new Material(shader);
        if (shader.name.Contains("Universal"))
        {
            mat.SetColor("_BaseColor", Color.yellow);
            mat.SetColor("_EmissionColor", Color.yellow * 2f);
            mat.EnableKeyword("_EMISSION");
        }
        else
        {
            mat.color = Color.yellow;
            mat.EnableKeyword("_EMISSION");
            mat.SetColor("_EmissionColor", Color.yellow);
        }
        AssetDatabase.CreateAsset(mat, "Assets/Materials/SunMaterial.mat");
        sun.GetComponent<MeshRenderer>().sharedMaterial = mat;

        sun.AddComponent<Sun>();
        
        if (!System.IO.Directory.Exists("Assets/Prefabs")) System.IO.Directory.CreateDirectory("Assets/Prefabs");
        PrefabUtility.SaveAsPrefabAsset(sun, path);
        GameObject.DestroyImmediate(sun);
    }

    static void SetupSunflowerPrefab()
    {
        GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Sunflower.prefab");
        if (prefab == null) return;

        if (prefab.GetComponent<SunflowerLogic>() == null)
        {
            var logic = prefab.AddComponent<SunflowerLogic>();
            logic.sunPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Sun.prefab");
        }
        if (prefab.GetComponent<Animator>() == null)
        {
            var anim = prefab.AddComponent<Animator>();
            anim.runtimeAnimatorController = AssetDatabase.LoadAssetAtPath<RuntimeAnimatorController>("Assets/Animation/Peashooter/PeashooterController.controller");
        }
        EditorUtility.SetDirty(prefab);
    }

    static void SetupSnowPeaPrefab()
    {
        GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/PeaShooterFroze.prefab");
        if (prefab == null) return;

        // Auto-create SpawnPoint if missing
        Transform spawnPoint = prefab.transform.Find("SpawnPoint");
        if (spawnPoint == null)
        {
            GameObject spObj = new GameObject("SpawnPoint");
            spObj.transform.SetParent(prefab.transform);
            spObj.transform.localPosition = new Vector3(0, 1.2f, 1f); // Approximate mouth position
            spObj.transform.localRotation = Quaternion.identity;
        }

        if (prefab.GetComponent<PeashooterCombat>() == null)
        {
            prefab.AddComponent<PeashooterCombat>();
        }
        if (prefab.GetComponent<SphereCollider>() == null)
        {
            var col = prefab.AddComponent<SphereCollider>();
            col.isTrigger = true;
            col.radius = 5f;
        }
        if (prefab.GetComponent<Animator>() == null)
        {
            var anim = prefab.AddComponent<Animator>();
            anim.runtimeAnimatorController = AssetDatabase.LoadAssetAtPath<RuntimeAnimatorController>("Assets/Animation/Peashooter/PeashooterController.controller");
        }
        EditorUtility.SetDirty(prefab);
    }

    static void SetupScene()
    {
        // Add EconomyManager to GameManager
        GameObject gm = GameObject.Find("GameManager");
        if (gm != null && gm.GetComponent<EconomyManager>() == null)
        {
            gm.AddComponent<EconomyManager>();
        }

        // Setup Player Controller Array
        PlayerController pc = GameObject.FindObjectOfType<PlayerController>();
        if (pc != null)
        {
            pc.plants = new PlantData[3];
            pc.plants[0] = new PlantData { name = "Peashooter", cost = 100, cooldownTime = 5f, prefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/PeaShooter.prefab") };
            pc.plants[1] = new PlantData { name = "Snow Pea", cost = 175, cooldownTime = 7f, prefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/PeaShooterFroze.prefab") };
            pc.plants[2] = new PlantData { name = "Sunflower", cost = 50, cooldownTime = 5f, prefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Sunflower.prefab") };
            EditorUtility.SetDirty(pc);
        }

        // Setup Simple UI Canvas
        if (GameObject.Find("UI_Canvas") == null)
        {
            GameObject canvasObj = new GameObject("UI_Canvas");
            Canvas canvas = canvasObj.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvasObj.AddComponent<CanvasScaler>();
            canvasObj.AddComponent<GraphicRaycaster>();

            GameObject textObj = new GameObject("SunText");
            textObj.transform.SetParent(canvasObj.transform, false);
            Text text = textObj.AddComponent<Text>();
            text.text = "Sun: 50";
            text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            text.fontSize = 40;
            text.color = Color.yellow;
            text.alignment = TextAnchor.UpperLeft;
            
            RectTransform rt = text.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(0, 1);
            rt.anchorMax = new Vector2(0, 1);
            rt.pivot = new Vector2(0, 1);
            rt.anchoredPosition = new Vector2(20, -20);
            rt.sizeDelta = new Vector2(300, 50);

            // Connect text to EconomyManager via simple script
            var updater = textObj.AddComponent<SunUIUpdater>();
        }
    }
}
