using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Top-level game state machine.
/// Listens to ZombieSpawner and ZombiePrototypeMover events to determine Win/Lose.
/// </summary>
public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    public enum GameState { Playing, Won, Lost }

    [Header("State (read-only)")]
    [SerializeField] private GameState currentState = GameState.Playing;

    [Header("Restart")]
    [Tooltip("Seconds after Win/Lose before the scene reloads. Set 0 to disable auto-restart.")]
    public float restartDelay = 5f;

    // ──────────────────────────────────────────────────────────
    private void Awake()
    {
        if (Instance == null) Instance = this;
        else { Destroy(gameObject); return; }
    }

    public GameState CurrentState => currentState;

    // ──────────────────────────────────────────────────────────
    // Called by ZombieSpawner when all waves are cleared
    // ──────────────────────────────────────────────────────────
    public void OnAllWavesComplete()
    {
        if (currentState != GameState.Playing) return;

        currentState = GameState.Won;
        Debug.Log("[GameManager] 🎉 YOU WIN! All waves cleared.");

        if (GameUIManager.Instance != null)
            GameUIManager.Instance.ShowWinScreen();

        if (restartDelay > 0f)
            Invoke(nameof(RestartScene), restartDelay);
    }

    // ──────────────────────────────────────────────────────────
    // Called by ZombiePrototypeMover when a zombie reaches the base
    // ──────────────────────────────────────────────────────────
    public void OnZombieReachedBase()
    {
        if (currentState != GameState.Playing) return;

        currentState = GameState.Lost;
        Debug.Log("[GameManager] 💀 GAME OVER! A zombie reached the base.");

        if (GameUIManager.Instance != null)
            GameUIManager.Instance.ShowLoseScreen();

        // Stop time so zombies freeze
        Time.timeScale = 0f;

        if (restartDelay > 0f)
            Invoke(nameof(RestartScene), restartDelay);
    }

    // ──────────────────────────────────────────────────────────
    private void RestartScene()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }
}
