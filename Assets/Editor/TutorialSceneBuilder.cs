using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// Editor utility to build the dedicated 3D Tutorial Map (Map_Tutorial.unity).
/// Automatically configures:
/// - Base Day environment, Baker's House, and East Road.
/// - 3D Tactical Instruction Table with modifiable Sprite slots for AI-generated art.
/// - 3D Waypoints for Checkpoints 1 and 2.
/// - TutorialManager state machine.
/// - Plantable soil squares, Sun economy, and East Road zombie route.
/// - Adds scene to Build Settings.
/// </summary>
public static class TutorialSceneBuilder
{
    public const string SourceScenePath = "Assets/Scenes/GameScenes/Map_Day.unity";
    public const string TutorialScenePath = "Assets/Scenes/GameScenes/Map_Tutorial.unity";

    private const string PlantableSquarePath = "Assets/Prefabs/PlantableSquare.prefab";
    private const string PeashooterPath = "Assets/Prefabs/PeaShooter.prefab";
    private const string SnowPeaPath = "Assets/Prefabs/PeaShooterFroze.prefab";
    private const string SunflowerPath = "Assets/Prefabs/Sunflower.prefab";
    private const string ZombiePrefabPath = "Assets/Prefabs/Zombie.prefab";
    private const string SunPrefabPath = "Assets/Prefabs/Sun.prefab";
    private const string RobotoBoldPath = "Assets/TextMesh Pro/Examples & Extras/Fonts/Roboto-Bold.ttf";
    private const string ShlopFontPath = "Assets/Fonts/shlop/shlop rg.otf";

    [MenuItem("Zombie House/Build Tutorial Map (Map_Tutorial)")]
    public static void BuildTutorialMap()
    {
        // 1. Ensure scene exists by copying from Map_Day if needed
        if (!File.Exists(TutorialScenePath))
        {
            if (File.Exists(SourceScenePath))
            {
                AssetDatabase.CopyAsset(SourceScenePath, TutorialScenePath);
                AssetDatabase.Refresh();
                Debug.Log($"[TutorialSceneBuilder] Cloned {SourceScenePath} to {TutorialScenePath}");
            }
            else
            {
                Debug.LogError($"[TutorialSceneBuilder] Source scene {SourceScenePath} not found!");
                return;
            }
        }

        // 2. Open Tutorial Scene
        Scene scene = EditorSceneManager.OpenScene(TutorialScenePath, OpenSceneMode.Single);
        Terrain terrain = Object.FindFirstObjectByType<Terrain>();
        Vector3 center = GroundPoint(terrain, Vector3.zero, 0.08f);

        // 3. Clean old integration roots or loose managers
        CleanExistingObjects();

        // 4. Root container
        GameObject tutorialRoot = new GameObject("[Tutorial Integration]");

        // 5. House Target
        HouseHealth houseHealth = CreateHouseTarget(tutorialRoot.transform, terrain, center);

        // 6. East Road (Tutorial Route)
        ZombieRoute route = CreateEastRoute(tutorialRoot.transform, terrain, center);

        // 7. Garden line (3 soil squares along East Road)
        List<PlantableSquare> squares = new List<PlantableSquare>(CreatePlantLine(tutorialRoot.transform, terrain, center));

        // 8. Player Spawner
        ConfigurePlayerSpawner(terrain, center);

        // 9. EventSystem
        CreateEventSystem(tutorialRoot.transform);

        // 10. Gameplay Managers (Economy, ObjectPool, GridManager)
        CreateManagers(tutorialRoot.transform);

        // 11. 3D Tactical Instruction Table (with AI Sprite slots!)
        TutorialTable3D table = Create3DTutorialTable(tutorialRoot.transform, terrain, center);

        // 12. 3D Waypoint Checkpoints
        TutorialWaypoint wp1 = CreateWaypoint(tutorialRoot.transform, terrain, center + new Vector3(-3.2f, 0f, -4.2f), "Checkpoint 1: Training Yard", new Color(0.2f, 0.8f, 1f));
        TutorialWaypoint wp2 = CreateWaypoint(tutorialRoot.transform, terrain, center + new Vector3(8.5f, 0f, 0f), "Checkpoint 2: Plant Line", new Color(1f, 0.85f, 0.2f));

        // 13. UI Setup
        UIAndSunGenerator.GenerateFullUI();
        GameObject completionPanel = CreateCompletionUI();

        // 14. TutorialManager State Machine
        GameObject managerObj = new GameObject("TutorialManager");
        managerObj.transform.SetParent(tutorialRoot.transform, false);
        TutorialManager manager = managerObj.AddComponent<TutorialManager>();
        manager.tutorialTable = table;
        manager.waypoint1 = wp1;
        manager.waypoint2 = wp2;
        manager.gardenSquares = squares.ToArray();
        manager.tutorialRoute = route;
        manager.houseHealth = houseHealth;
        manager.sunPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(SunPrefabPath);
        manager.zombiePrefab = AssetDatabase.LoadAssetAtPath<GameObject>(ZombiePrefabPath);
        manager.completionPanel = completionPanel;
        manager.titleFont = LoadShlopFont();
        manager.bodyFont = LoadUIFont();

        if (completionPanel != null)
        {
            Button[] buttons = completionPanel.GetComponentsInChildren<Button>(true);
            if (buttons.Length > 0) manager.nextRoundButton = buttons[0];
            if (buttons.Length > 1) manager.mainMenuButton = buttons[1];
        }

        // 15. Ensure in Build Settings
        AddSceneToBuildSettings(TutorialScenePath);

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        Debug.Log("[TutorialSceneBuilder] 🎉 Map_Tutorial built and saved successfully!");
    }

