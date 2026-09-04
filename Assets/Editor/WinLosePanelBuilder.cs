#if UNITY_EDITOR
using UnityEngine;
using UnityEngine.UI;
using UnityEditor;
using UnityEditor.SceneManagement;
using TMPro;

/// <summary>
/// Editor tool: Zombies > Build Win/Lose Panel
/// Creates the WinLosePanel prefab and wires it into all map scenes.
/// </summary>
public static class WinLosePanelBuilder
{
    // ── Asset GUIDs ──────────────────────────────────────────────────────────
    // ButtonUI.png  (Textures/ButtonUI.png)
    private const string ButtonUI_GUID = "045464f22c272f34abbdbd2f54a3b6d4";
    // Shlop RG SDF  (Fonts/shlop/shlop rg SDF.asset)
    private const string ShlopSDF_GUID  = "864c693d0ed5d40918adf71a832dea40";

    private static readonly string PrefabSavePath =
        "Assets/Prefabs/UI/WinLosePanel.prefab";

    private static readonly string[] MapScenes = new[]
    {
        "Assets/Scenes/GameScenes/Map_Day.unity",
        "Assets/Scenes/GameScenes/Map_Cloudy.unity",
        "Assets/Scenes/GameScenes/Map_Night.unity",
    };

    // ─────────────────────────────────────────────────────────────────────────
    [MenuItem("Zombies/Build Win-Lose Panel (All Maps)")]
    public static void BuildAndInjectAll()
    {
        // 1. Load assets
        Sprite btnSprite = LoadByGUID<Sprite>(ButtonUI_GUID, "ButtonUI");
        TMP_FontAsset font = LoadByGUID<TMP_FontAsset>(ShlopSDF_GUID, "shlop rg SDF");

        if (btnSprite == null || font == null)
        {
            Debug.LogError("[WinLosePanelBuilder] Missing assets — aborting.");
            return;
        }

        // 2. Create prefab
        GameObject prefab = BuildPrefab(btnSprite, font);
        if (prefab == null) return;

        // 3. Inject into each map scene
        foreach (string scenePath in MapScenes)
            InjectIntoScene(scenePath, prefab);

        EditorUtility.DisplayDialog(
            "Done!",
            "WinLosePanel prefab created and injected into all map scenes.\n\n" +
            "Prefab: " + PrefabSavePath + "\n\n" +
            "Assign winPanel/losePanel references in GameUIManager for each map.",
            "OK");
    }

    // ─────────────────────────────────────────────────────────────────────────
    private static GameObject BuildPrefab(Sprite btnSprite, TMP_FontAsset font)
    {
        // Ensure output directory exists
        System.IO.Directory.CreateDirectory(
            System.IO.Path.GetDirectoryName(
                System.IO.Path.Combine(Application.dataPath, "../", PrefabSavePath)));
        AssetDatabase.Refresh();

        // ── Root ─────────────────────────────────────────────────────────────
        var root = new GameObject("WinLosePanel");
        AddFullStretchCanvas(root);

        // Semi-transparent dark overlay on root
        var bgImg = root.AddComponent<Image>();
        bgImg.color = new Color(0f, 0f, 0f, 0.7f);
        bgImg.raycastTarget = true;

        var rootRect = root.GetComponent<RectTransform>();
        SetFullStretch(rootRect);

        // ── WIN sub-panel ─────────────────────────────────────────────────────
        var win = BuildResultPanel(root.transform, "WinPanel",
            "CHUC MUNG\nDA THANG!",
            new Color(1f, 0.85f, 0.1f),          // gold title
            btnSprite, font,
            new (string label, string id)[]
            {
                ("CHOI LAI", "Btn_WinRestart"),
                ("TIEP THEO", "Btn_Next"),
                ("VE MENU",  "Btn_WinHome"),
            });

        // ── LOSE sub-panel ────────────────────────────────────────────────────
        var lose = BuildResultPanel(root.transform, "LosePanel",
            "DA THUA!",
            new Color(1f, 0.25f, 0.15f),          // red title
            btnSprite, font,
            new (string label, string id)[]
            {
                ("CHOI LAI", "Btn_LoseRestart"),
                ("VE MENU",  "Btn_LoseHome"),
            });

        // ── WinLosePanelUI component ──────────────────────────────────────────
        var ui = root.AddComponent<WinLosePanelUI>();
        ui.winPanel  = win;
        ui.losePanel = lose;

        // Wire WinPanel references
        ui.winTitleText   = win.transform.Find("Title")?.GetComponent<TextMeshProUGUI>();
        ui.btnRestart_Win = win.transform.Find("Buttons/Btn_WinRestart")?.GetComponent<Button>();
        ui.btnNext        = win.transform.Find("Buttons/Btn_Next")?.GetComponent<Button>();
        ui.btnHome_Win    = win.transform.Find("Buttons/Btn_WinHome")?.GetComponent<Button>();

        // Wire LosePanel references
        ui.loseTitleText   = lose.transform.Find("Title")?.GetComponent<TextMeshProUGUI>();
        ui.btnRestart_Lose = lose.transform.Find("Buttons/Btn_LoseRestart")?.GetComponent<Button>();
        ui.btnHome_Lose    = lose.transform.Find("Buttons/Btn_LoseHome")?.GetComponent<Button>();

        // Both sub-panels start hidden
        win.SetActive(false);
        lose.SetActive(false);

        // ── Save as prefab ────────────────────────────────────────────────────
        GameObject savedPrefab = PrefabUtility.SaveAsPrefabAsset(root, PrefabSavePath);
        Object.DestroyImmediate(root);

        Debug.Log($"[WinLosePanelBuilder] Prefab saved: {PrefabSavePath}");
        return savedPrefab;
    }

