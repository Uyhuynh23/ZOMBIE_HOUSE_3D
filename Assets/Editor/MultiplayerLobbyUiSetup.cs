using TMPro;
using UnityEditor;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

public static class MultiplayerLobbyUiSetup
{
    private const string MainMenuPath = "Assets/Scenes/GameScenes/MainMenu.unity";
    private const string SpriteRoot = "Assets/UI/MultiplayerLobby/Sprites/";
    private const string GeneratedRoot = "Assets/UI/MultiplayerLobby/Generated/";

    [MenuItem("Tools/Multiplayer/Rebuild Polished Lobby UI")]
    public static void Build()
    {
        ImportUiSprites();
        EditorSceneManager.OpenScene(MainMenuPath, OpenSceneMode.Single);

        Canvas canvas = Object.FindFirstObjectByType<Canvas>();
        MainMenuManager menuManager = Object.FindFirstObjectByType<MainMenuManager>();
        if (canvas == null || menuManager == null)
            throw new MissingReferenceException("MainMenu requires a Canvas and MainMenuManager.");

        CanvasScaler scaler = canvas.GetComponent<CanvasScaler>();
        if (scaler == null) scaler = canvas.gameObject.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
        scaler.matchWidthOrHeight = 0.5f;

        Transform previousLobby = canvas.transform.Find("LobbyRoot");
        if (previousLobby != null) Object.DestroyImmediate(previousLobby.gameObject);
        PreserveOldUi(canvas.transform, "MapSelectionPanel", "Deprecated_MapSelectionPanel");
        PreserveOldUi(canvas.transform, "Phase1MultiplayerPanel", "Deprecated_Phase1MultiplayerPanel");
        PreserveOldUi(canvas.transform, "MainPanel", "Deprecated_MainPanel");
        PreserveOldUi(canvas.transform, "GameTitle", "Deprecated_GameTitle");

        Transform previousMain = canvas.transform.Find("MainLandingRoot");
        if (previousMain != null) Object.DestroyImmediate(previousMain.gameObject);
        GameObject mainRoot = BuildMainLanding(canvas.transform, menuManager);

        GameObject root = UiObject("LobbyRoot", canvas.transform);
        Stretch(root.GetComponent<RectTransform>());
        MultiplayerMenuUI ui = root.AddComponent<MultiplayerMenuUI>();
        ui.mainMenuManager = menuManager;
        ui.roundScenes = new[] { "Map_Day", "Map_Cloudy", "Map_Night" };

        // 1. Lobby Background
        Image bg = CreateArt(root.transform, "Background", "main_menu_background.png",
            Vector2.zero, new Vector2(1920f, 1080f));
        bg.preserveAspect = false;

        // 2. Title Banner (now trimmed to true 9.2:1 aspect ratio, stretches large across top)
        CreateArt(root.transform, "TitleBanner", "stand_your_ground_zombie_siege_banner.png",
            new Vector2(0f, 435f), new Vector2(1680f, 185f));

        // 3. Left Map Selection Area
        GameObject mapArea = UiObject("MapSelectionArea", root.transform);
        SetRect(mapArea.GetComponent<RectTransform>(), new Vector2(-275f, 65f), new Vector2(1230f, 560f));
        Image mapArt = CreateArt(mapArea.transform, "MapSelectionPanelArt", "neon_green_map_selection_panel.png",
            Vector2.zero, new Vector2(1230f, 560f));
        mapArt.preserveAspect = false;

        string[] cardSprites =
        {
            "round_card_1_day.png",
            "round_card_2_cloudy.png",
            "round_card_3_night.png"
        };
        string[] thumbnailSprites = { "Map_Day.png", "Map_Cloudy.png", "Map_Night.png" };
        float[] cardX = { -390f, 0f, 390f };
        ui.roundButtons = new Button[3];
        ui.selectionHighlights = new GameObject[3];

        for (int i = 0; i < 3; i++)
        {
            GameObject card = UiObject($"Round{i + 1}Card", mapArea.transform);
            SetRect(card.GetComponent<RectTransform>(), new Vector2(cardX[i], -22f), new Vector2(370f, 474f));

            Image thumbnail = CreateImage(card.transform, "MapThumbnail",
                AssetDatabase.LoadAssetAtPath<Sprite>(GeneratedRoot + thumbnailSprites[i]));
            SetRect(thumbnail.rectTransform, new Vector2(0f, 84f), new Vector2(284f, 212f));
            thumbnail.preserveAspect = false;
            thumbnail.raycastTarget = false;

            CreateArt(card.transform, "Artwork", cardSprites[i], Vector2.zero, new Vector2(370f, 474f));

            Image selection = CreateArt(card.transform, "SelectionHighlight", "selected_map_card_border_overlay.png",
                Vector2.zero, new Vector2(370f, 474f));
            selection.raycastTarget = false;
            selection.gameObject.SetActive(i == 0);
            ui.selectionHighlights[i] = selection.gameObject;

            Button btn = CreateHitbox(card.transform, "ClickTarget", Vector2.zero, new Vector2(370f, 474f));
            ui.roundButtons[i] = btn;

            // Smooth mouse hover effect
            UIButtonHoverEffect hover = btn.gameObject.AddComponent<UIButtonHoverEffect>();
            hover.targetTransform = card.transform;
            hover.hoverScale = 1.04f;
            hover.pressScale = 0.96f;
        }

        // 4. Character Plank Button (centered in path)
        GameObject character = UiObject("CharacterButton", root.transform);
        SetRect(character.GetComponent<RectTransform>(), new Vector2(-80f, -315f), new Vector2(580f, 152f));
        CreateArt(character.transform, "Artwork", "character_customization_wooden_plank_ui.png",
            Vector2.zero, new Vector2(580f, 152f));
        ui.characterButton = CreateHitbox(character.transform, "ClickTarget", Vector2.zero, new Vector2(580f, 152f));
        UIButtonHoverEffect charHover = ui.characterButton.gameObject.AddComponent<UIButtonHoverEffect>();
        charHover.targetTransform = character.transform;
        charHover.hoverScale = 1.04f;
        charHover.pressScale = 0.96f;

        // 5. Back Button (under character)
        GameObject back = UiObject("BackButton", root.transform);
        SetRect(back.GetComponent<RectTransform>(), new Vector2(-80f, -440f), new Vector2(280f, 83f));
        CreateArt(back.transform, "Artwork", "rugged_cartoon_game_ui_back_button.png",
            Vector2.zero, new Vector2(280f, 83f));
        ui.backButton = CreateHitbox(back.transform, "ClickTarget", Vector2.zero, new Vector2(280f, 83f));
        UIButtonHoverEffect backHover = ui.backButton.gameObject.AddComponent<UIButtonHoverEffect>();
        backHover.targetTransform = back.transform;
        backHover.hoverScale = 1.06f;
        backHover.pressScale = 0.94f;

        // 6. Right Multiplayer Panel (using user provided background frame)
        GameObject multiplayer = UiObject("MultiplayerArea", root.transform);
        SetRect(multiplayer.GetComponent<RectTransform>(), new Vector2(635f, -65f), new Vector2(490f, 860f));

        Image panelArt = CreateArt(multiplayer.transform, "MultiplayerPanelArt", "mp_panel_base.png",
            Vector2.zero, new Vector2(490f, 860f));
        panelArt.preserveAspect = false;

        GameObject interaction = UiObject("InteractionLayer", multiplayer.transform);
        Stretch(interaction.GetComponent<RectTransform>());

        // Multiplayer Header
        CreateArt(interaction.transform, "MultiplayerHeader", "hdr_multi.png",
            new Vector2(0f, 355f), new Vector2(416f, 70f));

        // Solo Button
        ui.soloButton = CreateSpriteButton(interaction.transform, "SoloButton",
            "btn_solo.png", "btn_solo_hover.png", "btn_solo_pressed.png", "btn_solo_disabled.png",
            new Vector2(0f, 270f), new Vector2(416f, 82f));
        ui.soloButton.gameObject.AddComponent<UIButtonHoverEffect>();

        // Host Online Button
        ui.hostButton = CreateSpriteButton(interaction.transform, "HostOnlineButton",
            "btn_host.png", "btn_host_hover.png", "btn_host_pressed.png", "btn_host_disabled.png",
            new Vector2(0f, 172f), new Vector2(416f, 82f));
        ui.hostButton.gameObject.AddComponent<UIButtonHoverEffect>();

        // Join Room Header
        CreateArt(interaction.transform, "JoinRoomHeader", "hdr_join.png",
            new Vector2(-60f, 95f), new Vector2(290f, 57f));

        // Room Code Input & Join Button
        ui.roomCodeInput = CreateInput(interaction.transform, "RoomCodeInput",
            new Vector2(-63f, 32f), new Vector2(254f, 55f));

        ui.joinButton = CreateSpriteButton(interaction.transform, "JoinButton",
            "btn_join.png", "btn_join_hover.png", "btn_join_pressed.png", "btn_join_disabled.png",
            new Vector2(148f, 32f), new Vector2(118f, 55f));
        ui.joinButton.gameObject.AddComponent<UIButtonHoverEffect>();

        // Info Box
        CreateArt(interaction.transform, "RoomInfoFill", "info_box_bg.png",
            new Vector2(0f, -62f), new Vector2(416f, 96f));

        CreateText(interaction.transform, "RoomCodeLabel", "Room code:", 18f,
            new Vector2(-85f, -46f), new Vector2(170f, 26f), TextAlignmentOptions.MidlineLeft);
        CreateText(interaction.transform, "PlayerCountLabel", "Players:", 18f,
            new Vector2(-85f, -78f), new Vector2(170f, 26f), TextAlignmentOptions.MidlineLeft);
        ui.roomCodeText = CreateText(interaction.transform, "RoomCodeValue", "—", 19f,
            new Vector2(85f, -46f), new Vector2(170f, 26f), TextAlignmentOptions.MidlineLeft);
        ui.rosterText = CreateText(interaction.transform, "PlayerCountValue", "offline", 19f,
            new Vector2(85f, -78f), new Vector2(170f, 26f), TextAlignmentOptions.MidlineLeft);

        // Start Match Button
        ui.startButton = CreateSpriteButton(interaction.transform, "StartMatchButton",
            "btn_start.png", "btn_start_hover.png", "btn_start_pressed.png", "btn_start_disabled.png",
            new Vector2(0f, -172f), new Vector2(416f, 82f));
        ui.startButton.gameObject.AddComponent<UIButtonHoverEffect>();

        // Leave Button
        ui.leaveButton = CreateSpriteButton(interaction.transform, "LeaveButton",
            "btn_leave.png", "btn_leave_hover.png", "btn_leave_pressed.png", "btn_leave_disabled.png",
            new Vector2(0f, -270f), new Vector2(416f, 82f));
        ui.leaveButton.gameObject.AddComponent<UIButtonHoverEffect>();

        // Session Status
        ui.statusText = CreateText(root.transform, "SessionStatus", "Choose solo, host, or join a room.", 19f,
            new Vector2(635f, -510f), new Vector2(490f, 40f), TextAlignmentOptions.Center);

        // Save & wire menuManager
        menuManager.mainMenuPanel = mainRoot;
        menuManager.mapSelectionPanel = root;
        menuManager.gameTitle = null;
        mainRoot.SetActive(true);
        root.SetActive(false);

        EditorUtility.SetDirty(menuManager);
        EditorUtility.SetDirty(canvas);
        EditorSceneManager.MarkSceneDirty(canvas.gameObject.scene);
        EditorSceneManager.SaveScene(canvas.gameObject.scene);
        AssetDatabase.SaveAssets();
        Debug.Log("[LobbyUI] Rebuilt balanced lobby UI matching mockup with hover effects successfully!");
    }