    [MenuItem("Zombie House/Open Tutorial Map")]
    public static void OpenTutorialMap()
    {
        if (File.Exists(TutorialScenePath))
        {
            EditorSceneManager.OpenScene(TutorialScenePath, OpenSceneMode.Single);
        }
        else
        {
            BuildTutorialMap();
        }
    }

    private static void CleanExistingObjects()
    {
        GameObject oldIntegration = GameObject.Find("[Zombie House Integration]");
        if (oldIntegration != null) Object.DestroyImmediate(oldIntegration);

        GameObject oldTutorial = GameObject.Find("[Tutorial Integration]");
        if (oldTutorial != null) Object.DestroyImmediate(oldTutorial);

        foreach (var obj in Object.FindObjectsByType<GameObject>(FindObjectsSortMode.None))
        {
            if (obj == null) continue;
            if (obj.name == "Map Game Managers" || obj.name == "House Target" || 
                obj.name == "InstructionBoard3D" || obj.name == "TutorialTable3D" ||
                obj.name == "TutorialManager")
            {
                Object.DestroyImmediate(obj);
            }
        }
    }

    private static HouseHealth CreateHouseTarget(Transform parent, Terrain terrain, Vector3 center)
    {
        GameObject target = new GameObject("House Target");
        target.transform.SetParent(parent);
        target.transform.position = GroundPoint(terrain, center, 0.08f);
        HouseHealth health = target.AddComponent<HouseHealth>();
        health.maxHealth = 500;

        GameObject fallback = new GameObject("Portable Baker House");
        fallback.transform.SetParent(target.transform, false);
        fallback.AddComponent<MapFallbackVisibility>();

        Material wallMat = CreateMaterial("Map House Walls", new Color(0.48f, 0.2f, 0.08f));
        Material roofMat = CreateMaterial("Map House Roof", new Color(0.2f, 0.06f, 0.035f));

        GameObject walls = GameObject.CreatePrimitive(PrimitiveType.Cube);
        walls.name = "House Walls";
        walls.transform.SetParent(fallback.transform, false);
        walls.transform.localPosition = new Vector3(0f, 1.15f, 0f);
        walls.transform.localScale = new Vector3(4.3f, 2.3f, 4.8f);
        walls.GetComponent<Renderer>().sharedMaterial = wallMat;

        GameObject roof = GameObject.CreatePrimitive(PrimitiveType.Cube);
        roof.name = "House Roof";
        roof.transform.SetParent(fallback.transform, false);
        roof.transform.localPosition = new Vector3(0f, 2.55f, 0f);
        roof.transform.localRotation = Quaternion.Euler(0f, 0f, 45f);
        roof.transform.localScale = new Vector3(3.7f, 3.7f, 5.2f);
        roof.GetComponent<Renderer>().sharedMaterial = roofMat;
        Object.DestroyImmediate(roof.GetComponent<Collider>());

        return health;
    }

    private static ZombieRoute CreateEastRoute(Transform parent, Terrain terrain, Vector3 center)
    {
        GameObject routeObject = new GameObject("East Road (Tutorial Route)");
        routeObject.transform.SetParent(parent);
        ZombieRoute route = routeObject.AddComponent<ZombieRoute>();

        Vector3 outward = Vector3.right;
        Vector3 side = Vector3.Cross(Vector3.up, outward).normalized;
        List<Vector3> positions = new List<Vector3>();

        Vector3 entryStart = center + outward * 34f + side * 5.5f;
        Vector3 entryControl = center + outward * 28f + side * 7f;
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
        positions.Add(center + outward * 3.5f);

        Transform[] points = new Transform[positions.Count];
        for (int i = 0; i < positions.Count; i++)
        {
            GameObject point = new GameObject($"Point {i:00}");
            point.transform.SetParent(routeObject.transform);
            point.transform.position = GroundPoint(terrain, positions[i], 0.08f);
            points[i] = point.transform;
        }

        route.Configure(points, 5, 8);
        return route;
    }

    private static IEnumerable<PlantableSquare> CreatePlantLine(Transform parent, Terrain terrain, Vector3 center)
    {
        GameObject line = new GameObject("East Road Plants");
        line.transform.SetParent(parent);
        float[] distances = { 15f, 11f, 7f };

        foreach (float distance in distances)
        {
            GameObject squarePrefab = AssetDatabase.LoadAssetAtPath<GameObject>(PlantableSquarePath);
            GameObject square = squarePrefab != null
                ? (GameObject)PrefabUtility.InstantiatePrefab(squarePrefab, line.transform)
                : GameObject.CreatePrimitive(PrimitiveType.Cube);

            square.name = $"Plantable East {distance:00}";
            square.transform.position = GroundPoint(terrain, center + Vector3.right * distance, 0.06f);
            square.transform.rotation = Quaternion.identity;
            square.transform.localScale = new Vector3(1.35f, 0.14f, 1.35f);

            PlantableSquare plantable = square.GetComponent<PlantableSquare>();
            if (plantable == null) plantable = square.AddComponent<PlantableSquare>();
            yield return plantable;
        }
    }

