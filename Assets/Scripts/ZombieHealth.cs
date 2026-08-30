using System;
using UnityEngine;

/// <summary>
/// Health component for zombies. Attach to the root of each zombie prefab.
/// Fires OnZombieDied so other systems (spawner, game manager) can react.
/// </summary>
public class ZombieHealth : MonoBehaviour
{
    [Header("Stats")]
    public int maxHealth = 100;
    public int currentHealth;

    [Header("Death FX (optional)")]
    [Tooltip("If assigned, instantiated at death position.")]
    public GameObject deathFXPrefab;
    [Tooltip("Seconds before the object is destroyed / returned to pool after death.")]
    public float deathDelay = 0.5f;

    /// <summary>Fired when this zombie dies. Passes the zombie's root GameObject.</summary>
    public static event Action<GameObject> OnZombieDied;
    public event Action<int, int> HealthChanged;

    private bool isDead = false;

    private void OnEnable()
    {
        currentHealth = maxHealth;
        isDead = false;
        HealthChanged?.Invoke(currentHealth, maxHealth);
    }

    private void Start()
    {
        currentHealth = maxHealth;
        HealthChanged?.Invoke(currentHealth, maxHealth);
    }

    public void TakeDamage(int amount)
    {
        if (isDead) return;

        currentHealth = Mathf.Max(0, currentHealth - Mathf.Max(0, amount));
        HealthChanged?.Invoke(currentHealth, maxHealth);
        Debug.Log($"[ZombieHealth] {gameObject.name} HP: {currentHealth}/{maxHealth}");

        if (currentHealth <= 0)
            Die();
    }

    private void Die()
    {
        if (isDead) return;
        isDead = true;

        if (deathFXPrefab != null)
            Instantiate(deathFXPrefab, transform.position, Quaternion.identity);

        OnZombieDied?.Invoke(gameObject);

        // Notify spawner so it can update its active zombie count
        ZombieSpawner.Instance?.OnZombieDied(gameObject);

        foreach (Collider bodyCollider in GetComponentsInChildren<Collider>())
            bodyCollider.enabled = false;

        ZombiePrototypeMover mover = GetComponent<ZombiePrototypeMover>();
        if (mover != null) mover.enabled = false;
        ZombieAttack attack = GetComponent<ZombieAttack>();
        if (attack != null) attack.enabled = false;

        Destroy(gameObject, deathDelay);
    }
}
