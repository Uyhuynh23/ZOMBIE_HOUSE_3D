using System;
using UnityEngine;

/// <summary>
/// Health component for all enemy types (Zombie, Spider).
/// Fires OnZombieDied so spawner and game manager can react.
/// Triggers hit stagger on EnemyNavAgent when damaged.
/// </summary>
public class ZombieHealth : MonoBehaviour
{
    [Header("Stats")]
    public int maxHealth = 100;
    public int currentHealth;

    [Header("Death FX (optional)")]
    [Tooltip("If assigned, instantiated at death position.")]
    public GameObject deathFXPrefab;
    [Tooltip("Seconds before the object is destroyed after death.")]
    public float deathDelay = 0.5f;

    /// <summary>Fired when this enemy dies. Passes the root GameObject.</summary>
    public static event Action<GameObject> OnZombieDied;
    public event Action<int, int> HealthChanged;

    private bool isDead;
    private EnemyNavAgent navAgent;

    private void Awake()
    {
        navAgent = GetComponent<EnemyNavAgent>();
    }

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

        // Trigger hit stagger (slow down briefly)
        navAgent?.TriggerHitStagger();

        if (currentHealth <= 0)
            Die();
    }

    private void Die()
    {
        if (isDead) return;
        isDead = true;

        // Notify nav agent to stop
        navAgent?.OnDeath();

        // FX
        if (deathFXPrefab != null)
            Instantiate(deathFXPrefab, transform.position, Quaternion.identity);

        // Events
        OnZombieDied?.Invoke(gameObject);
        ZombieSpawner.Instance?.OnZombieDied(gameObject);

        // Disable all colliders so corpse doesn't block
        foreach (Collider col in GetComponentsInChildren<Collider>())
            col.enabled = false;

        // Disable legacy mover if present
        ZombiePrototypeMover legacyMover = GetComponent<ZombiePrototypeMover>();
        if (legacyMover != null) legacyMover.enabled = false;

        ZombieAttack attack = GetComponent<ZombieAttack>();
        if (attack != null) attack.enabled = false;

        Destroy(gameObject, deathDelay);
    }
}
