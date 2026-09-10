#if UNITY_EDITOR
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// Editor utility to construct the MapIntroFlythrough prefab and inject it into:
/// - Map_Tutorial.unity
/// - Map_Day.unity
/// - Map_Cloudy.unity
/// - Map_Night.unity
/// Accessible via menu: Zombie House -> Setup Intro Flythrough (All Maps)
/// </summary>
public static class MapIntroFlythroughSetup
{
    private const string PrefabDirectory = "Assets/Prefabs/Camera";
    private const string PrefabPath = "Assets/Prefabs/Camera/MapIntroFlythrough.prefab";
    private const string ShlopFontPath = "Assets/Fonts/shlop/shlop rg.otf";

    private static readonly (string scenePath, string mapName, string threatSub, string objSub)[] ScenesConfig = new[]
    {
        (
            "Assets/Scenes/GameScenes/Map_Tutorial.unity",
            "TUTORIAL: BASIC TRAINING",
            "SCOUTING APPROACH: ZOMBIES WILL ATTACK FROM HERE AFTER PREPARATIONS",
            "PROTECT BAKER'S HOUSE & COMPLETE CHECKPOINTS"
        ),
        (
            "Assets/Scenes/GameScenes/Map_Day.unity",
            "ROUND 1: SUNNY DAY",
            "SCOUTING ENEMY APPROACH: EAST ROAD",
            "DEFEND BAKER'S HOUSE & PREPARE LAWN DEFENSES"
        ),
        (
            "Assets/Scenes/GameScenes/Map_Cloudy.unity",
            "ROUND 2: CLOUDY FOG",
            "HEAVY FOG DETECTED: MULTIPLE THREAT ROUTES",
            "FORTIFY THE GARDEN BEFORE THE HORDE ENGAGES"
        ),
        (
            "Assets/Scenes/GameScenes/Map_Night.unity",
            "ROUND 3: BLOOD NIGHT",
            "MIDNIGHT SIEGE: HIGH LEVEL THREAT DETECTED",
            "LAST STAND: PROTECT THE HOUSE UNTIL DAWN"
        )
    };

    [MenuItem("Zombie House/Setup Intro Flythrough (All Maps)")]
    public static void BuildAndInjectAll()
    {
        Debug.Log("[MapIntroFlythroughSetup] Starting setup for all maps...");

        // 1. Build and save the template prefab
        GameObject prefab = CreateOrUpdatePrefab();
        if (prefab == null)
        {
            Debug.LogError("[MapIntroFlythroughSetup] Failed to create prefab!");
            return;
        }

        string originalScene = SceneManager.GetActiveScene().path;

        // 2. Inject into each scene
        foreach (var cfg in ScenesConfig)
        {
            if (!File.Exists(cfg.scenePath))
            {
                Debug.LogWarning($"[MapIntroFlythroughSetup] Scene missing: {cfg.scenePath}");
                continue;
            }

            InjectIntoScene(cfg.scenePath, prefab, cfg.mapName, cfg.threatSub, cfg.objSub);
        }

        AssetDatabase.Refresh();

        // Restore original scene
        if (!string.IsNullOrEmpty(originalScene) && File.Exists(originalScene))
        {
            EditorSceneManager.OpenScene(originalScene, OpenSceneMode.Single);
        }

        Debug.Log("[MapIntroFlythroughSetup] All 4 map scenes configured with MapIntroFlythrough successfully!");
    }

