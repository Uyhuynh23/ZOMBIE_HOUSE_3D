#if UNITY_EDITOR
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// Editor utility to construct and wire the MapSelectionPanel inside MainMenu.unity,
/// configure the 3 main buttons (Play Game, Instruction, Character), and wire all navigation scripts.
/// Accessible via menu: Tools -> Setup Main Menu Flow
/// </summary>
public static class MapSelectionUIBuilder
{
    private const string MainMenuScenePath = "Assets/Scenes/GameScenes/MainMenu.unity";
    private const string ButtonUIPath = "Assets/Textures/ButtonUI.png";
    private const string ShlopFontPath = "Assets/Fonts/shlop/shlop rg.otf";

    private const string MapDayThumbPath = "Assets/UI/MapThumbnails/Map_Day.png";
    private const string MapCloudyThumbPath = "Assets/UI/MapThumbnails/Map_Cloudy.png";
    private const string MapNightThumbPath = "Assets/UI/MapThumbnails/Map_Night.png";

    [MenuItem("Zombie House/Setup Main Menu Flow")]
    public static void BuildAndWireMainMenu()
    {
        Debug.Log("[MapSelectionUIBuilder] Starting Main Menu setup...");

        // 1. Open MainMenu scene
        Scene scene = EditorSceneManager.OpenScene(MainMenuScenePath, OpenSceneMode.Single);
        if (!scene.IsValid())
        {
            Debug.LogError($"[MapSelectionUIBuilder] Unable to open scene: {MainMenuScenePath}");
            return;
        }

        // 2. Load common visual assets
        Sprite btnSprite = AssetDatabase.LoadAssetAtPath<Sprite>(ButtonUIPath);
        Font shlopFont = AssetDatabase.LoadAssetAtPath<Font>(ShlopFontPath);
        Sprite daySprite = AssetDatabase.LoadAssetAtPath<Sprite>(MapDayThumbPath);
        Sprite cloudySprite = AssetDatabase.LoadAssetAtPath<Sprite>(MapCloudyThumbPath);
        Sprite nightSprite = AssetDatabase.LoadAssetAtPath<Sprite>(MapNightThumbPath);

        // 3. Find MainMenuManager
        MainMenuManager manager = Object.FindFirstObjectByType<MainMenuManager>();
        if (manager == null)
        {
            Debug.LogError("[MapSelectionUIBuilder] MainMenuManager not found in scene!");
            return;
        }

        // 4. Find Canvas
        Canvas canvas = Object.FindFirstObjectByType<Canvas>();
        if (canvas == null)
        {
            Debug.LogError("[MapSelectionUIBuilder] Canvas not found in scene!");
            return;
        }

        // 5. Wire the 3 buttons on MainPanel
        Transform mainPanelTrans = canvas.transform.Find("MainPanel");
        if (mainPanelTrans == null)
        {
            Debug.LogError("[MapSelectionUIBuilder] MainPanel not found under Canvas!");
            return;
        }

        WireMainPanelButtons(mainPanelTrans);

        // 6. Build or refresh MapSelectionPanel
        GameObject mapSelectionPanel = SetupMapSelectionPanel(canvas.transform, btnSprite, shlopFont, daySprite, cloudySprite, nightSprite);

        // 7. Update MainMenuManager references
        manager.mainMenuPanel = mainPanelTrans.gameObject;
        manager.mapSelectionPanel = mapSelectionPanel;
        Transform charPanelTrans = canvas.transform.Find("CharPanel");
        if (charPanelTrans != null)
        {
            manager.characterSettingPanel = charPanelTrans.gameObject;
            charPanelTrans.gameObject.SetActive(false);
        }

        mainPanelTrans.gameObject.SetActive(true);
        mapSelectionPanel.SetActive(false);

        // 8. Mark dirty & save scene
        EditorUtility.SetDirty(manager);
        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);

