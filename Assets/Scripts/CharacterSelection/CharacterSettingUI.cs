using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// Character Setting UI - Controls character and equipment selection.
/// Directly binds 4 hero cards and dedicated equipment slots (Weapons & Shields).
/// </summary>
public class CharacterSettingUI : MonoBehaviour
{
    [Header("UI References")]
    public Text characterNameText;
    public Text equippedRightText;
    public Text equippedLeftText;
    public Button backButton;

    [Header("Containers")]
    public Transform characterListContainer;
    public Transform weaponsGridContainer;
    public Transform shieldsGridContainer;

    [Header("Templates (Optional/Unused)")]
    public GameObject characterCardTemplate;
    public GameObject equipmentButtonTemplate;

    [Header("Carousel Controls")]
    public Button weaponPrevBtn;
    public Button weaponNextBtn;
    public Button shieldPrevBtn;
    public Button shieldNextBtn;

    [Header("3D Portrait Renderer")]
    public PortraitRenderer portraitRenderer;

    private CharacterData[] characters;
    private EquipmentData[] allEquipment;
    private Transform previewSpot;

    private int selectedCharacterIndex = 0;
    private GameObject currentPreviewInstance;
    private EquipmentManager currentEquipmentManager;

    private List<GameObject> activeCharCards = new List<GameObject>();
    private List<EquipmentData> currentWeapons = new List<EquipmentData>();
    private List<EquipmentData> currentShields = new List<EquipmentData>();

    private int weaponStartIndex = 0;
    private int shieldStartIndex = 0;
    private const int WeaponPageSize = 3;
    private const int ShieldPageSize = 2; // Slot 0 is always "None", slots 1 and 2 show items

    public void Initialize(CharacterData[] characters, EquipmentData[] allEquipment, Transform previewSpot)
    {
        this.characters = characters;
        this.allEquipment = allEquipment;
        this.previewSpot = previewSpot;

        // Auto-find or create PortraitRenderer if not assigned
        if (portraitRenderer == null)
        {
            portraitRenderer = FindObjectOfType<PortraitRenderer>();
            if (portraitRenderer == null)
            {
                GameObject prObj = new GameObject("PortraitRenderer");
                prObj.transform.position = new Vector3(1000f, 1000f, 1000f);
                portraitRenderer = prObj.AddComponent<PortraitRenderer>();
                portraitRenderer.InitializeRenderer();
            }
        }

        if (characterCardTemplate != null) characterCardTemplate.SetActive(false);
        if (equipmentButtonTemplate != null) equipmentButtonTemplate.SetActive(false);

        // Auto-wire Back Button to return to Map/Round selection
        if (backButton != null)
        {
            backButton.onClick.RemoveAllListeners();
            backButton.onClick.AddListener(() =>
            {
                var m = FindObjectOfType<MainMenuManager>();
                if (m != null) m.ShowMapSelection();
            });
        }

        // Auto-wire Save Button
        Transform saveT = transform.Find("Btn_Save");
        if (saveT != null)
        {
            Button sBtn = saveT.GetComponent<Button>();
            if (sBtn != null)
            {
                sBtn.onClick.RemoveAllListeners();
                sBtn.onClick.AddListener(SaveSelection);
            }
        }

        if (weaponPrevBtn != null)
        {
            weaponPrevBtn.onClick.RemoveAllListeners();
            weaponPrevBtn.onClick.AddListener(() => { weaponStartIndex = Mathf.Max(0, weaponStartIndex - 1); RefreshWeapons(); });
        }
        if (weaponNextBtn != null)
        {
            weaponNextBtn.onClick.RemoveAllListeners();
            weaponNextBtn.onClick.AddListener(() => { weaponStartIndex = Mathf.Min(Mathf.Max(0, currentWeapons.Count - WeaponPageSize), weaponStartIndex + 1); RefreshWeapons(); });
        }
        if (shieldPrevBtn != null)
        {
            shieldPrevBtn.onClick.RemoveAllListeners();
            shieldPrevBtn.onClick.AddListener(() => { shieldStartIndex = Mathf.Max(0, shieldStartIndex - 1); RefreshShields(); });
        }
        if (shieldNextBtn != null)
        {
            shieldNextBtn.onClick.RemoveAllListeners();
            shieldNextBtn.onClick.AddListener(() => { shieldStartIndex = Mathf.Min(Mathf.Max(0, currentShields.Count - ShieldPageSize), shieldStartIndex + 1); RefreshShields(); });
        }

        BindCharacterCards();

        if (GameDataCarrier.Instance != null && GameDataCarrier.Instance.HasSelection)
        {
            for (int i = 0; i < characters.Length; i++)
            {
                if (characters[i] == GameDataCarrier.Instance.selectedCharacter)
                {
                    selectedCharacterIndex = i;
                    break;
                }
            }
        }
        else if (PlayerPrefs.HasKey("SelectedCharacter") && characters != null)
        {
            string savedChar = PlayerPrefs.GetString("SelectedCharacter", "");
            for (int i = 0; i < characters.Length; i++)
            {
                if (characters[i] != null && characters[i].characterName == savedChar)
                {
                    selectedCharacterIndex = i;
                    break;
                }
            }
        }

        SelectCharacter(selectedCharacterIndex);
    }