    private static TutorialTable3D Create3DTutorialTable(Transform parent, Terrain terrain, Vector3 center)
    {
        Vector3 tablePos = center + new Vector3(-6.2f, 0f, -3.2f);
        Vector3 groundPos = GroundPoint(terrain, tablePos, 0.02f);
        Vector3 playerSpawn = center + new Vector3(-4.5f, 0f, -4.5f);

        GameObject tableObj = new GameObject("TutorialTable3D");
        tableObj.transform.SetParent(parent, false);
        tableObj.transform.position = groundPos;

        // The front face of the table and display easel faces towards -Z local.
        // So +Z forward must point away from playerSpawn so the easel faces the player and training yard.
        Vector3 awayFromPlayer = (groundPos - playerSpawn);
        awayFromPlayer.y = 0f;
        if (awayFromPlayer != Vector3.zero)
            tableObj.transform.rotation = Quaternion.LookRotation(awayFromPlayer.normalized);
        else
            tableObj.transform.rotation = Quaternion.Euler(0f, 315f, 0f);

        TutorialTable3D table = tableObj.AddComponent<TutorialTable3D>();

        // Materials
        Material woodDark = CreateMaterial("TableWoodDark", new Color(0.24f, 0.14f, 0.08f));
        Material woodParchment = CreateMaterial("TableParchment", new Color(0.85f, 0.78f, 0.68f));
        Material keyBaseMat = CreateMaterial("TableKeyBase", new Color(0.12f, 0.12f, 0.15f));
        Material keyFaceMat = CreateMaterial("TableKeyFace", new Color(0.92f, 0.90f, 0.85f));
        Material lanternMat = CreateEmissiveMaterial("TableLanternGold", new Color(1f, 0.75f, 0.25f), 2.5f);

        // Physical Table Geometry
        GameObject tableGeo = new GameObject("TableGeometry");
        tableGeo.transform.SetParent(tableObj.transform, false);

        // Table Top Slab
        CreateCube(tableGeo.transform, "TableTop", new Vector3(0f, 0.85f, 0f), new Vector3(2.8f, 0.12f, 1.5f), woodDark);
        // Table Legs
        CreateCube(tableGeo.transform, "Leg_FL", new Vector3(-1.25f, 0.42f, -0.6f), new Vector3(0.14f, 0.85f, 0.14f), woodDark);
        CreateCube(tableGeo.transform, "Leg_FR", new Vector3(1.25f, 0.42f, -0.6f), new Vector3(0.14f, 0.85f, 0.14f), woodDark);
        CreateCube(tableGeo.transform, "Leg_BL", new Vector3(-1.25f, 0.42f, 0.6f), new Vector3(0.14f, 0.85f, 0.14f), woodDark);
        CreateCube(tableGeo.transform, "Leg_BR", new Vector3(1.25f, 0.42f, 0.6f), new Vector3(0.14f, 0.85f, 0.14f), woodDark);

        // Angled Display Easel on back of table (Enlarged for huge, clear text)
        GameObject easel = new GameObject("DisplayEasel");
        easel.transform.SetParent(tableObj.transform, false);
        easel.transform.localPosition = new Vector3(0f, 1.62f, 0.35f);
        easel.transform.localRotation = Quaternion.Euler(-12f, 0f, 0f);

        CreateCube(easel.transform, "Backboard", Vector3.zero, new Vector3(3.0f, 1.6f, 0.08f), woodDark);
        CreateCube(easel.transform, "ParchmentSurface", new Vector3(0f, 0f, -0.045f), new Vector3(2.9f, 1.5f, 0.02f), woodParchment);

        // Warm Lantern on Table Corner
        GameObject lantern = new GameObject("TableLantern");
        lantern.transform.SetParent(tableObj.transform, false);
        lantern.transform.localPosition = new Vector3(1.42f, 1.25f, -0.45f);

        CreateCube(lantern.transform, "Post", new Vector3(0f, -0.15f, 0f), new Vector3(0.06f, 0.45f, 0.06f), woodDark);
        CreateCube(lantern.transform, "LanternBody", new Vector3(0f, 0.15f, 0f), new Vector3(0.16f, 0.22f, 0.16f), lanternMat);

        Light pLight = lantern.AddComponent<Light>();
        pLight.type = LightType.Point;
        pLight.color = new Color(1f, 0.85f, 0.55f);
        pLight.range = 6.5f;
        pLight.intensity = 1.8f;

        // 3D Keycap Groups on Tabletop
        GameObject moveKeys = new GameObject("MovementKeyGroup");
        moveKeys.transform.SetParent(tableObj.transform, false);
        moveKeys.transform.localPosition = new Vector3(-0.75f, 0.93f, -0.15f);
        Create3DKey(moveKeys.transform, "Key_W", new Vector3(0f, 0f, 0.20f), "W", keyBaseMat, keyFaceMat);
        Create3DKey(moveKeys.transform, "Key_A", new Vector3(-0.22f, 0f, 0f), "A", keyBaseMat, keyFaceMat);
        Create3DKey(moveKeys.transform, "Key_S", new Vector3(0f, 0f, 0f), "S", keyBaseMat, keyFaceMat);
        Create3DKey(moveKeys.transform, "Key_D", new Vector3(0.22f, 0f, 0f), "D", keyBaseMat, keyFaceMat);
        table.movementKeycapGroup = moveKeys;

        GameObject plantKeys = new GameObject("PlantingKeyGroup");
        plantKeys.transform.SetParent(tableObj.transform, false);
        plantKeys.transform.localPosition = new Vector3(0f, 0.93f, -0.15f);
        Create3DKey(plantKeys.transform, "Key_1", new Vector3(-0.35f, 0f, 0f), "1", keyBaseMat, keyFaceMat);
        Create3DKey(plantKeys.transform, "Key_2", new Vector3(-0.12f, 0f, 0f), "2", keyBaseMat, keyFaceMat);
        Create3DKey(plantKeys.transform, "Key_3", new Vector3(0.12f, 0f, 0f), "3", keyBaseMat, keyFaceMat);
        Create3DKey(plantKeys.transform, "Key_E", new Vector3(0.38f, 0f, 0f), "E", keyBaseMat, keyFaceMat);
        plantKeys.SetActive(false);
        table.plantingKeycapGroup = plantKeys;

        GameObject combatKeys = new GameObject("CombatKeyGroup");
        combatKeys.transform.SetParent(tableObj.transform, false);
        combatKeys.transform.localPosition = new Vector3(0.75f, 0.93f, -0.15f);
        Create3DKey(combatKeys.transform, "Key_LMB", new Vector3(-0.22f, 0f, 0f), "LMB", keyBaseMat, keyFaceMat);
        Create3DKey(combatKeys.transform, "Key_SPACE", new Vector3(0.22f, 0f, 0f), "SPACE", keyBaseMat, keyFaceMat);
        combatKeys.SetActive(false);
        table.combatKeycapGroup = combatKeys;

        // Inspect Camera Anchor (Framed to show entire easel and tabletop tactile keys)
        GameObject inspectAnchor = new GameObject("InspectCameraAnchor");
        inspectAnchor.transform.SetParent(tableObj.transform, false);
        inspectAnchor.transform.localPosition = new Vector3(0f, 1.60f, -1.75f);
        inspectAnchor.transform.localRotation = Quaternion.Euler(5f, 0f, 0f);
        table.inspectCameraAnchor = inspectAnchor.transform;

        // Canvas on Display Easel (High resolution with large fonts)
        Font uiFont = LoadUIFont();
        GameObject canvasObj = new GameObject("TableCanvas");
        canvasObj.transform.SetParent(easel.transform, false);
        canvasObj.transform.localPosition = new Vector3(0f, 0f, -0.06f);
        Canvas canvas = canvasObj.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.WorldSpace;

        RectTransform cRt = canvasObj.GetComponent<RectTransform>();
        cRt.sizeDelta = new Vector2(1060f, 560f);
        cRt.localScale = new Vector3(0.0026f, 0.0026f, 0.0026f);

        // Panel Background Image (Modifiable Skin)
        Image bgImg = canvasObj.AddComponent<Image>();
        bgImg.color = new Color(0.14f, 0.10f, 0.06f, 0.94f);
        table.panelBackgroundImage = bgImg;

        // Header Banner (Modifiable Ribbon)
        GameObject headerObj = new GameObject("HeaderBanner");
        headerObj.transform.SetParent(canvasObj.transform, false);
        RectTransform hRt = headerObj.AddComponent<RectTransform>();
        hRt.anchorMin = new Vector2(0f, 1f);
        hRt.anchorMax = new Vector2(1f, 1f);
        hRt.pivot = new Vector2(0.5f, 1f);
        hRt.anchoredPosition = new Vector2(0f, -8f);
        hRt.sizeDelta = new Vector2(-16f, 68f);

        Image hImg = headerObj.AddComponent<Image>();
        hImg.color = new Color(0.24f, 0.15f, 0.08f, 0.95f);
        table.headerRibbonImage = hImg;

        GameObject titleObj = new GameObject("TitleText");
        titleObj.transform.SetParent(headerObj.transform, false);
        Text titleText = titleObj.AddComponent<Text>();
        titleText.font = LoadShlopFont();
        titleText.fontSize = 38; // BIG Title
        titleText.fontStyle = FontStyle.Bold;
        titleText.alignment = TextAnchor.MiddleCenter;
        titleText.color = new Color(1f, 0.88f, 0.35f);
        titleText.text = "TUTORIAL - STEP 1: HERO MOVEMENT";
        RectTransform tRt = titleObj.GetComponent<RectTransform>();
        tRt.anchorMin = Vector2.zero;
        tRt.anchorMax = Vector2.one;
        tRt.sizeDelta = Vector2.zero;
        table.headerTitleText = titleText;

        // Central Illustration Frame (Modifiable AI Sprite Slot)
        GameObject illusObj = new GameObject("IllustrationFrame");
        illusObj.transform.SetParent(canvasObj.transform, false);
        RectTransform iRt = illusObj.AddComponent<RectTransform>();
        iRt.anchorMin = new Vector2(0.04f, 0.10f);
        iRt.anchorMax = new Vector2(0.36f, 0.86f);
        iRt.offsetMin = Vector2.zero;
        iRt.offsetMax = Vector2.zero;

        Image illusImg = illusObj.AddComponent<Image>();
        illusImg.color = new Color(0.25f, 0.22f, 0.18f, 0.8f);
        illusImg.preserveAspect = true;
        table.illustrationImage = illusImg;

        // Instruction Text (Right Column - BIG FONT)
        GameObject bodyObj = new GameObject("BodyText");
        bodyObj.transform.SetParent(canvasObj.transform, false);
        Text bodyText = bodyObj.AddComponent<Text>();
        bodyText.font = uiFont;
        bodyText.fontSize = 26; // BIG Body Text (was 15)
        bodyText.fontStyle = FontStyle.Bold;
        bodyText.lineSpacing = 1.15f;
        bodyText.color = new Color(0.96f, 0.94f, 0.88f);
        bodyText.supportRichText = true;
        RectTransform bRt = bodyObj.GetComponent<RectTransform>();
        bRt.anchorMin = new Vector2(0.39f, 0.44f);
        bRt.anchorMax = new Vector2(0.97f, 0.86f);
        bRt.offsetMin = Vector2.zero;
        bRt.offsetMax = Vector2.zero;
        table.instructionBodyText = bodyText;

        // Objective Checklist (Right Bottom - BIG FONT)
        GameObject checkObj = new GameObject("ChecklistText");
        checkObj.transform.SetParent(canvasObj.transform, false);
        Text checkText = checkObj.AddComponent<Text>();
        checkText.font = uiFont;
        checkText.fontSize = 28; // BIG Checklist Text (was 15)
        checkText.fontStyle = FontStyle.Bold;
        checkText.lineSpacing = 1.2f;
        checkText.color = new Color(1f, 0.88f, 0.35f);
        checkText.supportRichText = true;
        RectTransform chRt = checkObj.GetComponent<RectTransform>();
        chRt.anchorMin = new Vector2(0.39f, 0.08f);
        chRt.anchorMax = new Vector2(0.97f, 0.42f);
        chRt.offsetMin = Vector2.zero;
        chRt.offsetMax = Vector2.zero;
        table.objectiveChecklistText = checkText;

        // Floating Proximity Prompt (BIG FONT)
        GameObject prompt = new GameObject("ProximityPrompt");
        prompt.transform.SetParent(tableObj.transform, false);
        prompt.transform.localPosition = new Vector3(0f, 2.55f, -0.2f);
        Canvas pCanvas = prompt.AddComponent<Canvas>();
        pCanvas.renderMode = RenderMode.WorldSpace;
        RectTransform prRt = prompt.GetComponent<RectTransform>();
        prRt.sizeDelta = new Vector2(520f, 85f);
        prRt.localScale = new Vector3(0.003f, 0.003f, 0.003f);

        GameObject pBg = new GameObject("Bg");
        pBg.transform.SetParent(prompt.transform, false);
        Image pbImg = pBg.AddComponent<Image>();
        pbImg.color = new Color(0.1f, 0.08f, 0.05f, 0.94f);
        RectTransform pbRt = pBg.GetComponent<RectTransform>();
        pbRt.anchorMin = Vector2.zero;
        pbRt.anchorMax = Vector2.one;
        pbRt.sizeDelta = Vector2.zero;

        GameObject pt = new GameObject("Text");
        pt.transform.SetParent(pBg.transform, false);
        Text pText = pt.AddComponent<Text>();
        pText.font = LoadShlopFont();
        pText.fontSize = 34; // BIG Prompt Text (was 22)
        pText.fontStyle = FontStyle.Bold;
        pText.alignment = TextAnchor.MiddleCenter;
        pText.color = new Color(1f, 0.88f, 0.35f);
        pText.text = "[E] or [H] Inspect 3D Table";
        RectTransform ptRt = pt.GetComponent<RectTransform>();
        ptRt.anchorMin = Vector2.zero;
        ptRt.anchorMax = Vector2.one;
        ptRt.sizeDelta = Vector2.zero;

        table.proximityPrompt = prompt;

        return table;
    }

