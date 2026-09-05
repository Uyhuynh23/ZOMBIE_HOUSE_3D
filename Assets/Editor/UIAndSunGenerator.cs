using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Tools > Generate UI and Better Sun  — rebuilds the full PvZ-style HUD.
/// Tools > Fix Plant Colliders         — patches prefabs with correct colliders.
/// </summary>
public class UIAndSunGenerator
{
    private const string RobotoBoldPath = "Assets/TextMesh Pro/Examples & Extras/Fonts/Roboto-Bold.ttf";
    private const string ShlopFontPath = "Assets/Fonts/shlop/shlop rg.otf";

    // -----------------------------------------------------------------------------
    //  MAIN ENTRY: regenerate Sun prefab + PvZ HUD
    // -----------------------------------------------------------------------------
    [MenuItem("Tools/Generate UI and Better Sun")]
    public static void Generate()
    {
        GenerateBetterSun();
        GenerateFullUI();
        AssetDatabase.SaveAssets();
        Debug.Log("[UIAndSunGenerator] UI and Sun updated!");
    }

    [MenuItem("Tools/Save HUDPanel As Prefab")]
    public static void SaveHUDPanelAsPrefab()
    {
        GameObject hudPanel = GameObject.Find("HUDPanel");
        if (hudPanel == null)
        {
            Debug.LogError("[UIAndSunGenerator] No HUDPanel found in the current scene!");
            return;
        }

        if (!System.IO.Directory.Exists("Assets/Prefabs"))
            System.IO.Directory.CreateDirectory("Assets/Prefabs");

        PrefabUtility.SaveAsPrefabAsset(hudPanel, "Assets/Prefabs/HUDPanel.prefab");
        AssetDatabase.SaveAssets();
        Debug.Log("[UIAndSunGenerator] HUDPanel saved to Assets/Prefabs/HUDPanel.prefab successfully!");
    }

    // -----------------------------------------------------------------------------
    //  SUN PREFAB
    // -----------------------------------------------------------------------------
    static void GenerateBetterSun()
    {
        GameObject sunRoot = new GameObject("Sun");

        Shader shader = Shader.Find("Universal Render Pipeline/Lit");
        if (shader == null) shader = Shader.Find("Standard");

        Material yellowMat = new Material(shader);
        Material orangeMat = new Material(shader);

        if (shader.name.Contains("Universal"))
        {
            yellowMat.SetColor("_BaseColor", Color.yellow);
            yellowMat.SetColor("_EmissionColor", Color.yellow * 0.5f);
            yellowMat.EnableKeyword("_EMISSION");
            orangeMat.SetColor("_BaseColor", new Color(1f, 0.6f, 0f));
            orangeMat.SetColor("_EmissionColor", new Color(1f, 0.6f, 0f) * 0.5f);
            orangeMat.EnableKeyword("_EMISSION");
        }
        else
        {
            yellowMat.color = Color.yellow;
            yellowMat.SetColor("_EmissionColor", Color.yellow);
            yellowMat.EnableKeyword("_EMISSION");
            orangeMat.color = new Color(1f, 0.6f, 0f);
            orangeMat.SetColor("_EmissionColor", new Color(1f, 0.6f, 0f));
            orangeMat.EnableKeyword("_EMISSION");
        }

        if (!System.IO.Directory.Exists("Assets/Materials"))
            System.IO.Directory.CreateDirectory("Assets/Materials");

        if (AssetDatabase.LoadAssetAtPath<Material>("Assets/Materials/SunYellow.mat") != null) AssetDatabase.DeleteAsset("Assets/Materials/SunYellow.mat");
        if (AssetDatabase.LoadAssetAtPath<Material>("Assets/Materials/SunOrange.mat") != null) AssetDatabase.DeleteAsset("Assets/Materials/SunOrange.mat");
        AssetDatabase.CreateAsset(yellowMat, "Assets/Materials/SunYellow.mat");
        AssetDatabase.CreateAsset(orangeMat, "Assets/Materials/SunOrange.mat");

        GameObject core = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        core.transform.SetParent(sunRoot.transform);
        core.transform.localPosition = Vector3.zero;
        core.transform.localScale = new Vector3(1f, 1f, 0.5f);
        core.GetComponent<MeshRenderer>().sharedMaterial = yellowMat;
        GameObject.DestroyImmediate(core.GetComponent<Collider>());

        for (int i = 0; i < 4; i++)
        {
            GameObject spike = GameObject.CreatePrimitive(PrimitiveType.Cube);
            spike.transform.SetParent(sunRoot.transform);
            spike.transform.localPosition = Vector3.zero;
            spike.transform.localRotation = Quaternion.Euler(0, 0, i * 45f);
            spike.transform.localScale = new Vector3(0.4f, 1.6f, 0.1f);
            spike.GetComponent<MeshRenderer>().sharedMaterial = orangeMat;
            GameObject.DestroyImmediate(spike.GetComponent<Collider>());
        }

        SphereCollider col = sunRoot.AddComponent<SphereCollider>();
        col.isTrigger = true;
        col.radius = 2f;
        sunRoot.AddComponent<Sun>();
        sunRoot.transform.localScale = Vector3.one * 0.4f;

        PrefabUtility.SaveAsPrefabAsset(sunRoot, "Assets/Prefabs/Sun.prefab");
        GameObject.DestroyImmediate(sunRoot);
    }

