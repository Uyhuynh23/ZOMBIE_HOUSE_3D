using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Editor utility to generate the complete 3D In-Game Instruction Board:
/// - Generates physical 3D timber posts, backboard, shingled roof canopy.
/// - Generates 3D lantern fixture with warm Point Light.
/// - Generates rotating 3D Sun crystal topper.
/// - Generates high-res World-Space Canvas with 3 distinct tactical sections:
///   1) Goals & Mission Directives
///   2) Complete Hero Controls & Keybindings
///   3) Defensive Arsenal & Survival Tips
/// - Adds physical 3D tactile keycaps protruding from the board.
/// - Configures MapInstruction3D controller with smooth 3D camera inspection.
/// - Exports clean prefab to Assets/Prefabs/InstructionBoard3D.prefab.
/// </summary>
public static class InstructionBoardBuilder
{
    private const string PrefabFolder = "Assets/Prefabs";
    private const string PrefabPath = "Assets/Prefabs/InstructionBoard3D.prefab";
    private const string MaterialsFolder = "Assets/Materials";
    private const string RobotoBoldPath = "Assets/TextMesh Pro/Examples & Extras/Fonts/Roboto-Bold.ttf";
    private const string ShlopFontPath = "Assets/Fonts/shlop/shlop rg.otf";