    private static void Create3DKey(Transform parent, string name, Vector3 localPos, string label, Material baseMat, Material faceMat)
    {
        GameObject key = new GameObject(name);
        key.transform.SetParent(parent, false);
        key.transform.localPosition = localPos;

        // Base
        GameObject b = GameObject.CreatePrimitive(PrimitiveType.Cube);
        b.name = "Base";
        b.transform.SetParent(key.transform, false);
        b.transform.localScale = new Vector3(0.18f, 0.04f, 0.18f);
        b.GetComponent<Renderer>().sharedMaterial = baseMat;
        Object.DestroyImmediate(b.GetComponent<Collider>());

        // Face
        GameObject f = GameObject.CreatePrimitive(PrimitiveType.Cube);
        f.name = "Face";
        f.transform.SetParent(key.transform, false);
        f.transform.localPosition = new Vector3(0f, 0.024f, 0f);
        f.transform.localScale = new Vector3(0.15f, 0.02f, 0.15f);
        f.GetComponent<Renderer>().sharedMaterial = faceMat;
        Object.DestroyImmediate(f.GetComponent<Collider>());

        // 3D Canvas Text (BIG FONT)
        GameObject tObj = new GameObject("KeyText");
        tObj.transform.SetParent(f.transform, false);
        tObj.transform.localPosition = new Vector3(0f, 0.52f, 0f);
        tObj.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
        Canvas c = tObj.AddComponent<Canvas>();
        c.renderMode = RenderMode.WorldSpace;
        RectTransform rt = tObj.GetComponent<RectTransform>();
        rt.sizeDelta = new Vector2(100f, 100f);
        rt.localScale = new Vector3(0.012f, 0.012f, 0.012f);

        Text txt = tObj.AddComponent<Text>();
        txt.font = LoadUIFont();
        txt.fontSize = 44; // BIG Keycap Font (was 32)
        txt.fontStyle = FontStyle.Bold;
        txt.alignment = TextAnchor.MiddleCenter;
        txt.color = new Color(0.12f, 0.12f, 0.15f);
        txt.text = label;
    }

