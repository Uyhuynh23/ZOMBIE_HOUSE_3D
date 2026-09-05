using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(ZombieHealth))]
public sealed class ZombieHealthBar : MonoBehaviour
{
    [SerializeField] private Vector3 worldOffset = new Vector3(0f, 2.3f, 0f);
    [SerializeField, Min(0.001f)] private float worldSpaceScale = 0.012f;
    private ZombieHealth health;
    private RectTransform fill;
    private Transform canvasTransform;

    private void Awake()
    {
        health = GetComponent<ZombieHealth>();
        BuildBar();
    }

    private void OnEnable()
    {
        if (health == null) health = GetComponent<ZombieHealth>();
        health.HealthChanged += Refresh;
        Refresh(health.currentHealth, health.maxHealth);
    }

    private void OnDisable()
    {
        if (health != null) health.HealthChanged -= Refresh;
    }

    private void LateUpdate()
    {
        if (canvasTransform != null && Camera.main != null)
            canvasTransform.rotation = Camera.main.transform.rotation;

        // Keep the displayed value in sync even if a pooled enemy is enabled
        // before the health event subscription is established.
        if (health != null)
            Refresh(health.currentHealth, health.maxHealth);
    }

    private void BuildBar()
    {
        // Zombie and Spider prefabs already contain a world-space bar. Reuse
        // it instead of drawing a second bar on top: the old static Fill was
        // the reason an apparently full green bar remained after damage.
        Transform existingBar = transform.Find("Zombie Health Bar");
        RectTransform existingFill = existingBar != null
            ? existingBar.Find("Background/Fill") as RectTransform
            : null;
        if (existingBar != null && existingFill != null)
        {
            canvasTransform = existingBar;
            fill = existingFill;
            ConfigureCanvas(existingBar.GetComponent<Canvas>());
            SetBarTransform();
            return;
        }

        GameObject canvasObject = new GameObject("Zombie Health Bar");
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
        backgroundImage.color = new Color(0.12f, 0.03f, 0.03f, 0.95f);
        Stretch(backgroundImage.rectTransform);

        GameObject fillObject = new GameObject("Fill");
        fillObject.transform.SetParent(background.transform, false);
        Image fillImage = fillObject.AddComponent<Image>();
        fillImage.color = new Color(0.25f, 0.95f, 0.22f, 1f);
        fill = fillImage.rectTransform;
        fill.anchorMin = Vector2.zero;
        fill.anchorMax = Vector2.one;
        fill.pivot = new Vector2(0f, 0.5f);
        fill.offsetMin = new Vector2(2f, 2f);
        fill.offsetMax = new Vector2(-2f, -2f);
    }

    private void ConfigureCanvas(Canvas canvas)
    {
        if (canvas == null) return;
        canvas.renderMode = RenderMode.WorldSpace;
        canvas.overrideSorting = true;
        canvas.sortingOrder = 20;
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
        if (fill == null) return;
        float ratio = maximum <= 0 ? 0f : Mathf.Clamp01((float)current / maximum);
        fill.anchorMax = new Vector2(ratio, 1f);
    }

    private static void Stretch(RectTransform rect)
    {
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
    }
}