    void BindCharacterCards()
    {
        activeCharCards.Clear();
        if (characterListContainer == null || characters == null) return;

        for (int i = 0; i < characters.Length; i++)
        {
            int index = i;
            CharacterData charData = characters[i];

            Transform cardTransform = characterListContainer.Find($"CharBtn_{charData.characterName}");
            if (cardTransform == null && i < characterListContainer.childCount)
            {
                cardTransform = characterListContainer.GetChild(i);
            }

            if (cardTransform != null)
            {
                GameObject card = cardTransform.gameObject;
                card.SetActive(true);

                Button btn = card.GetComponent<Button>();
                if (btn != null)
                {
                    btn.onClick.RemoveAllListeners();
                    btn.onClick.AddListener(() => SelectCharacter(index));
                }

                activeCharCards.Add(card);
            }
        }
    }

    public void SelectCharacter(int index)
    {
        if (characters == null || index < 0 || index >= characters.Length) return;
        selectedCharacterIndex = index;
        CharacterData charData = characters[index];

        if (characterNameText != null) characterNameText.text = charData.characterName.ToUpper();
        if (GameDataCarrier.Instance != null) GameDataCarrier.Instance.SelectCharacter(charData);

        SpawnPreview(charData);

        if (currentEquipmentManager != null)
        {
            currentEquipmentManager.ClearBuiltInEquipment();
            EquipmentData eR = null, eL = null;
            if (GameDataCarrier.Instance != null && GameDataCarrier.Instance.selectedCharacter == charData)
            {
                eR = GameDataCarrier.Instance.equippedRightHand;
                eL = GameDataCarrier.Instance.equippedLeftHand;
            }
            if (eR == null) eR = charData.defaultRightHand;
            if (eL == null) eL = charData.defaultLeftHand;

            if (eR != null) { currentEquipmentManager.EquipRight(eR); if (GameDataCarrier.Instance != null) GameDataCarrier.Instance.equippedRightHand = eR; }
            if (eL != null) { currentEquipmentManager.EquipLeft(eL); if (GameDataCarrier.Instance != null) GameDataCarrier.Instance.equippedLeftHand = eL; }
            else { currentEquipmentManager.ClearSlot(EquipSlot.LeftHand); }
        }

        HighlightCharacterButton(index);
        FilterEquipment(charData);
    }

    void SpawnPreview(CharacterData charData)
    {
        if (currentPreviewInstance != null)
        {
            if (Application.isPlaying) Destroy(currentPreviewInstance);
            else DestroyImmediate(currentPreviewInstance);
        }

        if (charData.characterPrefab == null || previewSpot == null) return;

        currentPreviewInstance = Instantiate(charData.characterPrefab, previewSpot.position, previewSpot.rotation);

        var cc = currentPreviewInstance.GetComponent<CharacterController>();
        if (cc != null) cc.enabled = false;
        var pc = currentPreviewInstance.GetComponent<PlayerController>();
        if (pc != null) pc.enabled = false;

        currentEquipmentManager = currentPreviewInstance.GetComponent<EquipmentManager>();
        if (currentEquipmentManager == null) currentEquipmentManager = currentPreviewInstance.AddComponent<EquipmentManager>();

        var animator = currentPreviewInstance.GetComponentInChildren<Animator>();
        if (animator != null)
        {
            animator.enabled = true;
            animator.SetBool("IsMoving", false);
            animator.Play("Idle", 0, 0f);
        }
    }