    private static TutorialWaypoint CreateWaypoint(Transform parent, Terrain terrain, Vector3 position, string name, Color ringColor)
    {
        Vector3 groundPos = GroundPoint(terrain, position, 0.05f);
        GameObject wpObj = new GameObject(name);
        wpObj.transform.SetParent(parent, false);
        wpObj.transform.position = groundPos;

        SphereCollider col = wpObj.AddComponent<SphereCollider>();
        col.isTrigger = true;
        col.radius = 1.8f;

        TutorialWaypoint wp = wpObj.AddComponent<TutorialWaypoint>();
        wp.checkpointName = name;

        // Ground Ring
        GameObject ring = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        ring.name = "GroundRing";
        ring.transform.SetParent(wpObj.transform, false);
        ring.transform.localScale = new Vector3(2.5f, 0.04f, 2.5f);
        Object.DestroyImmediate(ring.GetComponent<Collider>());

        Material ringMat = CreateEmissiveMaterial($"Ring_{name}", ringColor, 1.8f);
        ring.GetComponent<Renderer>().sharedMaterial = ringMat;
        wp.ringRenderer = ring.GetComponent<Renderer>();

        // Floating Beacon Crystal
        GameObject beacon = GameObject.CreatePrimitive(PrimitiveType.Cube);
        beacon.name = "FloatingBeacon";
        beacon.transform.SetParent(wpObj.transform, false);
        beacon.transform.localPosition = new Vector3(0f, 1.2f, 0f);
        beacon.transform.localRotation = Quaternion.Euler(45f, 45f, 0f);
        beacon.transform.localScale = new Vector3(0.4f, 0.4f, 0.4f);
        beacon.GetComponent<Renderer>().sharedMaterial = ringMat;
        Object.DestroyImmediate(beacon.GetComponent<Collider>());
        wp.floatingBeacon = beacon.transform;

        // Floating 3D Text Label in World Space (BIG FONT)
        GameObject labelObj = new GameObject("Waypoint3DLabel");
        labelObj.transform.SetParent(wpObj.transform, false);
        labelObj.transform.localPosition = new Vector3(0f, 2.1f, 0f);
        Canvas labelCanvas = labelObj.AddComponent<Canvas>();
        labelCanvas.renderMode = RenderMode.WorldSpace;
        RectTransform lRt = labelObj.GetComponent<RectTransform>();
        lRt.sizeDelta = new Vector2(480f, 80f);
        lRt.localScale = new Vector3(0.0035f, 0.0035f, 0.0035f);

        GameObject lTextObj = new GameObject("Text");
        lTextObj.transform.SetParent(labelObj.transform, false);
        Text lText = lTextObj.AddComponent<Text>();
        lText.font = LoadShlopFont();
        lText.fontSize = 36; // BIG 3D Waypoint Label
        lText.fontStyle = FontStyle.Bold;
        lText.alignment = TextAnchor.MiddleCenter;
        lText.color = ringColor;
        lText.text = name.ToUpper();
        RectTransform ltRt = lTextObj.GetComponent<RectTransform>();
        ltRt.anchorMin = Vector2.zero;
        ltRt.anchorMax = Vector2.one;
        ltRt.sizeDelta = Vector2.zero;

        wp.labelText = lText;

        return wp;
    }