    private static GameObject BuildMainLanding(Transform canvas, MainMenuManager menuManager)
    {
        GameObject root = UiObject("MainLandingRoot", canvas);
        Stretch(root.GetComponent<RectTransform>());
        Image background = CreateArt(root.transform, "Background", "main_menu_background.png", Vector2.zero, new Vector2(1920f, 1080f));
        background.preserveAspect = false;
        CreateArt(root.transform, "Title", "main_menu_title_only.png",
            new Vector2(0f, 430f), new Vector2(1450f, 632f));
        CreateArt(root.transform, "MainMenuLabel", "main_menu_label.png",
            new Vector2(0f, 318f), new Vector2(400f, 114f));

        GameObject play = UiObject("PlayGameButton", root.transform);
        SetRect(play.GetComponent<RectTransform>(), new Vector2(0f, 155f), new Vector2(650f, 217f));
        CreateArt(play.transform, "Artwork", "main_menu_play_game.png", Vector2.zero, new Vector2(650f, 217f));
        Button playButton = CreateHitbox(play.transform, "ClickTarget", Vector2.zero, new Vector2(580f, 125f));
        UIButtonHoverEffect playHover = playButton.gameObject.AddComponent<UIButtonHoverEffect>();
        playHover.targetTransform = play.transform;
        UnityEventTools.AddPersistentListener(playButton.onClick, menuManager.ShowMapSelection);

        GameObject instructions = UiObject("InstructionsButton", root.transform);
        SetRect(instructions.GetComponent<RectTransform>(), new Vector2(0f, 15f), new Vector2(650f, 217f));
        CreateArt(instructions.transform, "Artwork", "main_menu_instructions.png", Vector2.zero, new Vector2(650f, 217f));
        Button instructionButton = CreateHitbox(instructions.transform, "ClickTarget", Vector2.zero, new Vector2(580f, 125f));
        UIButtonHoverEffect instHover = instructionButton.gameObject.AddComponent<UIButtonHoverEffect>();
        instHover.targetTransform = instructions.transform;
        UnityEventTools.AddPersistentListener(instructionButton.onClick, menuManager.StartTutorial);

        CreateArt(root.transform, "QuickTips", "main_menu_quick_tips.png", new Vector2(0f, -245f), new Vector2(690f, 518f));
        return root;
    }

