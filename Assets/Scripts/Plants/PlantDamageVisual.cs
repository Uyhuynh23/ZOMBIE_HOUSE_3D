using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Lightweight, modular plant damage presentation. Optional staged GameObjects
/// can be assigned later for custom damaged meshes; otherwise a MaterialProperty
/// Block applies a reversible tint without cloning materials or deforming meshes.
/// </summary>
[RequireComponent(typeof(PlantBase))]
public sealed class PlantDamageVisual : MonoBehaviour
{
    [Tooltip("Optional visual states ordered from light damage to severe damage.")]
    [SerializeField] private GameObject[] damageStages;
    [SerializeField, Range(0f, 1f)] private float lightDamageThreshold = 0.66f;
    [SerializeField, Range(0f, 1f)] private float severeDamageThreshold = 0.33f;
    [SerializeField] private bool useFallbackTint = true;

    private PlantBase plant;
    private readonly List<Renderer> renderers = new List<Renderer>();
    private readonly Dictionary<Renderer, Color> originalColors = new Dictionary<Renderer, Color>();
    private MaterialPropertyBlock propertyBlock;

    private void Awake()
    {
        plant = GetComponent<PlantBase>();
        propertyBlock = new MaterialPropertyBlock();
        foreach (Renderer renderer in GetComponentsInChildren<Renderer>(true))
        {
            if (renderer == null) continue;
            renderers.Add(renderer);
            Color color = Color.white;
            if (renderer.sharedMaterial != null)
            {
                if (renderer.sharedMaterial.HasProperty("_BaseColor"))
                    color = renderer.sharedMaterial.GetColor("_BaseColor");
                else if (renderer.sharedMaterial.HasProperty("_Color"))
                    color = renderer.sharedMaterial.color;
            }
            originalColors[renderer] = color;
        }

        if (damageStages != null)
            SetStages(-1);
    }

    private void OnEnable()
    {
        if (plant == null) plant = GetComponent<PlantBase>();
        if (plant != null) plant.HealthChanged += OnHealthChanged;
    }

    private void OnDisable()
    {
        if (plant != null) plant.HealthChanged -= OnHealthChanged;
        ResetTint();
    }

    private void OnHealthChanged(int current, int maximum)
    {
        float ratio = maximum <= 0 ? 0f : Mathf.Clamp01((float)current / maximum);
        int stage = ratio <= severeDamageThreshold ? 1 : ratio <= lightDamageThreshold ? 0 : -1;
        SetStages(stage);

        if (!useFallbackTint || (damageStages != null && damageStages.Length > 0)) return;

        float damage = 1f - ratio;
        Color tint = Color.Lerp(Color.white, new Color(0.62f, 0.62f, 0.62f, 1f), damage * 0.65f);
        foreach (Renderer renderer in renderers)
        {
            if (renderer == null) continue;
            renderer.GetPropertyBlock(propertyBlock);
            if (renderer.sharedMaterial != null && renderer.sharedMaterial.HasProperty("_BaseColor"))
                propertyBlock.SetColor("_BaseColor", MultiplyAlpha(originalColors[renderer], tint));
            else if (renderer.sharedMaterial != null && renderer.sharedMaterial.HasProperty("_Color"))
                propertyBlock.SetColor("_Color", MultiplyAlpha(originalColors[renderer], tint));
            renderer.SetPropertyBlock(propertyBlock);
        }
    }

    private void SetStages(int activeStage)
    {
        if (damageStages == null) return;
        for (int i = 0; i < damageStages.Length; i++)
            if (damageStages[i] != null) damageStages[i].SetActive(i == activeStage);
    }

    private void ResetTint()
    {
        foreach (Renderer renderer in renderers)
            if (renderer != null) renderer.SetPropertyBlock(null);
    }

    private static Color MultiplyAlpha(Color a, Color b)
    {
        return new Color(a.r * b.r, a.g * b.g, a.b * b.b, a.a);
    }
}
