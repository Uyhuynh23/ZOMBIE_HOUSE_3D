using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>
/// Controls an interactive map selection card with smooth hover effects,
/// dynamic scaling, border highlights, and click-to-play round loading.
/// </summary>
public class MapCardUI : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerDownHandler, IPointerUpHandler, IPointerClickHandler
{
    [Header("Round Configuration")]
    [Tooltip("Round index (1 = Map_Day, 2 = Map_Cloudy, 3 = Map_Night)")]
    public int roundNumber = 1;
    public string mapDisplayName = "ROUND 1 - DAY";

    [Header("UI References")]
    public Image mapThumbnail;
    public Text mapNameText;
    public Image cardFrame;
    public Image highlightOutline;

    [Header("Hover & Animation Settings")]
    public float hoverScale = 1.06f;
    public float pressScale = 0.98f;
    public float transitionSpeed = 12f;

    [Header("Colors")]
    public Color normalBorderColor = new Color(0.25f, 0.25f, 0.25f, 0.85f);
    public Color hoverBorderColor = new Color(1f, 0.82f, 0.15f, 1f);
    public Color normalTextColor = Color.white;
    public Color hoverTextColor = new Color(1f, 0.9f, 0.4f, 1f);

    private Vector3 targetScale = Vector3.one;
    private Color targetBorderColor;
    private Color targetTextColor;
    private bool isHovered = false;
    private bool isPressed = false;

    void Awake()
    {
        targetScale = Vector3.one;
        targetBorderColor = normalBorderColor;
        targetTextColor = normalTextColor;

        if (cardFrame != null)
            cardFrame.color = normalBorderColor;

        if (highlightOutline != null)
            highlightOutline.color = new Color(1f, 0.85f, 0.2f, 0f);

        if (mapNameText != null && !string.IsNullOrEmpty(mapDisplayName))
            mapNameText.text = mapDisplayName;
    }

    void Update()
    {
        // Smooth scale interpolation (using unscaled delta time so it works even if timeScale is paused)
        transform.localScale = Vector3.Lerp(transform.localScale, targetScale, Time.unscaledDeltaTime * transitionSpeed);

        // Smooth frame border color transition
        if (cardFrame != null)
        {
            cardFrame.color = Color.Lerp(cardFrame.color, targetBorderColor, Time.unscaledDeltaTime * transitionSpeed);
        }

        // Smooth outline glow transition
        if (highlightOutline != null)
        {
            Color targetGlow = isHovered ? new Color(1f, 0.85f, 0.2f, 0.8f) : new Color(1f, 0.85f, 0.2f, 0f);
            highlightOutline.color = Color.Lerp(highlightOutline.color, targetGlow, Time.unscaledDeltaTime * transitionSpeed);
        }

        // Smooth text color transition
        if (mapNameText != null)
        {
            mapNameText.color = Color.Lerp(mapNameText.color, targetTextColor, Time.unscaledDeltaTime * transitionSpeed);
        }
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        isHovered = true;
        if (!isPressed)
        {
            targetScale = Vector3.one * hoverScale;
        }
        targetBorderColor = hoverBorderColor;
        targetTextColor = hoverTextColor;
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        isHovered = false;
        isPressed = false;
        targetScale = Vector3.one;
        targetBorderColor = normalBorderColor;
        targetTextColor = normalTextColor;
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        isPressed = true;
        targetScale = Vector3.one * pressScale;
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        isPressed = false;
        targetScale = isHovered ? (Vector3.one * hoverScale) : Vector3.one;
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        Debug.Log($"[MapCardUI] Clicked map card '{gameObject.name}' (round={roundNumber})");

        MainMenuManager mmm = Object.FindFirstObjectByType<MainMenuManager>();
        if (mmm != null)
        {
            mmm.OnRoundSelected(roundNumber);
        }
        else
        {
            Debug.LogError("[MapCardUI] MainMenuManager not found!");
        }
    }
}