    // -----------------------------------------------------------------------------
    //  PVZ-STYLE HUD
    // -----------------------------------------------------------------------------
    public static void GenerateFullUI()
    {
        // Remove old canvas
        GameObject oldCanvas = GameObject.Find("UI_Canvas");
        if (oldCanvas != null) GameObject.DestroyImmediate(oldCanvas);

        // -- Canvas --
        GameObject canvasObj = new GameObject("UI_Canvas");
        Canvas canvas = canvasObj.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 10;
        canvas.pixelPerfect = true;
        CanvasScaler scaler = canvasObj.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1280, 720);
        scaler.matchWidthOrHeight = 0.5f;
        scaler.referencePixelsPerUnit = 100f;
        canvasObj.AddComponent<GraphicRaycaster>();

        Font uiFont = LoadUIFont();

        GameUIManager uiManager = canvasObj.AddComponent<GameUIManager>();

        CreateBattleStatus(canvasObj, uiManager, uiFont);

        // -- Left Sidebar HUD Panel (dark wood-brown bar) --
        GameObject hudPanel = new GameObject("HUDPanel");
        hudPanel.transform.SetParent(canvasObj.transform, false);
        Image hudBg = hudPanel.AddComponent<Image>();
        hudBg.color = new Color(0.27f, 0.15f, 0.05f, 0.92f); // dark wood brown
        RectTransform hudRt = hudPanel.GetComponent<RectTransform>();
        hudRt.anchorMin = new Vector2(0f, 0f);
        hudRt.anchorMax = new Vector2(0f, 1f);
        hudRt.pivot = new Vector2(0f, 0.5f);
        hudRt.anchoredPosition = Vector2.zero;
        hudRt.sizeDelta = new Vector2(130f, 0f);

        VerticalLayoutGroup vlg = hudPanel.AddComponent<VerticalLayoutGroup>();
        vlg.padding = new RectOffset(15, 15, 15, 15);
        vlg.spacing = 10f;
        vlg.childAlignment = TextAnchor.MiddleCenter;
        vlg.childControlWidth = false;
        vlg.childControlHeight = false;
        vlg.childForceExpandWidth = false;
        vlg.childForceExpandHeight = false;

        // 1. Shovel Button (top item)
        CreateShovelButton(hudPanel, uiManager, uiFont);

