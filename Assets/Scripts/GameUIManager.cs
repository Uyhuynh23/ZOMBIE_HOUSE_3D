using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Drives the PvZ-style HUD each frame.
/// Owns the PlantCardUI array and the sun display text.
/// </summary>
public class GameUIManager : MonoBehaviour
{
    public static GameUIManager Instance { get; private set; }

    [Header("Sun Display")]
    public Text sunText;                // Top-left sun counter

    [Header("Plant Cards")]
    public PlantCardUI[] plantCards;    // One per plant (plus optionally shovel)
    
    [Header("Sun Flash Settings")]
    public float flashFrequency = 5f;
    public Color flashColor = Color.red;
    private Color originalSunTextColor = Color.white;

    [Header("End-Game Panels (optional)")]
    [Tooltip("Panel shown when player wins. Leave null to skip.")]
    public GameObject winPanel;
    [Tooltip("Panel shown when zombies reach base. Leave null to skip.")]
    public GameObject losePanel;

    [Header("Battle Status")]
    public Text waveText;
    public Text zombieText;
    public Text houseHealthText;
    public Image houseHealthFill;

    private PlayerController player;

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else { Destroy(gameObject); return; }

        if (winPanel != null) winPanel.SetActive(false);
        if (losePanel != null) losePanel.SetActive(false);
    }

    void Start()
    {
        if (sunText != null)
        {
            originalSunTextColor = sunText.color;
        }
        if (EconomyManager.Instance != null)
        {
            EconomyManager.Instance.OnSunChanged += UpdateSunText;
            UpdateSunText(EconomyManager.Instance.currentSun);
        }
    }

    void UpdateSunText(int sun)
    {
        if (sunText != null) sunText.text = sun.ToString();
    }

    void Update()
    {
        UpdateBattleStatus();
        
        // Lazy lookup for player
        if (player == null)
        {
            player = Object.FindFirstObjectByType<PlayerController>();
        }

        if (player == null || player.plants == null) return;
        if (EconomyManager.Instance == null) return;

        int currentSun = EconomyManager.Instance.currentSun;
        int selectedIndex = player.CurrentPlantIndex;
        bool selectedIsUnaffordable = false;

        for (int i = 0; i < plantCards.Length; i++)
        {
            PlantCardUI card = plantCards[i];
            if (card == null || card.isShovelCard) continue;

            int idx = card.plantIndex;
            if (idx < 0 || idx >= player.plants.Length) continue;

            PlantData data = player.plants[idx];
            bool isSelected = (!player.IsShovelMode) && (idx == selectedIndex);
            
            if (isSelected && currentSun < data.cost)
            {
                selectedIsUnaffordable = true;
            }
            
            card.UpdateCard(data, isSelected, currentSun);
        }
        
        // Flash sun text if selected plant is unaffordable
        if (sunText != null)
        {
            if (selectedIsUnaffordable)
            {
                float t = Mathf.Sin(Time.time * flashFrequency) * 0.5f + 0.5f;
                sunText.color = Color.Lerp(originalSunTextColor, flashColor, t);
            }
            else
            {
                sunText.color = originalSunTextColor;
            }
        }

    }

    private void UpdateBattleStatus()
    {
        ZombieSpawner spawner = ZombieSpawner.Instance;
        if (spawner != null)
        {
            if (waveText != null)
            {
                string countdown = spawner.NextWaveCountdown > 0.05f
                    ? $"  starts in {Mathf.CeilToInt(spawner.NextWaveCountdown)}s"
                    : string.Empty;
                waveText.text = $"WAVE {spawner.CurrentWaveNumber}/{spawner.TotalWaves}{countdown}";
            }

            if (zombieText != null)
                zombieText.text = $"ZOMBIES  {spawner.ActiveZombieCount} active  •  {spawner.RemainingToSpawn} incoming";
        }

        HouseHealth house = HouseHealth.Instance;
        if (house != null)
        {
            float ratio = house.maxHealth <= 0 ? 0f : (float)house.CurrentHealth / house.maxHealth;
            if (houseHealthFill != null) houseHealthFill.fillAmount = Mathf.Clamp01(ratio);
            if (houseHealthText != null) houseHealthText.text = $"HOUSE  {house.CurrentHealth}/{house.maxHealth}";
        }
    }

    /// <summary>Show victory overlay.</summary>
    public void ShowWinScreen()
    {
        if (winPanel != null) winPanel.SetActive(true);
        else Debug.Log("[UI] YOU WIN!");
    }

    /// <summary>Show game-over overlay.</summary>
    public void ShowLoseScreen()
    {
        if (losePanel != null) losePanel.SetActive(true);
        else Debug.Log("[UI] GAME OVER!");
    }

    void OnDestroy()
    {
        if (Instance == this) Instance = null;
        if (EconomyManager.Instance != null)
            EconomyManager.Instance.OnSunChanged -= UpdateSunText;
    }
}
