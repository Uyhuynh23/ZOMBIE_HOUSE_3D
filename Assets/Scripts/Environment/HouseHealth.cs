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

    private void Start()
    {
        if (NetworkMatchState.Instance == null) return;
        NetworkMatchState.Instance.HouseHealth.OnValueChanged += OnNetworkHealthChanged;
        if (NetworkGameplayAuthority.IsServer)
            NetworkMatchState.Instance.ServerSetHouseHealth(maxHealth);
        currentHealth = NetworkMatchState.Instance.HouseHealth.Value > 0
            ? NetworkMatchState.Instance.HouseHealth.Value : maxHealth;
        HealthChanged?.Invoke(currentHealth, maxHealth);
    }

    public void TakeDamage(int amount)
    {
        if (!NetworkGameplayAuthority.IsServer || !NetworkGameplayAuthority.CanMutate) return;
        if (currentHealth <= 0 || amount <= 0) return;

        currentHealth = Mathf.Max(0, currentHealth - amount);
        if (NetworkMatchState.Instance != null)
            NetworkMatchState.Instance.ServerSetHouseHealth(currentHealth);
        else OnNetworkHealthChanged(currentHealth + amount, currentHealth);
        Debug.Log($"[NET][MATCH] House HP={currentHealth}/{maxHealth} damage={amount}.");

        if (currentHealth == 0)
        {
            if (NetworkMatchState.Instance != null)
                NetworkMatchState.Instance.ServerTrySetTerminal(MatchPhase.Lost);
            else GameManager.Instance?.OnHouseDestroyed();
        }
    }

    private void OnNetworkHealthChanged(int previous, int current)
    {
        currentHealth = current;
        if (current < previous) AudioManager.PlaySfx(AudioCue.HouseHit);
        HealthChanged?.Invoke(currentHealth, maxHealth);
    }

    private void OnDestroy()
    {
        if (NetworkMatchState.Instance != null)
            NetworkMatchState.Instance.HouseHealth.OnValueChanged -= OnNetworkHealthChanged;
        if (Instance == this) Instance = null;
    }
}
