using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;

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
        Application.runInBackground = true;
        Time.timeScale = 1f;

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
    // Called when the house health reaches zero.
    // ──────────────────────────────────────────────────────────
    public void OnHouseDestroyed()
    {
        if (currentState != GameState.Playing) return;

        currentState = GameState.Lost;
        Debug.Log("[GameManager] GAME OVER! The house was destroyed.");

        if (GameUIManager.Instance != null)
            GameUIManager.Instance.ShowLoseScreen();

        Time.timeScale = 0f;

        if (restartDelay > 0f)
            StartCoroutine(RestartAfterRealtimeDelay());
    }

    // ──────────────────────────────────────────────────────────
    private void RestartScene()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }

    private IEnumerator RestartAfterRealtimeDelay()
    {
        yield return new WaitForSecondsRealtime(restartDelay);
        RestartScene();
    }

    private void OnDestroy()
    {
        if (Instance == this) Instance = null;
        Time.timeScale = 1f;
    }
}