    private static void PreserveOldUi(Transform canvas, string oldName, string deprecatedName)
    {
        Transform old = canvas.Find(oldName);
        if (old == null) return;
        Transform existing = canvas.Find(deprecatedName);
        if (existing != null) Object.DestroyImmediate(existing.gameObject);
        old.name = deprecatedName;
        old.gameObject.SetActive(false);
    }

    private static void ImportUiSprites()
    {
        string[] folders = { SpriteRoot, GeneratedRoot };
        string[] guids = AssetDatabase.FindAssets("t:Texture2D", folders);
        foreach (string guid in guids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            TextureImporter importer = AssetImporter.GetAtPath(path) as TextureImporter;
            if (importer == null) continue;
            bool changed = false;
            if (importer.textureType != TextureImporterType.Sprite) { importer.textureType = TextureImporterType.Sprite; changed = true; }
            if (importer.spriteImportMode != SpriteImportMode.Single) { importer.spriteImportMode = SpriteImportMode.Single; changed = true; }
            if (importer.alphaSource != TextureImporterAlphaSource.FromInput) { importer.alphaSource = TextureImporterAlphaSource.FromInput; changed = true; }
            if (!importer.alphaIsTransparency) { importer.alphaIsTransparency = true; changed = true; }
            if (importer.mipmapEnabled) { importer.mipmapEnabled = false; changed = true; }
            if (importer.textureCompression != TextureImporterCompression.Uncompressed) { importer.textureCompression = TextureImporterCompression.Uncompressed; changed = true; }
            if (importer.maxTextureSize < 4096) { importer.maxTextureSize = 4096; changed = true; }
            if (changed) importer.SaveAndReimport();
        }
        AssetDatabase.Refresh();
    }