        // 2. Plant Cards (middle items)
        string[] plantNames = { "Peashooter", "Snow Pea", "Sunflower" };
        Color[]  cardColors  = {
            new Color(0.4f, 0.8f, 0.35f, 1f),  // Peashooter — dark green
            new Color(0.4f, 0.7f, 0.9f, 1f),  // Snow Pea   — steel blue
            new Color(0.9f, 0.8f, 0.2f, 1f),  // Sunflower  — golden brown
        };
        int[] costs = { 100, 175, 50 };

        PlantCardUI[] cards = new PlantCardUI[plantNames.Length];
        for (int i = 0; i < plantNames.Length; i++)
        {
            cards[i] = CreatePlantCard(hudPanel, i, plantNames[i], costs[i], cardColors[i], uiFont);
        }
        uiManager.plantCards = cards;

        // 3. Sun Counter (bottom item)
        CreateSunDisplay(hudPanel, uiManager, uiFont);

        // -- Controls hint (bottom, next to sidebar) --
        GameObject hintObj = new GameObject("ControlsHint");
        hintObj.transform.SetParent(canvasObj.transform, false);
        Text hintText = hintObj.AddComponent<Text>();
        hintText.text = "1/2/3: Select | E: Plant | 4/R: Shovel | LMB/Space: Attack | H: 3D Guide";
        hintText.font = uiFont;
        hintText.fontSize = 16;
        hintText.fontStyle = FontStyle.Bold;
        hintText.color = Color.white;
        hintText.alignment = TextAnchor.UpperLeft;
        RectTransform hRt = hintText.GetComponent<RectTransform>();
        hRt.anchorMin = new Vector2(0f, 0f);
        hRt.anchorMax = new Vector2(1f, 0f);
        hRt.pivot = new Vector2(0f, 0f);
        hRt.anchoredPosition = new Vector2(145f, 12f);
        hRt.sizeDelta = new Vector2(-160f, 28f);

        CreateEndGamePanels(canvasObj, uiManager, uiFont);