    void HighlightCharacterButton(int selectedIndex)
    {
        for (int i = 0; i < activeCharCards.Count; i++)
        {
            if (activeCharCards[i] == null) continue;
            Transform glow = activeCharCards[i].transform.Find("SelectionGlow");
            if (glow != null)
            {
                glow.gameObject.SetActive(i == selectedIndex);
            }
        }
    }

    void FilterEquipment(CharacterData charData)
    {
        if (allEquipment == null) return;

        currentWeapons = allEquipment.Where(e => e != null && e.slot == EquipSlot.RightHand).ToList();
        currentShields = allEquipment.Where(e => e != null && e.slot == EquipSlot.LeftHand).ToList();

        if (charData.allowedEquipmentTypes != null && charData.allowedEquipmentTypes.Count > 0)
        {
            currentWeapons = currentWeapons.Where(e => charData.allowedEquipmentTypes.Contains(e.equipmentType)).ToList();
            currentShields = currentShields.Where(e => charData.allowedEquipmentTypes.Contains(e.equipmentType)).ToList();
        }

        weaponStartIndex = 0;
        shieldStartIndex = 0;

        RefreshWeapons();
        RefreshShields();
    }

    void RefreshWeapons()
    {
        if (weaponsGridContainer == null) return;

        int totalWeapons = currentWeapons.Count;
        if (weaponPrevBtn != null)
        {
            weaponPrevBtn.gameObject.SetActive(totalWeapons > WeaponPageSize);
            weaponPrevBtn.interactable = weaponStartIndex > 0;
        }
        if (weaponNextBtn != null)
        {
            weaponNextBtn.gameObject.SetActive(totalWeapons > WeaponPageSize);
            weaponNextBtn.interactable = (weaponStartIndex + WeaponPageSize < totalWeapons);
        }

        for (int i = 0; i < WeaponPageSize; i++)
        {
            Transform slot = weaponsGridContainer.Find($"Slot_{i}");
            if (slot == null && i < weaponsGridContainer.childCount) slot = weaponsGridContainer.GetChild(i);
            if (slot == null) continue;

            int itemIndex = weaponStartIndex + i;
            if (itemIndex < totalWeapons)
            {
                slot.gameObject.SetActive(true);
                EquipmentData weapon = currentWeapons[itemIndex];
                SetupWeaponSlot(slot.gameObject, weapon);
            }
            else
            {
                slot.gameObject.SetActive(false);
            }
        }

        UpdateWeaponCheckmarks();
    }

    void SetupWeaponSlot(GameObject slotObj, EquipmentData weapon)
    {
        Image icon = slotObj.transform.Find("Icon")?.GetComponent<Image>();
        if (icon != null)
        {
            Sprite portrait = (weapon != null) ? (weapon.icon != null ? weapon.icon : ((portraitRenderer != null) ? portraitRenderer.GetEquipmentPortrait(weapon) : null)) : null;
            if (portrait != null)
            {
                icon.sprite = portrait;
                icon.enabled = true;
            }
            else
            {
                icon.sprite = null;
                icon.enabled = false;
            }
        }

        Button btn = slotObj.GetComponent<Button>();
        if (btn != null)
        {
            btn.onClick.RemoveAllListeners();
            btn.onClick.AddListener(() => OnWeaponSelected(weapon));
        }
    }

    void OnWeaponSelected(EquipmentData weapon)
    {
        if (currentEquipmentManager == null || weapon == null) return;
        currentEquipmentManager.EquipRight(weapon);
        if (GameDataCarrier.Instance != null)
        {
            GameDataCarrier.Instance.equippedRightHand = weapon;
        }
        UpdateWeaponCheckmarks();
    }

    void UpdateWeaponCheckmarks()
    {
        if (weaponsGridContainer == null) return;
        EquipmentData equipped = GameDataCarrier.Instance != null ? GameDataCarrier.Instance.equippedRightHand : null;

        for (int i = 0; i < WeaponPageSize; i++)
        {
            Transform slot = weaponsGridContainer.Find($"Slot_{i}");
            if (slot == null && i < weaponsGridContainer.childCount) slot = weaponsGridContainer.GetChild(i);
            if (slot == null || !slot.gameObject.activeSelf) continue;

            int itemIndex = weaponStartIndex + i;
            bool isSelected = false;
            if (itemIndex < currentWeapons.Count)
            {
                isSelected = (currentWeapons[itemIndex] == equipped);
            }

            Transform highlight = slot.Find("Highlight");
            if (highlight != null) highlight.gameObject.SetActive(isSelected);

            Transform check = slot.Find("Checkmark");
            if (check != null) check.gameObject.SetActive(isSelected);
        }

        if (equippedRightText != null)
        {
            equippedRightText.text = equipped != null ? $"Weapon: {equipped.equipmentName}" : "Weapon: None";
        }
    }