    // ── Builds a centred panel with title + row of buttons ────────────────────
    private static GameObject BuildResultPanel(
        Transform parent,
        string panelName,
        string titleText,
        Color titleColor,
        Sprite btnSprite,
        TMP_FontAsset font,
        (string label, string id)[] buttons)
    {
        // Panel background card
        var panel = new GameObject(panelName);
        panel.transform.SetParent(parent, false);

        var panelRect = panel.AddComponent<RectTransform>();
        panelRect.anchorMin = new Vector2(0.5f, 0.5f);
        panelRect.anchorMax = new Vector2(0.5f, 0.5f);
        panelRect.pivot     = new Vector2(0.5f, 0.5f);
        panelRect.sizeDelta = new Vector2(700f, 450f);
        panelRect.anchoredPosition = Vector2.zero;

        // Rounded dark card bg
        var cardBg = panel.AddComponent<Image>();
        cardBg.color = new Color(0.08f, 0.05f, 0.02f, 0.92f);

        // ── Title ─────────────────────────────────────────────────────────────
        var titleGO = new GameObject("Title");
        titleGO.transform.SetParent(panel.transform, false);
        var titleRect = titleGO.AddComponent<RectTransform>();
        titleRect.anchorMin = new Vector2(0.05f, 0.55f);
        titleRect.anchorMax = new Vector2(0.95f, 0.95f);
        titleRect.offsetMin = titleRect.offsetMax = Vector2.zero;

        var title = titleGO.AddComponent<TextMeshProUGUI>();
        title.text          = titleText;
        title.font          = font;
        title.fontSize      = 72f;
        title.fontStyle     = FontStyles.Bold;
        title.color         = titleColor;
        title.alignment     = TextAlignmentOptions.Center;
        title.enableAutoSizing = true;
        title.fontSizeMin   = 24f;
        title.fontSizeMax   = 72f;

        // Drop shadow
        title.fontMaterial  = new Material(title.fontMaterial);
        title.fontMaterial.EnableKeyword("UNDERLAY_ON");
        title.fontMaterial.SetFloat("_UnderlayOffsetX",  0.5f);
        title.fontMaterial.SetFloat("_UnderlayOffsetY", -0.5f);
        title.fontMaterial.SetFloat("_UnderlaySoftness", 0.3f);
        title.fontMaterial.SetColor("_UnderlayColor",    new Color(0f, 0f, 0f, 0.8f));

        // ── Button row ────────────────────────────────────────────────────────
        var btnsGO = new GameObject("Buttons");
        btnsGO.transform.SetParent(panel.transform, false);
        var btnsRect = btnsGO.AddComponent<RectTransform>();
        btnsRect.anchorMin = new Vector2(0.05f, 0.05f);
        btnsRect.anchorMax = new Vector2(0.95f, 0.48f);
        btnsRect.offsetMin = btnsRect.offsetMax = Vector2.zero;

        var hGroup = btnsGO.AddComponent<HorizontalLayoutGroup>();
        hGroup.spacing            = 20f;
        hGroup.childAlignment     = TextAnchor.MiddleCenter;
        hGroup.childForceExpandWidth  = true;
        hGroup.childForceExpandHeight = true;
        hGroup.padding = new RectOffset(10, 10, 0, 0);

        foreach (var (label, id) in buttons)
            MakeButton(btnsGO.transform, id, label, btnSprite, font);

        return panel;
    }