        Debug.Log("[MapSelectionUIBuilder] Main Menu flow and MapSelectionPanel setup completed successfully!");
    }

    private static void WireMainPanelButtons(Transform mainPanel)
    {
        // Loop through children of MainPanel to find the 3 buttons by their text content
        for (int i = 0; i < mainPanel.childCount; i++)
        {
            Transform child = mainPanel.GetChild(i);
            Text textComp = child.GetComponentInChildren<Text>();
            if (textComp == null) continue;

            string label = textComp.text.Trim().ToUpperInvariant();
            Debug.Log($"[MapSelectionUIBuilder] Found button: '{child.name}' with label: '{label}'");

            if (label.Contains("PLAY"))
            {
                // Remove old RoundButtonHandler if present
                RoundButtonHandler oldRBH = child.GetComponent<RoundButtonHandler>();
                if (oldRBH != null) Object.DestroyImmediate(oldRBH);

                // Ensure PlayGameButtonHandler is attached
                if (child.GetComponent<PlayGameButtonHandler>() == null)
                {
                    child.gameObject.AddComponent<PlayGameButtonHandler>();
                }
                child.name = "Btn_PlayGame";
                Debug.Log("[MapSelectionUIBuilder] Wired PLAY GAME button.");
            }
            else if (label.Contains("INSTRUCTION"))
            {
                // Remove old RoundButtonHandler if present
                RoundButtonHandler oldRBH = child.GetComponent<RoundButtonHandler>();
                if (oldRBH != null) Object.DestroyImmediate(oldRBH);

                // Ensure InstructionButtonHandler is attached
                if (child.GetComponent<InstructionButtonHandler>() == null)
                {
                    child.gameObject.AddComponent<InstructionButtonHandler>();
                }
                child.name = "Btn_Instruction";
                Debug.Log("[MapSelectionUIBuilder] Wired INSTRUCTION button.");
            }
            else if (label.Contains("CHARACTER"))
            {
                // Ensure CharacterButtonHandler is attached
                if (child.GetComponent<CharacterButtonHandler>() == null)
                {
                    child.gameObject.AddComponent<CharacterButtonHandler>();
                }
                child.name = "Btn_Character";
                Debug.Log("[MapSelectionUIBuilder] Wired CHARACTER button.");
            }
        }
    }

    private static GameObject SetupMapSelectionPanel(Transform canvasTrans, Sprite btnSprite, Font font, Sprite daySprite, Sprite cloudySprite, Sprite nightSprite)
    {
        // Check if existing panel exists, delete to recreate cleanly
        Transform existing = canvasTrans.Find("MapSelectionPanel");
        if (existing != null)
        {
            Object.DestroyImmediate(existing.gameObject);
        }

        // Create MapSelectionPanel root
        GameObject panelObj = new GameObject("MapSelectionPanel", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        panelObj.transform.SetParent(canvasTrans, false);

        // Put it right after MainPanel in sibling order
        Transform mainPanel = canvasTrans.Find("MainPanel");
        if (mainPanel != null)
        {
            panelObj.transform.SetSiblingIndex(mainPanel.GetSiblingIndex() + 1);
        }

        RectTransform panelRect = panelObj.GetComponent<RectTransform>();
        panelRect.anchorMin = Vector2.zero;
        panelRect.anchorMax = Vector2.one;
        panelRect.offsetMin = Vector2.zero;
        panelRect.offsetMax = Vector2.zero;

        // Semi-transparent dark vignette background
        Image panelBg = panelObj.GetComponent<Image>();
        panelBg.color = new Color(0.04f, 0.05f, 0.08f, 0.82f);
        panelBg.raycastTarget = true;

        // Title Header
        GameObject titleObj = new GameObject("Title", typeof(RectTransform), typeof(CanvasRenderer), typeof(Text));
        titleObj.transform.SetParent(panelObj.transform, false);
        RectTransform titleRect = titleObj.GetComponent<RectTransform>();
        titleRect.anchorMin = new Vector2(0.5f, 1f);
        titleRect.anchorMax = new Vector2(0.5f, 1f);
        titleRect.pivot = new Vector2(0.5f, 1f);
        titleRect.anchoredPosition = new Vector2(0f, -40f);
        titleRect.sizeDelta = new Vector2(800f, 75f);

        Text titleText = titleObj.GetComponent<Text>();
        titleText.text = "SELECT MAP";
        if (font != null) titleText.font = font;
        titleText.fontSize = 58;
        titleText.fontStyle = FontStyle.Bold;
        titleText.alignment = TextAnchor.MiddleCenter;
        titleText.color = new Color(1f, 0.85f, 0.15f, 1f);

        // Cards Container
        GameObject containerObj = new GameObject("CardsContainer", typeof(RectTransform), typeof(HorizontalLayoutGroup));
        containerObj.transform.SetParent(panelObj.transform, false);
        RectTransform contRect = containerObj.GetComponent<RectTransform>();
        contRect.anchorMin = new Vector2(0.5f, 0.5f);
        contRect.anchorMax = new Vector2(0.5f, 0.5f);
        contRect.pivot = new Vector2(0.5f, 0.5f);
        contRect.anchoredPosition = new Vector2(0f, 15f);
        contRect.sizeDelta = new Vector2(1100f, 420f);

        HorizontalLayoutGroup hGroup = containerObj.GetComponent<HorizontalLayoutGroup>();
        hGroup.spacing = 35f;
        hGroup.childAlignment = TextAnchor.MiddleCenter;
        hGroup.childForceExpandWidth = false;
        hGroup.childForceExpandHeight = false;
        hGroup.childControlWidth = false;
        hGroup.childControlHeight = false;

        // Build 3 Cards
        CreateMapCard(containerObj.transform, 1, "ROUND 1", "DAY MAP", daySprite, font, btnSprite);
        CreateMapCard(containerObj.transform, 2, "ROUND 2", "CLOUDY MAP", cloudySprite, font, btnSprite);
        CreateMapCard(containerObj.transform, 3, "ROUND 3", "NIGHT MAP", nightSprite, font, btnSprite);

        // Back Button
        GameObject backBtnObj = new GameObject("Btn_Back", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button), typeof(BackToMainMenuButtonHandler));
        backBtnObj.transform.SetParent(panelObj.transform, false);
        RectTransform backRect = backBtnObj.GetComponent<RectTransform>();
        backRect.anchorMin = new Vector2(0.5f, 0f);
        backRect.anchorMax = new Vector2(0.5f, 0f);
        backRect.pivot = new Vector2(0.5f, 0f);
        backRect.anchoredPosition = new Vector2(0f, 40f);
        backRect.sizeDelta = new Vector2(240f, 65f);

        Image backImg = backBtnObj.GetComponent<Image>();
        if (btnSprite != null)
        {
            backImg.sprite = btnSprite;
            backImg.type = Image.Type.Sliced;
        }
        backImg.color = Color.white;

        Button backBtn = backBtnObj.GetComponent<Button>();
        ColorBlock colors = backBtn.colors;
        colors.normalColor = Color.white;
        colors.highlightedColor = new Color(1f, 0.95f, 0.8f, 1f);
        colors.pressedColor = new Color(0.8f, 0.8f, 0.8f, 1f);
        backBtn.colors = colors;

        GameObject backTextObj = new GameObject("Text", typeof(RectTransform), typeof(CanvasRenderer), typeof(Text));
        backTextObj.transform.SetParent(backBtnObj.transform, false);
        RectTransform backTextRect = backTextObj.GetComponent<RectTransform>();
        backTextRect.anchorMin = Vector2.zero;
        backTextRect.anchorMax = Vector2.one;
        backTextRect.offsetMin = Vector2.zero;
        backTextRect.offsetMax = Vector2.zero;

        Text backText = backTextObj.GetComponent<Text>();
        backText.text = "BACK";
        if (font != null) backText.font = font;
        backText.fontSize = 42;
        backText.alignment = TextAnchor.MiddleCenter;
        backText.color = Color.white;

        return panelObj;
    }

    private static GameObject CreateMapCard(Transform parent, int roundNum, string roundTitle, string subName, Sprite thumbSprite, Font font, Sprite btnSprite)
    {
        // Root card object
        GameObject cardObj = new GameObject($"MapCard_{roundNum}", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(MapCardUI));
        cardObj.transform.SetParent(parent, false);

        RectTransform cardRect = cardObj.GetComponent<RectTransform>();
        cardRect.sizeDelta = new Vector2(330f, 390f);

        Image cardBg = cardObj.GetComponent<Image>();
        cardBg.color = new Color(0.12f, 0.14f, 0.18f, 0.95f);
        cardBg.raycastTarget = true;

        // Outer Frame / Glow Outline (child)
        GameObject outlineObj = new GameObject("OutlineGlow", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        outlineObj.transform.SetParent(cardObj.transform, false);
        RectTransform outRect = outlineObj.GetComponent<RectTransform>();
        outRect.anchorMin = Vector2.zero;
        outRect.anchorMax = Vector2.one;
        outRect.offsetMin = new Vector2(-4f, -4f);
        outRect.offsetMax = new Vector2(4f, 4f);
        outlineObj.transform.SetAsFirstSibling(); // behind cardBg

        Image outlineImg = outlineObj.GetComponent<Image>();
        outlineImg.color = new Color(1f, 0.85f, 0.2f, 0f);
        outlineImg.raycastTarget = false;

        // Map Thumbnail Image
        GameObject thumbObj = new GameObject("Thumbnail", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        thumbObj.transform.SetParent(cardObj.transform, false);
        RectTransform thumbRect = thumbObj.GetComponent<RectTransform>();
        thumbRect.anchorMin = new Vector2(0.5f, 1f);
        thumbRect.anchorMax = new Vector2(0.5f, 1f);
        thumbRect.pivot = new Vector2(0.5f, 1f);
        thumbRect.anchoredPosition = new Vector2(0f, -14f);
        thumbRect.sizeDelta = new Vector2(302f, 185f);

        Image thumbImg = thumbObj.GetComponent<Image>();
        if (thumbSprite != null)
        {
            thumbImg.sprite = thumbSprite;
            thumbImg.preserveAspect = false;
        }
        thumbImg.color = Color.white;
        thumbImg.raycastTarget = false;

        // Details Container
        GameObject detailsObj = new GameObject("Details", typeof(RectTransform));
        detailsObj.transform.SetParent(cardObj.transform, false);
        RectTransform detailsRect = detailsObj.GetComponent<RectTransform>();
        detailsRect.anchorMin = new Vector2(0.5f, 0f);
        detailsRect.anchorMax = new Vector2(0.5f, 0f);
        detailsRect.pivot = new Vector2(0.5f, 0f);
        detailsRect.anchoredPosition = new Vector2(0f, 15f);
        detailsRect.sizeDelta = new Vector2(302f, 170f);

        // Title Text
        GameObject titleObj = new GameObject("RoundTitle", typeof(RectTransform), typeof(CanvasRenderer), typeof(Text));
        titleObj.transform.SetParent(detailsObj.transform, false);
        RectTransform tRect = titleObj.GetComponent<RectTransform>();
        tRect.anchorMin = new Vector2(0.5f, 1f);
        tRect.anchorMax = new Vector2(0.5f, 1f);
        tRect.pivot = new Vector2(0.5f, 1f);
        tRect.anchoredPosition = new Vector2(0f, -5f);
        tRect.sizeDelta = new Vector2(290f, 45f);

        Text tText = titleObj.GetComponent<Text>();
        tText.text = roundTitle;
        if (font != null) tText.font = font;
        tText.fontSize = 36;
        tText.alignment = TextAnchor.MiddleCenter;
        tText.color = Color.white;

        // Subtitle Text (Map Name)
        GameObject subObj = new GameObject("MapSubtitle", typeof(RectTransform), typeof(CanvasRenderer), typeof(Text));
        subObj.transform.SetParent(detailsObj.transform, false);
        RectTransform sRect = subObj.GetComponent<RectTransform>();
        sRect.anchorMin = new Vector2(0.5f, 1f);
        sRect.anchorMax = new Vector2(0.5f, 1f);
        sRect.pivot = new Vector2(0.5f, 1f);
        sRect.anchoredPosition = new Vector2(0f, -50f);
        sRect.sizeDelta = new Vector2(290f, 30f);

        Text sText = subObj.GetComponent<Text>();
        sText.text = subName;
        sText.fontSize = 18;
        sText.fontStyle = FontStyle.Bold;
        sText.alignment = TextAnchor.MiddleCenter;
        sText.color = new Color(0.95f, 0.8f, 0.35f, 1f);

        // "PLAY" Action indicator / mini button
        GameObject playIndicator = new GameObject("PlayIndicator", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        playIndicator.transform.SetParent(detailsObj.transform, false);
        RectTransform pRect = playIndicator.GetComponent<RectTransform>();
        pRect.anchorMin = new Vector2(0.5f, 0f);
        pRect.anchorMax = new Vector2(0.5f, 0f);
        pRect.pivot = new Vector2(0.5f, 0f);
        pRect.anchoredPosition = new Vector2(0f, 10f);
        pRect.sizeDelta = new Vector2(200f, 44f);

        Image pImg = playIndicator.GetComponent<Image>();
        if (btnSprite != null)
        {
            pImg.sprite = btnSprite;
            pImg.type = Image.Type.Sliced;
        }
        pImg.color = new Color(1f, 1f, 1f, 0.95f);
        pImg.raycastTarget = false;

        GameObject pTextObj = new GameObject("Text", typeof(RectTransform), typeof(CanvasRenderer), typeof(Text));
        pTextObj.transform.SetParent(playIndicator.transform, false);
        RectTransform ptRect = pTextObj.GetComponent<RectTransform>();
        ptRect.anchorMin = Vector2.zero;
        ptRect.anchorMax = Vector2.one;
        ptRect.offsetMin = Vector2.zero;
        ptRect.offsetMax = Vector2.zero;

        Text pText = pTextObj.GetComponent<Text>();
        pText.text = "CHOOSE";
        if (font != null) pText.font = font;
        pText.fontSize = 26;
        pText.alignment = TextAnchor.MiddleCenter;
        pText.color = Color.white;

        // Configure MapCardUI component
        MapCardUI cardUI = cardObj.GetComponent<MapCardUI>();
        cardUI.roundNumber = roundNum;
        cardUI.mapDisplayName = $"{roundTitle}: {subName}";
        cardUI.mapThumbnail = thumbImg;
        cardUI.mapNameText = tText;
        cardUI.cardFrame = cardBg;
        cardUI.highlightOutline = outlineImg;

        return cardObj;
    }
}
#endif
