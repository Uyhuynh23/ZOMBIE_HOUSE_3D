using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>
/// Attach to the Back button on MapSelectionPanel.
/// Provides smooth hover scaling, text/button color highlight, and returns to MainMenu.
/// </summary>
[DefaultExecutionOrder(100)]
[RequireComponent(typeof(Button))]
public class BackToMainMenuButtonHandler : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerDownHandler, IPointerUpHandler
{
    [Header("Hover & Animation")]
    public float hoverScale = 1.08f;
    public float pressScale = 0.95f;
    public float transitionSpeed = 12f;

    [Header("Colors")]
    public Color normalTextColor = Color.white;
    public Color hoverTextColor = new Color(1f, 0.9f, 0.35f, 1f);
    public Color normalButtonColor = Color.white;
    public Color hoverButtonColor = new Color(1f, 0.98f, 0.88f, 1f);

    private Button _btn;
    private Image _img;
    private Text _text;

    private Vector3 targetScale = Vector3.one;
    private Color targetTextColor;
    private Color targetButtonColor;
    private bool isHovered = false;
    private bool isPressed = false;

    void Awake()
    {
        _btn = GetComponent<Button>();
        _img = GetComponent<Image>();
        _text = GetComponentInChildren<Text>();

        targetScale = Vector3.one;
        targetTextColor = normalTextColor;
        targetButtonColor = normalButtonColor;

        _btn.onClick = new Button.ButtonClickedEvent();
        _btn.onClick.AddListener(OnClick);
        Debug.Log($"[BackToMainMenuButtonHandler] Awake: {gameObject.name} wired with hover effects.");
    }

    void Update()
    {
        // Smooth scale transition
        transform.localScale = Vector3.Lerp(transform.localScale, targetScale, Time.unscaledDeltaTime * transitionSpeed);

        // Smooth text color transition
        if (_text != null)
        {
            _text.color = Color.Lerp(_text.color, targetTextColor, Time.unscaledDeltaTime * transitionSpeed);
        }

        // Smooth button image color transition
        if (_img != null)
        {
            _img.color = Color.Lerp(_img.color, targetButtonColor, Time.unscaledDeltaTime * transitionSpeed);
        }
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        isHovered = true;
        if (!isPressed)
        {
            targetScale = Vector3.one * hoverScale;
        }
        targetTextColor = hoverTextColor;
        targetButtonColor = hoverButtonColor;
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        isHovered = false;
        isPressed = false;
        targetScale = Vector3.one;
        targetTextColor = normalTextColor;
        targetButtonColor = normalButtonColor;
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

    private void OnClick()
    {
        Debug.Log($"[BackToMainMenuButtonHandler] CLICKED '{gameObject.name}' -> ShowMainMenu()");

        MainMenuManager mmm = Object.FindFirstObjectByType<MainMenuManager>();
        if (mmm != null)
        {
            mmm.ShowMainMenu();
        }
        else
        {
            Debug.LogError("[BackToMainMenuButtonHandler] MainMenuManager not found in scene!");
        }
    }
}
