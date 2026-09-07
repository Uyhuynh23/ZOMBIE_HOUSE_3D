using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// A small world-space health bar for any PlantBase. It stays hidden until an
/// enemy actually damages the plant, keeping the field readable during play.
/// </summary>
[RequireComponent(typeof(PlantBase))]
public sealed class PlantHealthBar : MonoBehaviour
{
    [SerializeField] private Vector3 worldOffset = new Vector3(0f, 1.35f, 0f);
    [SerializeField, Min(0.1f)] private float visibleAfterHit = 2f;

    private PlantBase plant;
    private GameObject barObject;
    private Transform barTransform;
    private RectTransform fill;
    private float hideAt;

    private void Awake()
    {
        plant = GetComponent<PlantBase>();
        BuildBar();
    }

    private void OnEnable()
    {
        if (plant == null) plant = GetComponent<PlantBase>();
        if (plant != null) plant.HealthChanged += OnHealthChanged;
    }

    private void OnDisable()
    {
        if (plant != null) plant.HealthChanged -= OnHealthChanged;
    }

    private void LateUpdate()
    {
        if (barObject == null || !barObject.activeSelf) return;

        if (Camera.main != null)
            barTransform.rotation = Camera.main.transform.rotation;

        if (Time.time >= hideAt)
            barObject.SetActive(false);
    }

    private void OnHealthChanged(int current, int maximum)
    {
        if (fill == null) return;

        float ratio = maximum <= 0 ? 0f : Mathf.Clamp01((float)current / maximum);
        fill.localScale = new Vector3(ratio, 1f, 1f);
        barObject.SetActive(true);
        hideAt = Time.time + visibleAfterHit;
    }

    private void BuildBar()
    {
        barObject = new GameObject("Plant Health Bar");
        barObject.transform.SetParent(transform, false);
        barObject.transform.localPosition = worldOffset;
        barObject.transform.localScale = Vector3.one * 0.012f;
        barTransform = barObject.transform;

        Canvas canvas = barObject.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.WorldSpace;
        canvas.overrideSorting = true;
        canvas.sortingOrder = 21;
        RectTransform canvasRect = canvas.GetComponent<RectTransform>();
        canvasRect.sizeDelta = new Vector2(90f, 10f);

        GameObject background = new GameObject("Background");
        background.transform.SetParent(barTransform, false);
        Image backgroundImage = background.AddComponent<Image>();
        backgroundImage.color = new Color(0.15f, 0.02f, 0.02f, 0.95f);
        Stretch(backgroundImage.rectTransform, Vector2.zero, Vector2.zero);

        GameObject fillObject = new GameObject("Fill");
        fillObject.transform.SetParent(background.transform, false);
        Image fillImage = fillObject.AddComponent<Image>();
        fillImage.color = new Color(0.25f, 0.95f, 0.22f, 1f);
        fill = fillImage.rectTransform;
        Stretch(fill, new Vector2(2f, 2f), new Vector2(-2f, -2f));
        fill.pivot = new Vector2(0f, 0.5f);

        barObject.SetActive(false);
    }

    private static void Stretch(RectTransform rect, Vector2 offsetMin, Vector2 offsetMax)
    {
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = offsetMin;
        rect.offsetMax = offsetMax;
    }
}