    private static GameObject CreateCompletionUI()
    {
        Canvas canvas = Object.FindFirstObjectByType<Canvas>();
        if (canvas == null) return null;

        Font shlopFont = LoadShlopFont();
        Font uiFont = LoadUIFont();

        // 1. Root Modal Background (Fullscreen Dimmer Backdrop)
        GameObject modalRoot = new GameObject("TutorialCompletionPanel");
        modalRoot.transform.SetParent(canvas.transform, false);
        RectTransform mRt = modalRoot.AddComponent<RectTransform>();
        mRt.anchorMin = Vector2.zero;
        mRt.anchorMax = Vector2.one;
        mRt.sizeDelta = Vector2.zero;
        mRt.anchoredPosition = Vector2.zero;

        Image modalDimmer = modalRoot.AddComponent<Image>();
        modalDimmer.color = new Color(0f, 0f, 0f, 0.65f); // Focus attention on victory card

        // 2. Central Victory Card
        GameObject card = new GameObject("VictoryCard");
        card.transform.SetParent(modalRoot.transform, false);
        RectTransform cRt = card.AddComponent<RectTransform>();
        cRt.anchorMin = new Vector2(0.5f, 0.5f);
        cRt.anchorMax = new Vector2(0.5f, 0.5f);
        cRt.pivot = new Vector2(0.5f, 0.5f);
        cRt.sizeDelta = new Vector2(620f, 440f);

        // Card Outer Border (Gold/Bronze)
        Image cardBorder = card.AddComponent<Image>();
        cardBorder.color = new Color(0.72f, 0.56f, 0.22f, 1f);

        // Card Inner Body
        GameObject cardBody = new GameObject("CardBody");
        cardBody.transform.SetParent(card.transform, false);
        RectTransform cbRt = cardBody.AddComponent<RectTransform>();
        cbRt.anchorMin = Vector2.zero;
        cbRt.anchorMax = Vector2.one;
        cbRt.offsetMin = new Vector2(4f, 4f); // 4px border frame
        cbRt.offsetMax = new Vector2(-4f, -4f);
        Image cardBg = cardBody.AddComponent<Image>();
        cardBg.color = new Color(0.11f, 0.09f, 0.07f, 0.98f); // Deep rich wood/obsidian

        // 3. Header Ribbon Banner
        GameObject ribbon = new GameObject("HeaderRibbon");
        ribbon.transform.SetParent(cardBody.transform, false);
        RectTransform rRt = ribbon.AddComponent<RectTransform>();
        rRt.anchorMin = new Vector2(0f, 1f);
        rRt.anchorMax = new Vector2(1f, 1f);
        rRt.pivot = new Vector2(0.5f, 1f);
        rRt.sizeDelta = new Vector2(0f, 82f);
        rRt.anchoredPosition = Vector2.zero;
        Image ribbonBg = ribbon.AddComponent<Image>();
        ribbonBg.color = new Color(0.24f, 0.15f, 0.08f, 1f);

        // Ribbon Bottom Gold Trim
        GameObject ribbonTrim = new GameObject("RibbonTrim");
        ribbonTrim.transform.SetParent(ribbon.transform, false);
        RectTransform rtRt = ribbonTrim.AddComponent<RectTransform>();
        rtRt.anchorMin = new Vector2(0f, 0f);
        rtRt.anchorMax = new Vector2(1f, 0f);
        rtRt.pivot = new Vector2(0.5f, 0f);
        rtRt.sizeDelta = new Vector2(0f, 3f);
        rtRt.anchoredPosition = Vector2.zero;
        Image rtImg = ribbonTrim.AddComponent<Image>();
        rtImg.color = new Color(0.85f, 0.70f, 0.25f, 1f);

        // Header Title Text (SHLOP FONT!)
        GameObject titleObj = new GameObject("Title");
        titleObj.transform.SetParent(ribbon.transform, false);
        Text title = titleObj.AddComponent<Text>();
        title.font = shlopFont;
        title.fontSize = 44;
        title.fontStyle = FontStyle.Bold;
        title.alignment = TextAnchor.MiddleCenter;
        title.color = new Color(1f, 0.88f, 0.35f); // Radiant gold
        title.text = "TUTORIAL COMPLETED!";
        Shadow tShadow = titleObj.AddComponent<Shadow>();
        tShadow.effectColor = new Color(0f, 0f, 0f, 0.85f);
        tShadow.effectDistance = new Vector2(2f, -3f);
        RectTransform tRt = titleObj.GetComponent<RectTransform>();
        tRt.anchorMin = Vector2.zero;
        tRt.anchorMax = Vector2.one;
        tRt.sizeDelta = Vector2.zero;

        // 4. Subtitle / Congratulations Message
        GameObject subObj = new GameObject("Subtitle");
        subObj.transform.SetParent(cardBody.transform, false);
        Text sub = subObj.AddComponent<Text>();
        sub.font = uiFont;
        sub.fontSize = 16;
        sub.fontStyle = FontStyle.Bold;
        sub.alignment = TextAnchor.MiddleCenter;
        sub.color = new Color(0.96f, 0.92f, 0.80f);
        sub.text = "Hero, you have mastered all essential combat and defensive tactics!";
        RectTransform sRt = sub.GetComponent<RectTransform>();
        sRt.anchorMin = new Vector2(0.04f, 1f);
        sRt.anchorMax = new Vector2(0.96f, 1f);
        sRt.pivot = new Vector2(0.5f, 1f);
        sRt.sizeDelta = new Vector2(0f, 32f);
        sRt.anchoredPosition = new Vector2(0f, -90f);

        // 5. Achievement Checklist Container
        GameObject listObj = new GameObject("Achievements");
        listObj.transform.SetParent(cardBody.transform, false);
        RectTransform lRt = listObj.AddComponent<RectTransform>();
        lRt.anchorMin = new Vector2(0.06f, 1f);
        lRt.anchorMax = new Vector2(0.94f, 1f);
        lRt.pivot = new Vector2(0.5f, 1f);
        lRt.sizeDelta = new Vector2(0f, 115f);
        lRt.anchoredPosition = new Vector2(0f, -126f);

        Image listBg = listObj.AddComponent<Image>();
        listBg.color = new Color(0.06f, 0.05f, 0.04f, 0.75f); // Subtle inset box

        GameObject listTextObj = new GameObject("ListText");
        listTextObj.transform.SetParent(listObj.transform, false);
        Text listText = listTextObj.AddComponent<Text>();
        listText.font = uiFont;
        listText.fontSize = 15;
        listText.fontStyle = FontStyle.Normal;
        listText.lineSpacing = 1.35f;
        listText.color = new Color(0.92f, 0.90f, 0.85f);
        listText.alignment = TextAnchor.MiddleLeft;
        listText.supportRichText = true;
        listText.text = 
            "  <color=#55FF55><b>[✓]</b></color>  <b>Hero Maneuvers</b>: Movement, Camera Orbit & Melee Sword Attacks\n" +
            "  <color=#55FF55><b>[✓]</b></color>  <b>Botanical Economy</b>: Sun Gathering & Crop Line Defense\n" +
            "  <color=#55FF55><b>[✓]</b></color>  <b>House Protection</b>: Repelled the Zombie Vanguard!";
        RectTransform ltRt = listTextObj.GetComponent<RectTransform>();
        ltRt.anchorMin = Vector2.zero;
        ltRt.anchorMax = Vector2.one;
        ltRt.offsetMin = new Vector2(16f, 8f);
        ltRt.offsetMax = new Vector2(-16f, -8f);

        // 6. Action Buttons
        // Button 1: Start Round 1 (Emerald Victory Button)
        CreateStyledButton(cardBody, "Btn_PlayRound1", "PLAY ROUND 1 (MAP_DAY)", new Vector2(0f, -272f),
            shlopFont, 24, new Color(0.18f, 0.58f, 0.24f), new Color(0.24f, 0.72f, 0.30f), new Color(0.12f, 0.42f, 0.16f),
            new Color(0.40f, 0.85f, 0.45f), new Vector2(380f, 50f));

        // Button 2: Main Menu (Warm Leather Wood Button)
        CreateStyledButton(cardBody, "Btn_MainMenu", "RETURN TO MAIN MENU", new Vector2(0f, -336f),
            uiFont, 16, new Color(0.36f, 0.22f, 0.14f), new Color(0.46f, 0.28f, 0.18f), new Color(0.26f, 0.15f, 0.09f),
            new Color(0.58f, 0.40f, 0.26f), new Vector2(380f, 44f));

        modalRoot.SetActive(false);
        return modalRoot;
    }

