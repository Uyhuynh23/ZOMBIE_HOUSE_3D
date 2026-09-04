using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// Menu: Zombie House → Build Step 3 – Plant + Zombie + Character Integration (F8)
/// Builds the full integration scene with:
///   - Ground + zombie lanes
///   - 5×3 PlantableSquare grid
///   - Knight character with PlayerController
///   - ZombieSpawner + GridManager
///   - GameManager (win/lose)
///   - Full HUD UI
/// </summary>
public static class ZombiePlantIntegrationSceneBuilder
{
    // ──────────────────────────────────────────────────────────
    // Asset paths
    // ──────────────────────────────────────────────────────────
    private const string ScenePath              = "Assets/Scenes/ZombiePlantIntegration.unity";
    private const string KnightModelPath        = "Assets/KayKit_Character_Pack_Adventures/Characters/fbx/Knight.fbx";
    private const string PlayerAnimatorPath     = "Assets/Animation/PlayerAnimator.controller";
    private const string PlantableSquarePath    = "Assets/Prefabs/PlantableSquare.prefab";
    private const string PeashooterPath         = "Assets/Prefabs/PeaShooter.prefab";
    private const string SnowPeaPath            = "Assets/Prefabs/PeaShooterFroze.prefab";
    private const string SunflowerPath          = "Assets/Prefabs/Sunflower.prefab";
    private const string PeaProjectilePath      = "Assets/Prefabs/Pea_Prefab.prefab";

    // ──────────────────────────────────────────────────────────
    // Entry point
    // ──────────────────────────────────────────────────────────
    [MenuItem("Zombie House/Build Step 3 - Plant + Zombie Integration _F8")]
    public static void Build()
    {
        Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

        ConfigureEnvironment();
        CreateGround();
        CreateHouse();

        PlantableSquare[,] grid = CreatePlantGrid(5, 3, 1.55f);

        // Managers first so Start() ordering is safe
        GameObject gameManagerGO = CreateManagers();

        GameObject player = CreatePlayer(grid[0, 0].transform.position);
        CreateCamera(player.transform);

        // Pre-plant a Peashooter for demo
        CreateShowcasePlant(grid[2, 1]);

        // Use a persistent prefab asset so this scene keeps working after Git merges.
        GameObject zombiePrefab = ZombiePrefabBuilder.LoadOrCreatePrefab();
        WireSpawner(gameManagerGO, zombiePrefab);

        CreateEventSystem();
        UIAndSunGenerator.GenerateFullUI();

        EditorSceneManager.SaveScene(scene, ScenePath);
        EnsureInBuildSettings();
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        Debug.Log("[ZombiePlantIntegration] Scene ready: " + ScenePath);
    }

    // ──────────────────────────────────────────────────────────
    // Environment
    // ──────────────────────────────────────────────────────────
    private static void ConfigureEnvironment()
    {
        RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Trilight;
        RenderSettings.ambientSkyColor    = new Color(0.26f, 0.36f, 0.48f);
        RenderSettings.ambientEquatorColor = new Color(0.34f, 0.42f, 0.34f);
        RenderSettings.ambientGroundColor  = new Color(0.12f, 0.16f, 0.12f);

        GameObject sun = new GameObject("Directional Light");
        Light light = sun.AddComponent<Light>();
        light.type      = LightType.Directional;
        light.color     = new Color(1f, 0.94f, 0.8f);
        light.intensity = 1.35f;
        light.shadows   = LightShadows.Soft;
        sun.transform.rotation = Quaternion.Euler(48f, -28f, 0f);
    }

