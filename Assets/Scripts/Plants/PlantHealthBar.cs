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
    [Header("Positioning & Height")]
    [Tooltip("Extra vertical clearance in meters above the highest point of the plant model.")]
    [SerializeField, Min(0f)] private float heightAbovePlant = 0.5f;
    [Tooltip("Manual fine-tuning offset in world space.")]
    [SerializeField] private Vector3 worldOffset = Vector3.zero;
    [SerializeField, Min(0.001f)] private float worldSpaceScale = 0.012f;

    [Header("Visibility / UX")]
    [Tooltip("Hide health bar when plant is at full health.")]
    [SerializeField] private bool hideAtFullHealth = true;
    [Tooltip("Seconds to keep the health bar visible after taking damage. Set to 0 to keep it visible continuously while damaged.")]
    [SerializeField, Min(0f)] private float hideDelayAfterAttack = 4.0f;

    [Header("Optional Custom Art")]
    [SerializeField] private Sprite backgroundSprite;
    [SerializeField] private Sprite fillSprite;
    [SerializeField] private Sprite iconSprite;

    private PlantBase plant;
    private RectTransform fill;
    private Image fillImage;
    private Transform canvasTransform;
    private float hideTimer;
    private bool isUnderAttack;

    public bool IsVisible => canvasTransform != null && canvasTransform.gameObject.activeSelf;
    public float HeightAbovePlant { get => heightAbovePlant; set => heightAbovePlant = value; }
    public float HideDelayAfterAttack { get => hideDelayAfterAttack; set => hideDelayAfterAttack = value; }

    public void Initialize()
    {
        if (plant == null) plant = GetComponent<PlantBase>();
        BuildBar();

        if (plant != null)
        {
            plant.HealthChanged -= OnHealthChanged;
            plant.HealthChanged += OnHealthChanged;
            Refresh(plant.currentHealth, plant.maxHealth);

            if (hideAtFullHealth && plant.currentHealth >= plant.maxHealth)
            {
                SetVisible(false);
                isUnderAttack = false;
            }
            else
            {
                SetVisible(plant.currentHealth > 0);
            }
        }
    }

    private void Awake()
    {
        Initialize();
    }

    private void OnEnable()
    {
        Initialize();
    }

    private void OnDisable()
    {
        if (plant != null) plant.HealthChanged -= OnHealthChanged;
    }

    private void Update()
    {
        if (hideAtFullHealth && hideDelayAfterAttack > 0f && isUnderAttack)
        {
            hideTimer -= Time.deltaTime;
            if (hideTimer <= 0f)
            {
                isUnderAttack = false;
                SetVisible(false);
            }
        }
    }

    private void LateUpdate()
    {
        UpdateBarTransform();

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
            UpdateBarTransform();

            if (hideAtFullHealth && (plant == null || plant.currentHealth >= plant.maxHealth))
                SetVisible(false);
            else
                SetVisible(plant == null || plant.currentHealth > 0);
            return;
        }

        // Fallback: build procedural health bar if not already present in prefab
        GameObject canvasObject = new GameObject("Plant Health Bar");
        canvasObject.transform.SetParent(transform, false);
        canvasTransform = canvasObject.transform;

        Canvas canvas = canvasObject.AddComponent<Canvas>();
        ConfigureCanvas(canvas);
        UpdateBarTransform();
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

        if (hideAtFullHealth && (plant == null || plant.currentHealth >= plant.maxHealth))
            SetVisible(false);
        else
            SetVisible(plant == null || plant.currentHealth > 0);
    }

    private void ConfigureCanvas(Canvas canvas)
    {
        if (canvas == null) return;
        canvas.renderMode = RenderMode.WorldSpace;
        canvas.overrideSorting = true;
        canvas.sortingOrder = 50;
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

    private float GetPlantTopWorldY()
    {
        float maxY = transform.position.y;
        bool found = false;
        Renderer[] renderers = GetComponentsInChildren<Renderer>();
        foreach (Renderer r in renderers)
        {
            if (canvasTransform != null && r.transform.IsChildOf(canvasTransform)) continue;
            if (!r.enabled || !r.gameObject.activeInHierarchy) continue;

            if (!found)
            {
                maxY = r.bounds.max.y;
                found = true;
            }
            else
            {
                maxY = Mathf.Max(maxY, r.bounds.max.y);
            }
        }

        if (!found)
        {
            Collider col = GetComponent<Collider>();
            if (col != null) maxY = col.bounds.max.y;
            else maxY = transform.position.y + 1.25f;
        }

        return maxY;
    }

    private void UpdateBarTransform()
    {
        if (canvasTransform == null) return;

        // Position directly in world space above the topmost point of the plant mesh
        float topY = GetPlantTopWorldY();
        Vector3 targetWorldPos = new Vector3(
            transform.position.x + worldOffset.x,
            topY + heightAbovePlant + worldOffset.y,
            transform.position.z + worldOffset.z
        );
        canvasTransform.position = targetWorldPos;

        // Face the main camera
        if (Camera.main != null)
            canvasTransform.rotation = Camera.main.transform.rotation;

        // Scale proportionally so world size remains consistent regardless of parent lossy scale
        float parentScale = Mathf.Max(0.001f, transform.lossyScale.x, transform.lossyScale.y, transform.lossyScale.z);
        canvasTransform.localScale = Vector3.one * (worldSpaceScale / parentScale);
    }

    private void OnHealthChanged(int current, int maximum)
    {
        if (current < maximum && current > 0)
        {
            isUnderAttack = true;
            hideTimer = hideDelayAfterAttack;
            SetVisible(true);
        }
        else if (current <= 0)
        {
            isUnderAttack = false;
            SetVisible(false);
        }
        else if (current >= maximum)
        {
            isUnderAttack = false;
            SetVisible(!hideAtFullHealth);
        }

        Refresh(current, maximum);
    }

    public void TriggerDamageReveal()
    {
        if (plant != null && plant.currentHealth <= 0) return;
        isUnderAttack = true;
        hideTimer = hideDelayAfterAttack;
        SetVisible(true);
    }

    private void SetVisible(bool visible)
    {
        if (canvasTransform != null && canvasTransform.gameObject.activeSelf != visible)
        {
            canvasTransform.gameObject.SetActive(visible);
        }
    }

    private void Refresh(int current, int maximum)
    {
        if (fill == null || canvasTransform == null) return;

        if (current <= 0)
        {
            SetVisible(false);
            return;
        }

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