    public static GameObject CreateOrUpdatePrefab()
    {
        if (!Directory.Exists(PrefabDirectory))
        {
            Directory.CreateDirectory(PrefabDirectory);
        }

        Font shlopFont = AssetDatabase.LoadAssetAtPath<Font>(ShlopFontPath);

        // Root GameObject
        GameObject root = new GameObject("MapIntroFlythrough");
        MapIntroFlythrough comp = root.AddComponent<MapIntroFlythrough>();

        // ═══════════════════════════════════════════════════════════════════════
        // 8-Shot Cinematic: Elevated Clockwise Orbital (House-Safe & Nausea-Free)
        //
        // KEY DESIGN PRINCIPLES:
        // 1. Overview looks North from the South (yaw = 0°).
        // 2. Rooftop descends to Y=22 (4.3m above 17.7m roof peak), turning to
        //    East (yaw = 90°). This is a perpendicular 90° turn (NO 180° whip!).
        // 3. From East, the camera orbits CLOCKWISE: East -> South -> West -> North.
        //    Every turn is a gentle +90° right pan in the exact same direction.
        // 4. ALL threat waypoints are at radius=20 and Y=16. The Catmull-Rom spline
        //    midpoints are at distance 17.7m (house corner is only 13.7m).
        //    Horizontally 4m clear of house corners, vertically 7m above eaves!
        // 5. Shot 7 (Objective) continues clockwise to Southeast (18, 15, -8)
        //    framing Baker's House and lawn defenses without crossing the house.
        // 6. Shot 8 (Hero Ready) eases down behind the south-facing player.
        // 7. Base transition times are increased to 2.4s-2.8s for smooth cinematic
        //    motion, fully configurable via durationMultiplier.
        // ═══════════════════════════════════════════════════════════════════════

        // Shot 1: High Overview - aerial establishing shot from the South
        GameObject wp1 = new GameObject("Waypoint_1_Overview");
        wp1.transform.SetParent(root.transform, false);
        wp1.transform.position = new Vector3(0f, 26f, -36f);
        wp1.transform.rotation = Quaternion.Euler(35f, 0f, 0f);

        // Shot 2: Rooftop Descent - descends above roof peak, faces perpendicular (East)
        GameObject wp2 = new GameObject("Waypoint_2_Rooftop");
        wp2.transform.SetParent(root.transform, false);
        wp2.transform.position = new Vector3(0f, 22f, 0f);
        wp2.transform.rotation = Quaternion.Euler(20f, 90f, 0f);

        // Shot 3: Threat Route 1 - East Highway (looking East down the road)
        GameObject wp3 = new GameObject("Waypoint_3_Threat_East");
        wp3.transform.SetParent(root.transform, false);
        wp3.transform.position = new Vector3(20f, 16f, 0f);
        wp3.transform.rotation = Quaternion.Euler(15f, 90f, 0f);

        // Shot 4: Threat Route 2 - South Road (orbited clockwise: E->S)
        GameObject wp4 = new GameObject("Waypoint_4_Threat_South");
        wp4.transform.SetParent(root.transform, false);
        wp4.transform.position = new Vector3(0f, 16f, -20f);
        wp4.transform.rotation = Quaternion.Euler(15f, 180f, 0f);

        // Shot 5: Threat Route 3 - West Forest (orbited clockwise: S->W)
        GameObject wp5 = new GameObject("Waypoint_5_Threat_West");
        wp5.transform.SetParent(root.transform, false);
        wp5.transform.position = new Vector3(-20f, 16f, 0f);
        wp5.transform.rotation = Quaternion.Euler(15f, 270f, 0f);

        // Shot 6: Threat Route 4 - North Gate (orbited clockwise: W->N)
        GameObject wp6 = new GameObject("Waypoint_6_Threat_North");
        wp6.transform.SetParent(root.transform, false);
        wp6.transform.position = new Vector3(0f, 16f, 20f);
        wp6.transform.rotation = Quaternion.Euler(15f, 0f, 0f);

        // Shot 7: Objective - continues clockwise to Southeast, framing house & lawn defenses
        GameObject wp7 = new GameObject("Waypoint_7_Objective");
        wp7.transform.SetParent(root.transform, false);
        wp7.transform.position = new Vector3(18f, 15f, -8f);
        wp7.transform.rotation = Quaternion.Euler(20f, 210f, 0f);

        // Shot 8: Hero Reveal - settles behind south-facing player at (0, 0.1, -18)
        GameObject wp8 = new GameObject("Waypoint_8_Hero");
        wp8.transform.SetParent(root.transform, false);
        wp8.transform.position = new Vector3(0f, 3.2f, -12.5f);
        wp8.transform.rotation = Quaternion.Euler(18f, 180f, 0f);

        // Populate waypoints list
        comp.waypoints = new List<MapIntroFlythrough.WaypointData>
        {
            new MapIntroFlythrough.WaypointData
            {
                shotName = "Overview",
                point = wp1.transform,
                title = "VALLEY OVERVIEW",
                subtitle = "BAKER'S HOMESTEAD & DEFENSE PERIMETER",
                duration = 2.8f
            },
            new MapIntroFlythrough.WaypointData
            {
                shotName = "Rooftop Survey",
                point = wp2.transform,
                title = "SCOUTING PERIMETER",
                subtitle = "SURVEYING ALL APPROACH VECTORS",
                duration = 2.4f
            },
            new MapIntroFlythrough.WaypointData
            {
                shotName = "Threat: East Highway",
                point = wp3.transform,
                title = "THREAT ROUTE 1: EAST HIGHWAY",
                subtitle = "ENEMY APPROACH VECTOR: EAST GATE",
                duration = 2.6f,
                straightTransition = true
            },
            new MapIntroFlythrough.WaypointData
            {
                shotName = "Threat: South Road",
                point = wp4.transform,
                title = "THREAT ROUTE 2: SOUTH ROAD",
                subtitle = "ENEMY APPROACH VECTOR: SOUTH GATE",
                duration = 2.6f
            },
            new MapIntroFlythrough.WaypointData
            {
                shotName = "Threat: West Forest",
                point = wp5.transform,
                title = "THREAT ROUTE 3: WEST CORRIDOR",
                subtitle = "ENEMY APPROACH VECTOR: WEST GATE",
                duration = 2.6f
            },
            new MapIntroFlythrough.WaypointData
            {
                shotName = "Threat: North Gate",
                point = wp6.transform,
                title = "THREAT ROUTE 4: NORTH GATE",
                subtitle = "ENEMY APPROACH VECTOR: NORTH ROAD",
                duration = 2.6f
            },
            new MapIntroFlythrough.WaypointData
            {
                shotName = "Objective",
                point = wp7.transform,
                title = "PRIMARY OBJECTIVE",
                subtitle = "DEFEND BAKER'S HOUSE & SECURE THE LAWN",
                duration = 2.8f
            },
            new MapIntroFlythrough.WaypointData
            {
                shotName = "Hero Ready",
                point = wp8.transform,
                title = "HERO READY",
                subtitle = "TAKE COMMAND & PREPARE DEFENSES",
                duration = 2.4f
            }
        };

        // ── Cinematic Letterbox UI Canvas ─────────────────────────────────────
        GameObject canvasObj = new GameObject("CinematicUI", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(CanvasGroup));
        canvasObj.transform.SetParent(root.transform, false);

        Canvas canvas = canvasObj.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 999;

        CanvasScaler scaler = canvasObj.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1280f, 720f);
        scaler.matchWidthOrHeight = 0.5f;