    // ──────────────────────────────────────────────────────────
    // Ground + zombie lanes
    // ──────────────────────────────────────────────────────────
    private static void CreateGround()
    {
        // Base ground
        GameObject ground = GameObject.CreatePrimitive(PrimitiveType.Plane);
        ground.name = "Gameplay Ground (12 x 10)";
        ground.transform.localScale = new Vector3(1.4f, 1f, 1.2f);
        ground.GetComponent<Renderer>().sharedMaterial = CreateMaterial(
            "GameplayGround_Mat",
            new Color(0.16f, 0.34f, 0.17f));

        // Three zombie lanes (one per grid row)
        float[] laneZs = { -1.55f, 0f, 1.55f };
        for (int i = 0; i < laneZs.Length; i++)
        {
            GameObject lane = GameObject.CreatePrimitive(PrimitiveType.Cube);
            lane.name = $"Zombie Lane {i + 1}";
            lane.transform.position  = new Vector3(1f, 0.025f, laneZs[i]);
            lane.transform.localScale = new Vector3(14f, 0.05f, 1.1f);
            lane.GetComponent<Renderer>().sharedMaterial = CreateMaterial(
                $"ZombieLane{i}_Mat",
                new Color(0.27f, 0.2f, 0.13f));
            // No collision on lanes — just visual
            Object.DestroyImmediate(lane.GetComponent<Collider>());
        }
    }

    // ──────────────────────────────────────────────────────────
    // Plant grid
    // ──────────────────────────────────────────────────────────
    private static PlantableSquare[,] CreatePlantGrid(int columns, int rows, float spacing)
    {
        GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PlantableSquarePath);
        GameObject root   = new GameObject("Plant Grid (5 x 3)");
        PlantableSquare[,] grid = new PlantableSquare[columns, rows];

        float startX = -(columns - 1) * spacing * 0.5f;
        float startZ = -(rows - 1)    * spacing * 0.5f;

        for (int x = 0; x < columns; x++)
        {
            for (int z = 0; z < rows; z++)
            {
                GameObject square;
                if (prefab != null)
                    square = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
                else
                {
                    square = GameObject.CreatePrimitive(PrimitiveType.Cube);
                    square.AddComponent<PlantableSquare>();
                }

                square.name = $"Plantable [{x},{z}]";
                square.transform.SetParent(root.transform);
                square.transform.position   = new Vector3(startX + x * spacing, 0.08f, startZ + z * spacing);
                square.transform.localScale = new Vector3(1.4f, 0.16f, 1.4f);
                square.tag = "PlantableNode";
                grid[x, z] = square.GetComponent<PlantableSquare>();
            }
        }

