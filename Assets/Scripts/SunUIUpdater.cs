using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(Text))]
public class SunUIUpdater : MonoBehaviour
{
    private Text sunText;

    void Start()
    {
        sunText = GetComponent<Text>();
        if (EconomyManager.Instance != null)
        {
            EconomyManager.Instance.OnSunChanged += UpdateText;
            UpdateText(EconomyManager.Instance.currentSun);
        }
    }

    void UpdateText(int currentSun)
    {
        sunText.text = "Sun: " + currentSun;
    }

    void OnDestroy()
    {
        if (EconomyManager.Instance != null)
        {
            EconomyManager.Instance.OnSunChanged -= UpdateText;
        }
    }
}
