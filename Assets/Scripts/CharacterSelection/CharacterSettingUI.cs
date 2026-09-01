using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// Character Setting UI - Uses static hierarchy objects rather than dynamic generation.
/// Updated to support smooth carousels via object pooling and transitions.
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

    [Header("Templates (Should be deactivated)")]
    public GameObject characterCardTemplate;
    public GameObject equipmentButtonTemplate;

    [Header("Carousel Controls")]
    public Button weaponPrevBtn;
    public Button weaponNextBtn;
    public Button shieldPrevBtn;
    public Button shieldNextBtn;

    private CharacterData[] characters;
    private EquipmentData[] allEquipment;
    private Transform previewSpot;

    private int selectedCharacterIndex = 0;
    private GameObject currentPreviewInstance;
    private EquipmentManager currentEquipmentManager;

    private Color normalBorderColor = new Color(0.3f, 0.3f, 0.3f, 1f);
    private Color selectedBorderColor = new Color(1f, 0.8f, 0.2f, 1f);

    private List<GameObject> activeCharCards = new List<GameObject>();
    private List<GameObject> activeWeaponBtns = new List<GameObject>();
    private List<GameObject> activeShieldBtns = new List<GameObject>();

    private List<EquipmentData> currentWeapons = new List<EquipmentData>();
    private List<EquipmentData> currentShields = new List<EquipmentData>();

    private int weaponStartIndex = 0;
    private int shieldStartIndex = 0;
    private const int ItemsPerPage = 3;

    private CanvasGroup weaponsCanvasGroup;
    private CanvasGroup shieldsCanvasGroup;

    public void Initialize(CharacterData[] characters, EquipmentData[] allEquipment, Transform previewSpot)
    {
        this.characters = characters;
        this.allEquipment = allEquipment;
        this.previewSpot = previewSpot;

        // Ensure templates are disabled
        if (characterCardTemplate != null) characterCardTemplate.SetActive(false);
        if (equipmentButtonTemplate != null) equipmentButtonTemplate.SetActive(false);

        // Setup CanvasGroups for smooth transitions
        if (weaponsGridContainer != null)
        {
            weaponsCanvasGroup = weaponsGridContainer.GetComponent<CanvasGroup>();
            if (weaponsCanvasGroup == null) weaponsCanvasGroup = weaponsGridContainer.gameObject.AddComponent<CanvasGroup>();
        }

        if (shieldsGridContainer != null)
        {
            shieldsCanvasGroup = shieldsGridContainer.GetComponent<CanvasGroup>();
            if (shieldsCanvasGroup == null) shieldsCanvasGroup = shieldsGridContainer.gameObject.AddComponent<CanvasGroup>();
        }

        if (weaponPrevBtn != null) weaponPrevBtn.onClick.AddListener(() => { weaponStartIndex = Mathf.Max(0, weaponStartIndex - 1); StartCoroutine(TransitionWeaponsCarousel()); });
        if (weaponNextBtn != null) weaponNextBtn.onClick.AddListener(() => { weaponStartIndex = Mathf.Min(Mathf.Max(0, currentWeapons.Count - ItemsPerPage), weaponStartIndex + 1); StartCoroutine(TransitionWeaponsCarousel()); });
        if (shieldPrevBtn != null) shieldPrevBtn.onClick.AddListener(() => { shieldStartIndex = Mathf.Max(0, shieldStartIndex - 1); StartCoroutine(TransitionShieldsCarousel()); });
        if (shieldNextBtn != null) shieldNextBtn.onClick.AddListener(() => { shieldStartIndex = Mathf.Min(Mathf.Max(0, currentShields.Count - ItemsPerPage), shieldStartIndex + 1); StartCoroutine(TransitionShieldsCarousel()); });

        BuildCharacterList();

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

        SelectCharacter(selectedCharacterIndex);
    }

    void BuildCharacterList()
    {
        foreach (var c in activeCharCards) if (c != null) Destroy(c);
        activeCharCards.Clear();

        if (characters == null || characterCardTemplate == null || characterListContainer == null) return;

        for (int i = 0; i < characters.Length; i++)
        {
            int index = i;
            CharacterData charData = characters[i];

            GameObject card = Instantiate(characterCardTemplate, characterListContainer);
            card.SetActive(true);
            card.name = $"CharBtn_{charData.characterName}";

            Text nameTxt = card.transform.Find("Text_Name")?.GetComponent<Text>();
            if (nameTxt != null) nameTxt.text = charData.characterName.ToUpper();

            Transform portrait = card.transform.Find("Portrait");
            if (portrait != null)
            {
                portrait.gameObject.SetActive(false);
            }

            Button btn = card.GetComponent<Button>();
            if (btn != null)
            {
                btn.onClick.RemoveAllListeners();
                btn.onClick.AddListener(() => SelectCharacter(index));
            }

            activeCharCards.Add(card);
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
        }

        UpdateEquipmentData(charData);
        HighlightCharacterButton(index);
    }

    void SpawnPreview(CharacterData charData)
    {
        if (currentPreviewInstance != null) Destroy(currentPreviewInstance);
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
            animator.SetBool("IsMoving", false);
            animator.Play("Idle");
        }
    }

    void UpdateEquipmentData(CharacterData charData)
    {
        if (allEquipment == null || equipmentButtonTemplate == null) return;

        currentWeapons = allEquipment.Where(e => e != null && e.slot == EquipSlot.RightHand).ToList();
        currentShields = allEquipment.Where(e => e != null && e.slot == EquipSlot.LeftHand).ToList();

        if (charData.allowedEquipmentTypes != null && charData.allowedEquipmentTypes.Count > 0)
        {
            currentWeapons = currentWeapons.Where(e => charData.allowedEquipmentTypes.Contains(e.equipmentType)).ToList();
            currentShields = currentShields.Where(e => charData.allowedEquipmentTypes.Contains(e.equipmentType)).ToList();
        }

        weaponStartIndex = 0;
        shieldStartIndex = 0;

        RefreshWeaponsCarousel();
        RefreshShieldsCarousel();
    }

    IEnumerator TransitionWeaponsCarousel()
    {
        if (weaponsCanvasGroup != null)
        {
            // Quick fade out
            for (float t = 0; t < 0.15f; t += Time.deltaTime)
            {
                weaponsCanvasGroup.alpha = Mathf.Lerp(1, 0, t / 0.15f);
                yield return null;
            }
            weaponsCanvasGroup.alpha = 0;
        }

        RefreshWeaponsCarousel();

        if (weaponsCanvasGroup != null)
        {
            // Quick fade in
            for (float t = 0; t < 0.15f; t += Time.deltaTime)
            {
                weaponsCanvasGroup.alpha = Mathf.Lerp(0, 1, t / 0.15f);
                yield return null;
            }
            weaponsCanvasGroup.alpha = 1;
        }
    }

    IEnumerator TransitionShieldsCarousel()
    {
        if (shieldsCanvasGroup != null)
        {
            for (float t = 0; t < 0.15f; t += Time.deltaTime)
            {
                shieldsCanvasGroup.alpha = Mathf.Lerp(1, 0, t / 0.15f);
                yield return null;
            }
            shieldsCanvasGroup.alpha = 0;
        }

        RefreshShieldsCarousel();

        if (shieldsCanvasGroup != null)
        {
            for (float t = 0; t < 0.15f; t += Time.deltaTime)
            {
                shieldsCanvasGroup.alpha = Mathf.Lerp(0, 1, t / 0.15f);
                yield return null;
            }
            shieldsCanvasGroup.alpha = 1;
        }
    }

    void RefreshWeaponsCarousel()
    {
        if (weaponPrevBtn != null) weaponPrevBtn.interactable = (weaponStartIndex > 0);
        if (weaponNextBtn != null) weaponNextBtn.interactable = (weaponStartIndex + ItemsPerPage < currentWeapons.Count);

        var displayWeapons = currentWeapons.Skip(weaponStartIndex).Take(ItemsPerPage).ToList();
        UpdateEquipmentPool(activeWeaponBtns, displayWeapons, weaponsGridContainer);
    }

    void RefreshShieldsCarousel()
    {
        if (shieldPrevBtn != null) shieldPrevBtn.interactable = (shieldStartIndex > 0);
        if (shieldNextBtn != null) shieldNextBtn.interactable = (shieldStartIndex + ItemsPerPage < currentShields.Count);

        var displayShields = currentShields.Skip(shieldStartIndex).Take(ItemsPerPage).ToList();
        UpdateEquipmentPool(activeShieldBtns, displayShields, shieldsGridContainer);
    }

    void UpdateEquipmentPool(List<GameObject> buttonPool, List<EquipmentData> displayItems, Transform container)
    {
        // Ensure pool has enough items
        while (buttonPool.Count < displayItems.Count)
        {
            GameObject btnObj = Instantiate(equipmentButtonTemplate, container);
            buttonPool.Add(btnObj);
        }

        // Update active buttons
        for (int i = 0; i < buttonPool.Count; i++)
        {
            if (i < displayItems.Count)
            {
                buttonPool[i].SetActive(true);
                UpdateEquipmentButtonData(buttonPool[i], displayItems[i]);
            }
            else
            {
                buttonPool[i].SetActive(false);
            }
        }

        UpdateEquipmentLabels();
    }

    void UpdateEquipmentButtonData(GameObject btnObj, EquipmentData equip)
    {
        btnObj.name = $"Btn_{equip.equipmentName}";

        Text nameTxt = btnObj.transform.Find("Text_Name")?.GetComponent<Text>();
        if (nameTxt != null) nameTxt.gameObject.SetActive(false);

        Image icon = btnObj.transform.Find("Icon")?.GetComponent<Image>();
        if (icon != null)
        {
            if (equip.icon != null)
            {
                icon.sprite = equip.icon;
                icon.enabled = true;
            }
            else
            {
                icon.enabled = false;
            }
        }

        Transform checkmark = btnObj.transform.Find("Checkmark");
        if (checkmark != null) checkmark.gameObject.SetActive(false);

        Button btn = btnObj.GetComponent<Button>();
        if (btn != null)
        {
            btn.onClick.RemoveAllListeners();
            btn.onClick.AddListener(() => OnEquipmentSelected(equip));
        }
    }

    void OnEquipmentSelected(EquipmentData equipment)
    {
        if (currentEquipmentManager == null || equipment == null) return;
        currentEquipmentManager.Equip(equipment);
        if (GameDataCarrier.Instance != null)
        {
            if (equipment.slot == EquipSlot.RightHand) GameDataCarrier.Instance.equippedRightHand = equipment;
            else GameDataCarrier.Instance.equippedLeftHand = equipment;
        }
        UpdateEquipmentLabels();
    }

    void UpdateEquipmentLabels()
    {
        if (GameDataCarrier.Instance == null) return;
        if (equippedRightText != null)
            equippedRightText.text = GameDataCarrier.Instance.equippedRightHand != null ? $"Weapon: {GameDataCarrier.Instance.equippedRightHand.equipmentName}" : "Weapon: None";
        if (equippedLeftText != null)
            equippedLeftText.text = GameDataCarrier.Instance.equippedLeftHand != null ? $"Shield: {GameDataCarrier.Instance.equippedLeftHand.equipmentName}" : "Shield: None";

        UpdateCheckmarks(activeWeaponBtns, GameDataCarrier.Instance.equippedRightHand);
        UpdateCheckmarks(activeShieldBtns, GameDataCarrier.Instance.equippedLeftHand);
    }

    void UpdateCheckmarks(List<GameObject> buttons, EquipmentData equipped)
    {
        foreach (var btnObj in buttons)
        {
            if (btnObj == null || !btnObj.activeSelf) continue;
            Transform check = btnObj.transform.Find("Checkmark");
            bool isSelected = (equipped != null && btnObj.name == $"Btn_{equipped.equipmentName}");
            
            if (check != null) check.gameObject.SetActive(isSelected);
            
            Outline outline = btnObj.GetComponent<Outline>();
            if (outline != null) outline.effectColor = isSelected ? selectedBorderColor : normalBorderColor;
        }
    }

    void HighlightCharacterButton(int selectedIndex)
    {
        for (int i = 0; i < activeCharCards.Count; i++)
        {
            if (activeCharCards[i] == null) continue;
            Outline outline = activeCharCards[i].GetComponent<Outline>();
            if (outline != null)
                outline.effectColor = (i == selectedIndex) ? selectedBorderColor : normalBorderColor;
        }
    }

    void OnDisable()
    {
        if (currentPreviewInstance != null) Destroy(currentPreviewInstance);
    }
}

