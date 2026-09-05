using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.SceneManagement;

/// <summary>
/// Replicates the full Zombie, Routes, Planting Grid, Economy, UI, and Win/Lose mechanics
/// from MapZombieIntegration into the 3 official game maps:
/// - Map_Day.unity (Round 1)
/// - Map_Cloudy.unity (Round 2)
/// - Map_Night.unity (Round 3)
/// </summary>
public static class GameMapsIntegrationBuilder
{
    public const string MapDayScenePath = "Assets/Scenes/GameScenes/Map_Day.unity";
    public const string MapCloudyScenePath = "Assets/Scenes/GameScenes/Map_Cloudy.unity";
    public const string MapNightScenePath = "Assets/Scenes/GameScenes/Map_Night.unity";

    public const string WaveConfigsFolder = "Assets/Data/Waves";
    public const string WaveConfigDayPath = "Assets/Data/Waves/WaveConfig_Map_Day.asset";
    public const string WaveConfigCloudyPath = "Assets/Data/Waves/WaveConfig_Map_Cloudy.asset";
    public const string WaveConfigNightPath = "Assets/Data/Waves/WaveConfig_Map_Night.asset";

    private const string PlantableSquarePath = "Assets/Prefabs/PlantableSquare.prefab";
    private const string PeashooterPath = "Assets/Prefabs/PeaShooter.prefab";
    private const string SnowPeaPath = "Assets/Prefabs/PeaShooterFroze.prefab";
    private const string SunflowerPath = "Assets/Prefabs/Sunflower.prefab";
    private const string PeaProjectilePath = "Assets/Prefabs/Pea_Prefab.prefab";
    private const string ZombiePrefabPath = "Assets/Prefabs/Zombie.prefab";
    private const string SpiderPrefabPath = "Assets/Prefabs/Spider.prefab";
    private const string MinimapPrefabPath = "Assets/Prefabs/MinimapSystem.prefab";
    private const string InstructionBoardPrefabPath = "Assets/Prefabs/InstructionBoard3D.prefab";

    private static readonly Vector3[] RouteDirections =
    {
        Vector3.right,
        Vector3.forward,
        Vector3.left,
        Vector3.back,
    };

    private static readonly string[] RouteNames =
    {
        "East Road",
        "North Road",
        "West Road",
        "South Road",
    };

    [MenuItem("Zombie House/Integrate All Game Maps (Day, Cloudy, Night)")]
    public static void IntegrateAllMaps()
    {
        EnsureWaveConfigs();

        MapWaveConfig dayConfig = AssetDatabase.LoadAssetAtPath<MapWaveConfig>(WaveConfigDayPath);
        MapWaveConfig cloudyConfig = AssetDatabase.LoadAssetAtPath<MapWaveConfig>(WaveConfigCloudyPath);
        MapWaveConfig nightConfig = AssetDatabase.LoadAssetAtPath<MapWaveConfig>(WaveConfigNightPath);

        IntegrateScene(MapDayScenePath, dayConfig);
        IntegrateScene(MapCloudyScenePath, cloudyConfig);
        IntegrateScene(MapNightScenePath, nightConfig);

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log("[GameMapsIntegrationBuilder] 🎉 Successfully integrated all 3 maps (Day, Cloudy, Night)!");
    }

    [MenuItem("Zombie House/Integrate Scene -> Map_Day")]
    public static void IntegrateDay()
    {
        EnsureWaveConfigs();
        MapWaveConfig config = AssetDatabase.LoadAssetAtPath<MapWaveConfig>(WaveConfigDayPath);
        IntegrateScene(MapDayScenePath, config);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
    }

    [MenuItem("Zombie House/Integrate Scene -> Map_Cloudy")]
    public static void IntegrateCloudy()
    {
        EnsureWaveConfigs();
        MapWaveConfig config = AssetDatabase.LoadAssetAtPath<MapWaveConfig>(WaveConfigCloudyPath);
        IntegrateScene(MapCloudyScenePath, config);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
    }

    [MenuItem("Zombie House/Integrate Scene -> Map_Night")]
    public static void IntegrateNight()
    {
        EnsureWaveConfigs();
        MapWaveConfig config = AssetDatabase.LoadAssetAtPath<MapWaveConfig>(WaveConfigNightPath);
        IntegrateScene(MapNightScenePath, config);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
    }

    public static void IntegrateScene(string scenePath, MapWaveConfig waveConfig)
    {
        Scene scene = EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single);
        Terrain terrain = Object.FindFirstObjectByType<Terrain>();
        Vector3 center = GroundPoint(terrain, Vector3.zero, 0.08f);

