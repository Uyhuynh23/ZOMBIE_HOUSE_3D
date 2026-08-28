using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

public class UIAndSunGenerator
{
    [MenuItem("Tools/Generate UI and Better Sun")]
    public static void Generate()
    {
        GenerateBetterSun();
        GenerateFullUI();
        Debug.Log("UI and Sun updated!");
    }

    static void GenerateBetterSun()
    {
        GameObject sunRoot = new GameObject("Sun");
        
        // Materials
        Shader shader = Shader.Find("Universal Render Pipeline/Lit");
        if (shader == null) shader = Shader.Find("Standard");
        
        Material yellowMat = new Material(shader);
        Material orangeMat = new Material(shader);
        
        if (shader.name.Contains("Universal"))
        {
            yellowMat.SetColor("_BaseColor", Color.yellow);
            yellowMat.SetColor("_EmissionColor", Color.yellow);
            yellowMat.EnableKeyword("_EMISSION");
            
            orangeMat.SetColor("_BaseColor", new Color(1f, 0.6f, 0f));
            orangeMat.SetColor("_EmissionColor", new Color(1f, 0.6f, 0f));
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
        {
            System.IO.Directory.CreateDirectory("Assets/Materials");
        }
        
        if (AssetDatabase.LoadAssetAtPath<Material>("Assets/Materials/SunYellow.mat") != null) AssetDatabase.DeleteAsset("Assets/Materials/SunYellow.mat");
        if (AssetDatabase.LoadAssetAtPath<Material>("Assets/Materials/SunOrange.mat") != null) AssetDatabase.DeleteAsset("Assets/Materials/SunOrange.mat");
        
        AssetDatabase.CreateAsset(yellowMat, "Assets/Materials/SunYellow.mat");
        AssetDatabase.CreateAsset(orangeMat, "Assets/Materials/SunOrange.mat");

        // 1. Core Sphere
        GameObject core = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        core.transform.SetParent(sunRoot.transform);
        core.transform.localPosition = Vector3.zero;
        core.transform.localScale = new Vector3(1f, 1f, 0.5f); // Flattened sphere
        core.GetComponent<MeshRenderer>().sharedMaterial = yellowMat;
        GameObject.DestroyImmediate(core.GetComponent<Collider>());

        // 2. Star Spikes (4 overlapping stretched cubes rotated to make 8 points)
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

        // 3. Logic
        SphereCollider col = sunRoot.AddComponent<SphereCollider>();
        col.isTrigger = true;
        col.radius = 2f;
        sunRoot.AddComponent<Sun>();
        
        sunRoot.transform.localScale = Vector3.one * 0.4f;

        PrefabUtility.SaveAsPrefabAsset(sunRoot, "Assets/Prefabs/Sun.prefab");
        GameObject.DestroyImmediate(sunRoot);
    }

    static void GenerateFullUI()
    {
        // Remove old simple UI if it exists
        GameObject oldCanvas = GameObject.Find("UI_Canvas");
        if (oldCanvas != null) GameObject.DestroyImmediate(oldCanvas);

        // Create Canvas
        GameObject canvasObj = new GameObject("UI_Canvas");
        Canvas canvas = canvasObj.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvasObj.AddComponent<CanvasScaler>();
        canvasObj.AddComponent<GraphicRaycaster>();

        GameUIManager uiManager = canvasObj.AddComponent<GameUIManager>();
        uiManager.plantCards = new Image[3];
        uiManager.cooldownOverlays = new Image[3];
        uiManager.costTexts = new Text[3];

        // Sun Text
        GameObject sunTextObj = new GameObject("SunText");
        sunTextObj.transform.SetParent(canvasObj.transform, false);
        Text sunText = sunTextObj.AddComponent<Text>();
        sunText.text = "Sun: 50";
        sunText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        sunText.fontSize = 40;
        sunText.color = Color.yellow;
        sunText.alignment = TextAnchor.UpperLeft;
        RectTransform stRt = sunText.GetComponent<RectTransform>();
        stRt.anchorMin = new Vector2(0, 1);
        stRt.anchorMax = new Vector2(0, 1);
        stRt.pivot = new Vector2(0, 1);
        stRt.anchoredPosition = new Vector2(20, -20);
        stRt.sizeDelta = new Vector2(300, 50);
        
        uiManager.sunText = sunText;

        // Plant Cards Panel
        GameObject panelObj = new GameObject("PlantCardsPanel");
        panelObj.transform.SetParent(canvasObj.transform, false);
        RectTransform panelRt = panelObj.AddComponent<RectTransform>();
        panelRt.anchorMin = new Vector2(0.5f, 0);
        panelRt.anchorMax = new Vector2(0.5f, 0);
        panelRt.pivot = new Vector2(0.5f, 0);
        panelRt.anchoredPosition = new Vector2(0, 20);
        panelRt.sizeDelta = new Vector2(400, 120);
        
        HorizontalLayoutGroup hlg = panelObj.AddComponent<HorizontalLayoutGroup>();
        hlg.childControlWidth = false;
        hlg.childControlHeight = false;
        hlg.childAlignment = TextAnchor.MiddleCenter;
        hlg.spacing = 20;

        string[] names = { "1. Peashooter", "2. Snow Pea", "3. Sunflower" };
        Color[] colors = { Color.green, Color.cyan, Color.yellow };

        for (int i = 0; i < 3; i++)
        {
            // Card BG
            GameObject cardObj = new GameObject("Card_" + i);
            cardObj.transform.SetParent(panelObj.transform, false);
            Image cardImg = cardObj.AddComponent<Image>();
            cardImg.color = colors[i];
            RectTransform cardRt = cardObj.GetComponent<RectTransform>();
            cardRt.sizeDelta = new Vector2(80, 100);
            uiManager.plantCards[i] = cardImg;

            // Name Text
            GameObject nameObj = new GameObject("Name");
            nameObj.transform.SetParent(cardObj.transform, false);
            Text nameText = nameObj.AddComponent<Text>();
            nameText.text = names[i];
            nameText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            nameText.fontSize = 14;
            nameText.color = Color.black;
            nameText.alignment = TextAnchor.UpperCenter;
            RectTransform nameRt = nameText.GetComponent<RectTransform>();
            nameRt.anchorMin = new Vector2(0, 1);
            nameRt.anchorMax = new Vector2(1, 1);
            nameRt.anchoredPosition = new Vector2(0, -10);
            nameRt.sizeDelta = new Vector2(0, 30);

            // Cost Text
            GameObject costObj = new GameObject("Cost");
            costObj.transform.SetParent(cardObj.transform, false);
            Text costText = costObj.AddComponent<Text>();
            costText.text = "100";
            costText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            costText.fontSize = 20;
            costText.color = Color.black;
            costText.alignment = TextAnchor.LowerCenter;
            RectTransform costRt = costText.GetComponent<RectTransform>();
            costRt.anchorMin = new Vector2(0, 0);
            costRt.anchorMax = new Vector2(1, 0);
            costRt.anchoredPosition = new Vector2(0, 10);
            costRt.sizeDelta = new Vector2(0, 30);
            uiManager.costTexts[i] = costText;

            // Cooldown Overlay (Dark Tint)
            GameObject cdObj = new GameObject("CooldownOverlay");
            cdObj.transform.SetParent(cardObj.transform, false);
            Image cdImg = cdObj.AddComponent<Image>();
            cdImg.color = new Color(0, 0, 0, 0.7f);
            cdImg.type = Image.Type.Filled;
            cdImg.fillMethod = Image.FillMethod.Radial360;
            cdImg.fillAmount = 0; // Starts at 0
            RectTransform cdRt = cdImg.GetComponent<RectTransform>();
            cdRt.anchorMin = Vector2.zero;
            cdRt.anchorMax = Vector2.one;
            cdRt.sizeDelta = Vector2.zero;
            uiManager.cooldownOverlays[i] = cdImg;
        }

        // Shovel Hint
        GameObject shovelObj = new GameObject("ShovelHint");
        shovelObj.transform.SetParent(canvasObj.transform, false);
        Text shovelText = shovelObj.AddComponent<Text>();
        shovelText.text = "Press '4' or 'R' to equip Shovel\nPress 'E' on a plant to remove it";
        shovelText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        shovelText.fontSize = 24;
        shovelText.color = Color.white;
        shovelText.alignment = TextAnchor.LowerRight;
        RectTransform shRt = shovelText.GetComponent<RectTransform>();
        shRt.anchorMin = new Vector2(1, 0);
        shRt.anchorMax = new Vector2(1, 0);
        shRt.pivot = new Vector2(1, 0);
        shRt.anchoredPosition = new Vector2(-20, 20);
        shRt.sizeDelta = new Vector2(400, 60);
    }
}
