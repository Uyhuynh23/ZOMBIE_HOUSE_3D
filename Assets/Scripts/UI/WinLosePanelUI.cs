using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;

/// <summary>
/// Attach to the root WinLosePanel GameObject.
/// Called by GameUIManager.ShowWinScreen() / ShowLoseScreen().
/// </summary>
public class WinLosePanelUI : MonoBehaviour
{
    [Header("Sub-panels")]
    public GameObject winPanel;
    public GameObject losePanel;

    [Header("Win Panel Elements")]
    public TextMeshProUGUI winTitleText;
    public Button btnRestart_Win;
    public Button btnNext;
    public Button btnHome_Win;

    [Header("Lose Panel Elements")]
    public TextMeshProUGUI loseTitleText;
    public Button btnRestart_Lose;
    public Button btnHome_Lose;

    void Awake()
    {
        if (winPanel  != null) winPanel.SetActive(false);
        if (losePanel != null) losePanel.SetActive(false);

        if (btnRestart_Win  != null) btnRestart_Win.onClick.AddListener(OnRestart);
        if (btnNext         != null) btnNext.onClick.AddListener(OnNext);
        if (btnHome_Win     != null) btnHome_Win.onClick.AddListener(OnHome);
        if (btnRestart_Lose != null) btnRestart_Lose.onClick.AddListener(OnRestart);
        if (btnHome_Lose    != null) btnHome_Lose.onClick.AddListener(OnHome);
    }

    // ── Public API ────────────────────────────────────────────────────────────

    public void ShowWin(bool hasNextRound)
    {
        gameObject.SetActive(true);
        if (winPanel  != null) winPanel.SetActive(true);
        if (losePanel != null) losePanel.SetActive(false);
        // Title text stays as-is from the prefab — do NOT override here
        // "Next" button only visible when a next round exists
        if (btnNext != null) btnNext.gameObject.SetActive(hasNextRound);
    }

    public void ShowLose()
    {
        gameObject.SetActive(true);
        if (winPanel  != null) winPanel.SetActive(false);
        if (losePanel != null) losePanel.SetActive(true);
        // Title text stays as-is from the prefab — do NOT override here
    }

    // ── Buttons ───────────────────────────────────────────────────────────────

    private void OnRestart()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }

    private void OnNext()
    {
        if (GameManager.Instance != null)
            GameManager.Instance.LoadNextRound();
        else
        {
            Time.timeScale = 1f;
            SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex + 1);
        }
    }

    private void OnHome()
    {
        Time.timeScale = 1f;
        string menuScene = GameDataCarrier.MainMenuSceneName;
        if (string.IsNullOrEmpty(menuScene)) menuScene = "MainMenu";
        SceneManager.LoadScene(menuScene);
    }
}