    [MenuItem("Zombie House/Create 3D Instruction Board Prefab")]
    public static GameObject BuildPrefab()
    {
        EnsureFolders();

        // 1. Create or load materials
        Material woodDarkMat = GetOrCreateMaterial("BoardWoodDark", new Color(0.24f, 0.14f, 0.08f), 0.1f);
        Material woodParchmentMat = GetOrCreateMaterial("BoardWoodParchment", new Color(0.85f, 0.78f, 0.68f), 0.05f);
        Material roofShingleMat = GetOrCreateMaterial("BoardRoofShingle", new Color(0.18f, 0.10f, 0.06f), 0.1f);
        Material keyBaseMat = GetOrCreateMaterial("BoardKeycapBase", new Color(0.12f, 0.12f, 0.15f), 0.3f);
        Material keyFaceMat = GetOrCreateMaterial("BoardKeycapFace", new Color(0.92f, 0.90f, 0.85f), 0.2f);
        Material keyAccentMat = GetOrCreateMaterial("BoardKeycapAccent", new Color(0.95f, 0.65f, 0.15f), 0.2f);
        Material lanternMat = GetOrCreateEmissiveMaterial("BoardLanternGold", new Color(1f, 0.75f, 0.25f), 2.5f);

        // 2. Root GameObject
        GameObject root = new GameObject("InstructionBoard3D");
        MapInstruction3D controller = root.AddComponent<MapInstruction3D>();

        // 3. Physical 3D Wooden Structure
        GameObject frameRoot = new GameObject("PhysicalFrame");
        frameRoot.transform.SetParent(root.transform, false);

        // Vertical Support Posts (Left & Right)
        CreateCube(frameRoot.transform, "Post_Left", new Vector3(-1.85f, 1.25f, 0f), new Vector3(0.18f, 2.5f, 0.18f), woodDarkMat);
        CreateCube(frameRoot.transform, "Post_Right", new Vector3(1.85f, 1.25f, 0f), new Vector3(0.18f, 2.5f, 0.18f), woodDarkMat);

        // Horizontal Timber Beams (Bottom, Middle, Top)
        CreateCube(frameRoot.transform, "Beam_Bottom", new Vector3(0f, 0.32f, 0f), new Vector3(3.8f, 0.16f, 0.16f), woodDarkMat);
        CreateCube(frameRoot.transform, "Beam_Top", new Vector3(0f, 2.32f, 0f), new Vector3(3.8f, 0.16f, 0.16f), woodDarkMat);

        // Board Backing Planks (Sturdy Wood Panel)
        CreateCube(frameRoot.transform, "BackingPlank", new Vector3(0f, 1.32f, 0.04f), new Vector3(3.55f, 1.88f, 0.08f), woodDarkMat);
        
        // Parchment Display Board (Front Surface)
        CreateCube(frameRoot.transform, "ParchmentSurface", new Vector3(0f, 1.32f, -0.01f), new Vector3(3.45f, 1.78f, 0.02f), woodParchmentMat);

        // Side Support Brackets (Diagonal 45-deg struts)
        GameObject bracketLeft = CreateCube(frameRoot.transform, "Bracket_Left", new Vector3(-1.65f, 2.15f, 0f), new Vector3(0.1f, 0.45f, 0.12f), woodDarkMat);
        bracketLeft.transform.localRotation = Quaternion.Euler(0f, 0f, 45f);
        GameObject bracketRight = CreateCube(frameRoot.transform, "Bracket_Right", new Vector3(1.65f, 2.15f, 0f), new Vector3(0.1f, 0.45f, 0.12f), woodDarkMat);
        bracketRight.transform.localRotation = Quaternion.Euler(0f, 0f, -45f);

        // Ground Foundation Stones
        CreateCube(frameRoot.transform, "StoneFooting_Left", new Vector3(-1.85f, 0.08f, 0f), new Vector3(0.35f, 0.16f, 0.35f), woodDarkMat);
        CreateCube(frameRoot.transform, "StoneFooting_Right", new Vector3(1.85f, 0.08f, 0f), new Vector3(0.35f, 0.16f, 0.35f), woodDarkMat);

        // 4. Shingled Roof Canopy (Overhang)
        GameObject roofRoot = new GameObject("RoofCanopy");
        roofRoot.transform.SetParent(frameRoot.transform, false);

        GameObject frontEave = CreateCube(roofRoot.transform, "Roof_FrontEave", new Vector3(0f, 2.48f, -0.2f), new Vector3(4.0f, 0.08f, 0.65f), roofShingleMat);
        frontEave.transform.localRotation = Quaternion.Euler(24f, 0f, 0f);

        GameObject backEave = CreateCube(roofRoot.transform, "Roof_BackEave", new Vector3(0f, 2.52f, 0.15f), new Vector3(4.0f, 0.08f, 0.45f), roofShingleMat);
        backEave.transform.localRotation = Quaternion.Euler(-24f, 0f, 0f);

        CreateCube(roofRoot.transform, "Roof_RidgeCap", new Vector3(0f, 2.62f, -0.02f), new Vector3(4.05f, 0.09f, 0.15f), woodDarkMat);

        // 5. Hanging 3D Lantern Fixture (Illumination for Night/Cloudy Maps)
        GameObject lanternRoot = new GameObject("LanternFixture");
        lanternRoot.transform.SetParent(root.transform, false);
        lanternRoot.transform.localPosition = new Vector3(1.95f, 2.2f, -0.28f);

        // Bracket arm
        CreateCube(lanternRoot.transform, "ArmHorizontal", new Vector3(-0.1f, 0.05f, 0.15f), new Vector3(0.06f, 0.06f, 0.4f), woodDarkMat);
        CreateCube(lanternRoot.transform, "ArmPost", new Vector3(-0.1f, -0.1f, 0.32f), new Vector3(0.06f, 0.3f, 0.06f), woodDarkMat);

        // Lantern body
        GameObject lanternBody = CreateCube(lanternRoot.transform, "LanternBody", new Vector3(-0.1f, -0.25f, 0f), new Vector3(0.18f, 0.28f, 0.18f), lanternMat);
        CreateCube(lanternRoot.transform, "LanternCap", new Vector3(-0.1f, -0.08f, 0f), new Vector3(0.24f, 0.06f, 0.24f), woodDarkMat);
        CreateCube(lanternRoot.transform, "LanternBase", new Vector3(-0.1f, -0.41f, 0f), new Vector3(0.22f, 0.05f, 0.22f), woodDarkMat);

        // Realtime Warm Light
        GameObject lightObj = new GameObject("LanternPointLight");
        lightObj.transform.SetParent(lanternBody.transform, false);
        lightObj.transform.localPosition = Vector3.zero;
        Light lightComp = lightObj.AddComponent<Light>();
        lightComp.type = LightType.Point;
        lightComp.color = new Color(1f, 0.82f, 0.55f);
        lightComp.range = 6.5f;
        lightComp.intensity = 1.8f;
        lightComp.shadows = LightShadows.Soft;

        // 6. Miniature Rotating 3D Sun Crystal Topper
        GameObject sunTopper = new GameObject("RotatingSunTopper");
        sunTopper.transform.SetParent(root.transform, false);
        sunTopper.transform.localPosition = new Vector3(0f, 2.82f, -0.02f);

        GameObject sunCore = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        sunCore.name = "SunCore";
        sunCore.transform.SetParent(sunTopper.transform, false);
        sunCore.transform.localScale = new Vector3(0.35f, 0.35f, 0.35f);
        sunCore.GetComponent<Renderer>().sharedMaterial = lanternMat;
        Object.DestroyImmediate(sunCore.GetComponent<Collider>());

        // Sun Rays
        for (int r = 0; r < 4; r++)
        {
            GameObject ray = GameObject.CreatePrimitive(PrimitiveType.Cube);
            ray.name = $"Ray_{r}";
            ray.transform.SetParent(sunTopper.transform, false);
            ray.transform.localRotation = Quaternion.Euler(0f, 0f, r * 45f);
            ray.transform.localScale = new Vector3(0.52f, 0.08f, 0.08f);
            ray.GetComponent<Renderer>().sharedMaterial = lanternMat;
            Object.DestroyImmediate(ray.GetComponent<Collider>());
        }

        controller.rotatingSunTopper = sunTopper.transform;

        // 7. Inspect Camera Anchor (Positioned closer for large, clear text)
        GameObject inspectAnchor = new GameObject("InspectCameraAnchor");
        inspectAnchor.transform.SetParent(root.transform, false);
        inspectAnchor.transform.localPosition = new Vector3(0f, 1.40f, -1.85f);
        inspectAnchor.transform.localRotation = Quaternion.Euler(5.5f, 0f, 0f);
        controller.inspectCameraAnchor = inspectAnchor.transform;

        // 8. In-World High-Resolution 3D Canvas
        Font uiFont = LoadUIFont();
        GameObject canvasObj = new GameObject("Canvas_3D_Content");
        canvasObj.transform.SetParent(root.transform, false);
        canvasObj.transform.localPosition = new Vector3(0f, 1.32f, -0.025f);
        canvasObj.transform.localRotation = Quaternion.identity;

        Canvas canvas = canvasObj.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.WorldSpace;
        
        RectTransform canvasRt = canvasObj.GetComponent<RectTransform>();
        canvasRt.sizeDelta = new Vector2(1160f, 620f);
        canvasRt.localScale = new Vector3(0.0029f, 0.0029f, 0.0029f); // exactly fits parchment face

        canvasObj.AddComponent<GraphicRaycaster>();

        // Build the 3 Panels on the Canvas
        BuildCanvasContent(canvasObj, uiFont);

        // 9. Floating Proximity Prompt "[E] or [H] to Inspect 3D Instructions" (BIG FONT)
        GameObject promptObj = new GameObject("ProximityPrompt");
        promptObj.transform.SetParent(root.transform, false);
        promptObj.transform.localPosition = new Vector3(0f, 2.75f, -0.6f);

        Canvas promptCanvas = promptObj.AddComponent<Canvas>();
        promptCanvas.renderMode = RenderMode.WorldSpace;
        RectTransform promptRt = promptObj.GetComponent<RectTransform>();
        promptRt.sizeDelta = new Vector2(500f, 80f);
        promptRt.localScale = new Vector3(0.003f, 0.003f, 0.003f);

        // Background pill
        GameObject pill = new GameObject("PromptBg");
        pill.transform.SetParent(promptObj.transform, false);
        Image pillImg = pill.AddComponent<Image>();
        pillImg.color = new Color(0.1f, 0.08f, 0.05f, 0.94f);
        RectTransform pillRt = pill.GetComponent<RectTransform>();
        pillRt.anchorMin = Vector2.zero;
        pillRt.anchorMax = Vector1();
        pillRt.sizeDelta = Vector2.zero;

        // Text
        GameObject promptTextObj = new GameObject("PromptText");
        promptTextObj.transform.SetParent(pill.transform, false);
        Text pText = promptTextObj.AddComponent<Text>();
        pText.font = LoadShlopFont();
        pText.fontSize = 34; // BIG Prompt Text (was 24)
        pText.fontStyle = FontStyle.Bold;
        pText.alignment = TextAnchor.MiddleCenter;
        pText.color = new Color(1f, 0.88f, 0.35f);
        pText.text = "[E] or [H] Inspect 3D Guide";
        RectTransform ptRt = promptTextObj.GetComponent<RectTransform>();
        ptRt.anchorMin = Vector2.zero;
        ptRt.anchorMax = Vector1();
        ptRt.sizeDelta = Vector2.zero;

        controller.proximityPrompt = promptObj;

        // 10. Save Prefab
        if (!System.IO.Directory.Exists(PrefabFolder))
        {
            System.IO.Directory.CreateDirectory(PrefabFolder);
        }

        GameObject prefab = PrefabUtility.SaveAsPrefabAsset(root, PrefabPath);
        Debug.Log($"[InstructionBoardBuilder] 🎉 Saved 3D Instruction Board Prefab at: {PrefabPath}");

        Object.DestroyImmediate(root);
        return prefab;
    }