    void RefreshShields()
    {
        if (shieldsGridContainer == null) return;

        // Slot_0 is always the "None" slot
        Transform noneSlot = shieldsGridContainer.Find("Slot_0");
        if (noneSlot == null && shieldsGridContainer.childCount > 0) noneSlot = shieldsGridContainer.GetChild(0);
        if (noneSlot != null)
        {
            noneSlot.gameObject.SetActive(true);
            Button noneBtn = noneSlot.GetComponent<Button>();
            if (noneBtn != null)
            {
                noneBtn.onClick.RemoveAllListeners();
                noneBtn.onClick.AddListener(OnShieldNoneSelected);
            }
        }

        int totalShields = currentShields.Count;
        if (shieldPrevBtn != null)
        {
            shieldPrevBtn.gameObject.SetActive(totalShields > ShieldPageSize);
            shieldPrevBtn.interactable = shieldStartIndex > 0;
        }
        if (shieldNextBtn != null)
        {
            shieldNextBtn.gameObject.SetActive(totalShields > ShieldPageSize);
            shieldNextBtn.interactable = (shieldStartIndex + ShieldPageSize < totalShields);
        }

        // Slot_1 and Slot_2 show shields from currentShields
        for (int i = 0; i < ShieldPageSize; i++)
        {
            int slotIdx = i + 1;
            Transform slot = shieldsGridContainer.Find($"Slot_{slotIdx}");
            if (slot == null && slotIdx < shieldsGridContainer.childCount) slot = shieldsGridContainer.GetChild(slotIdx);
            if (slot == null) continue;

            int itemIndex = shieldStartIndex + i;
            if (itemIndex < totalShields)
            {
                slot.gameObject.SetActive(true);
                EquipmentData shield = currentShields[itemIndex];
                SetupShieldSlot(slot.gameObject, shield);
            }
            else
            {
                slot.gameObject.SetActive(false);
            }
        }

        UpdateShieldCheckmarks();
    }

    void SetupShieldSlot(GameObject slotObj, EquipmentData shield)
    {
        Image icon = slotObj.transform.Find("Icon")?.GetComponent<Image>();
        if (icon != null)
        {
            Sprite portrait = (shield != null) ? (shield.icon != null ? shield.icon : ((portraitRenderer != null) ? portraitRenderer.GetEquipmentPortrait(shield) : null)) : null;
            if (portrait != null)
            {
                icon.sprite = portrait;
                icon.enabled = true;
            }
            else
            {
                icon.sprite = null;
                icon.enabled = false;
            }
        }

        Button btn = slotObj.GetComponent<Button>();
        if (btn != null)
        {
            btn.onClick.RemoveAllListeners();
            btn.onClick.AddListener(() => OnShieldSelected(shield));
        }
    }

    void OnShieldSelected(EquipmentData shield)
    {
        if (currentEquipmentManager == null || shield == null) return;
        currentEquipmentManager.EquipLeft(shield);
        if (GameDataCarrier.Instance != null)
        {
            GameDataCarrier.Instance.equippedLeftHand = shield;
        }
        UpdateShieldCheckmarks();
    }

    void OnShieldNoneSelected()
    {
        if (currentEquipmentManager != null)
        {
            currentEquipmentManager.ClearSlot(EquipSlot.LeftHand);
        }
        if (GameDataCarrier.Instance != null)
        {
            GameDataCarrier.Instance.equippedLeftHand = null;
        }
        UpdateShieldCheckmarks();
    }

