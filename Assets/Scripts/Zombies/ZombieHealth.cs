using System;
using UnityEngine;
using Unity.Netcode;

/// <summary>
/// Health component for all enemy types (Zombie, Spider).
/// Fires OnZombieDied so spawner and game manager can react.
/// Triggers hit stagger on EnemyNavAgent when damaged.
/// </summary>
public class ZombieHealth : NetworkBehaviour
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
    public readonly NetworkVariable<int> NetworkHealth = new(0,
        NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
    public readonly NetworkVariable<bool> NetworkDead = new(false,
        NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

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

    public override void OnNetworkSpawn()
    {
        NetworkHealth.OnValueChanged += OnNetworkHealthChanged;
        NetworkDead.OnValueChanged += OnNetworkDeadChanged;
        if (IsServer)
        {
            NetworkHealth.Value = maxHealth;
            NetworkDead.Value = false;
        }
        currentHealth = NetworkHealth.Value > 0 ? NetworkHealth.Value : maxHealth;
        HealthChanged?.Invoke(currentHealth, maxHealth);
    }

    public override void OnNetworkDespawn()
    {
        NetworkHealth.OnValueChanged -= OnNetworkHealthChanged;
        NetworkDead.OnValueChanged -= OnNetworkDeadChanged;
    }

    private void Start()
    {
        currentHealth = maxHealth;
        HealthChanged?.Invoke(currentHealth, maxHealth);
    }

    public void TakeDamage(int amount)
    {
        if (!NetworkGameplayAuthority.IsServer || !NetworkGameplayAuthority.CanMutate) return;
        if (isDead) return;

        currentHealth = Mathf.Max(0, currentHealth - Mathf.Max(0, amount));
        if (IsSpawned) NetworkHealth.Value = currentHealth;
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
        if (IsSpawned) NetworkDead.Value = true;

        AudioManager.PlaySfx(AudioCue.ZombieDeath);

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

        StartCoroutine(ServerDespawnAfterDelay());
    }

    private System.Collections.IEnumerator ServerDespawnAfterDelay()
    {
        yield return new WaitForSeconds(deathDelay);
        NetworkObject no = GetComponent<NetworkObject>();
        if (no != null && no.IsSpawned && IsServer) no.Despawn(true);
        else if (this != null) Destroy(gameObject);
    }

    private void OnNetworkHealthChanged(int previous, int current)
    {
        currentHealth = current;
        HealthChanged?.Invoke(currentHealth, maxHealth);
        if (!IsServer && current < previous) navAgent?.TriggerHitStaggerVisual();
    }

    private void OnNetworkDeadChanged(bool previous, bool current)
    {
        if (!current || IsServer) return;
        isDead = true;
        navAgent?.ApplyRemoteDeathVisual();
        foreach (Collider col in GetComponentsInChildren<Collider>()) col.enabled = false;
        AudioManager.PlaySfx(AudioCue.ZombieDeath);
        if (deathFXPrefab != null) Instantiate(deathFXPrefab, transform.position, Quaternion.identity);
    }
}
