using UnityEngine;
using System;

public class EconomyManager : MonoBehaviour
{
    public static EconomyManager Instance { get; private set; }

    public int currentSun = 50;
    
    // Event to notify UI
    public event Action<int> OnSunChanged;

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
        }
    }

    void Start()
    {
        if (NetworkMatchState.Instance != null)
        {
            currentSun = NetworkMatchState.Instance.TeamSun.Value;
            NetworkMatchState.Instance.TeamSun.OnValueChanged += OnNetworkSunChanged;
        }
        // Broadcast initial value
        OnSunChanged?.Invoke(currentSun);
    }

    public void AddSun(int amount)
    {
        if (NetworkMatchState.Instance != null)
        {
            if (!NetworkGameplayAuthority.IsServer) return;
            NetworkMatchState.Instance.ServerAddSun(amount);
            return;
        }
        currentSun += amount;
        OnSunChanged?.Invoke(currentSun);
    }

    public bool SpendSun(int amount)
    {
        if (NetworkMatchState.Instance != null)
        {
            if (!NetworkGameplayAuthority.IsServer) return false;
            return NetworkMatchState.Instance.ServerTrySpendSun(amount);
        }
        if (currentSun >= amount)
        {
            currentSun -= amount;
            OnSunChanged?.Invoke(currentSun);
            return true;
        }
        return false;
    }

    private void OnNetworkSunChanged(int previous, int current)
    {
        currentSun = current;
        OnSunChanged?.Invoke(currentSun);
    }

    private void OnDestroy()
    {
        if (NetworkMatchState.Instance != null)
            NetworkMatchState.Instance.TeamSun.OnValueChanged -= OnNetworkSunChanged;
        if (Instance == this) Instance = null;
    }
}
