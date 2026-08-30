using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.SceneManagement;

/// <summary>
/// Creates a gameplay scene from DemoMap_An without modifying the source map.
/// Four routes use smooth entry/exit samples and a straight central combat segment.
/// </summary>
public static class MapZombieIntegrationSceneBuilder
{
    public const string SourceScenePath = "Assets/Scenes/SampleMap.unity";
    public const string ScenePath = "Assets/Scenes/MapZombieIntegration.unity";

    private const string PlantableSquarePath = "Assets/Prefabs/PlantableSquare.prefab";
    private const string PeashooterPath = "Assets/Prefabs/PeaShooter.prefab";
    private const string PeaProjectilePath = "Assets/Prefabs/Pea_Prefab.prefab";

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

    [MenuItem("Zombie House/Build Map + Plant + Zombie Integration")]
    public static void Build()
    {
        Scene scene = EditorSceneManager.OpenScene(SourceScenePath, OpenSceneMode.Single);
        Terrain terrain = Object.FindFirstObjectByType<Terrain>();
        Vector3 center = GroundPoint(terrain, Vector3.zero, 0.08f);

        RemoveSourceGameplayObjects();

        GameObject integrationRoot = new GameObject("[Zombie House Integration]");
        CreateHouseTarget(integrationRoot.transform, terrain, center);

        ZombieRoute[] routes = new ZombieRoute[RouteDirections.Length];
        List<PlantableSquare> showcaseSquares = new List<PlantableSquare>();
        for (int i = 0; i < RouteDirections.Length; i++)
        {
            routes[i] = CreateRoute(integrationRoot.transform, terrain, center, RouteDirections[i], RouteNames[i], i);
            showcaseSquares.AddRange(CreatePlantLine(integrationRoot.transform, terrain, center, RouteDirections[i], RouteNames[i]));
        }

        GameObject player = ZombiePlantIntegrationSceneBuilder.CreatePlayer(
            GroundPoint(terrain, center + new Vector3(-4.5f, 0f, -4.5f), 0.18f));
        player.transform.SetParent(integrationRoot.transform);

        CreatePlayerCamera(integrationRoot.transform, player.transform);
        CreateManagers(integrationRoot.transform, routes);
        CreateShowcasePlants(showcaseSquares);
        CreateEventSystem(integrationRoot.transform);
        UIAndSunGenerator.GenerateFullUI();

        EditorSceneManager.SaveScene(scene, ScenePath);
        EnsureInBuildSettings();
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log($"[MapZombieIntegration] Scene ready: {ScenePath}");
    }

    private static void RemoveSourceGameplayObjects()
    {
        foreach (Camera camera in Object.FindObjectsByType<Camera>(FindObjectsSortMode.None))
            Object.DestroyImmediate(camera.gameObject);

        foreach (Transform item in Object.FindObjectsByType<Transform>(FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            if (item.parent != null)
                continue;

            if (item.name.StartsWith("Player") || item.name.StartsWith("Knight"))
                item.gameObject.SetActive(false);
        }
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

    private static void CreateManagers(Transform parent, ZombieRoute[] routes)
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
        gameManager.restartDelay = 0f;

        ZombieSpawner spawner = managers.AddComponent<ZombieSpawner>();
        spawner.zombiePrefab = ZombiePrefabBuilder.LoadOrCreatePrefab();
        spawner.routes = routes;
        spawner.routeMoveSpeed = 2.55f;
        spawner.routeSpawnJitter = 0.28f;
        spawner.waves = new[]
        {
            new ZombieSpawner.WaveData { zombieCount = 4, spawnInterval = 1.4f, delayBeforeWave = 2.5f },
            new ZombieSpawner.WaveData { zombieCount = 8, spawnInterval = 1.1f, delayBeforeWave = 6f },
            new ZombieSpawner.WaveData { zombieCount = 12, spawnInterval = 0.85f, delayBeforeWave = 7f },
        };
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

    private static void CreateShowcasePlants(IReadOnlyList<PlantableSquare> squares)
    {
        GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PeashooterPath);
        if (prefab == null)
            return;

        // The middle square of each road is index 1, 4, 7 and 10.
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

    private static void CreatePlayerCamera(Transform parent, Transform player)
    {
        GameObject cameraObject = new GameObject("Main Camera");
        cameraObject.transform.SetParent(parent);
        Camera camera = cameraObject.AddComponent<Camera>();
        camera.tag = "MainCamera";
        camera.fieldOfView = 60f;
        camera.nearClipPlane = 0.3f;
        camera.farClipPlane = 1000f;

        Vector3 targetOffset = new Vector3(0f, 1.5f, 0f);
        Quaternion initialRotation = Quaternion.Euler(34f, player.eulerAngles.y, 0f);
        camera.transform.position = player.position + targetOffset + initialRotation * Vector3.back * 6f;
        camera.transform.rotation = initialRotation;

        CameraFollow follow = cameraObject.AddComponent<CameraFollow>();
        follow.target = player;
        follow.targetOffset = targetOffset;
        follow.distance = 6f;
        follow.minDistance = 1.5f;
        follow.maxDistance = 10f;
        follow.initialPitch = 34f;
        follow.mouseSensitivity = 1f;
        follow.smoothTime = 15f;
        follow.lockCursorDuringPlay = false;
        follow.collisionMask = ~0;
    }

    private static void CreateEventSystem(Transform parent)
    {
        GameObject eventSystem = new GameObject("EventSystem");
        eventSystem.transform.SetParent(parent);
        eventSystem.AddComponent<EventSystem>();
        eventSystem.AddComponent<InputSystemUIInputModule>();
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

    private static void EnsureInBuildSettings()
    {
        EditorBuildSettingsScene[] scenes = EditorBuildSettings.scenes;
        if (scenes.Any(item => item.path == ScenePath))
            return;

        EditorBuildSettings.scenes = scenes
            .Concat(new[] { new EditorBuildSettingsScene(ScenePath, true) })
            .ToArray();
    }
}
