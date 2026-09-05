using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;

/// <summary>
/// Attach to the root WinLosePanel GameObject.
/// Called by GameUIManager.ShowWinScreen() / ShowLoseScreen().
///
/// Win flow:
///   hasNextRound = true  → WinPanel       (rounds 1 & 2, has "Next Round" button)
///   hasNextRound = false → WinPanel_Final (round 3 final, no "Next Round" button)
/// </summary>
public class WinLosePanelUI : MonoBehaviour
{
    [Header("Sub-panels")]
    public GameObject winPanel;         // Rounds 1 & 2
    public GameObject winPanelFinal;    // Round 3 — final victory
    public GameObject losePanel;

    [Header("Win Panel Elements (rounds 1-2)")]
    public TextMeshProUGUI winTitleText;
    public Button btnRestart_Win;
    public Button btnNext;
    public Button btnHome_Win;

    [Header("Win Final Panel Elements (round 3)")]
    public TextMeshProUGUI winFinalTitleText;
    public Button btnRestart_WinFinal;
    public Button btnHome_WinFinal;

    [Header("Lose Panel Elements")]
    public TextMeshProUGUI loseTitleText;
    public Button btnRestart_Lose;
    public Button btnHome_Lose;


    void Awake()
    {
        if (winPanel      != null) winPanel.SetActive(false);
        if (winPanelFinal != null) winPanelFinal.SetActive(false);
        if (losePanel     != null) losePanel.SetActive(false);

        // Wire Win panel buttons
        if (btnRestart_Win  != null) btnRestart_Win.onClick.AddListener(OnRestart);
        if (btnNext         != null) btnNext.onClick.AddListener(OnNext);
        if (btnHome_Win     != null) btnHome_Win.onClick.AddListener(OnHome);

        // Wire Win Final panel buttons
        if (btnRestart_WinFinal != null) btnRestart_WinFinal.onClick.AddListener(OnRestart);
        if (btnHome_WinFinal    != null) btnHome_WinFinal.onClick.AddListener(OnHome);

        // Wire Lose panel buttons
        if (btnRestart_Lose != null) btnRestart_Lose.onClick.AddListener(OnRestart);
        if (btnHome_Lose    != null) btnHome_Lose.onClick.AddListener(OnHome);
    }

    // ── Public API ────────────────────────────────────────────────────────────

    public void ShowWin(bool hasNextRound)
    {
        gameObject.SetActive(true);
        if (losePanel != null) losePanel.SetActive(false);

        Debug.Log($"[WinLosePanelUI] ShowWin called — hasNextRound={hasNextRound} | winPanel={winPanel != null} | winPanelFinal={winPanelFinal != null}");

        if (hasNextRound)
        {
            // Rounds 1 & 2 — normal win with "Next Round" button
            if (winPanel      != null) winPanel.SetActive(true);
            if (winPanelFinal != null) winPanelFinal.SetActive(false);
        }
        else
        {
            // Round 3 (final) — grand victory screen
            if (winPanel      != null) winPanel.SetActive(false);
            if (winPanelFinal != null) winPanelFinal.SetActive(true);
            else Debug.LogError("[WinLosePanelUI] winPanelFinal is NULL — please assign WinPanel_Final in the Inspector!");
        }
    }

    public void ShowLose()
    {
        gameObject.SetActive(true);
        if (winPanel      != null) winPanel.SetActive(false);
        if (winPanelFinal != null) winPanelFinal.SetActive(false);
        if (losePanel     != null) losePanel.SetActive(true);
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