        return grid;
    }

    // ──────────────────────────────────────────────────────────
    // Player character
    // ──────────────────────────────────────────────────────────
    internal static GameObject CreatePlayer(Vector3 squarePosition)
    {
        GameObject player = new GameObject("Player Character");
        player.transform.position = squarePosition + new Vector3(0f, 0.2f, 0f);

        CharacterController controller = player.AddComponent<CharacterController>();
        controller.height     = 1.7f;
        controller.radius     = 0.32f;
        controller.center     = new Vector3(0f, 0.85f, 0f);
        controller.stepOffset = 0.25f;

        PlayerController playerController = player.AddComponent<PlayerController>();
        playerController.moveSpeed        = 3.6f;
        playerController.plantingDuration = 0.35f;
        playerController.plants = new[]
        {
            CreatePlantData("Peashooter", PeashooterPath,  "Assets/UI/PlantPortraits/PeaShooter.png",   100, 2.5f),
            CreatePlantData("Snow Pea",   SnowPeaPath,     "Assets/UI/PlantPortraits/PeaShooterFroze.png", 175, 3.5f),
            CreatePlantData("Sunflower",  SunflowerPath,   "Assets/UI/PlantPortraits/Sunflower.png",    50,  2.5f)
        };

        // Try KayKit Knight model as visual
        GameObject fallback = CreateFallbackPlayerVisual(player.transform);
        GameObject preferred = null;
        GameObject model = AssetDatabase.LoadAssetAtPath<GameObject>(KnightModelPath);
        if (model != null)
        {
            preferred = (GameObject)PrefabUtility.InstantiatePrefab(model);
            preferred.name = "Knight Visual";
            preferred.transform.SetParent(player.transform, false);
            preferred.transform.localRotation = Quaternion.identity;
            ZombiePrototypeSceneBuilder.FitZombieVisual(preferred, 1.65f);

            Animator anim = preferred.GetComponent<Animator>();
            if (anim == null) anim = preferred.AddComponent<Animator>();
            anim.runtimeAnimatorController =
                AssetDatabase.LoadAssetAtPath<RuntimeAnimatorController>(PlayerAnimatorPath);
            anim.applyRootMotion = false;
        }

        PortablePlayerVisual portableVisual = player.AddComponent<PortablePlayerVisual>();
        portableVisual.Configure(preferred, fallback);
        return player;
    }

    private static PlantData CreatePlantData(string name, string prefabPath, string portraitPath, int cost, float cooldown)
    {
        return new PlantData
        {
            name         = name,
            prefab       = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath),
            portrait     = AssetDatabase.LoadAssetAtPath<Sprite>(portraitPath),
            cost         = cost,
            cooldownTime = cooldown
        };
    }

    private static GameObject CreateFallbackPlayerVisual(Transform parent)
    {
        GameObject root = new GameObject("Portable Player Visual");
        root.transform.SetParent(parent, false);
        Material bodyMat = CreateMaterial("PlayerBody_Mat", new Color(0.12f, 0.42f, 0.75f));
        Material skinMat = CreateMaterial("PlayerSkin_Mat", new Color(0.95f, 0.72f, 0.52f));

        GameObject body = GameObject.CreatePrimitive(PrimitiveType.Capsule);
        body.name = "Body";
        body.transform.SetParent(root.transform, false);
        body.transform.localPosition = new Vector3(0f, 0.75f, 0f);
        body.transform.localScale    = new Vector3(0.52f, 0.62f, 0.42f);
        body.GetComponent<Renderer>().sharedMaterial = bodyMat;
        Object.DestroyImmediate(body.GetComponent<Collider>());

        GameObject head = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        head.name = "Head";
        head.transform.SetParent(root.transform, false);
        head.transform.localPosition = new Vector3(0f, 1.55f, 0f);
        head.transform.localScale    = Vector3.one * 0.48f;
        head.GetComponent<Renderer>().sharedMaterial = skinMat;
        Object.DestroyImmediate(head.GetComponent<Collider>());
        return root;
    }

    // ──────────────────────────────────────────────────────────
    // Camera
    // ──────────────────────────────────────────────────────────
    private static void CreateCamera(Transform player)
    {
        GameObject camObj = new GameObject("Main Camera");
        Camera cam        = camObj.AddComponent<Camera>();
        cam.tag           = "MainCamera";
        cam.fieldOfView   = 56f;
        cam.clearFlags    = CameraClearFlags.SolidColor;
        cam.backgroundColor = new Color(0.12f, 0.2f, 0.28f);
        camObj.transform.position = new Vector3(0f, 8.2f, -8.8f);
        camObj.transform.LookAt(new Vector3(0f, 0.45f, 1.2f));

        CameraFollow follow = camObj.AddComponent<CameraFollow>();
        follow.target = player;
        follow.distance = 12.5f;
        follow.maxDistance = 15f;
        follow.initialPitch = 42f;
        follow.targetOffset = new Vector3(3.1f, 0.8f, 0f);
        follow.smoothTime = 9f;
        follow.lockCursorDuringPlay = false;
        follow.collisionMask = 0;
    }

    // ──────────────────────────────────────────────────────────
    // Managers (returns root GameObject so spawner can be wired)
    // ──────────────────────────────────────────────────────────
    private static GameObject CreateManagers()
    {
        GameObject go = new GameObject("GameManager");

        // Economy
        EconomyManager economy = go.AddComponent<EconomyManager>();
        economy.currentSun = 300;

        // Object Pool (projectiles)
        ObjectPoolManager pool  = go.AddComponent<ObjectPoolManager>();
        pool.peaPrefab          = AssetDatabase.LoadAssetAtPath<GameObject>(PeaProjectilePath);
        pool.initialPoolSize    = 20;

        // Grid manager (fixes missing script + provides lane data)
        GridManager grid = go.AddComponent<GridManager>();
        grid.rows    = 3;
        grid.columns = 5;

        // Game state
        go.AddComponent<GameManager>();

        return go;
    }

    // ──────────────────────────────────────────────────────────
    // Wire ZombieSpawner onto the GameManager object
    // ──────────────────────────────────────────────────────────
    private static void WireSpawner(GameObject gameManagerGO, GameObject zombieTemplate)
    {
        ZombieSpawner spawner     = gameManagerGO.AddComponent<ZombieSpawner>();
        spawner.zombiePrefab      = zombieTemplate;
        // Legacy lane fields removed — ZombieSpawner now uses NavMesh spawnPoints.
        spawner.waves = new ZombieSpawner.WaveData[]
        {
            new ZombieSpawner.WaveData { zombieCount = 3, spawnInterval = 2.2f, delayBeforeWave = 4f  },
            new ZombieSpawner.WaveData { zombieCount = 5, spawnInterval = 1.7f, delayBeforeWave = 7f },
            new ZombieSpawner.WaveData { zombieCount = 7, spawnInterval = 1.2f, delayBeforeWave = 7f },
        };
    }

    private static void CreateHouse()
    {
        GameObject house = GameObject.CreatePrimitive(PrimitiveType.Cube);
        house.name = "House Base";
        house.transform.position = new Vector3(-5.55f, 1f, 0f);
        house.transform.localScale = new Vector3(1.15f, 2f, 5.8f);
        house.GetComponent<Renderer>().sharedMaterial = CreateMaterial(
            "HouseBase_Mat", new Color(0.48f, 0.19f, 0.08f));

        HouseHealth health = house.AddComponent<HouseHealth>();
        health.maxHealth = 300;

        GameObject roof = GameObject.CreatePrimitive(PrimitiveType.Cube);
        roof.name = "House Roof";
        roof.transform.SetParent(house.transform, false);
        roof.transform.localPosition = new Vector3(0f, 0.68f, 0f);
        roof.transform.localRotation = Quaternion.Euler(0f, 0f, 45f);
        roof.transform.localScale = new Vector3(0.72f, 0.72f, 1.06f);
        roof.GetComponent<Renderer>().sharedMaterial = CreateMaterial(
            "HouseRoof_Mat", new Color(0.22f, 0.08f, 0.04f));
        Object.DestroyImmediate(roof.GetComponent<Collider>());
    }

    // ──────────────────────────────────────────────────────────
    // Pre-planted showcase
    // ──────────────────────────────────────────────────────────
    private static void CreateShowcasePlant(PlantableSquare square)
    {
        GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PeashooterPath);
        if (prefab == null || square == null) return;

        GameObject plant = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
        plant.name = "Showcase Peashooter";
        plant.transform.position = square.transform.position + Vector3.up * 0.08f;
        plant.transform.rotation = Quaternion.identity;

        PlantBase plantBase = plant.GetComponent<PlantBase>();
        if (plantBase != null) square.PlantHere(plantBase);
    }

    // ──────────────────────────────────────────────────────────
    // EventSystem
    // ──────────────────────────────────────────────────────────
    internal static void CreateEventSystem()
    {
        GameObject es = new GameObject("EventSystem");
        es.AddComponent<EventSystem>();
        es.AddComponent<InputSystemUIInputModule>();
    }

    // ──────────────────────────────────────────────────────────
    // Utilities
    // ──────────────────────────────────────────────────────────
    private static Material CreateMaterial(string name, Color color)
    {
        Shader shader = Shader.Find("Universal Render Pipeline/Lit")
                     ?? Shader.Find("Standard");

        Material mat = new Material(shader) { name = name };
        if (mat.HasProperty("_BaseColor")) mat.SetColor("_BaseColor", color);
        if (mat.HasProperty("_Color"))     mat.color = color;
        return mat;
    }

    private static void EnsureInBuildSettings()
    {
        EditorBuildSettingsScene[] scenes = EditorBuildSettings.scenes;
        if (scenes.Any(item => item.path == ScenePath)) return;

        EditorBuildSettings.scenes = scenes
            .Concat(new[] { new EditorBuildSettingsScene(ScenePath, true) })
            .ToArray();
    }
}
