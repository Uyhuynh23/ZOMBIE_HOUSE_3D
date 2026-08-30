using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Drives the PvZ-style HUD each frame.
/// Owns the PlantCardUI array and the sun display text.
/// </summary>
public class GameUIManager : MonoBehaviour
{
    [Header("Sun Display")]
    public Text sunText;                // Top-left sun counter

    [Header("Plant Cards")]
    public PlantCardUI[] plantCards;    // One per plant (plus optionally shovel)
    
    [Header("Sun Flash Settings")]
    public float flashFrequency = 5f;
    public Color flashColor = Color.red;
    private Color originalSunTextColor = Color.white;

    private PlayerController player;

    void Start()
    {
        player = FindObjectOfType<PlayerController>();
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

    void OnDestroy()
    {
        if (EconomyManager.Instance != null)
            EconomyManager.Instance.OnSunChanged -= UpdateSunText;
    }
}
