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
    public Image cardBackground;        // The main card image (wood/brown frame)
    public Image portraitImage;         // The plant portrait (RenderTexture capture)
    public Image selectionBorder;       // Gold border shown when selected
    public Image cooldownOverlay;       // Radial360 dark overlay for cooldown
    public Image insufficientFlash;     // Not used on the card anymore, kept for backwards compatibility
    public Text  costText;              // Cost number at bottom of card
    public Image sunIcon;               // Small sun icon next to cost (optional)

    [Header("Card Data")]
    public int plantIndex = -1;         // Which plant in PlayerController.plants[] this card maps to
    public bool isShovelCard = false;   // Special flag for the shovel button

    // Visual settings
    [Header("Visual Settings")]
    public Color selectedBorderColor = new Color(1f, 0.85f, 0f, 1f);   // Gold
    public Color normalBorderColor   = new Color(0f, 0f, 0f, 0f);       // Transparent (hidden)
    public float selectedScaleBoost = 1.08f;                            // Card scale when selected

    // Private state
    private PlayerController player;
    private RectTransform rt;
    private Vector3 normalScale;
    private bool wasSelected = false;
    private Color originalCardColor = Color.white;

    void Awake()
    {
        rt = GetComponent<RectTransform>();
        normalScale = rt.localScale;
    }

    void Start()
    {
        player = FindObjectOfType<PlayerController>();
        if (selectionBorder != null) selectionBorder.color = normalBorderColor;
        if (insufficientFlash != null) insufficientFlash.gameObject.SetActive(false); // Hide the old flash
        if (cardBackground != null) originalCardColor = cardBackground.color;
    }

    /// <summary>
    /// Called every frame by GameUIManager to push current state.
    /// </summary>
    public void UpdateCard(PlantData data, bool isSelected, int currentSun)
    {
        

        // Portrait
        if (portraitImage != null && data.portrait != null)
        {
            portraitImage.sprite = data.portrait;
        }

        // Cost text
        if (costText != null)
            costText.text = data.cost.ToString();

        // Cooldown overlay
        if (cooldownOverlay != null)
        {
            float fill = (data.cooldownTime > 0f) ? Mathf.Clamp01(data.currentCooldown / data.cooldownTime) : 0f;
            cooldownOverlay.fillAmount = fill;
        }

        // Selection highlight
        bool notEnoughSun = (currentSun < data.cost);

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

    /// <summary>
    /// Click handler to select this plant (or shovel mode) in PlayerController.
    /// </summary>
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
