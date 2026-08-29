using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Tools > Generate UI and Better Sun  — rebuilds the full PvZ-style HUD.
/// Tools > Fix Plant Colliders         — patches prefabs with correct colliders.
/// </summary>
public class UIAndSunGenerator
{
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
    static void GenerateFullUI()
    {
        // Remove old canvas
        GameObject oldCanvas = GameObject.Find("UI_Canvas");
        if (oldCanvas != null) GameObject.DestroyImmediate(oldCanvas);

        // -- Canvas --
        GameObject canvasObj = new GameObject("UI_Canvas");
        Canvas canvas = canvasObj.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 10;
        CanvasScaler scaler = canvasObj.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        scaler.matchWidthOrHeight = 0.5f;
        canvasObj.AddComponent<GraphicRaycaster>();

        GameUIManager uiManager = canvasObj.AddComponent<GameUIManager>();

        // -- Bottom HUD Panel (dark wood-brown bar) --
        GameObject hudPanel = new GameObject("HUDPanel");
        hudPanel.transform.SetParent(canvasObj.transform, false);
        Image hudBg = hudPanel.AddComponent<Image>();
        hudBg.color = new Color(0.27f, 0.15f, 0.05f, 0.92f); // dark wood brown
        RectTransform hudRt = hudPanel.GetComponent<RectTransform>();
        hudRt.anchorMin = new Vector2(0f, 0f);
        hudRt.anchorMax = new Vector2(1f, 0f);
        hudRt.pivot = new Vector2(0.5f, 0f);
        hudRt.anchoredPosition = Vector2.zero;
        hudRt.sizeDelta = new Vector2(0f, 130f);

        // -- Sun Counter (left side of HUD) --
        GameObject sunDisplay = new GameObject("SunDisplay");
        sunDisplay.transform.SetParent(hudPanel.transform, false);
        RectTransform sdRt = sunDisplay.AddComponent<RectTransform>();
        sdRt.anchorMin = new Vector2(0f, 0.5f);
        sdRt.anchorMax = new Vector2(0f, 0.5f);
        sdRt.pivot = new Vector2(0f, 0.5f);
        sdRt.anchoredPosition = new Vector2(10f, 0f);
        sdRt.sizeDelta = new Vector2(120f, 100f);

        // Sun icon (circle)
        GameObject sunCircle = new GameObject("SunCircle");
        sunCircle.transform.SetParent(sunDisplay.transform, false);
        Image sunCircleImg = sunCircle.AddComponent<Image>();
        sunCircleImg.color = new Color(1f, 0.9f, 0f, 1f);
        RectTransform scRt = sunCircle.GetComponent<RectTransform>();
        scRt.anchorMin = new Vector2(0.5f, 0.55f);
        scRt.anchorMax = new Vector2(0.5f, 0.55f);
        scRt.pivot = new Vector2(0.5f, 0.5f);
        scRt.anchoredPosition = Vector2.zero;
        scRt.sizeDelta = new Vector2(60f, 60f);

        // Sun text
        GameObject sunTextObj = new GameObject("SunText");
        sunTextObj.transform.SetParent(sunDisplay.transform, false);
        Text sunText = sunTextObj.AddComponent<Text>();
        sunText.text = "50";
        sunText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        sunText.fontSize = 30;
        sunText.fontStyle = FontStyle.Bold;
        sunText.color = Color.white;
        sunText.alignment = TextAnchor.MiddleCenter;
        RectTransform stRt = sunText.GetComponent<RectTransform>();
        stRt.anchorMin = new Vector2(0f, 0f);
        stRt.anchorMax = new Vector2(1f, 0.45f);
        stRt.sizeDelta = Vector2.zero;
        stRt.anchoredPosition = Vector2.zero;

        uiManager.sunText = sunText;

        // -- Cards Container (centered in HUD) --
        GameObject cardsContainer = new GameObject("CardsContainer");
        cardsContainer.transform.SetParent(hudPanel.transform, false);
        RectTransform ccRt = cardsContainer.AddComponent<RectTransform>();
        ccRt.anchorMin = new Vector2(0.5f, 0.5f);
        ccRt.anchorMax = new Vector2(0.5f, 0.5f);
        ccRt.pivot = new Vector2(0.5f, 0.5f);
        ccRt.anchoredPosition = new Vector2(0f, 0f);
        ccRt.sizeDelta = new Vector2(400f, 120f);

        HorizontalLayoutGroup hlg = cardsContainer.AddComponent<HorizontalLayoutGroup>();
        hlg.childControlWidth = false;
        hlg.childControlHeight = false;
        hlg.childAlignment = TextAnchor.MiddleCenter;
        hlg.spacing = 8f;

        // Plant definitions: name, cost, PvZ-inspired background color
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
            cards[i] = CreatePlantCard(cardsContainer, i, plantNames[i], costs[i], cardColors[i]);
        }

        uiManager.plantCards = cards;

        // -- Shovel Button (right side of HUD) --
        CreateShovelButton(hudPanel, uiManager);

        // -- Controls hint (top-left, small) --
        GameObject hintObj = new GameObject("ControlsHint");
        hintObj.transform.SetParent(canvasObj.transform, false);
        Text hintText = hintObj.AddComponent<Text>();
        hintText.text = "1/2/3 or Click: Select Plant | 4/R: Shovel | E: Plant/Remove";
        hintText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        hintText.fontSize = 18;
        hintText.color = new Color(1f, 1f, 1f, 0.7f);
        hintText.alignment = TextAnchor.UpperLeft;
        RectTransform hRt = hintText.GetComponent<RectTransform>();
        hRt.anchorMin = new Vector2(0f, 1f);
        hRt.anchorMax = new Vector2(1f, 1f);
        hRt.pivot = new Vector2(0f, 1f);
        hRt.anchoredPosition = new Vector2(10f, -10f);
        hRt.sizeDelta = new Vector2(0f, 30f);
    }

    // -----------------------------------------------------------------------------
    //  CARD BUILDER
    // -----------------------------------------------------------------------------
    static PlantCardUI CreatePlantCard(GameObject parent, int index, string plantName, int cost, Color cardColor)
    {
        // -- Card root --
        GameObject cardObj = new GameObject("Card_" + plantName);
        cardObj.transform.SetParent(parent.transform, false);
        RectTransform cardRt = cardObj.AddComponent<RectTransform>();
        cardRt.sizeDelta = new Vector2(90f, 115f);

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
        costText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        costText.fontSize = 22;
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
    static void CreateShovelButton(GameObject hudPanel, GameUIManager uiManager)
    {
        GameObject shovelObj = new GameObject("ShovelButton");
        shovelObj.transform.SetParent(hudPanel.transform, false);
        RectTransform sRt = shovelObj.AddComponent<RectTransform>();
        sRt.anchorMin = new Vector2(1f, 0.5f);
        sRt.anchorMax = new Vector2(1f, 0.5f);
        sRt.pivot = new Vector2(1f, 0.5f);
        sRt.anchoredPosition = new Vector2(-12f, 0f);
        sRt.sizeDelta = new Vector2(80f, 95f);

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
        labelText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        labelText.fontSize = 16;
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