        // Remove old integration roots or loose managers
        CleanExistingIntegration();

        // 1. Root gameplay container
        GameObject integrationRoot = new GameObject("[Zombie House Integration]");
        CreateHouseTarget(integrationRoot.transform, terrain, center);

        // 2. Routes & Plant lines
        ZombieRoute[] routes = new ZombieRoute[RouteDirections.Length];
        List<PlantableSquare> showcaseSquares = new List<PlantableSquare>();
        for (int i = 0; i < RouteDirections.Length; i++)
        {
            routes[i] = CreateRoute(integrationRoot.transform, terrain, center, RouteDirections[i], RouteNames[i], i);
            showcaseSquares.AddRange(CreatePlantLine(integrationRoot.transform, terrain, center, RouteDirections[i], RouteNames[i]));
        }

        // 3. Gameplay Managers
        CreateManagers(integrationRoot.transform, routes, waveConfig);

        // 4. Showcase Starter Peashooters (middle tile of each road)
        CreateShowcasePlants(showcaseSquares);

        // 5. EventSystem
        CreateEventSystem(integrationRoot.transform);

        // 6. PlayerSpawner setup
        ConfigurePlayerSpawner(terrain, center);

        // 7. Full UI (HUD, Health bar, Wave counter, Win/Lose panels)
        UIAndSunGenerator.GenerateFullUI();

        // 8. Minimap System
        EnsureMinimapSystem();

