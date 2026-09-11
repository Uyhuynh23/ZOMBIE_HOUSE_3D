using System;
using UnityEngine;
using Unity.Netcode;

/// <summary>
/// Abstract base class for all plants.
/// Provides health, damage, shoveling, and square registration.
/// </summary>
public abstract class PlantBase : NetworkBehaviour
{
    [Header("Plant Base Settings")]
    public int maxHealth = 100;
    public int currentHealth = 100;
    [Tooltip("Tinh chỉnh độ to/nhỏ của cây (nhân với scale tự động của Player)")]
    public float customScaleMultiplier = 1.0f;
    public event Action<int, int> HealthChanged;

    /// <summary>
    /// The square this plant is planted on. Set by PlayerController after instantiation.
    /// </summary>
    [HideInInspector] public PlantableSquare mySquare;
    public readonly NetworkVariable<int> NetworkHealth = new(0,
        NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
    public readonly NetworkVariable<int> SquareId = new(-1,
        NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
    public readonly NetworkVariable<bool> NetworkDead = new(false,
        NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

    protected virtual void Awake()
    {
        currentHealth = maxHealth;

        // Every plant receives the same on-demand world health bar without
        // requiring each plant prefab to carry another serialized component.
        if (GetComponent<PlantHealthBar>() == null)
            gameObject.AddComponent<PlantHealthBar>();
        if (GetComponent<PlantDamageVisual>() == null)
            gameObject.AddComponent<PlantDamageVisual>();
    }

    public override void OnNetworkSpawn()
    {
        NetworkHealth.OnValueChanged += OnHealthNetworkChanged;
        SquareId.OnValueChanged += OnSquareChanged;
        if (IsServer)
        {
            if (NetworkHealth.Value <= 0) NetworkHealth.Value = maxHealth;
            NetworkDead.Value = false;
        }
        currentHealth = NetworkHealth.Value > 0 ? NetworkHealth.Value : maxHealth;
        BindSquare(SquareId.Value);
        HealthChanged?.Invoke(currentHealth, maxHealth);
    }

    public override void OnNetworkDespawn()
    {
        NetworkHealth.OnValueChanged -= OnHealthNetworkChanged;
        SquareId.OnValueChanged -= OnSquareChanged;
        FreeSquare();
    }

    public void ServerInitialize(PlantableSquare square)
    {
        if (!NetworkGameplayAuthority.IsServer || square == null) return;
        SquareId.Value = square.StableId;
        NetworkHealth.Value = maxHealth;
        NetworkDead.Value = false;
        currentHealth = maxHealth;
        square.PlantHere(this);
    }

    /// <summary>
    /// Called by zombies or other damage sources.
    /// </summary>
    public virtual void TakeDamage(int amount)
    {
        if (!NetworkGameplayAuthority.IsServer || !NetworkGameplayAuthority.CanMutate) return;
        if (amount <= 0 || currentHealth <= 0) return;

        currentHealth = Mathf.Max(0, currentHealth - amount);
        if (IsSpawned) NetworkHealth.Value = currentHealth;
        HealthChanged?.Invoke(currentHealth, maxHealth);
        if (currentHealth <= 0)
        {
            Die();
        }
    }

    /// <summary>
    /// Called when the player shovels this plant.
    /// Override to add sun refund or special effects.
    /// </summary>
    public virtual void OnShoveled()
    {
        if (!NetworkGameplayAuthority.IsServer) return;
        // Default: just free the square and destroy
        FreeSquare();
        DespawnOrDestroy();
    }

    /// <summary>
    /// Called when the plant dies from damage.
    /// </summary>
    protected virtual void Die()
    {
        if (!NetworkGameplayAuthority.IsServer || (IsSpawned && NetworkDead.Value)) return;
        if (IsSpawned) NetworkDead.Value = true;
        FreeSquare();
        DespawnOrDestroy();
    }

    /// <summary>
    /// Frees the PlantableSquare this plant was on.
    /// </summary>
    protected void FreeSquare()
    {
        if (mySquare != null)
        {
            mySquare.RemovePlant();
            mySquare = null;
        }
    }

    private void DespawnOrDestroy()
    {
        NetworkObject no = GetComponent<NetworkObject>();
        if (no != null && no.IsSpawned && IsServer) no.Despawn(true);
        else Destroy(gameObject);
    }

    private void OnHealthNetworkChanged(int previous, int current)
    {
        currentHealth = current;
        HealthChanged?.Invoke(currentHealth, maxHealth);
    }

    private void OnSquareChanged(int previous, int current) => BindSquare(current);

    private void BindSquare(int id)
    {
        if (id < 0) return;
        PlantableSquare square = PlantableSquare.Find(id);
        if (square != null && square.currentPlant != this) square.PlantHere(this);
    }
}
