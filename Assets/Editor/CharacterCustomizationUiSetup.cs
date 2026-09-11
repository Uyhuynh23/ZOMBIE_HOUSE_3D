using UnityEngine;
using UnityEngine.UI;
using UnityEditor;
using UnityEditor.SceneManagement;

public static class CharacterCustomizationUiSetup
{
    private const string ScenePath = "Assets/Scenes/GameScenes/MainMenu.unity";
    private const string SpriteDir = "Assets/UI/CharacterCustomization/Sprites/";

    [MenuItem("Tools/Customization/Rebuild Character Customization UI")]
    public static void Build()
    {
        var scene = EditorSceneManager.OpenScene(ScenePath);
        var canvas = GameObject.Find("Canvas");
        if (canvas == null)
        {
            Debug.LogError("Canvas not found in scene!");
            return;
        }

        var mmm = GameObject.FindObjectOfType<MainMenuManager>();
        if (mmm == null)
        {
            Debug.LogError("MainMenuManager not found!");
            return;
        }

        // 1. Find or create CharPanel
        Transform charPanelT = canvas.transform.Find("CharPanel");
        GameObject charPanel;
        if (charPanelT != null)
        {
            charPanel = charPanelT.gameObject;
            for (int i = charPanel.transform.childCount - 1; i >= 0; i--)
            {
                Object.DestroyImmediate(charPanel.transform.GetChild(i).gameObject);
            }
        }
        else
        {
            charPanel = new GameObject("CharPanel", typeof(RectTransform));
            charPanel.transform.SetParent(canvas.transform, false);
        }

        RectTransform charRt = charPanel.GetComponent<RectTransform>();
        charRt.anchorMin = Vector2.zero;
        charRt.anchorMax = Vector2.one;
        charRt.offsetMin = Vector2.zero;
        charRt.offsetMax = Vector2.zero;
        charPanel.transform.SetSiblingIndex(6); // Right below MainLandingRoot
        charPanel.SetActive(true);

        var ui = charPanel.GetComponent<CharacterSettingUI>();
        if (ui == null) ui = charPanel.AddComponent<CharacterSettingUI>();

        // Load Sprites
        Sprite pedestalSprite = AssetDatabase.LoadAssetAtPath<Sprite>(SpriteDir + "character_pedestal.png");
        Sprite charFrameSprite = AssetDatabase.LoadAssetAtPath<Sprite>(SpriteDir + "character_panel_frame.png");
        Sprite equipFrameSprite = AssetDatabase.LoadAssetAtPath<Sprite>(SpriteDir + "equipment_panel_frame.png");
        Sprite slotDefSprite = AssetDatabase.LoadAssetAtPath<Sprite>(SpriteDir + "slot_frame_default.png");
        Sprite slotHighSprite = AssetDatabase.LoadAssetAtPath<Sprite>(SpriteDir + "slot_frame_highlight.png");
        Sprite checkmarkSprite = AssetDatabase.LoadAssetAtPath<Sprite>(SpriteDir + "icon_checkmark.png");
        Sprite noneSprite = AssetDatabase.LoadAssetAtPath<Sprite>(SpriteDir + "icon_none.png");
        Sprite glowSprite = AssetDatabase.LoadAssetAtPath<Sprite>(SpriteDir + "card_selection_glow.png");
        Sprite saveSprite = AssetDatabase.LoadAssetAtPath<Sprite>(SpriteDir + "btn_save.png");
        Sprite backSprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/UI/MultiplayerLobby/Sprites/rugged_cartoon_game_ui_back_button.png");

        Sprite cardBarbarian = AssetDatabase.LoadAssetAtPath<Sprite>(SpriteDir + "card_barbarian.png");
        Sprite cardKnight = AssetDatabase.LoadAssetAtPath<Sprite>(SpriteDir + "card_knight.png");
        Sprite cardMage = AssetDatabase.LoadAssetAtPath<Sprite>(SpriteDir + "card_mage.png");
        Sprite cardRogue = AssetDatabase.LoadAssetAtPath<Sprite>(SpriteDir + "card_rogue.png");

        Font defaultFont = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

        // 2. Pedestal
        GameObject pedestalObj = new GameObject("Pedestal", typeof(RectTransform), typeof(Image));
        pedestalObj.transform.SetParent(charPanel.transform, false);
        RectTransform pedRt = pedestalObj.GetComponent<RectTransform>();
        pedRt.anchorMin = new Vector2(0.5f, 0.5f);
        pedRt.anchorMax = new Vector2(0.5f, 0.5f);
        pedRt.anchoredPosition = new Vector2(0f, -325f);
        pedRt.sizeDelta = new Vector2(740f, 278f);
        Image pedImg = pedestalObj.GetComponent<Image>();
        pedImg.sprite = pedestalSprite;
        pedImg.raycastTarget = false;

        // 3. Left Panel (Select Character)
        GameObject leftPanel = new GameObject("LeftPanel", typeof(RectTransform), typeof(Image));
        leftPanel.transform.SetParent(charPanel.transform, false);
        RectTransform leftRt = leftPanel.GetComponent<RectTransform>();
        leftRt.anchorMin = new Vector2(0.5f, 0.5f);
        leftRt.anchorMax = new Vector2(0.5f, 0.5f);
        leftRt.anchoredPosition = new Vector2(-580f, 25f);
        leftRt.sizeDelta = new Vector2(560f, 830f);
        Image leftImg = leftPanel.GetComponent<Image>();
        leftImg.sprite = charFrameSprite;

        // CharListContainer
        GameObject charListObj = new GameObject("CharListContainer", typeof(RectTransform));
        charListObj.transform.SetParent(leftPanel.transform, false);
        RectTransform listRt = charListObj.GetComponent<RectTransform>();
        listRt.anchorMin = new Vector2(0.5f, 0.5f);
        listRt.anchorMax = new Vector2(0.5f, 0.5f);
        listRt.anchoredPosition = new Vector2(0f, -35f);
        listRt.sizeDelta = new Vector2(480f, 640f);

        // 4 Cards
        CreateCharCard(charListObj.transform, "CharBtn_Barbarian", cardBarbarian, glowSprite, new Vector2(0f, 225f), true);
        CreateCharCard(charListObj.transform, "CharBtn_Knight", cardKnight, glowSprite, new Vector2(0f, 75f), false);
        CreateCharCard(charListObj.transform, "CharBtn_Mage", cardMage, glowSprite, new Vector2(0f, -75f), false);
        CreateCharCard(charListObj.transform, "CharBtn_Rogue", cardRogue, glowSprite, new Vector2(0f, -225f), false);

        // 4. Right Panel (Equipment)
        GameObject rightPanel = new GameObject("RightPanel", typeof(RectTransform), typeof(Image));
        rightPanel.transform.SetParent(charPanel.transform, false);
        RectTransform rightRt = rightPanel.GetComponent<RectTransform>();
        rightRt.anchorMin = new Vector2(0.5f, 0.5f);
        rightRt.anchorMax = new Vector2(0.5f, 0.5f);
        rightRt.anchoredPosition = new Vector2(580f, 25f);
        rightRt.sizeDelta = new Vector2(560f, 830f);
        Image rightImg = rightPanel.GetComponent<Image>();
        rightImg.sprite = equipFrameSprite;

        // WeaponsGrid
        GameObject weaponsGrid = new GameObject("WeaponsGrid", typeof(RectTransform));
        weaponsGrid.transform.SetParent(rightPanel.transform, false);
        RectTransform wGridRt = weaponsGrid.GetComponent<RectTransform>();
        wGridRt.anchorMin = new Vector2(0.5f, 0.5f);
        wGridRt.anchorMax = new Vector2(0.5f, 0.5f);
        wGridRt.anchoredPosition = new Vector2(0f, 150f);
        wGridRt.sizeDelta = new Vector2(450f, 130f);

        CreateItemSlot(weaponsGrid.transform, "Slot_0", new Vector2(-145f, 0f), slotDefSprite, slotHighSprite, checkmarkSprite, false, null, null);
        CreateItemSlot(weaponsGrid.transform, "Slot_1", new Vector2(0f, 0f), slotDefSprite, slotHighSprite, checkmarkSprite, false, null, null);
        CreateItemSlot(weaponsGrid.transform, "Slot_2", new Vector2(145f, 0f), slotDefSprite, slotHighSprite, checkmarkSprite, false, null, null);

        // Weapon Carousel Buttons
        GameObject wPrev = CreateCarouselButton(rightPanel.transform, "WeaponPrevBtn", new Vector2(-235f, 150f), "<", defaultFont);
        GameObject wNext = CreateCarouselButton(rightPanel.transform, "WeaponNextBtn", new Vector2(235f, 150f), ">", defaultFont);

        // ShieldsGrid
        GameObject shieldsGrid = new GameObject("ShieldsGrid", typeof(RectTransform));
        shieldsGrid.transform.SetParent(rightPanel.transform, false);
        RectTransform sGridRt = shieldsGrid.GetComponent<RectTransform>();
        sGridRt.anchorMin = new Vector2(0.5f, 0.5f);
        sGridRt.anchorMax = new Vector2(0.5f, 0.5f);
        sGridRt.anchoredPosition = new Vector2(0f, -115f);
        sGridRt.sizeDelta = new Vector2(450f, 130f);

        // Slot_0 for shields is None
        CreateItemSlot(shieldsGrid.transform, "Slot_0", new Vector2(-145f, 0f), slotDefSprite, slotHighSprite, checkmarkSprite, true, noneSprite, defaultFont);
        CreateItemSlot(shieldsGrid.transform, "Slot_1", new Vector2(0f, 0f), slotDefSprite, slotHighSprite, checkmarkSprite, false, null, null);
        CreateItemSlot(shieldsGrid.transform, "Slot_2", new Vector2(145f, 0f), slotDefSprite, slotHighSprite, checkmarkSprite, false, null, null);

        // Shield Carousel Buttons
        GameObject sPrev = CreateCarouselButton(rightPanel.transform, "ShieldPrevBtn", new Vector2(-235f, -115f), "<", defaultFont);
        GameObject sNext = CreateCarouselButton(rightPanel.transform, "ShieldNextBtn", new Vector2(235f, -115f), ">", defaultFont);

        // 5. Back Button (from Round selection screen)
        GameObject btnBackObj = new GameObject("Btn_Back", typeof(RectTransform), typeof(Image), typeof(Button), typeof(UIButtonHoverEffect));
        btnBackObj.transform.SetParent(charPanel.transform, false);
        RectTransform backRt = btnBackObj.GetComponent<RectTransform>();
        backRt.anchorMin = new Vector2(0.5f, 0.5f);
        backRt.anchorMax = new Vector2(0.5f, 0.5f);
        backRt.anchoredPosition = new Vector2(-780f, -455f);
        backRt.sizeDelta = new Vector2(280f, 83f);
        Image backImg = btnBackObj.GetComponent<Image>();
        backImg.sprite = backSprite;
        Button backBtn = btnBackObj.GetComponent<Button>();
        backBtn.transition = Selectable.Transition.None;
        var backHover = btnBackObj.GetComponent<UIButtonHoverEffect>();
        backHover.hoverScale = 1.04f;
        backHover.pressScale = 0.96f;

        // 6. Save Button (from user provided asset)
        GameObject btnSaveObj = new GameObject("Btn_Save", typeof(RectTransform), typeof(Image), typeof(Button), typeof(UIButtonHoverEffect));
        btnSaveObj.transform.SetParent(charPanel.transform, false);
        RectTransform saveRt = btnSaveObj.GetComponent<RectTransform>();
        saveRt.anchorMin = new Vector2(0.5f, 0.5f);
        saveRt.anchorMax = new Vector2(0.5f, 0.5f);
        saveRt.anchoredPosition = new Vector2(0f, -425f);
        saveRt.sizeDelta = new Vector2(340f, 113f);
        Image saveImg = btnSaveObj.GetComponent<Image>();
        saveImg.sprite = saveSprite;
        Button saveBtn = btnSaveObj.GetComponent<Button>();
        saveBtn.transition = Selectable.Transition.None;
        var saveHover = btnSaveObj.GetComponent<UIButtonHoverEffect>();
        saveHover.hoverScale = 1.04f;
        saveHover.pressScale = 0.96f;

        // 7. Wire UI References
        ui.characterListContainer = charListObj.transform;
        ui.weaponsGridContainer = weaponsGrid.transform;
        ui.shieldsGridContainer = shieldsGrid.transform;
        ui.weaponPrevBtn = wPrev.GetComponent<Button>();
        ui.weaponNextBtn = wNext.GetComponent<Button>();
        ui.shieldPrevBtn = sPrev.GetComponent<Button>();
        ui.shieldNextBtn = sNext.GetComponent<Button>();
        ui.backButton = backBtn;
        // Pre-initialize UI with data so slots and portraits are baked into the scene
        if (mmm.availableCharacters != null && mmm.availableCharacters.Length > 0)
        {
            ui.Initialize(mmm.availableCharacters, mmm.allEquipment, mmm.characterPreviewSpot);
        }

        // Wire MainMenuManager
        mmm.characterSettingPanel = charPanel;
        if (mmm.characterPreviewSpot != null)
        {
            mmm.characterPreviewSpot.position = new Vector3(0f, -0.05f, 4.8f);
            mmm.characterPreviewSpot.rotation = Quaternion.Euler(0f, 180f, 0f);
        }

        // Set initial state
        charPanel.SetActive(false);

        EditorUtility.SetDirty(charPanel);
        EditorUtility.SetDirty(mmm);
        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);