    [MenuItem("Zombie House/Spawn 3D Instruction Board in Current Scene")]
    public static void SpawnInCurrentScene()
    {
        GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath);
        if (prefab == null)
        {
            prefab = BuildPrefab();
        }

        GameObject existing = GameObject.Find("InstructionBoard3D");
        if (existing != null)
        {
            Object.DestroyImmediate(existing);
        }

        GameObject instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
        instance.name = "InstructionBoard3D";

        // Position near center / player spawn
        Terrain terrain = Object.FindFirstObjectByType<Terrain>();
        Vector3 spawnPos = new Vector3(-3.2f, 0f, -2.8f);
        if (terrain != null)
        {
            spawnPos.y = terrain.SampleHeight(spawnPos) + terrain.transform.position.y + 0.02f;
        }
        instance.transform.position = spawnPos;
        instance.transform.rotation = Quaternion.Euler(0f, 45f, 0f);

        Selection.activeGameObject = instance;
        Debug.Log("[InstructionBoardBuilder] Spawned InstructionBoard3D in active scene!");
    }

    private static void BuildCanvasContent(GameObject canvasObj, Font uiFont)
    {
        // Top Header Banner
        GameObject header = new GameObject("HeaderBanner");
        header.transform.SetParent(canvasObj.transform, false);
        RectTransform headerRt = header.AddComponent<RectTransform>();
        headerRt.anchorMin = new Vector2(0f, 1f);
        headerRt.anchorMax = new Vector2(1f, 1f);
        headerRt.pivot = new Vector2(0.5f, 1f);
        headerRt.anchoredPosition = new Vector2(0f, -10f);
        headerRt.sizeDelta = new Vector2(-20f, 75f);

        Image headerBg = header.AddComponent<Image>();
        headerBg.color = new Color(0.22f, 0.12f, 0.06f, 0.95f);

        GameObject titleObj = new GameObject("TitleText");
        titleObj.transform.SetParent(header.transform, false);
        Text titleText = titleObj.AddComponent<Text>();
        titleText.font = LoadShlopFont();
        titleText.fontSize = 36; // BIG Title (was 30)
        titleText.fontStyle = FontStyle.Bold;
        titleText.alignment = TextAnchor.MiddleCenter;
        titleText.color = new Color(1f, 0.88f, 0.35f);
        titleText.text = "DEFEND THE BAKER'S HOUSE - COMMAND BRIEFING";
        RectTransform ttRt = titleObj.GetComponent<RectTransform>();
        ttRt.anchorMin = Vector2.zero;
        ttRt.anchorMax = Vector1();
        ttRt.sizeDelta = Vector2.zero;

        // Bottom Footer Bar
        GameObject footer = new GameObject("FooterBar");
        footer.transform.SetParent(canvasObj.transform, false);
        RectTransform footerRt = footer.AddComponent<RectTransform>();
        footerRt.anchorMin = new Vector2(0f, 0f);
        footerRt.anchorMax = new Vector2(1f, 0f);
        footerRt.pivot = new Vector2(0.5f, 0f);
        footerRt.anchoredPosition = new Vector2(0f, 10f);
        footerRt.sizeDelta = new Vector2(-20f, 48f);

        Image footerBg = footer.AddComponent<Image>();
        footerBg.color = new Color(0.18f, 0.10f, 0.05f, 0.95f);

        GameObject footerTextObj = new GameObject("FooterText");
        footerTextObj.transform.SetParent(footer.transform, false);
        Text fText = footerTextObj.AddComponent<Text>();
        fText.font = uiFont;
        fText.fontSize = 22; // BIG Footer (was 18)
        fText.fontStyle = FontStyle.Bold;
        fText.alignment = TextAnchor.MiddleCenter;
        fText.color = new Color(0.92f, 0.88f, 0.82f);
        fText.text = "[H] Toggle 3D Guide  |  [WASD] Move  |  [LMB / SPACE] Attack  |  [1-3] Choose  |  [E] Plant / Dig";
        RectTransform ftRt = footerTextObj.GetComponent<RectTransform>();
        ftRt.anchorMin = Vector2.zero;
        ftRt.anchorMax = Vector1();
        ftRt.sizeDelta = Vector2.zero;

        // 3-Column Container
        GameObject columnsRoot = new GameObject("ColumnsContainer");
        columnsRoot.transform.SetParent(canvasObj.transform, false);
        RectTransform crRt = columnsRoot.AddComponent<RectTransform>();
        crRt.anchorMin = new Vector2(0f, 0f);
        crRt.anchorMax = new Vector2(1f, 1f);
        crRt.anchoredPosition = new Vector2(0f, 2f);
        crRt.sizeDelta = new Vector2(-20f, -145f);

        // COLUMN 1: GOALS & MISSIONS (Left)
        BuildGoalsColumn(columnsRoot, uiFont);

        // COLUMN 2: KEYBOARD & CONTROLS (Center)
        BuildControlsColumn(columnsRoot, uiFont);

        // COLUMN 3: ARSENAL & SURVIVAL TIPS (Right)
        BuildArsenalColumn(columnsRoot, uiFont);
    }

    private static void BuildGoalsColumn(GameObject parent, Font uiFont)
    {
        GameObject col = CreateColumnCard(parent, "Col_Goals", new Vector2(0f, 0.5f), new Vector2(0.32f, 0.5f), 
            new Vector2(190f, 0f), new Vector2(370f, 470f), new Color(0.15f, 0.18f, 0.14f, 0.92f));

        CreateHeader(col, "MISSION GOALS", LoadShlopFont(), new Color(0.45f, 0.85f, 0.45f));

        string content = 
            "<b>1. PROTECT THE HOUSE</b>\n" +
            "Zombies march down 4 roads (East, North, West, South) to attack the House.\n\n" +
            "<b>2. SURVIVE ALL WAVES</b>\n" +
            "Defeat all zombie waves to advance:\n" +
            "  * Round 1: <i>Daylight</i>\n" +
            "  * Round 2: <i>Cloudy Fog</i>\n" +
            "  * Round 3: <i>Night Terror</i>\n\n" +
            "<b>3. DEFEAT CONDITION</b>\n" +
            "If House HP reaches <b>0%</b>, game is lost!";

        CreateBodyText(col, content, uiFont, 21, TextAnchor.UpperLeft, new Vector2(0f, -48f), new Vector2(-24f, -90f));
    }

    private static void BuildControlsColumn(GameObject parent, Font uiFont)
    {
        GameObject col = CreateColumnCard(parent, "Col_Controls", new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), 
            Vector2.zero, new Vector2(410f, 470f), new Color(0.16f, 0.15f, 0.22f, 0.92f));

        CreateHeader(col, "KEYBOARD & CONTROLS", LoadShlopFont(), new Color(0.55f, 0.8f, 1f));

        string content = 
            "<b>[ W ] [ A ] [ S ] [ D ]</b> / Arrows\n" +
            "  Move your hero in 3rd person\n\n" +
            "<b>[ MOUSE MOVE ]</b>\n" +
            "  Orbit camera & look around\n\n" +
            "<b>[ LEFT CLICK ] / [ SPACE ]</b>\n" +
            "  Melee sword strike against zombies\n\n" +
            "<b>[ 1 ] [ 2 ] [ 3 ]</b>\n" +
            "  Select Plant (Peashooter / Snow / Sun)\n\n" +
            "<b>[ 4 ] or [ R ]</b>\n" +
            "  Equip Shovel (removes plant)\n\n" +
            "<b>[ E ]</b>\n" +
            "  Action: Plant or Dig on soil square\n\n" +
            "<b>[ H ]</b>\n" +
            "  Inspect / Close 3D Guide";

        CreateBodyText(col, content, uiFont, 20, TextAnchor.UpperLeft, new Vector2(0f, -48f), new Vector2(-24f, -90f));
    }

    private static void BuildArsenalColumn(GameObject parent, Font uiFont)
    {
        GameObject col = CreateColumnCard(parent, "Col_Arsenal", new Vector2(1f, 0.5f), new Vector2(0.68f, 0.5f), 
            new Vector2(-190f, 0f), new Vector2(370f, 470f), new Color(0.22f, 0.16f, 0.12f, 0.92f));

        CreateHeader(col, "ARSENAL & TIPS", LoadShlopFont(), new Color(1f, 0.78f, 0.35f));

        string content = 
            "<b>PEASHOOTER (100 Sun)</b>\n" +
            "  Fires straight-line peas at horde.\n\n" +
            "<b>SNOW PEA (175 Sun)</b>\n" +
            "  Chills and slows enemy march.\n\n" +
            "<b>SUNFLOWER (50 Sun)</b>\n" +
            "  Generates bonus Sun currency.\n\n" +
            "<b>SUN ORBS (+25)</b>\n" +
            "  Walk over floating Sun to claim!\n\n" +
            "<b>HERO COMBAT (35 DMG)</b>\n" +
            "  Chop zombies slipping past plants!";

        CreateBodyText(col, content, uiFont, 21, TextAnchor.UpperLeft, new Vector2(0f, -48f), new Vector2(-24f, -90f));
    }

    private static GameObject CreateColumnCard(GameObject parent, string name, Vector2 anchorMin, Vector2 anchorMax, Vector2 pos, Vector2 size, Color bgColor)
    {
        GameObject card = new GameObject(name);
        card.transform.SetParent(parent.transform, false);
        RectTransform rt = card.AddComponent<RectTransform>();
        rt.anchorMin = anchorMin;
        rt.anchorMax = anchorMax;
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = pos;
        rt.sizeDelta = size;

        Image img = card.AddComponent<Image>();
        img.color = bgColor;
        return card;
    }

    private static void CreateHeader(GameObject card, string title, Font font, Color textColor)
    {
        GameObject headerObj = new GameObject("Header");
        headerObj.transform.SetParent(card.transform, false);
        RectTransform rt = headerObj.AddComponent<RectTransform>();
        rt.anchorMin = new Vector2(0f, 1f);
        rt.anchorMax = new Vector2(1f, 1f);
        rt.pivot = new Vector2(0.5f, 1f);
        rt.anchoredPosition = new Vector2(0f, -8f);
        rt.sizeDelta = new Vector2(-16f, 44f);

        Image bg = headerObj.AddComponent<Image>();
        bg.color = new Color(0.08f, 0.08f, 0.08f, 0.75f);

        GameObject textObj = new GameObject("HeaderText");
        textObj.transform.SetParent(headerObj.transform, false);
        Text text = textObj.AddComponent<Text>();
        text.font = font;
        text.fontSize = 24; // BIG Header (was 20)
        text.fontStyle = FontStyle.Bold;
        text.alignment = TextAnchor.MiddleCenter;
        text.color = textColor;
        text.text = title;

        RectTransform tRt = textObj.GetComponent<RectTransform>();
        tRt.anchorMin = Vector2.zero;
        tRt.anchorMax = Vector1();
        tRt.sizeDelta = Vector2.zero;
    }

    private static void CreateBodyText(GameObject card, string textContent, Font font, int size, TextAnchor align, Vector2 pos, Vector2 sizeDelta)
    {
        GameObject textObj = new GameObject("BodyText");
        textObj.transform.SetParent(card.transform, false);
        Text text = textObj.AddComponent<Text>();
        text.font = font;
        text.fontSize = size;
        text.lineSpacing = 1.15f;
        text.alignment = align;
        text.color = new Color(0.96f, 0.94f, 0.90f);
        text.text = textContent;
        text.supportRichText = true;

        RectTransform rt = textObj.GetComponent<RectTransform>();
        rt.anchorMin = new Vector2(0f, 0f);
        rt.anchorMax = new Vector2(1f, 1f);
        rt.pivot = new Vector2(0.5f, 1f);
        rt.anchoredPosition = pos;
        rt.sizeDelta = sizeDelta;
    }

    private static GameObject CreateCube(Transform parent, string name, Vector3 localPos, Vector3 localScale, Material material)
    {
        GameObject cube = GameObject.CreatePrimitive(PrimitiveType.Cube);
        cube.name = name;
        cube.transform.SetParent(parent, false);
        cube.transform.localPosition = localPos;
        cube.transform.localScale = localScale;
        cube.GetComponent<Renderer>().sharedMaterial = material;
        
        // Remove or adjust colliders
        BoxCollider col = cube.GetComponent<BoxCollider>();
        if (col != null)
        {
            // Keep main backing collider for physical blocking, destroy minor decor colliders
            if (!name.StartsWith("Post_") && !name.StartsWith("BackingPlank"))
            {
                Object.DestroyImmediate(col);
            }
        }

        return cube;
    }

    private static Material GetOrCreateMaterial(string name, Color baseColor, float smoothness)
    {
        string path = $"{MaterialsFolder}/{name}.mat";
        Material mat = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (mat != null) return mat;

        Shader shader = Shader.Find("Universal Render Pipeline/Lit");
        if (shader == null) shader = Shader.Find("Standard");

        mat = new Material(shader);
        if (shader.name.Contains("Universal"))
        {
            mat.SetColor("_BaseColor", baseColor);
            mat.SetFloat("_Smoothness", smoothness);
        }
        else
        {
            mat.color = baseColor;
            mat.SetFloat("_Glossiness", smoothness);
        }

        AssetDatabase.CreateAsset(mat, path);
        return mat;
    }

    private static Material GetOrCreateEmissiveMaterial(string name, Color color, float emissionIntensity)
    {
        string path = $"{MaterialsFolder}/{name}.mat";
        Material mat = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (mat != null) return mat;

        Shader shader = Shader.Find("Universal Render Pipeline/Lit");
        if (shader == null) shader = Shader.Find("Standard");

        mat = new Material(shader);
        Color emission = color * emissionIntensity;

        if (shader.name.Contains("Universal"))
        {
            mat.SetColor("_BaseColor", color);
            mat.SetColor("_EmissionColor", emission);
            mat.EnableKeyword("_EMISSION");
        }
        else
        {
            mat.color = color;
            mat.SetColor("_EmissionColor", emission);
            mat.EnableKeyword("_EMISSION");
        }

        AssetDatabase.CreateAsset(mat, path);
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
        if (font == null)
            font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        if (font == null)
            font = Resources.GetBuiltinResource<Font>("Arial.ttf");
        return font;
    }

    private static Vector2 Vector1()
    {
        return new Vector2(1f, 1f);
    }

    private static void EnsureFolders()
    {
        if (!System.IO.Directory.Exists(PrefabFolder))
            System.IO.Directory.CreateDirectory(PrefabFolder);
        if (!System.IO.Directory.Exists(MaterialsFolder))
            System.IO.Directory.CreateDirectory(MaterialsFolder);
    }
}