    private static Button CreateStyledButton(GameObject parent, string name, string label, Vector2 pos,
        Font font, int fontSize, Color normalColor, Color highlightColor, Color pressedColor, Color borderColor, Vector2 size)
    {
        // Border frame
        GameObject frameObj = new GameObject(name);
        frameObj.transform.SetParent(parent.transform, false);
        RectTransform fRt = frameObj.AddComponent<RectTransform>();
        fRt.anchorMin = new Vector2(0.5f, 1f);
        fRt.anchorMax = new Vector2(0.5f, 1f);
        fRt.pivot = new Vector2(0.5f, 1f);
        fRt.anchoredPosition = pos;
        fRt.sizeDelta = size;

        Image borderImg = frameObj.AddComponent<Image>();
        borderImg.color = borderColor;

        // Inner button
        GameObject btnObj = new GameObject("InnerBtn");
        btnObj.transform.SetParent(frameObj.transform, false);
        RectTransform bRt = btnObj.AddComponent<RectTransform>();
        bRt.anchorMin = Vector2.zero;
        bRt.anchorMax = Vector2.one;
        bRt.offsetMin = new Vector2(2f, 2f);
        bRt.offsetMax = new Vector2(-2f, -2f);

        Image btnImg = btnObj.AddComponent<Image>();
        btnImg.color = normalColor;

        Button btn = frameObj.AddComponent<Button>();
        btn.targetGraphic = btnImg;
        ColorBlock cb = btn.colors;
        cb.normalColor = normalColor;
        cb.highlightedColor = highlightColor;
        cb.pressedColor = pressedColor;
        cb.selectedColor = highlightColor;
        btn.colors = cb;

        // Text
        GameObject txtObj = new GameObject("Label");
        txtObj.transform.SetParent(btnObj.transform, false);
        Text txt = txtObj.AddComponent<Text>();
        txt.font = font;
        txt.fontSize = fontSize;
        txt.fontStyle = FontStyle.Bold;
        txt.alignment = TextAnchor.MiddleCenter;
        txt.color = Color.white;
        txt.text = label;

        Shadow sh = txtObj.AddComponent<Shadow>();
        sh.effectColor = new Color(0f, 0f, 0f, 0.8f);
        sh.effectDistance = new Vector2(1.5f, -2f);

        RectTransform tRt = txtObj.GetComponent<RectTransform>();
        tRt.anchorMin = Vector2.zero;
        tRt.anchorMax = Vector2.one;
        tRt.sizeDelta = Vector2.zero;

        return btn;
    }