        Debug.Log("Character Customization UI Rebuilt Successfully!");
    }

    private static void CreateCharCard(Transform parent, string name, Sprite cardSprite, Sprite glowSprite, Vector2 pos, bool isInitialSelected)
    {
        GameObject card = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(Button), typeof(UIButtonHoverEffect));
        card.transform.SetParent(parent, false);
        RectTransform rt = card.GetComponent<RectTransform>();
        rt.anchorMin = new Vector2(0.5f, 0.5f);
        rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = pos;
        rt.sizeDelta = new Vector2(470f, 138f);

        Image img = card.GetComponent<Image>();
        img.sprite = cardSprite;

        Button btn = card.GetComponent<Button>();
        btn.transition = Selectable.Transition.None;

        var hover = card.GetComponent<UIButtonHoverEffect>();
        hover.hoverScale = 1.03f;
        hover.pressScale = 0.97f;

        // Glow child
        GameObject glow = new GameObject("SelectionGlow", typeof(RectTransform), typeof(Image));
        glow.transform.SetParent(card.transform, false);
        RectTransform glowRt = glow.GetComponent<RectTransform>();
        glowRt.anchorMin = new Vector2(0.5f, 0.5f);
        glowRt.anchorMax = new Vector2(0.5f, 0.5f);
        glowRt.anchoredPosition = Vector2.zero;
        glowRt.sizeDelta = new Vector2(476f, 144f);
        Image glowImg = glow.GetComponent<Image>();
        glowImg.sprite = glowSprite;
        glowImg.raycastTarget = false;
        glow.SetActive(isInitialSelected);
    }

    private static void CreateItemSlot(Transform parent, string name, Vector2 pos, Sprite defSprite, Sprite highSprite, Sprite checkSprite, bool isNoneSlot, Sprite noneIcon, Font font)
    {
        GameObject slot = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(Button), typeof(UIButtonHoverEffect));
        slot.transform.SetParent(parent, false);
        RectTransform rt = slot.GetComponent<RectTransform>();
        rt.anchorMin = new Vector2(0.5f, 0.5f);
        rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = pos;
        rt.sizeDelta = new Vector2(125f, 125f);

        Image baseImg = slot.GetComponent<Image>();
        baseImg.sprite = defSprite;

        Button btn = slot.GetComponent<Button>();
        btn.transition = Selectable.Transition.None;

        var hover = slot.GetComponent<UIButtonHoverEffect>();
        hover.hoverScale = 1.04f;
        hover.pressScale = 0.96f;

        // Icon
        GameObject iconObj = new GameObject("Icon", typeof(RectTransform), typeof(Image));
        iconObj.transform.SetParent(slot.transform, false);
        RectTransform iconRt = iconObj.GetComponent<RectTransform>();
        iconRt.anchorMin = new Vector2(0.5f, 0.5f);
        iconRt.anchorMax = new Vector2(0.5f, 0.5f);
        Image iconImg = iconObj.GetComponent<Image>();
        iconImg.raycastTarget = false;

        if (isNoneSlot)
        {
            iconRt.anchoredPosition = new Vector2(0f, 12f);
            iconRt.sizeDelta = new Vector2(48f, 48f);
            iconImg.sprite = noneIcon;
            iconImg.color = new Color(0.85f, 0.85f, 0.85f, 0.9f);

            // None Text
            GameObject textObj = new GameObject("Text_None", typeof(RectTransform), typeof(Text));
            textObj.transform.SetParent(slot.transform, false);
            RectTransform textRt = textObj.GetComponent<RectTransform>();
            textRt.anchorMin = new Vector2(0.5f, 0.5f);
            textRt.anchorMax = new Vector2(0.5f, 0.5f);
            textRt.anchoredPosition = new Vector2(0f, -38f);
            textRt.sizeDelta = new Vector2(100f, 26f);
            Text t = textObj.GetComponent<Text>();
            t.text = "None";
            t.font = font;
            t.fontSize = 15;
            t.fontStyle = FontStyle.Bold;
            t.alignment = TextAnchor.MiddleCenter;
            t.color = new Color(0.85f, 0.85f, 0.85f, 0.9f);
            t.raycastTarget = false;
        }
        else
        {
            iconRt.anchoredPosition = Vector2.zero;
            iconRt.sizeDelta = new Vector2(88f, 88f);
            iconImg.enabled = false;
        }

        // Highlight
        GameObject highObj = new GameObject("Highlight", typeof(RectTransform), typeof(Image));
        highObj.transform.SetParent(slot.transform, false);
        RectTransform highRt = highObj.GetComponent<RectTransform>();
        highRt.anchorMin = new Vector2(0.5f, 0.5f);
        highRt.anchorMax = new Vector2(0.5f, 0.5f);
        highRt.anchoredPosition = Vector2.zero;
        highRt.sizeDelta = new Vector2(129f, 129f);
        Image highImg = highObj.GetComponent<Image>();
        highImg.sprite = highSprite;
        highImg.raycastTarget = false;
        highObj.SetActive(false);

        // Checkmark
        GameObject checkObj = new GameObject("Checkmark", typeof(RectTransform), typeof(Image));
        checkObj.transform.SetParent(slot.transform, false);
        RectTransform checkRt = checkObj.GetComponent<RectTransform>();
        checkRt.anchorMin = new Vector2(0.5f, 0.5f);
        checkRt.anchorMax = new Vector2(0.5f, 0.5f);
        checkRt.anchoredPosition = new Vector2(42f, -42f);
        checkRt.sizeDelta = new Vector2(34f, 34f);
        Image checkImg = checkObj.GetComponent<Image>();
        checkImg.sprite = checkSprite;
        checkImg.raycastTarget = false;
        checkObj.SetActive(false);
    }

    private static GameObject CreateCarouselButton(Transform parent, string name, Vector2 pos, string label, Font font)
    {
        GameObject btnObj = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(Button), typeof(UIButtonHoverEffect));
        btnObj.transform.SetParent(parent, false);
        RectTransform rt = btnObj.GetComponent<RectTransform>();
        rt.anchorMin = new Vector2(0.5f, 0.5f);
        rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = pos;
        rt.sizeDelta = new Vector2(34f, 40f);

        Image img = btnObj.GetComponent<Image>();
        img.color = new Color(0.1f, 0.15f, 0.25f, 0.8f);

        Button btn = btnObj.GetComponent<Button>();
        btn.transition = Selectable.Transition.ColorTint;

        var hover = btnObj.GetComponent<UIButtonHoverEffect>();
        hover.hoverScale = 1.1f;
        hover.pressScale = 0.9f;

        GameObject txtObj = new GameObject("Text", typeof(RectTransform), typeof(Text));
        txtObj.transform.SetParent(btnObj.transform, false);
        RectTransform txtRt = txtObj.GetComponent<RectTransform>();
        txtRt.anchorMin = Vector2.zero;
        txtRt.anchorMax = Vector2.one;
        txtRt.offsetMin = Vector2.zero;
        txtRt.offsetMax = Vector2.zero;
        Text t = txtObj.GetComponent<Text>();
        t.text = label;
        t.font = font;
        t.fontSize = 20;
        t.fontStyle = FontStyle.Bold;
        t.alignment = TextAnchor.MiddleCenter;
        t.color = new Color(1f, 0.85f, 0.3f, 1f);

        btnObj.SetActive(false); // Initially hidden unless needed
        return btnObj;
    }
}