    // ── Creates one wooden button ─────────────────────────────────────────────
    private static Button MakeButton(Transform parent, string id, string label,
                                     Sprite sprite, TMP_FontAsset font)
    {
        var go = new GameObject(id);
        go.transform.SetParent(parent, false);

        var img = go.AddComponent<Image>();
        img.sprite = sprite;
        img.type   = Image.Type.Sliced;
        img.color  = Color.white;

        var btn = go.AddComponent<Button>();
        // Colour tint transitions
        var colors = btn.colors;
        colors.highlightedColor = new Color(1f, 1f, 0.7f);
        colors.pressedColor     = new Color(0.7f, 0.55f, 0.2f);
        btn.colors = colors;
        btn.targetGraphic = img;

        // Label
        var textGO   = new GameObject("Label");
        textGO.transform.SetParent(go.transform, false);
        var textRect = textGO.AddComponent<RectTransform>();
        SetFullStretch(textRect);

        var tmp = textGO.AddComponent<TextMeshProUGUI>();
        tmp.text      = label;
        tmp.font      = font;
        tmp.fontSize  = 32f;
        tmp.fontStyle = FontStyles.Bold;
        tmp.color     = new Color(0.98f, 0.92f, 0.72f);  // warm parchment
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.enableAutoSizing = true;
        tmp.fontSizeMin = 14f;
        tmp.fontSizeMax = 32f;

        return btn;
    }

    // ── Injects a prefab instance into a scene ────────────────────────────────
    private static void InjectIntoScene(string scenePath, GameObject prefab)
    {
        bool wasOpen = false;
        UnityEngine.SceneManagement.Scene scene =
            EditorSceneManager.GetSceneByPath(scenePath);

        if (!scene.IsValid() || !scene.isLoaded)
        {
            scene  = EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Additive);
            wasOpen = false;
        }
        else wasOpen = true;

        // Remove any existing WinLosePanel
        foreach (var go in scene.GetRootGameObjects())
        {
            if (go.name == "WinLosePanel")
            {
                Object.DestroyImmediate(go);
                break;
            }
        }

        // Instantiate prefab into scene
        GameObject instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab, scene);
        instance.name = "WinLosePanel";

        // Auto-link to GameUIManager if present
        var guim = Object.FindFirstObjectByType<GameUIManager>();
        if (guim != null)
        {
            var panelUI = instance.GetComponent<WinLosePanelUI>();
            // winPanel / losePanel live inside the prefab, GameUIManager only needs
            // the root panel objects – these map to the old winPanel/losePanel fields.
            if (panelUI != null)
            {
                guim.winPanel  = panelUI.winPanel;
                guim.losePanel = panelUI.losePanel;
                EditorUtility.SetDirty(guim);
            }
            Debug.Log($"[WinLosePanelBuilder] Linked to GameUIManager in {scenePath}");
        }
        else
        {
            Debug.LogWarning($"[WinLosePanelBuilder] No GameUIManager found in {scenePath} — link manually.");
        }

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);

        if (!wasOpen)
            EditorSceneManager.CloseScene(scene, true);

        Debug.Log($"[WinLosePanelBuilder] Injected into {scenePath}");
    }

    // ── Helpers ───────────────────────────────────────────────────────────────
    private static void AddFullStretchCanvas(GameObject go)
    {
        var canvas          = go.AddComponent<Canvas>();
        canvas.renderMode   = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 50;

        var scaler          = go.AddComponent<CanvasScaler>();
        scaler.uiScaleMode  = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        scaler.matchWidthOrHeight  = 0.5f;

        go.AddComponent<GraphicRaycaster>();
    }

    private static void SetFullStretch(RectTransform rt)
    {
        rt.anchorMin    = Vector2.zero;
        rt.anchorMax    = Vector2.one;
        rt.offsetMin    = Vector2.zero;
        rt.offsetMax    = Vector2.zero;
    }

    private static T LoadByGUID<T>(string guid, string label) where T : Object
    {
        string path = AssetDatabase.GUIDToAssetPath(guid);
        if (string.IsNullOrEmpty(path))
        {
            Debug.LogError($"[WinLosePanelBuilder] Cannot resolve GUID for {label}: {guid}");
            return null;
        }
        T asset = AssetDatabase.LoadAssetAtPath<T>(path);
        if (asset == null)
            Debug.LogError($"[WinLosePanelBuilder] Loaded null for {label} at {path}");
        return asset;
    }
}
#endif
