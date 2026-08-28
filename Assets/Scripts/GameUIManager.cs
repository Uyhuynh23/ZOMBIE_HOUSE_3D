using UnityEngine;
using UnityEngine.UI;

public class GameUIManager : MonoBehaviour
{
    public Text sunText;
    
    [Header("Plant Cards (UI Images)")]
    public Image[] plantCards;
    public Image[] cooldownOverlays;
    public Text[] costTexts;

    private PlayerController player;

    void Start()
    {
        player = FindObjectOfType<PlayerController>();
        if (EconomyManager.Instance != null)
        {
            EconomyManager.Instance.OnSunChanged += UpdateSunText;
            UpdateSunText(EconomyManager.Instance.currentSun);
        }
    }

    void UpdateSunText(int sun)
    {
        if (sunText != null) sunText.text = "Sun: " + sun;
    }

    void Update()
    {
        if (player == null || player.plants == null) return;

        for (int i = 0; i < plantCards.Length && i < player.plants.Length; i++)
        {
            PlantData data = player.plants[i];
            
            // Update cost text
            if (costTexts[i] != null) costTexts[i].text = data.cost.ToString();

            // Update cooldown overlay (fill amount)
            if (cooldownOverlays[i] != null)
            {
                if (data.cooldownTime > 0)
                {
                    cooldownOverlays[i].fillAmount = data.currentCooldown / data.cooldownTime;
                }
                else
                {
                    cooldownOverlays[i].fillAmount = 0;
                }
            }

            // Dim card if not enough sun
            if (EconomyManager.Instance != null)
            {
                if (EconomyManager.Instance.currentSun < data.cost)
                {
                    plantCards[i].color = new Color(0.5f, 0.5f, 0.5f, 1f);
                }
                else
                {
                    plantCards[i].color = Color.white;
                }
            }
        }
    }

    void OnDestroy()
    {
        if (EconomyManager.Instance != null)
        {
            EconomyManager.Instance.OnSunChanged -= UpdateSunText;
        }
    }
}
