using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Drives the PvZ-style HUD each frame.
/// Owns the PlantCardUI array, shovel button, and the sun display text and icon.
/// </summary>
public class GameUIManager : MonoBehaviour
{
    public static GameUIManager Instance { get; private set; }

    [Header("Sun Display")]
    public Text sunText;                // Top-left sun counter
    public Image sunIcon;               // Sun icon image (SunCircle)

    [Header("Plant Cards")]
    public PlantCardUI[] plantCards;    // One per plant
    public PlantCardUI shovelCard;      // Shovel card

    [Header("Sun Flash Settings")]
    public float flashFrequency = 5f;
    public Color flashColor = Color.red;
    public Color normalSunTextColor = Color.white;
    public Color normalSunIconColor = new Color(1f, 0.9f, 0f, 1f);
    private float insufficientFlashTimer = 0f;
    private bool colorsCaptured = false;
    private int lastSelectedIndex = -1;
    private bool lastShovelMode = false;

    [Header("End-Game Panels (optional)")]
    [Tooltip("Panel shown when player wins. Leave null to skip.")]
    public GameObject winPanel;
    [Tooltip("Panel shown when zombies reach base. Leave null to skip.")]
    public GameObject losePanel;

    [Header("Battle Status")]
    public Text roundText;
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
        EnsureUIReferences();

        if (EconomyManager.Instance != null)
        {
            EconomyManager.Instance.OnSunChanged += UpdateSunText;
            UpdateSunText(EconomyManager.Instance.currentSun);
        }
    }

    public void TriggerInsufficientSunFlash(float duration = 1.0f)
    {
        insufficientFlashTimer = Mathf.Max(insufficientFlashTimer, duration);
    }

    private void EnsureUIReferences()
    {
        // Auto-find sunText if missing
        if (sunText == null)
        {
            var stObj = GameObject.Find("SunText");
            if (stObj != null) sunText = stObj.GetComponent<Text>();
        }

        // Auto-find sunIcon if missing
        if (sunIcon == null)
        {
            var scObj = GameObject.Find("SunCircle");
            if (scObj != null) sunIcon = scObj.GetComponent<Image>();
        }

        // Capture normal baseline colors once (protects against overwriting with flashing colors)
        if (!colorsCaptured)
        {
            if (sunText != null && sunText.color != flashColor)
            {
                normalSunTextColor = sunText.color;
            }
            if (sunIcon != null && sunIcon.color != flashColor)
            {
                normalSunIconColor = sunIcon.color;
            }
            if (sunText != null && sunIcon != null)
            {
                colorsCaptured = true;
            }
        }

        // Auto-find plantCards if missing or containing null elements
        bool needsCards = (plantCards == null || plantCards.Length == 0);
        if (!needsCards)
        {
            bool anyValid = false;
            for (int i = 0; i < plantCards.Length; i++)
            {
                if (plantCards[i] != null) { anyValid = true; break; }
            }
            if (!anyValid) needsCards = true;
        }

        if (needsCards)
        {
            var allCards = Object.FindObjectsByType<PlantCardUI>(FindObjectsSortMode.None);
            var cardList = new System.Collections.Generic.List<PlantCardUI>();
            foreach (var card in allCards)
            {
                if (card.isShovelCard)
                {
                    if (shovelCard == null) shovelCard = card;
                }
                else
                {
                    cardList.Add(card);
                }
            }
            cardList.Sort((a, b) => a.plantIndex.CompareTo(b.plantIndex));
            plantCards = cardList.ToArray();
        }

        // Auto-find shovel card if missing
        if (shovelCard == null)
        {
            var allCards = Object.FindObjectsByType<PlantCardUI>(FindObjectsSortMode.None);
            foreach (var card in allCards)
            {
                if (card.isShovelCard)
                {
                    shovelCard = card;
                    break;
                }
            }
        }
    }

    void UpdateSunText(int sun)
    {
        if (sunText != null) sunText.text = sun.ToString();
    }

    void Update()
    {
        EnsureUIReferences();
        UpdateBattleStatus();

        if (insufficientFlashTimer > 0f)
        {
            insufficientFlashTimer -= Time.deltaTime;
        }

        // Lazy lookup for player
        if (player == null)
        {
            player = Object.FindFirstObjectByType<PlayerController>();
        }

        // Clear burst flash timer immediately when selection changes
        if (player != null)
        {
            if (player.CurrentPlantIndex != lastSelectedIndex || player.IsShovelMode != lastShovelMode)
            {
                lastSelectedIndex = player.CurrentPlantIndex;
                lastShovelMode = player.IsShovelMode;
                insufficientFlashTimer = 0f;
            }
        }

        // Update shovel card selection state
        if (shovelCard != null && player != null)
        {
            shovelCard.UpdateShovel(player.IsShovelMode);
        }

        bool selectedIsUnaffordable = false;

        if (player != null && player.plants != null && EconomyManager.Instance != null)
        {
            int currentSun = EconomyManager.Instance.currentSun;
            int selectedIndex = player.CurrentPlantIndex;

            if (plantCards != null)
            {
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
            }
        }

        // Flash sun text and sun icon if selected plant is unaffordable or triggered
        bool shouldFlash = selectedIsUnaffordable || insufficientFlashTimer > 0f;
        if (shouldFlash)
        {
            float t = Mathf.Sin(Time.time * flashFrequency) * 0.5f + 0.5f;
            if (sunText != null)
                sunText.color = Color.Lerp(normalSunTextColor, flashColor, t);
            if (sunIcon != null)
                sunIcon.color = Color.Lerp(normalSunIconColor, flashColor, t);
        }
        else
        {
            if (sunText != null)
                sunText.color = normalSunTextColor;
            if (sunIcon != null)
                sunIcon.color = normalSunIconColor;
        }
    }

    private void UpdateBattleStatus()
    {
        if (roundText != null && GameDataCarrier.Instance != null)
        {
            roundText.text = $"ROUND {GameDataCarrier.Instance.currentRound} / {GameDataCarrier.Instance.roundSceneNames.Length}";
        }

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
    public void ShowWinScreen(bool hasNextRound = false)
    {
        if (winPanel != null)
        {
            winPanel.SetActive(true);
            Text titleText = winPanel.GetComponentInChildren<Text>();
            if (titleText != null)
            {
                if (hasNextRound)
                {
                    titleText.text = "Round Complete!\nNext round starting...";
                }
                else
                {
                    titleText.text = "Victory!\nAll rounds cleared!";
                }
            }
        }
        else
        {
            Debug.Log(hasNextRound ? "[UI] Round Complete!" : "[UI] YOU WIN! All rounds cleared!");
        }
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
