using UnityEngine;

/// <summary>
/// Defensive barrier plant. Boasts high health (400 HP, 4x regular plants)
/// and provides responsive hit-reaction wobble/squash animation when attacked by zombies.
/// </summary>
public class WallnutLogic : PlantBase
{
    [Header("Wallnut Settings")]
    [Tooltip("Base squish intensity when bitten by zombies.")]
    [SerializeField, Range(0.05f, 0.4f)] private float squishIntensity = 0.18f;

    private Vector3 initialChildScale = Vector3.one;
    private Transform visualChild;
    private float hitReactionTime = 0f;
    private const float ReactionDuration = 0.28f;

    protected override void Awake()
    {
        // High durability tank plant
        if (maxHealth <= 100) maxHealth = 400;
        currentHealth = maxHealth;

        base.Awake();

        // Find visual child (the glb model) to apply hit squash without affecting root collider
        if (transform.childCount > 0)
        {
            visualChild = transform.GetChild(0);
            if (visualChild != null)
                initialChildScale = visualChild.localScale;
        }
    }

    public override void TakeDamage(int amount)
    {
        base.TakeDamage(amount);

        if (currentHealth <= 0) return;

        // Trigger squash & wobble reaction when bitten
        hitReactionTime = ReactionDuration;

        // Ensure health bar is immediately revealed
        PlantHealthBar healthBar = GetComponent<PlantHealthBar>();
        if (healthBar != null)
            healthBar.TriggerDamageReveal();
    }

    private void Update()
    {
        if (visualChild == null || hitReactionTime <= 0f) return;

        hitReactionTime -= Time.deltaTime;
        float progress = Mathf.Clamp01(1f - (hitReactionTime / ReactionDuration));

        // Damped sine wave for springy hit bounce: squishes down (Y) and bulges out (X, Z) then bounces back
        float wobble = Mathf.Sin(progress * Mathf.PI * 2f) * (1f - progress) * squishIntensity;

        visualChild.localScale = new Vector3(
            initialChildScale.x * (1f + wobble * 0.8f),
            initialChildScale.y * (1f - wobble),
            initialChildScale.z * (1f + wobble * 0.8f)
        );

        if (hitReactionTime <= 0f)
            visualChild.localScale = initialChildScale;
    }
}
