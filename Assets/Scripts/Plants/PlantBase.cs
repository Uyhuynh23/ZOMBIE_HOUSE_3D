using System;
using UnityEngine;

/// <summary>
/// Abstract base class for all plants.
/// Provides health, damage, shoveling, and square registration.
/// </summary>
public abstract class PlantBase : MonoBehaviour
{
    [Header("Plant Base Settings")]
    public int maxHealth = 100;
    public int currentHealth = 100;
    public event Action<int, int> HealthChanged;

    /// <summary>
    /// The square this plant is planted on. Set by PlayerController after instantiation.
    /// </summary>
    [HideInInspector] public PlantableSquare mySquare;

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

    /// <summary>
    /// Called by zombies or other damage sources.
    /// </summary>
    public virtual void TakeDamage(int amount)
    {
        if (amount <= 0 || currentHealth <= 0) return;

        currentHealth = Mathf.Max(0, currentHealth - amount);
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
        // Default: just free the square and destroy
        FreeSquare();
        Destroy(gameObject);
    }

    /// <summary>
    /// Called when the plant dies from damage.
    /// </summary>
    protected virtual void Die()
    {
        FreeSquare();
        Destroy(gameObject);
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
}
