using System;
using UnityEngine;

public sealed class HouseHealth : MonoBehaviour
{
    public static HouseHealth Instance { get; private set; }

    [Min(1)] public int maxHealth = 300;
    [SerializeField] private int currentHealth;

    public event Action<int, int> HealthChanged;
    public int CurrentHealth => currentHealth;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        currentHealth = maxHealth;
    }

    public void TakeDamage(int amount)
    {
        if (currentHealth <= 0 || amount <= 0) return;

        currentHealth = Mathf.Max(0, currentHealth - amount);
        AudioManager.PlaySfx(AudioCue.HouseHit);
        HealthChanged?.Invoke(currentHealth, maxHealth);
        Debug.Log($"[HouseHealth] House HP: {currentHealth}/{maxHealth}");

        if (currentHealth == 0)
            GameManager.Instance?.OnHouseDestroyed();
    }

    private void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }
}
