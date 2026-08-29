using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

/// <summary>
/// Represents a single plant card in the PvZ-style HUD.
/// Handles: portrait, cost display, cooldown overlay, selection highlight.
/// </summary>
public class PlantCardUI : MonoBehaviour, IPointerClickHandler
{
    [Header("Card References")]
    public Image cardBackground;
    public Image portraitImage;
    public Image selectionBorder;
    public Image cooldownOverlay;
    public Image insufficientFlash;     // Legacy — kept for backwards compatibility
    public Text  costText;
    public Image sunIcon;

    [Header("Card Data")]
    public int plantIndex = -1;
    public bool isShovelCard = false;

    [Header("Visual Settings")]
    public Color selectedBorderColor = new Color(1f, 0.85f, 0f, 1f);
    public Color normalBorderColor   = new Color(0f, 0f, 0f, 0f);
    public float selectedScaleBoost = 1.08f;

    private PlayerController player;
    private RectTransform rt;
    private Vector3 normalScale;
    private bool wasSelected = false;
    private Color originalCardColor = Color.white;
    private int lastCostSet = -1; // Track to avoid per-frame string alloc

    void Awake()
    {
        rt = GetComponent<RectTransform>();
        normalScale = rt.localScale;
    }

    void Start()
    {
        player = FindObjectOfType<PlayerController>();
        if (selectionBorder != null) selectionBorder.color = normalBorderColor;
        if (insufficientFlash != null) insufficientFlash.gameObject.SetActive(false);
        if (cardBackground != null) originalCardColor = cardBackground.color;
    }

    public void UpdateCard(PlantData data, bool isSelected, int currentSun)
    {
        // Portrait (set sprite only when changed)
        if (portraitImage != null && data.portrait != null)
        {
            if (portraitImage.sprite != data.portrait)
                portraitImage.sprite = data.portrait;
        }

        // Cost text — only update when cost changes (avoids per-frame string alloc)
        if (costText != null && data.cost != lastCostSet)
        {
            costText.text = data.cost.ToString();
            lastCostSet = data.cost;
        }

        // Cooldown overlay
        if (cooldownOverlay != null)
        {
            float fill = (data.cooldownTime > 0f) ? Mathf.Clamp01(data.currentCooldown / data.cooldownTime) : 0f;
            cooldownOverlay.fillAmount = fill;
        }

        // Selection highlight
        if (selectionBorder != null)
            selectionBorder.color = isSelected ? selectedBorderColor : normalBorderColor;

        // Scale boost when selected
        if (isSelected && !wasSelected)
            rt.localScale = normalScale * selectedScaleBoost;
        else if (!isSelected && wasSelected)
            rt.localScale = normalScale;
        wasSelected = isSelected;

        // Dim card if not affordable or on cooldown
        bool canAfford = currentSun >= data.cost;
        bool onCooldown = data.currentCooldown > 0f;
        Color dimColor = new Color(0.4f, 0.4f, 0.4f, 1f);
        
        if (cardBackground != null)
        {
            cardBackground.color = (canAfford && !onCooldown) ? originalCardColor : (originalCardColor * dimColor);
        }

        if (portraitImage != null)
        {
            portraitImage.color = (canAfford && !onCooldown) ? Color.white : dimColor;
        }
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        if (player == null) return;

        if (isShovelCard)
        {
            player.SetShovelMode(true);
        }
        else if (plantIndex >= 0 && player.plants != null && plantIndex < player.plants.Length)
        {
            player.SelectPlant(plantIndex);
        }
    }
}