    void UpdateShieldCheckmarks()
    {
        if (shieldsGridContainer == null) return;
        EquipmentData equipped = GameDataCarrier.Instance != null ? GameDataCarrier.Instance.equippedLeftHand : null;

        // Slot_0 (None)
        Transform noneSlot = shieldsGridContainer.Find("Slot_0");
        if (noneSlot == null && shieldsGridContainer.childCount > 0) noneSlot = shieldsGridContainer.GetChild(0);
        if (noneSlot != null)
        {
            bool isNone = (equipped == null);
            Transform highlight = noneSlot.Find("Highlight");
            if (highlight != null) highlight.gameObject.SetActive(isNone);
            Transform check = noneSlot.Find("Checkmark");
            if (check != null) check.gameObject.SetActive(isNone);
        }

        // Slot_1 and Slot_2
        for (int i = 0; i < ShieldPageSize; i++)
        {
            int slotIdx = i + 1;
            Transform slot = shieldsGridContainer.Find($"Slot_{slotIdx}");
            if (slot == null && slotIdx < shieldsGridContainer.childCount) slot = shieldsGridContainer.GetChild(slotIdx);
            if (slot == null || !slot.gameObject.activeSelf) continue;

            int itemIndex = shieldStartIndex + i;
            bool isSelected = false;
            if (itemIndex < currentShields.Count)
            {
                isSelected = (equipped != null && currentShields[itemIndex] == equipped);
            }

            Transform highlight = slot.Find("Highlight");
            if (highlight != null) highlight.gameObject.SetActive(isSelected);

            Transform check = slot.Find("Checkmark");
            if (check != null) check.gameObject.SetActive(isSelected);
        }

        if (equippedLeftText != null)
        {
            equippedLeftText.text = equipped != null ? $"Shield: {equipped.equipmentName}" : "Shield: None";
        }
    }

    /// <summary>
    /// Saves current character and equipment selection to GameDataCarrier and PlayerPrefs.
    /// Does NOT exit or close the screen (supports online/web workflow).
    /// </summary>
    public void SaveSelection()
    {
        if (characters != null && selectedCharacterIndex >= 0 && selectedCharacterIndex < characters.Length)
        {
            CharacterData selectedChar = characters[selectedCharacterIndex];
            if (GameDataCarrier.Instance != null)
            {
                GameDataCarrier.Instance.selectedCharacter = selectedChar;
            }

            // Persist to PlayerPrefs for WebGL / browser sessions
            PlayerPrefs.SetString("SelectedCharacter", selectedChar.characterName);
            if (GameDataCarrier.Instance != null)
            {
                PlayerPrefs.SetString("EquippedRightHand", GameDataCarrier.Instance.equippedRightHand != null ? GameDataCarrier.Instance.equippedRightHand.equipmentName : "");
                PlayerPrefs.SetString("EquippedLeftHand", GameDataCarrier.Instance.equippedLeftHand != null ? GameDataCarrier.Instance.equippedLeftHand.equipmentName : "");
            }
            PlayerPrefs.Save();
            Debug.Log($"[CharacterSettingUI] Selection saved: {selectedChar.characterName} (Right: {(GameDataCarrier.Instance?.equippedRightHand?.equipmentName ?? "None")}, Left: {(GameDataCarrier.Instance?.equippedLeftHand?.equipmentName ?? "None")})");
        }

        StopAllCoroutines();
        StartCoroutine(ShowSaveFeedbackRoutine());
    }

    private IEnumerator ShowSaveFeedbackRoutine()
    {
        Transform saveT = transform.Find("Btn_Save");
        if (saveT != null)
        {
            Vector3 origScale = Vector3.one;
            float elapsed = 0f;
            float dur = 0.2f;
            while (elapsed < dur)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = elapsed / dur;
                saveT.localScale = origScale * (1f + 0.12f * Mathf.Sin(t * Mathf.PI));
                yield return null;
            }
            saveT.localScale = origScale;
        }
    }

    void OnEnable()
    {
        if (characters == null || characters.Length == 0)
        {
            var mmm = FindObjectOfType<MainMenuManager>();
            if (mmm != null && mmm.availableCharacters != null && mmm.availableCharacters.Length > 0)
            {
                Initialize(mmm.availableCharacters, mmm.allEquipment, mmm.characterPreviewSpot);
                return;
            }
        }
        else
        {
            SelectCharacter(selectedCharacterIndex);
        }
    }

    void OnDisable()
    {
        if (currentPreviewInstance != null)
        {
            if (Application.isPlaying) Destroy(currentPreviewInstance);
            else DestroyImmediate(currentPreviewInstance);
        }
    }
}