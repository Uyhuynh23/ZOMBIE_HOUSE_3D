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
        // Broadcast initial value
        OnSunChanged?.Invoke(currentSun);
    }

    public void AddSun(int amount)
    {
        currentSun += amount;
        OnSunChanged?.Invoke(currentSun);
    }

    public bool SpendSun(int amount)
    {
        if (currentSun >= amount)
        {
            currentSun -= amount;
            OnSunChanged?.Invoke(currentSun);
            return true;
        }
        return false;
    }
}