        // Save HUDPanel as Prefab
        if (!System.IO.Directory.Exists("Assets/Prefabs"))
            System.IO.Directory.CreateDirectory("Assets/Prefabs");
        PrefabUtility.SaveAsPrefabAsset(hudPanel, "Assets/Prefabs/HUDPanel.prefab");
    }

    private static void CreateBattleStatus(GameObject canvasObj, GameUIManager uiManager, Font uiFont)
    {
        GameObject panel = new GameObject("BattleStatus");
        panel.transform.SetParent(canvasObj.transform, false);
        Image panelImage = panel.AddComponent<Image>();
        panelImage.color = new Color(0.035f, 0.06f, 0.055f, 0.9f);
        RectTransform panelRect = panelImage.rectTransform;
        panelRect.anchorMin = new Vector2(0.5f, 1f);
        panelRect.anchorMax = new Vector2(0.5f, 1f);
        panelRect.pivot = new Vector2(0.5f, 1f);
        panelRect.anchoredPosition = new Vector2(0f, -12f);
        panelRect.sizeDelta = new Vector2(620f, 76f);

        uiManager.waveText = CreateText(panel, "WaveText", "WAVE 1/3", LoadShlopFont(), 28,
            new Vector2(0.02f, 0.48f), new Vector2(0.49f, 0.96f), TextAnchor.MiddleLeft);
        uiManager.zombieText = CreateText(panel, "ZombieText", "ZOMBIES  0 active", uiFont, 18,
            new Vector2(0.02f, 0.04f), new Vector2(0.49f, 0.48f), TextAnchor.MiddleLeft);
        uiManager.houseHealthText = CreateText(panel, "HouseHealthText", "HOUSE  300/300", uiFont, 20,
            new Vector2(0.53f, 0.50f), new Vector2(0.98f, 0.94f), TextAnchor.MiddleCenter);

        GameObject healthBack = new GameObject("HouseHealthBar");
        healthBack.transform.SetParent(panel.transform, false);
        Image healthBackImage = healthBack.AddComponent<Image>();
        healthBackImage.color = new Color(0.16f, 0.03f, 0.03f, 1f);
        RectTransform healthRect = healthBackImage.rectTransform;
        healthRect.anchorMin = new Vector2(0.55f, 0.14f);
        healthRect.anchorMax = new Vector2(0.96f, 0.40f);
        healthRect.offsetMin = Vector2.zero;
        healthRect.offsetMax = Vector2.zero;

        GameObject healthFill = new GameObject("Fill");
        healthFill.transform.SetParent(healthBack.transform, false);
        Image fillImage = healthFill.AddComponent<Image>();
        fillImage.color = new Color(0.25f, 0.9f, 0.28f, 1f);
        fillImage.type = Image.Type.Filled;
        fillImage.fillMethod = Image.FillMethod.Horizontal;
        fillImage.fillOrigin = 0;
        fillImage.fillAmount = 1f;
        RectTransform fillRect = fillImage.rectTransform;
        fillRect.anchorMin = Vector2.zero;
        fillRect.anchorMax = Vector2.one;
        fillRect.offsetMin = new Vector2(3f, 3f);
        fillRect.offsetMax = new Vector2(-3f, -3f);
        uiManager.houseHealthFill = fillImage;
    }

    private static void CreateEndGamePanels(GameObject canvasObj, GameUIManager uiManager, Font uiFont)
    {
        uiManager.winPanel = CreateEndPanel(canvasObj, "WinPanel", "WAVE CLEARED!", "The house is safe.",
            new Color(0.08f, 0.35f, 0.13f, 0.94f), uiFont);
        uiManager.losePanel = CreateEndPanel(canvasObj, "LosePanel", "HOUSE DESTROYED", "The zombies broke through.",
            new Color(0.42f, 0.06f, 0.05f, 0.94f), uiFont);
        uiManager.winPanel.SetActive(false);
        uiManager.losePanel.SetActive(false);
    }

    private static GameObject CreateEndPanel(GameObject canvasObj, string name, string title, string subtitle,
        Color color, Font uiFont)
    {
        GameObject panel = new GameObject(name);
        panel.transform.SetParent(canvasObj.transform, false);
        Image image = panel.AddComponent<Image>();
        image.color = color;
        RectTransform rect = image.rectTransform;
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.sizeDelta = new Vector2(540f, 190f);

        CreateText(panel, "Title", title, LoadShlopFont(), 44,
            new Vector2(0.04f, 0.48f), new Vector2(0.96f, 0.92f), TextAnchor.MiddleCenter);
        CreateText(panel, "Subtitle", subtitle, uiFont, 23,
            new Vector2(0.04f, 0.12f), new Vector2(0.96f, 0.48f), TextAnchor.MiddleCenter);
        return panel;
    }

    private static Text CreateText(GameObject parent, string name, string value, Font font, int size,
        Vector2 anchorMin, Vector2 anchorMax, TextAnchor alignment)
    {
        GameObject textObject = new GameObject(name);
        textObject.transform.SetParent(parent.transform, false);
        Text text = textObject.AddComponent<Text>();
        text.text = value;
        text.font = font;
        text.fontSize = size;
        text.fontStyle = FontStyle.Bold;
        text.color = Color.white;
        text.alignment = alignment;
        RectTransform rect = text.rectTransform;
        rect.anchorMin = anchorMin;
        rect.anchorMax = anchorMax;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
        return text;
    }

    // -----------------------------------------------------------------------------
    //  CARD BUILDER
    // -----------------------------------------------------------------------------
    static PlantCardUI CreatePlantCard(GameObject parent, int index, string plantName, int cost, Color cardColor, Font uiFont)
    {
        // -- Card root --
        GameObject cardObj = new GameObject("Card_" + plantName);
        cardObj.transform.SetParent(parent.transform, false);
        RectTransform cardRt = cardObj.AddComponent<RectTransform>();
        cardRt.sizeDelta = new Vector2(100f, 120f);

        PlantCardUI cardUI = cardObj.AddComponent<PlantCardUI>();
        cardUI.plantIndex = index;

        // -- Card background (colored panel) --
        Image cardBg = cardObj.AddComponent<Image>();
        cardBg.color = cardColor;
        cardUI.cardBackground = cardBg;

        // -- Gold selection border (slightly larger, behind content) --
        GameObject borderObj = new GameObject("SelectionBorder");
        borderObj.transform.SetParent(cardObj.transform, false);
        Image borderImg = borderObj.AddComponent<Image>();
        borderImg.color = new Color(0f, 0f, 0f, 0f); // hidden by default
        borderImg.raycastTarget = false;
        RectTransform borderRt = borderImg.GetComponent<RectTransform>();
        borderRt.anchorMin = Vector2.zero;
        borderRt.anchorMax = Vector2.one;
        borderRt.sizeDelta = new Vector2(6f, 6f); // 3px border outset
        borderRt.anchoredPosition = Vector2.zero;
        cardUI.selectionBorder = borderImg;

        // -- Portrait area (top 65% of card) --
        GameObject portraitObj = new GameObject("Portrait");
        portraitObj.transform.SetParent(cardObj.transform, false);
        Image portraitImg = portraitObj.AddComponent<Image>();
        portraitImg.color = Color.white;
        portraitImg.preserveAspect = true;
        RectTransform portRt = portraitImg.GetComponent<RectTransform>();
        portRt.anchorMin = new Vector2(0.05f, 0.30f);
        portRt.anchorMax = new Vector2(0.95f, 0.97f);
        portRt.sizeDelta = Vector2.zero;
        portRt.anchoredPosition = Vector2.zero;
        cardUI.portraitImage = portraitImg;

        // -- Cost row (bottom 28% of card) --
        GameObject costRow = new GameObject("CostRow");
        costRow.transform.SetParent(cardObj.transform, false);
        RectTransform crRt = costRow.AddComponent<RectTransform>();
        crRt.anchorMin = new Vector2(0f, 0f);
        crRt.anchorMax = new Vector2(1f, 0.30f);
        crRt.sizeDelta = Vector2.zero;
        crRt.anchoredPosition = Vector2.zero;

        // Small sun icon in cost row
        GameObject costSunIcon = new GameObject("SunIcon");
        costSunIcon.transform.SetParent(costRow.transform, false);
        Image costSunImg = costSunIcon.AddComponent<Image>();
        costSunImg.color = new Color(1f, 0.9f, 0f, 1f);
        RectTransform csiRt = costSunImg.GetComponent<RectTransform>();
        csiRt.anchorMin = new Vector2(0.05f, 0.15f);
        csiRt.anchorMax = new Vector2(0.45f, 0.85f);
        csiRt.sizeDelta = Vector2.zero;
        csiRt.anchoredPosition = Vector2.zero;
        cardUI.sunIcon = costSunImg;

        // Cost text
        GameObject costObj = new GameObject("CostText");
        costObj.transform.SetParent(costRow.transform, false);
        Text costText = costObj.AddComponent<Text>();
        costText.text = cost.ToString();
        costText.font = uiFont;
        costText.fontSize = 24;
        costText.fontStyle = FontStyle.Bold;
        costText.color = Color.white;
        costText.alignment = TextAnchor.MiddleLeft;
        RectTransform ctRt = costText.GetComponent<RectTransform>();
        ctRt.anchorMin = new Vector2(0.45f, 0f);
        ctRt.anchorMax = new Vector2(1f, 1f);
        ctRt.sizeDelta = Vector2.zero;
        ctRt.anchoredPosition = Vector2.zero;
        cardUI.costText = costText;

        // -- Cooldown overlay (Radial360 dark) --
        GameObject cdObj = new GameObject("CooldownOverlay");
        cdObj.transform.SetParent(cardObj.transform, false);
        Image cdImg = cdObj.AddComponent<Image>();
        cdImg.color = new Color(0f, 0f, 0f, 0.75f);
        cdImg.type = Image.Type.Filled;
        cdImg.fillMethod = Image.FillMethod.Radial360;
        cdImg.fillAmount = 0f;
        cdImg.raycastTarget = false;
        RectTransform cdRt = cdImg.GetComponent<RectTransform>();
        cdRt.anchorMin = Vector2.zero;
        cdRt.anchorMax = Vector2.one;
        cdRt.sizeDelta = Vector2.zero;
        cdRt.anchoredPosition = Vector2.zero;
        cardUI.cooldownOverlay = cdImg;

        // -- Insufficient sun flash (red overlay) --
        GameObject flashObj = new GameObject("InsufficientFlash");
        flashObj.transform.SetParent(cardObj.transform, false);
        Image flashImg = flashObj.AddComponent<Image>();
        flashImg.color = new Color(1f, 0f, 0f, 0f); // starts transparent
        flashImg.raycastTarget = false;
        RectTransform flRt = flashImg.GetComponent<RectTransform>();
        flRt.anchorMin = Vector2.zero;
        flRt.anchorMax = Vector2.one;
        flRt.sizeDelta = Vector2.zero;
        flRt.anchoredPosition = Vector2.zero;
        cardUI.insufficientFlash = flashImg;

        return cardUI;
    }

    // -----------------------------------------------------------------------------
    //  SHOVEL BUTTON
    // -----------------------------------------------------------------------------
    static void CreateShovelButton(GameObject hudPanel, GameUIManager uiManager, Font uiFont)
    {
        GameObject shovelObj = new GameObject("ShovelButton");
        shovelObj.transform.SetParent(hudPanel.transform, false);
        RectTransform sRt = shovelObj.AddComponent<RectTransform>();
        sRt.anchorMin = new Vector2(0.5f, 0.5f);
        sRt.anchorMax = new Vector2(0.5f, 0.5f);
        sRt.pivot = new Vector2(0.5f, 0.5f);
        sRt.anchoredPosition = Vector2.zero;
        sRt.sizeDelta = new Vector2(100f, 75f);

        Image shovelBg = shovelObj.AddComponent<Image>();
        shovelBg.color = new Color(0.45f, 0.25f, 0.08f, 1f);

        // PlantCardUI used as shovel card for click handling
        PlantCardUI shovelCard = shovelObj.AddComponent<PlantCardUI>();
        shovelCard.isShovelCard = true;

        // "Shovel" label
        GameObject labelObj = new GameObject("ShovelLabel");
        labelObj.transform.SetParent(shovelObj.transform, false);
        Text labelText = labelObj.AddComponent<Text>();
        labelText.text = "SHOVEL\n(4/R)";
        labelText.font = uiFont;
        labelText.fontSize = 18;
        labelText.fontStyle = FontStyle.Bold;
        labelText.color = Color.white;
        labelText.alignment = TextAnchor.MiddleCenter;
        RectTransform lRt = labelText.GetComponent<RectTransform>();
        lRt.anchorMin = Vector2.zero;
        lRt.anchorMax = Vector2.one;
        lRt.sizeDelta = Vector2.zero;
        lRt.anchoredPosition = Vector2.zero;
    }

    // -----------------------------------------------------------------------------
    //  SUN DISPLAY
    // -----------------------------------------------------------------------------
    static void CreateSunDisplay(GameObject hudPanel, GameUIManager uiManager, Font uiFont)
    {
        GameObject sunDisplay = new GameObject("SunDisplay");
        sunDisplay.transform.SetParent(hudPanel.transform, false);
        RectTransform sdRt = sunDisplay.AddComponent<RectTransform>();
        sdRt.anchorMin = new Vector2(0.5f, 0.5f);
        sdRt.anchorMax = new Vector2(0.5f, 0.5f);
        sdRt.pivot = new Vector2(0.5f, 0.5f);
        sdRt.anchoredPosition = Vector2.zero;
        sdRt.sizeDelta = new Vector2(100f, 95f);

        // Sun icon (circle)
        GameObject sunCircle = new GameObject("SunCircle");
        sunCircle.transform.SetParent(sunDisplay.transform, false);
        Image sunCircleImg = sunCircle.AddComponent<Image>();
        sunCircleImg.color = new Color(1f, 0.9f, 0f, 1f);
        RectTransform scRt = sunCircle.GetComponent<RectTransform>();
        scRt.anchorMin = new Vector2(0.5f, 0.60f);
        scRt.anchorMax = new Vector2(0.5f, 0.60f);
        scRt.pivot = new Vector2(0.5f, 0.5f);
        scRt.anchoredPosition = Vector2.zero;
        scRt.sizeDelta = new Vector2(55f, 55f);

        // Sun text
        GameObject sunTextObj = new GameObject("SunText");
        sunTextObj.transform.SetParent(sunDisplay.transform, false);
        Text sunText = sunTextObj.AddComponent<Text>();
        sunText.text = "50";
        sunText.font = uiFont;
        sunText.fontSize = 28;
        sunText.fontStyle = FontStyle.Bold;
        sunText.color = Color.white;
        sunText.alignment = TextAnchor.MiddleCenter;
        RectTransform stRt = sunText.GetComponent<RectTransform>();
        stRt.anchorMin = new Vector2(0f, 0f);
        stRt.anchorMax = new Vector2(1f, 0.38f);
        stRt.sizeDelta = Vector2.zero;
        stRt.anchoredPosition = Vector2.zero;

        uiManager.sunText = sunText;
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

    // -----------------------------------------------------------------------------
    //  PLANT COLLIDER FIXER
    // -----------------------------------------------------------------------------
    [MenuItem("Tools/Fix Plant Colliders")]
    public static void FixPlantColliders()
    {
        string[] prefabGuids = AssetDatabase.FindAssets("t:Prefab", new[] { "Assets/Prefabs" });
        int fixed_count = 0;

        foreach (string guid in prefabGuids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (prefab == null) continue;

            bool isPlant = prefab.GetComponentInChildren<PeashooterCombat>() != null
                        || prefab.GetComponentInChildren<SunflowerLogic>() != null;
            if (!isPlant) continue;

            using (var scope = new PrefabUtility.EditPrefabContentsScope(path))
            {
                GameObject root = scope.prefabContentsRoot;

                // -- Remove leftover CharacterController --
                CharacterController cc = root.GetComponent<CharacterController>();
                if (cc != null)
                {
                    GameObject.DestroyImmediate(cc);
                    Debug.Log("[FixPlantColliders] Removed CharacterController from: " + root.name);
                }

                // -- Ensure CapsuleCollider (physical body) --
                CapsuleCollider capsule = root.GetComponent<CapsuleCollider>();
                if (capsule == null)
                {
                    capsule = root.AddComponent<CapsuleCollider>();
                    Debug.Log("[FixPlantColliders] Added CapsuleCollider to: " + root.name);
                }
                capsule.isTrigger = false;
                capsule.height = 1.0f;
                capsule.radius = 0.35f;
                capsule.center = new Vector3(0f, 0.5f, 0f);
                capsule.direction = 1; // Y-axis

                // -- Ensure SphereCollider exists (for PeashooterCombat aggro) --
                SphereCollider sphere = root.GetComponent<SphereCollider>();
                if (sphere == null)
                {
                    sphere = root.AddComponent<SphereCollider>();
                    Debug.Log("[FixPlantColliders] Added SphereCollider to: " + root.name);
                }
                // PeashooterCombat.Start() will set isTrigger=true & radius at runtime.
                // For Sunflower (no PeashooterCombat), set it as trigger with small radius now.
                if (root.GetComponent<PeashooterCombat>() == null && root.GetComponentInChildren<PeashooterCombat>() == null)
                {
                    sphere.isTrigger = true;
                    sphere.radius = 0.4f;
                    sphere.enabled = false; // Sunflower doesn't need aggro sphere active
                }

                fixed_count++;
            }
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log($"[FixPlantColliders] Done! Fixed {fixed_count} plant prefab(s).");
    }
}
