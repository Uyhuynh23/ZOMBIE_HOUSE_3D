using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

/// <summary>
/// Repeatable Play Mode smoke test for the four-road map integration.
/// It records gameplay state and Game-view screenshots at useful combat timestamps.
/// </summary>
[InitializeOnLoad]
public static class MapIntegrationPlayVerifier
{
    private const string TriggerPath = "/private/tmp/zombie_house_map_verify.trigger";
    private const string ReportPath = "/private/tmp/zombie_house_map_verify.txt";
    private const string ScreenshotPrefix = "/private/tmp/zombie_house_map_verify";
    private const string RunningKey = "ZombieHouse.MapIntegrationVerificationRunning";

    private static double playStartedAt;
    private static bool capturedSixSeconds;
    private static bool capturedFourteenSeconds;
    private static bool capturedTwentyFourSeconds;

    static MapIntegrationPlayVerifier()
    {
        EditorApplication.playModeStateChanged -= OnPlayModeStateChanged;
        EditorApplication.playModeStateChanged += OnPlayModeStateChanged;

        if (SessionState.GetBool(RunningKey, false))
        {
            EditorApplication.delayCall += ResumeAfterDomainReload;
            return;
        }

        if (!File.Exists(TriggerPath))
            return;

        File.Delete(TriggerPath);
        EditorApplication.delayCall += Run;
    }

    [MenuItem("Zombie House/Verify Map Integration Play Mode")]
    public static void Run()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode)
            return;

        File.WriteAllText(ReportPath, "Zombie House map integration verification\n");
        MapZombieIntegrationSceneBuilder.Build();
        ResetCaptures();
        SessionState.SetBool(RunningKey, true);
        EditorApplication.isPlaying = true;
    }

    private static void ResumeAfterDomainReload()
    {
        if (!EditorApplication.isPlaying)
            return;

        EditorApplication.isPaused = false;
        Time.timeScale = 1f;
        playStartedAt = EditorApplication.timeSinceStartup;
        ResetCaptures();
        EditorApplication.update -= Tick;
        EditorApplication.update += Tick;
        AppendSnapshot("Resumed after domain reload");
    }

    private static void OnPlayModeStateChanged(PlayModeStateChange state)
    {
        if (!SessionState.GetBool(RunningKey, false))
            return;

        if (state == PlayModeStateChange.EnteredPlayMode)
        {
            EditorApplication.isPaused = false;
            Time.timeScale = 1f;
            playStartedAt = EditorApplication.timeSinceStartup;
            EditorApplication.update -= Tick;
            EditorApplication.update += Tick;
            AppendSnapshot("Entered Play Mode");
        }
        else if (state == PlayModeStateChange.EnteredEditMode)
        {
            EditorApplication.update -= Tick;
            SessionState.SetBool(RunningKey, false);
            File.AppendAllText(ReportPath, "Verification finished and Play Mode exited.\n");
            Debug.Log("[MapIntegrationVerifier] Finished. Report: " + ReportPath);
        }
    }

    private static void Tick()
    {
        double elapsed = EditorApplication.timeSinceStartup - playStartedAt;

        if (!capturedSixSeconds && elapsed >= 6d)
        {
            capturedSixSeconds = true;
            Capture("06s");
        }

        if (!capturedFourteenSeconds && elapsed >= 14d)
        {
            capturedFourteenSeconds = true;
            Capture("14s");
        }

        if (!capturedTwentyFourSeconds && elapsed >= 24d)
        {
            capturedTwentyFourSeconds = true;
            Capture("24s");
        }

        if (elapsed >= 26d)
            EditorApplication.isPlaying = false;
    }

    private static void ResetCaptures()
    {
        capturedSixSeconds = false;
        capturedFourteenSeconds = false;
        capturedTwentyFourSeconds = false;
    }

    private static void Capture(string label)
    {
        ScreenCapture.CaptureScreenshot($"{ScreenshotPrefix}_{label}.png", 1);
        AppendSnapshot(label);
    }

    private static void AppendSnapshot(string label)
    {
        ZombieHealth[] zombies = Object.FindObjectsByType<ZombieHealth>(FindObjectsSortMode.None)
            .Where(item => item.gameObject.activeInHierarchy)
            .ToArray();
        PlantBase[] plants = Object.FindObjectsByType<PlantBase>(FindObjectsSortMode.None)
            .Where(item => item.gameObject.activeInHierarchy)
            .ToArray();

        string zombieState = zombies.Length == 0
            ? "none"
            : string.Join(", ", zombies.Select(item =>
                $"{item.name}:{item.currentHealth}/{item.maxHealth}@({item.transform.position.x:F1},{item.transform.position.z:F1})"));
        string plantState = plants.Length == 0
            ? "none"
            : string.Join(", ", plants.Select(item =>
                $"{item.name}:{item.currentHealth}/{item.maxHealth}"));
        string houseHealth = HouseHealth.Instance == null
            ? "missing"
            : $"{HouseHealth.Instance.CurrentHealth}/{HouseHealth.Instance.maxHealth}";
        string spawner = ZombieSpawner.Instance == null
            ? "missing"
            : $"wave={ZombieSpawner.Instance.CurrentWaveNumber}/{ZombieSpawner.Instance.TotalWaves}, " +
              $"active={ZombieSpawner.Instance.ActiveZombieCount}, incoming={ZombieSpawner.Instance.RemainingToSpawn}";

        File.AppendAllText(ReportPath,
            $"[{label}] spawner {spawner}; house={houseHealth}; plants={plantState}; zombies={zombieState}\n");
    }
}