        CanvasGroup cg = canvasObj.GetComponent<CanvasGroup>();
        cg.alpha = 0f;
        comp.letterboxCanvasGroup = cg;

        // Top Letterbox Bar
        GameObject topBar = new GameObject("TopBar", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        topBar.transform.SetParent(canvasObj.transform, false);
        RectTransform topRect = topBar.GetComponent<RectTransform>();
        topRect.anchorMin = new Vector2(0f, 1f);
        topRect.anchorMax = new Vector2(1f, 1f);
        topRect.pivot = new Vector2(0.5f, 1f);
        topRect.sizeDelta = new Vector2(0f, 75f);
        topBar.GetComponent<Image>().color = new Color(0.04f, 0.04f, 0.06f, 0.95f);

        // Bottom Letterbox Bar
        GameObject botBar = new GameObject("BottomBar", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        botBar.transform.SetParent(canvasObj.transform, false);
        RectTransform botRect = botBar.GetComponent<RectTransform>();
        botRect.anchorMin = new Vector2(0f, 0f);
        botRect.anchorMax = new Vector2(1f, 0f);
        botRect.pivot = new Vector2(0.5f, 0f);
        botRect.sizeDelta = new Vector2(0f, 75f);
        botBar.GetComponent<Image>().color = new Color(0.04f, 0.04f, 0.06f, 0.95f);

        // Center Title & Subtitle Banner
        GameObject bannerObj = new GameObject("Banner", typeof(RectTransform));
        bannerObj.transform.SetParent(canvasObj.transform, false);
        RectTransform bRect = bannerObj.GetComponent<RectTransform>();
        bRect.anchorMin = new Vector2(0.5f, 1f);
        bRect.anchorMax = new Vector2(0.5f, 1f);
        bRect.pivot = new Vector2(0.5f, 1f);
        bRect.anchoredPosition = new Vector2(0f, -85f);
        bRect.sizeDelta = new Vector2(900f, 90f);

        // Shot Title Text
        GameObject titleObj = new GameObject("ShotTitle", typeof(RectTransform), typeof(CanvasRenderer), typeof(Text));
        titleObj.transform.SetParent(bannerObj.transform, false);
        RectTransform tRect = titleObj.GetComponent<RectTransform>();
        tRect.anchorMin = new Vector2(0.5f, 1f);
        tRect.anchorMax = new Vector2(0.5f, 1f);
        tRect.pivot = new Vector2(0.5f, 1f);
        tRect.anchoredPosition = new Vector2(0f, 0f);
        tRect.sizeDelta = new Vector2(880f, 48f);

        Text titleText = titleObj.GetComponent<Text>();
        if (shlopFont != null) titleText.font = shlopFont;
        titleText.fontSize = 44;
        titleText.fontStyle = FontStyle.Bold;
        titleText.alignment = TextAnchor.MiddleCenter;
        titleText.color = new Color(1f, 0.88f, 0.2f, 1f);
        comp.shotTitleText = titleText;

        // Shot Subtitle Text
        GameObject subObj = new GameObject("ShotSubtitle", typeof(RectTransform), typeof(CanvasRenderer), typeof(Text));
        subObj.transform.SetParent(bannerObj.transform, false);
        RectTransform sRect = subObj.GetComponent<RectTransform>();
        sRect.anchorMin = new Vector2(0.5f, 0f);
        sRect.anchorMax = new Vector2(0.5f, 0f);
        sRect.pivot = new Vector2(0.5f, 0f);
        sRect.anchoredPosition = new Vector2(0f, 0f);
        sRect.sizeDelta = new Vector2(880f, 32f);

        Text subText = subObj.GetComponent<Text>();
        subText.fontSize = 17;
        subText.fontStyle = FontStyle.Bold;
        subText.alignment = TextAnchor.MiddleCenter;
        subText.color = new Color(0.9f, 0.9f, 0.95f, 0.95f);
        comp.shotSubtitleText = subText;

        // Skip Prompt (inside bottom bar)
        GameObject skipObj = new GameObject("SkipPrompt", typeof(RectTransform), typeof(CanvasRenderer), typeof(Text));
        skipObj.transform.SetParent(botBar.transform, false);
        RectTransform skipRect = skipObj.GetComponent<RectTransform>();
        skipRect.anchorMin = new Vector2(1f, 0.5f);
        skipRect.anchorMax = new Vector2(1f, 0.5f);
        skipRect.pivot = new Vector2(1f, 0.5f);
        skipRect.anchoredPosition = new Vector2(-25f, 0f);
        skipRect.sizeDelta = new Vector2(350f, 35f);

        Text skipText = skipObj.GetComponent<Text>();
        skipText.text = "PRESS [SPACE] TO SKIP";
        skipText.fontSize = 15;
        skipText.fontStyle = FontStyle.Bold;
        skipText.alignment = TextAnchor.MiddleRight;
        skipText.color = new Color(0.7f, 0.7f, 0.7f, 0.85f);
        comp.skipPromptText = skipText;

        // Save as Prefab
        GameObject savedPrefab = PrefabUtility.SaveAsPrefabAsset(root, PrefabPath);
        Object.DestroyImmediate(root);

        Debug.Log($"[MapIntroFlythroughSetup] Created reusable prefab: {PrefabPath}");
        return savedPrefab;
    }

    private static void InjectIntoScene(string scenePath, GameObject prefab, string mapTitle, string threatSubtitle, string objSubtitle)
    {
        Debug.Log($"[MapIntroFlythroughSetup] Injecting flythrough into: {scenePath}...");
        Scene scene = EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single);

        // Remove old flythrough if present
        MapIntroFlythrough existing = Object.FindFirstObjectByType<MapIntroFlythrough>();
        if (existing != null)
        {
            Object.DestroyImmediate(existing.gameObject);
        }

        // Instantiate prefab instance
        GameObject instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
        instance.name = "MapIntroFlythrough";

        MapIntroFlythrough flyComp = instance.GetComponent<MapIntroFlythrough>();
        if (flyComp != null && flyComp.waypoints.Count >= 8)
        {
            // Tailor shot titles for this map
            // [0]=Overview, [1]=Rooftop, [2]=East, [3]=South, [4]=West, [5]=North, [6]=Objective, [7]=Hero
            flyComp.waypoints[0].title = mapTitle;
            flyComp.waypoints[0].subtitle = "BAKER'S HOMESTEAD & SURROUNDING ROADS";

            if (scenePath.Contains("Tutorial"))
            {
                flyComp.waypoints[1].subtitle = "TUTORIAL: SURVEYING COMPOUND FROM ROOFTOP";
                flyComp.waypoints[2].subtitle = "TUTORIAL: EAST HIGHWAY (APPROACH CURRENTLY CLEAR)";
                flyComp.waypoints[3].subtitle = "TUTORIAL: SOUTH ROAD (APPROACH CURRENTLY CLEAR)";
                flyComp.waypoints[4].subtitle = "TUTORIAL: WEST FOREST (APPROACH CURRENTLY CLEAR)";
                flyComp.waypoints[5].subtitle = "TUTORIAL: NORTH GATE (APPROACH CURRENTLY CLEAR)";
                flyComp.waypoints[6].subtitle = "COMPLETE TRAINING CHECKPOINTS BEFORE HORDE ARRIVES";
                flyComp.waypoints[7].subtitle = "PROCEED TO CHECKPOINT 1 TO BEGIN TRAINING";
            }
            else
            {
                flyComp.waypoints[1].subtitle = "SURVEYING ALL APPROACH VECTORS";
                flyComp.waypoints[2].subtitle = threatSubtitle;
                flyComp.waypoints[3].subtitle = "SECTOR 2: SOUTH INVASION ROUTE";
                flyComp.waypoints[4].subtitle = "SECTOR 3: WEST INVASION ROUTE";
                flyComp.waypoints[5].subtitle = "SECTOR 4: NORTH INVASION ROUTE";
                flyComp.waypoints[6].subtitle = objSubtitle;
                flyComp.waypoints[7].subtitle = "STAND YOUR GROUND & DEFEND THE LAWN";
            }
        }

        // Save scene
        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        Debug.Log($"[MapIntroFlythroughSetup] Saved {scenePath} with MapIntroFlythrough.");
    }
}
#endif