    private static Image CreateArt(Transform parent, string name, string filename, Vector2 position, Vector2 size)
    {
        Image image = CreateImage(parent, name, AssetDatabase.LoadAssetAtPath<Sprite>(SpriteRoot + filename));
        SetRect(image.rectTransform, position, size);
        image.preserveAspect = true;
        image.raycastTarget = false;
        return image;
    }

    private static Image CreateImage(Transform parent, string name, Sprite sprite)
    {
        GameObject result = UiObject(name, parent);
        Image image = result.AddComponent<Image>();
        image.sprite = sprite;
        image.color = Color.white;
        return image;
    }

    private static Button CreateHitbox(Transform parent, string name, Vector2 position, Vector2 size)
    {
        GameObject result = UiObject(name, parent);
        Image image = result.AddComponent<Image>();
        image.color = new Color(1f, 1f, 1f, 0.001f);
        image.raycastTarget = true;
        Button button = result.AddComponent<Button>();
        button.targetGraphic = image;
        button.transition = Selectable.Transition.None;
        SetRect(result.GetComponent<RectTransform>(), position, size);
        return button;
    }

    private static Button CreateSpriteButton(Transform parent, string name, string normal, string highlighted,
        string pressed, string disabled, Vector2 position, Vector2 size)
    {
        GameObject result = UiObject(name, parent);
        Image image = result.AddComponent<Image>();
        image.sprite = AssetDatabase.LoadAssetAtPath<Sprite>(SpriteRoot + normal);
        image.preserveAspect = false;
        image.raycastTarget = true;
        Button button = result.AddComponent<Button>();
        button.targetGraphic = image;
        if (!string.IsNullOrWhiteSpace(highlighted))
        {
            button.transition = Selectable.Transition.SpriteSwap;
            SpriteState states = button.spriteState;
            states.highlightedSprite = AssetDatabase.LoadAssetAtPath<Sprite>(SpriteRoot + highlighted);
            states.selectedSprite = states.highlightedSprite;
            states.pressedSprite = AssetDatabase.LoadAssetAtPath<Sprite>(SpriteRoot + pressed);
            states.disabledSprite = AssetDatabase.LoadAssetAtPath<Sprite>(SpriteRoot + disabled);
            button.spriteState = states;
        }
        else
        {
            button.transition = Selectable.Transition.ColorTint;
            ColorBlock colors = button.colors;
            colors.highlightedColor = new Color(1.08f, 1.08f, 1.08f, 1f);
            colors.pressedColor = new Color(0.82f, 0.9f, 0.82f, 1f);
            button.colors = colors;
        }
        SetRect(result.GetComponent<RectTransform>(), position, size);
        return button;
    }