    private static void CreateManagers(Transform parent)
    {
        GameObject managers = new GameObject("Map Game Managers");
        managers.transform.SetParent(parent);

        EconomyManager economy = managers.AddComponent<EconomyManager>();
        economy.currentSun = 150;

        ObjectPoolManager pool = managers.AddComponent<ObjectPoolManager>();
        pool.peaPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Pea_Prefab.prefab");
        pool.initialPoolSize = 24;

        GridManager grid = managers.AddComponent<GridManager>();
        grid.rows = 4;
        grid.columns = 3;
    }

    private static void ConfigurePlayerSpawner(Terrain terrain, Vector3 center)
    {
        PlayerSpawner spawner = Object.FindFirstObjectByType<PlayerSpawner>();
        if (spawner == null)
        {
            GameObject spawnerObj = new GameObject("Spawner");
            spawner = spawnerObj.AddComponent<PlayerSpawner>();
        }

        Vector3 spawnPos = GroundPoint(terrain, center + new Vector3(-4.5f, 0f, -4.5f), 0.18f);
        spawner.transform.position = spawnPos;

        spawner.plantLoadout = new PlantData[]
        {
            new PlantData
            {
                name = "Peashooter",
                prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PeashooterPath),
                portrait = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/UI/PlantPortraits/PeaShooter.png"),
                cost = 100,
                cooldownTime = 2.0f
            },
            new PlantData
            {
                name = "Snow Pea",
                prefab = AssetDatabase.LoadAssetAtPath<GameObject>(SnowPeaPath),
                portrait = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/UI/PlantPortraits/PeaShooterFroze.png"),
                cost = 175,
                cooldownTime = 4.0f
            },
            new PlantData
            {
                name = "Sunflower",
                prefab = AssetDatabase.LoadAssetAtPath<GameObject>(SunflowerPath),
                portrait = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/UI/PlantPortraits/Sunflower.png"),
                cost = 50,
                cooldownTime = 3.0f
            }
        };
    }

    private static void CreateEventSystem(Transform parent)
    {
        if (Object.FindFirstObjectByType<EventSystem>() != null) return;
        GameObject eventSystem = new GameObject("EventSystem");
        eventSystem.transform.SetParent(parent);
        eventSystem.AddComponent<EventSystem>();
        eventSystem.AddComponent<InputSystemUIInputModule>();
    }

    private static void AddSceneToBuildSettings(string scenePath)
    {
        List<EditorBuildSettingsScene> scenes = new List<EditorBuildSettingsScene>(EditorBuildSettings.scenes);
        if (scenes.Exists(s => s.path == scenePath)) return;

        scenes.Add(new EditorBuildSettingsScene(scenePath, true));
        EditorBuildSettings.scenes = scenes.ToArray();
        Debug.Log($"[TutorialSceneBuilder] Added {scenePath} to Build Settings.");
    }

    private static GameObject CreateCube(Transform parent, string name, Vector3 localPos, Vector3 localScale, Material mat)
    {
        GameObject cube = GameObject.CreatePrimitive(PrimitiveType.Cube);
        cube.name = name;
        cube.transform.SetParent(parent, false);
        cube.transform.localPosition = localPos;
        cube.transform.localScale = localScale;
        cube.GetComponent<Renderer>().sharedMaterial = mat;
        Object.DestroyImmediate(cube.GetComponent<Collider>());
        return cube;
    }

    private static Vector3 GroundPoint(Terrain terrain, Vector3 position, float offset)
    {
        if (terrain == null) return new Vector3(position.x, offset, position.z);
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
        Material mat = new Material(shader) { name = name };
        if (mat.HasProperty("_BaseColor")) mat.SetColor("_BaseColor", color);
        if (mat.HasProperty("_Color")) mat.color = color;
        return mat;
    }

    private static Material CreateEmissiveMaterial(string name, Color color, float intensity)
    {
        Shader shader = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");
        Material mat = new Material(shader) { name = name };
        Color emission = color * intensity;
        if (mat.HasProperty("_BaseColor")) mat.SetColor("_BaseColor", color);
        if (mat.HasProperty("_Color")) mat.color = color;
        if (mat.HasProperty("_EmissionColor")) mat.SetColor("_EmissionColor", emission);
        mat.EnableKeyword("_EMISSION");
        return mat;
    }

    private static Font LoadShlopFont()
    {
        Font font = AssetDatabase.LoadAssetAtPath<Font>(ShlopFontPath);
        if (font == null) font = LoadUIFont();
        return font;
    }

    private static Font LoadUIFont()
    {
        Font font = AssetDatabase.LoadAssetAtPath<Font>(RobotoBoldPath);
        if (font == null) font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        if (font == null) font = Resources.GetBuiltinResource<Font>("Arial.ttf");
        return font;
    }
}