        // 9. 3D In-Game Instruction Board (Tutorial map only; removed from main maps)
        // EnsureInstructionBoard(integrationRoot.transform, terrain, center);

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        Debug.Log($"[GameMapsIntegrationBuilder] Scene integrated successfully: {scenePath}");
    }

    private static void CleanExistingIntegration()
    {
        GameObject oldRoot = GameObject.Find("[Zombie House Integration]");
        if (oldRoot != null)
            Object.DestroyImmediate(oldRoot);

        GameObject oldBoard = GameObject.Find("InstructionBoard3D");
        if (oldBoard != null)
            Object.DestroyImmediate(oldBoard);

        // Clean loose managers if any
        foreach (var obj in Object.FindObjectsByType<GameObject>(FindObjectsSortMode.None))
        {
            if (obj == null) continue;
            if (obj.name == "Map Game Managers" || obj.name == "House Target" || obj.name == "InstructionBoard3D")
                Object.DestroyImmediate(obj);
        }
    }

    private static void CreateHouseTarget(Transform parent, Terrain terrain, Vector3 center)
    {
        GameObject target = new GameObject("House Target");
        target.transform.SetParent(parent);
        target.transform.position = GroundPoint(terrain, center, 0.08f);
        HouseHealth health = target.AddComponent<HouseHealth>();
        health.maxHealth = 500;

        GameObject fallback = new GameObject("Portable Baker House");
        fallback.transform.SetParent(target.transform, false);
        fallback.AddComponent<MapFallbackVisibility>();

        Material wallMaterial = CreateMaterial("Map House Walls", new Color(0.48f, 0.2f, 0.08f));
        Material roofMaterial = CreateMaterial("Map House Roof", new Color(0.2f, 0.06f, 0.035f));

        GameObject walls = GameObject.CreatePrimitive(PrimitiveType.Cube);
        walls.name = "House Walls";
        walls.transform.SetParent(fallback.transform, false);
        walls.transform.localPosition = new Vector3(0f, 1.15f, 0f);
        walls.transform.localScale = new Vector3(4.3f, 2.3f, 4.8f);
        walls.GetComponent<Renderer>().sharedMaterial = wallMaterial;

        GameObject roof = GameObject.CreatePrimitive(PrimitiveType.Cube);
        roof.name = "House Roof";
        roof.transform.SetParent(fallback.transform, false);
        roof.transform.localPosition = new Vector3(0f, 2.55f, 0f);
        roof.transform.localRotation = Quaternion.Euler(0f, 0f, 45f);
        roof.transform.localScale = new Vector3(3.7f, 3.7f, 5.2f);
        roof.GetComponent<Renderer>().sharedMaterial = roofMaterial;
        Object.DestroyImmediate(roof.GetComponent<Collider>());
    }

    private static ZombieRoute CreateRoute(
        Transform parent,
        Terrain terrain,
        Vector3 center,
        Vector3 outward,
        string routeName,
        int routeIndex)
    {
        GameObject routeObject = new GameObject(routeName);
        routeObject.transform.SetParent(parent);
        ZombieRoute route = routeObject.AddComponent<ZombieRoute>();

        Vector3 side = Vector3.Cross(Vector3.up, outward).normalized;
        float curveSign = routeIndex % 2 == 0 ? 1f : -1f;
        List<Vector3> positions = new List<Vector3>();

        Vector3 entryStart = center + outward * 34f + side * (5.5f * curveSign);
        Vector3 entryControl = center + outward * 28f + side * (7f * curveSign);
        Vector3 combatStart = center + outward * 23f;
        for (int sample = 0; sample <= 5; sample++)
        {
            float t = sample / 5f;
            positions.Add(QuadraticBezier(entryStart, entryControl, combatStart, t));
        }

        positions.Add(center + outward * 19f);
        positions.Add(center + outward * 15f);
        positions.Add(center + outward * 11f);
        positions.Add(center + outward * 7f);

        Vector3 exitStart = center + outward * 7f;
        Vector3 exitControl = center + outward * 4.6f + side * (2.8f * curveSign);
        Vector3 houseAttack = center + outward * 2.8f + side * (0.35f * curveSign);
        for (int sample = 1; sample <= 4; sample++)
        {
            float t = sample / 4f;
            positions.Add(QuadraticBezier(exitStart, exitControl, houseAttack, t));
        }

        Transform[] points = new Transform[positions.Count];
        for (int i = 0; i < positions.Count; i++)
        {
            GameObject point = new GameObject($"Point {i:00}");
            point.transform.SetParent(routeObject.transform);
            point.transform.position = GroundPoint(terrain, positions[i], 0.08f);
            points[i] = point.transform;
        }

        route.Configure(points, 5, 9);
        return route;
    }

    private static IEnumerable<PlantableSquare> CreatePlantLine(
        Transform parent,
        Terrain terrain,
        Vector3 center,
        Vector3 outward,
        string routeName)
    {
        GameObject line = new GameObject(routeName + " Plants");
        line.transform.SetParent(parent);
        Quaternion aimRotation = Quaternion.FromToRotation(Vector3.right, outward);
        float[] distances = { 19f, 15f, 11f };

        foreach (float distance in distances)
        {
            GameObject squarePrefab = AssetDatabase.LoadAssetAtPath<GameObject>(PlantableSquarePath);
            GameObject square = squarePrefab != null
                ? (GameObject)PrefabUtility.InstantiatePrefab(squarePrefab)
                : GameObject.CreatePrimitive(PrimitiveType.Cube);

            square.name = $"Plantable {routeName} {distance:00}";
            square.transform.SetParent(line.transform);
            square.transform.position = GroundPoint(terrain, center + outward * distance, 0.06f);
            square.transform.rotation = aimRotation;
            square.transform.localScale = new Vector3(1.35f, 0.14f, 1.35f);

            PlantableSquare plantable = square.GetComponent<PlantableSquare>();
            if (plantable == null)
                plantable = square.AddComponent<PlantableSquare>();
            yield return plantable;
        }
    }

    private static void CreateManagers(Transform parent, ZombieRoute[] routes, MapWaveConfig waveConfig)
    {
        GameObject managers = new GameObject("Map Game Managers");
        managers.transform.SetParent(parent);

        EconomyManager economy = managers.AddComponent<EconomyManager>();
        economy.currentSun = 400;

        ObjectPoolManager pool = managers.AddComponent<ObjectPoolManager>();
        pool.peaPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(PeaProjectilePath);
        pool.initialPoolSize = 32;

        GridManager grid = managers.AddComponent<GridManager>();
        grid.rows = 4;
        grid.columns = 3;

        GameManager gameManager = managers.AddComponent<GameManager>();
        gameManager.restartDelay = 4f; // 4 seconds delay to transition to next round or return to menu

        ZombieSpawner spawner = managers.AddComponent<ZombieSpawner>();
        spawner.zombiePrefab = AssetDatabase.LoadAssetAtPath<GameObject>(ZombiePrefabPath);
        spawner.routes = routes;
        spawner.waveConfig = waveConfig;

        if (waveConfig != null)
        {
            spawner.routeMoveSpeed = waveConfig.routeMoveSpeed;
            spawner.routeSpawnJitter = waveConfig.routeSpawnJitter;
            spawner.enemyPrefabs = waveConfig.allowedEnemyPrefabs;
            spawner.waves = waveConfig.waves;
        }
    }

    private static void CreateShowcasePlants(IReadOnlyList<PlantableSquare> squares)
    {
        GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PeashooterPath);
        if (prefab == null) return;

        // Middle square of each road (index 1, 4, 7, 10)
        for (int i = 1; i < squares.Count; i += 3)
        {
            PlantableSquare square = squares[i];
            GameObject plant = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
            plant.name = "Map Showcase Peashooter";
            plant.transform.position = square.transform.position + Vector3.up * 0.08f;
            plant.transform.rotation = square.transform.rotation * prefab.transform.rotation;
            PlantBase plantBase = plant.GetComponent<PlantBase>();
            if (plantBase != null)
                square.PlantHere(plantBase);
        }
    }

    private static void CreateEventSystem(Transform parent)
    {
        if (Object.FindFirstObjectByType<EventSystem>() != null)
            return;

        GameObject eventSystem = new GameObject("EventSystem");
        eventSystem.transform.SetParent(parent);
        eventSystem.AddComponent<EventSystem>();
        eventSystem.AddComponent<InputSystemUIInputModule>();
    }

    private static void ConfigurePlayerSpawner(Terrain terrain, Vector3 center)
    {
        PlayerSpawner spawner = Object.FindFirstObjectByType<PlayerSpawner>();
        if (spawner == null)
        {
            GameObject spawnerObj = new GameObject("Spawner");
            spawner = spawnerObj.AddComponent<PlayerSpawner>();
        }

        // Setup spawn position
        Vector3 spawnPos = GroundPoint(terrain, center + new Vector3(-4.5f, 0f, -4.5f), 0.18f);
        spawner.transform.position = spawnPos;

        // Create PlantLoadout
        spawner.plantLoadout = new PlantData[]
        {
            new PlantData
            {
                name = "Peashooter",
                prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PeashooterPath),
                portrait = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/UI/PlantPortraits/PeaShooter.png"),
                cost = 100,
                cooldownTime = 2.5f
            },
            new PlantData
            {
                name = "Snow Pea",
                prefab = AssetDatabase.LoadAssetAtPath<GameObject>(SnowPeaPath),
                portrait = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/UI/PlantPortraits/PeaShooterFroze.png"),
                cost = 175,
                cooldownTime = 3.5f
            },
            new PlantData
            {
                name = "Sunflower",
                prefab = AssetDatabase.LoadAssetAtPath<GameObject>(SunflowerPath),
                portrait = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/UI/PlantPortraits/Sunflower.png"),
                cost = 50,
                cooldownTime = 2.5f
            }
        };

        // Wire CameraFollow
        CameraFollow camFollow = Object.FindFirstObjectByType<CameraFollow>();
        if (camFollow != null)
        {
            spawner.cameraFollow = camFollow;
        }
    }

    private static void EnsureMinimapSystem()
    {
        if (GameObject.Find("MinimapSystem") != null)
            return;

        GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(MinimapPrefabPath);
        if (prefab != null)
        {
            GameObject minimap = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
            minimap.name = "MinimapSystem";
        }
    }

    public static void EnsureWaveConfigs()
    {
        if (!Directory.Exists(WaveConfigsFolder))
            Directory.CreateDirectory(WaveConfigsFolder);

        GameObject zombiePrefab = AssetDatabase.LoadAssetAtPath<GameObject>(ZombiePrefabPath);
        GameObject spiderPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(SpiderPrefabPath);

        // Day Config (Round 1 / Easy)
        if (AssetDatabase.LoadAssetAtPath<MapWaveConfig>(WaveConfigDayPath) == null)
        {
            MapWaveConfig day = ScriptableObject.CreateInstance<MapWaveConfig>();
            day.mapName = "Round 1 - Day";
            day.routeMoveSpeed = 2.35f;
            day.routeSpawnJitter = 0.25f;
            day.allowedEnemyPrefabs = new[] { zombiePrefab };
            day.waves = new[]
            {
                new ZombieSpawner.WaveData { zombieCount = 4, spawnInterval = 1.8f, delayBeforeWave = 3f },
                new ZombieSpawner.WaveData { zombieCount = 8, spawnInterval = 1.4f, delayBeforeWave = 6f },
                new ZombieSpawner.WaveData { zombieCount = 12, spawnInterval = 1.0f, delayBeforeWave = 7f }
            };
            AssetDatabase.CreateAsset(day, WaveConfigDayPath);
            Debug.Log($"[GameMapsIntegrationBuilder] Created {WaveConfigDayPath}");
        }

        // Cloudy Config (Round 2 / Medium)
        if (AssetDatabase.LoadAssetAtPath<MapWaveConfig>(WaveConfigCloudyPath) == null)
        {
            MapWaveConfig cloudy = ScriptableObject.CreateInstance<MapWaveConfig>();
            cloudy.mapName = "Round 2 - Cloudy";
            cloudy.routeMoveSpeed = 2.65f;
            cloudy.routeSpawnJitter = 0.30f;
            cloudy.allowedEnemyPrefabs = spiderPrefab != null
                ? new[] { zombiePrefab, spiderPrefab }
                : new[] { zombiePrefab };
            cloudy.waves = new[]
            {
                new ZombieSpawner.WaveData { zombieCount = 6, spawnInterval = 1.4f, delayBeforeWave = 3f },
                new ZombieSpawner.WaveData { zombieCount = 10, spawnInterval = 1.1f, delayBeforeWave = 5.5f },
                new ZombieSpawner.WaveData { zombieCount = 14, spawnInterval = 0.9f, delayBeforeWave = 6f },
                new ZombieSpawner.WaveData { zombieCount = 18, spawnInterval = 0.8f, delayBeforeWave = 6.5f }
            };
            AssetDatabase.CreateAsset(cloudy, WaveConfigCloudyPath);
            Debug.Log($"[GameMapsIntegrationBuilder] Created {WaveConfigCloudyPath}");
        }

        // Night Config (Round 3 / Hard)
        if (AssetDatabase.LoadAssetAtPath<MapWaveConfig>(WaveConfigNightPath) == null)
        {
            MapWaveConfig night = ScriptableObject.CreateInstance<MapWaveConfig>();
            night.mapName = "Round 3 - Night";
            night.routeMoveSpeed = 2.95f;
            night.routeSpawnJitter = 0.35f;
            night.allowedEnemyPrefabs = spiderPrefab != null
                ? new[] { zombiePrefab, spiderPrefab }
                : new[] { zombiePrefab };
            night.waves = new[]
            {
                new ZombieSpawner.WaveData { zombieCount = 8, spawnInterval = 1.2f, delayBeforeWave = 3f },
                new ZombieSpawner.WaveData { zombieCount = 12, spawnInterval = 1.0f, delayBeforeWave = 5f },
                new ZombieSpawner.WaveData { zombieCount = 16, spawnInterval = 0.85f, delayBeforeWave = 5f },
                new ZombieSpawner.WaveData { zombieCount = 20, spawnInterval = 0.75f, delayBeforeWave = 5.5f },
                new ZombieSpawner.WaveData { zombieCount = 25, spawnInterval = 0.65f, delayBeforeWave = 6f }
            };
            AssetDatabase.CreateAsset(night, WaveConfigNightPath);
            Debug.Log($"[GameMapsIntegrationBuilder] Created {WaveConfigNightPath}");
        }

        AssetDatabase.SaveAssets();
    }

    private static void EnsureInstructionBoard(Transform parent, Terrain terrain, Vector3 center)
    {
        GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(InstructionBoardPrefabPath);
        if (prefab == null)
        {
            prefab = InstructionBoardBuilder.BuildPrefab();
        }

        Vector3 boardPos = center + new Vector3(-2.6f, 0f, -3.2f);
        Vector3 groundPos = GroundPoint(terrain, boardPos, 0.02f);
        Vector3 playerSpawn = center + new Vector3(-4.5f, 0f, -4.5f);

        GameObject instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab, parent);
        instance.name = "InstructionBoard3D";
        instance.transform.position = groundPos;

        Vector3 lookDir = (playerSpawn - groundPos);
        lookDir.y = 0f;
        if (lookDir != Vector3.zero)
        {
            instance.transform.rotation = Quaternion.LookRotation(lookDir.normalized);
        }
        else
        {
            instance.transform.rotation = Quaternion.Euler(0f, 45f, 0f);
        }
    }

    private static Vector3 GroundPoint(Terrain terrain, Vector3 position, float offset)
    {
        if (terrain == null)
            return new Vector3(position.x, offset, position.z);

        float height = terrain.SampleHeight(position) + terrain.transform.position.y;
        return new Vector3(position.x, height + offset, position.z);
    }

    private static Vector3 QuadraticBezier(Vector3 start, Vector3 control, Vector3 end, float t)
    {
        float inverse = 1f - t;
        return inverse * inverse * start + 2f * inverse * t * control + t * t * end;
    }

    private static Material CreateMaterial(string name, Color color)
    {
        Shader shader = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");
        Material material = new Material(shader) { name = name };
        if (material.HasProperty("_BaseColor")) material.SetColor("_BaseColor", color);
        if (material.HasProperty("_Color")) material.color = color;
        return material;
    }
}