    private static TMP_InputField CreateInput(Transform parent, string name, Vector2 position, Vector2 size)
    {
        GameObject root = UiObject(name, parent);
        SetRect(root.GetComponent<RectTransform>(), position, size);
        Image target = root.AddComponent<Image>();
        target.sprite = AssetDatabase.LoadAssetAtPath<Sprite>(SpriteRoot + "input_bg.png");
        target.type = Image.Type.Simple;

        GameObject viewport = UiObject("Text Area", root.transform);
        RectTransform viewportRect = viewport.GetComponent<RectTransform>();
        viewportRect.anchorMin = Vector2.zero;
        viewportRect.anchorMax = Vector2.one;
        viewportRect.offsetMin = new Vector2(16f, 4f);
        viewportRect.offsetMax = new Vector2(-16f, -4f);
        viewport.AddComponent<RectMask2D>();

        TMP_Text value = CreateText(viewport.transform, "Text", string.Empty, 20f, Vector2.zero,
            Vector2.zero, TextAlignmentOptions.MidlineLeft);
        Stretch(value.rectTransform);
        TMP_Text placeholder = CreateText(viewport.transform, "Placeholder", "Enter room code...", 17f,
            Vector2.zero, Vector2.zero, TextAlignmentOptions.MidlineLeft);
        Stretch(placeholder.rectTransform);
        placeholder.color = new Color(0.75f, 0.83f, 0.9f, 0.65f);

        TMP_InputField input = root.AddComponent<TMP_InputField>();
        input.targetGraphic = target;
        input.textViewport = viewportRect;
        input.textComponent = value;
        input.placeholder = placeholder;
        input.characterLimit = 12;
        input.contentType = TMP_InputField.ContentType.Alphanumeric;
        input.lineType = TMP_InputField.LineType.SingleLine;
        input.transition = Selectable.Transition.ColorTint;
        return input;
    }

    private static TMP_Text CreateText(Transform parent, string name, string value, float fontSize,
        Vector2 position, Vector2 size, TextAlignmentOptions alignment)
    {
        GameObject result = UiObject(name, parent);
        TextMeshProUGUI text = result.AddComponent<TextMeshProUGUI>();
        text.text = value;
        text.font = TMP_Settings.defaultFontAsset;
        text.fontSize = fontSize;
        text.color = Color.white;
        text.alignment = alignment;
        text.enableWordWrapping = true;
        text.raycastTarget = false;
        SetRect(result.GetComponent<RectTransform>(), position, size);
        return text;
    }

    private static GameObject UiObject(string name, Transform parent)
    {
        GameObject result = new GameObject(name, typeof(RectTransform));
        result.layer = LayerMask.NameToLayer("UI");
        result.transform.SetParent(parent, false);
        return result;
    }

    private static void SetRect(RectTransform rect, Vector2 position, Vector2 size)
    {
        rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = position;
        rect.sizeDelta = size;
        rect.localScale = Vector3.one;
    }

    private static void Stretch(RectTransform rect)
    {
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
        rect.localScale = Vector3.one;
    }
}
