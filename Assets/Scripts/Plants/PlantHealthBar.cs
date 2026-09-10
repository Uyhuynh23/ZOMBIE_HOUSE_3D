using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// World-space health bar for any PlantBase. Follows the same architecture and visual
/// style as ZombieHealthBar (Spider.prefab), supporting both pre-baked prefab UI
/// hierarchies and procedural generation fallbacks.
/// </summary>
[RequireComponent(typeof(PlantBase))]
public sealed class PlantHealthBar : MonoBehaviour
{
    [SerializeField] private Vector3 worldOffset = new Vector3(0f, 1.25f, 0f);
    [SerializeField, Min(0.001f)] private float worldSpaceScale = 0.012f;
    [Header("Optional Custom Art")]
    [SerializeField] private Sprite backgroundSprite;
    [SerializeField] private Sprite fillSprite;
    [SerializeField] private Sprite iconSprite;

    private PlantBase plant;
    private RectTransform fill;
    private Image fillImage;
    private Transform canvasTransform;

    private void Awake()
    {
        plant = GetComponent<PlantBase>();
        BuildBar();
    }

    private void OnEnable()
    {
        if (plant == null) plant = GetComponent<PlantBase>();
        if (plant != null)
        {
            plant.HealthChanged += Refresh;
            Refresh(plant.currentHealth, plant.maxHealth);
        }
    }

    private void OnDisable()
    {
        if (plant != null) plant.HealthChanged -= Refresh;
    }

    private void LateUpdate()
    {
        if (canvasTransform == null) return;

        if (Camera.main != null)
            canvasTransform.rotation = Camera.main.transform.rotation;

        // Keep localScale scaled against parent lossy scale so world size stays consistent
        float parentScale = Mathf.Max(0.001f, transform.lossyScale.x, transform.lossyScale.y, transform.lossyScale.z);
        canvasTransform.localScale = Vector3.one * (worldSpaceScale / parentScale);
        canvasTransform.localPosition = worldOffset;

        if (plant != null)
            Refresh(plant.currentHealth, plant.maxHealth);
    }

    private void BuildBar()
    {
        // Check if the plant prefab already contains a "Plant Health Bar" (matching Spider.prefab)
        Transform existingBar = transform.Find("Plant Health Bar");
        RectTransform existingFill = existingBar != null
            ? existingBar.Find("Background/Fill") as RectTransform
            : null;

        if (existingBar != null && existingFill != null)
        {
            canvasTransform = existingBar;
            fill = existingFill;
            fillImage = existingFill.GetComponent<Image>();
            ApplySprites(existingBar);
            ConfigureCanvas(existingBar.GetComponent<Canvas>());
            SetBarTransform();
            return;
        }

        // Fallback: build procedural health bar if not already present in prefab
        GameObject canvasObject = new GameObject("Plant Health Bar");
        canvasObject.transform.SetParent(transform, false);
        canvasTransform = canvasObject.transform;

        Canvas canvas = canvasObject.AddComponent<Canvas>();
        ConfigureCanvas(canvas);
        SetBarTransform();
        RectTransform canvasRect = canvas.GetComponent<RectTransform>();
        canvasRect.sizeDelta = new Vector2(100f, 12f);

        GameObject background = new GameObject("Background");
        background.transform.SetParent(canvasObject.transform, false);
        Image backgroundImage = background.AddComponent<Image>();
        backgroundImage.sprite = backgroundSprite;
        backgroundImage.type = backgroundSprite != null ? Image.Type.Sliced : Image.Type.Simple;
        backgroundImage.color = new Color(0.12f, 0.03f, 0.03f, 0.95f);
        Stretch(backgroundImage.rectTransform);

        GameObject fillObject = new GameObject("Fill");
        fillObject.transform.SetParent(background.transform, false);
        Image newFillImage = fillObject.AddComponent<Image>();
        newFillImage.sprite = fillSprite;
        newFillImage.type = Image.Type.Simple;
        this.fillImage = newFillImage;
        this.fillImage.color = new Color(0.25f, 0.95f, 0.22f, 1f);
        fill = this.fillImage.rectTransform;
        fill.anchorMin = Vector2.zero;
        fill.anchorMax = Vector2.one;
        fill.pivot = new Vector2(0f, 0.5f);
        fill.offsetMin = new Vector2(2f, 2f);
        fill.offsetMax = new Vector2(-2f, -2f);

        if (iconSprite != null)
        {
            GameObject icon = new GameObject("Icon");
            icon.transform.SetParent(canvasObject.transform, false);
            Image iconImage = icon.AddComponent<Image>();
            iconImage.sprite = iconSprite;
            iconImage.preserveAspect = true;
            iconImage.rectTransform.anchorMin = new Vector2(0f, 0.5f);
            iconImage.rectTransform.anchorMax = new Vector2(0f, 0.5f);
            iconImage.rectTransform.sizeDelta = new Vector2(18f, 18f);
            iconImage.rectTransform.anchoredPosition = new Vector2(-10f, 0f);
        }
    }

    private void ConfigureCanvas(Canvas canvas)
    {
        if (canvas == null) return;
        canvas.renderMode = RenderMode.WorldSpace;
        canvas.overrideSorting = true;
        canvas.sortingOrder = 20;
    }

    private void ApplySprites(Transform existingBar)
    {
        Image background = existingBar.Find("Background")?.GetComponent<Image>();
        if (background != null && backgroundSprite != null)
        {
            background.sprite = backgroundSprite;
            background.type = Image.Type.Sliced;
        }
        if (fillImage != null && fillSprite != null)
            fillImage.sprite = fillSprite;
    }

    private void SetBarTransform()
    {
        if (canvasTransform == null) return;

        canvasTransform.localPosition = worldOffset;
        float parentScale = Mathf.Max(0.001f, transform.lossyScale.x, transform.lossyScale.y, transform.lossyScale.z);
        canvasTransform.localScale = Vector3.one * (worldSpaceScale / parentScale);
    }

    private void Refresh(int current, int maximum)
    {
        if (fill == null || canvasTransform == null) return;

        if (current <= 0)
        {
            if (canvasTransform.gameObject.activeSelf)
                canvasTransform.gameObject.SetActive(false);
            return;
        }

        if (!canvasTransform.gameObject.activeSelf)
            canvasTransform.gameObject.SetActive(true);

        float ratio = maximum <= 0 ? 0f : Mathf.Clamp01((float)current / maximum);

        fill.anchorMin = Vector2.zero;
        fill.anchorMax = Vector2.one;
        fill.pivot = new Vector2(0f, 0.5f);
        fill.offsetMin = new Vector2(2f, 2f);
        fill.offsetMax = new Vector2(-2f, -2f);
        fill.localScale = new Vector3(ratio, 1f, 1f);

        if (fillImage == null) fillImage = fill.GetComponent<Image>();
        if (fillImage != null)
        {
            fillImage.type = Image.Type.Simple;
        }
    }

    private static void Stretch(RectTransform rect)
    {
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
    }
}
