using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Spawns waves of zombies into random lanes.
/// Requires GridManager to know lane Z positions.
/// Place on any persistent GameObject (e.g. GameManager).
/// </summary>
public class ZombieSpawner : MonoBehaviour
{
    public static ZombieSpawner Instance { get; private set; }

    // ──────────────────────────────────────────────────────────
    // Wave Definition
    // ──────────────────────────────────────────────────────────
    [System.Serializable]
    public struct WaveData
    {
        [Tooltip("How many zombies spawn in this wave")]
        public int zombieCount;
        [Tooltip("Seconds between individual zombie spawns within the wave")]
        public float spawnInterval;
        [Tooltip("Seconds before this wave starts (after previous wave ends)")]
        public float delayBeforeWave;
    }

    // ──────────────────────────────────────────────────────────
    // Inspector
    // ──────────────────────────────────────────────────────────
    [Header("Zombie Prefab")]
    [Tooltip("Root zombie prefab to instantiate. Must have ZombieHealth, ZombiePrototypeMover, ZombieAttack.")]
    public GameObject zombiePrefab;

    [Header("Spawn Position")]
    [Tooltip("X position where zombies spawn (right side of the map).")]
    public float spawnX = 8f;
    [Tooltip("Y (height) at which zombies are spawned.")]
    public float spawnY = 0f;

    [Header("Waves")]
    public WaveData[] waves = new WaveData[]
    {
        new WaveData { zombieCount = 3, spawnInterval = 2f, delayBeforeWave = 5f },
        new WaveData { zombieCount = 5, spawnInterval = 1.5f, delayBeforeWave = 10f },
        new WaveData { zombieCount = 8, spawnInterval = 1f, delayBeforeWave = 10f },
    };

    [Header("State (read-only)")]
    [SerializeField] private int currentWaveIndex = 0;
    [SerializeField] private int activeZombieCount = 0;

    // ──────────────────────────────────────────────────────────
    // Private
    // ──────────────────────────────────────────────────────────
    private List<GameObject> activeZombies = new List<GameObject>();
    private bool allWavesComplete = false;

    // ──────────────────────────────────────────────────────────
    // Unity lifecycle
    // ──────────────────────────────────────────────────────────
    private void Awake()
    {
        if (Instance == null) Instance = this;
        else { Destroy(gameObject); return; }
    }

    private void Start()
    {
        if (zombiePrefab == null)
        {
            Debug.LogError("[ZombieSpawner] zombiePrefab is not assigned!");
            return;
        }

        if (GridManager.Instance == null)
        {
            Debug.LogError("[ZombieSpawner] GridManager not found in scene!");
            return;
        }

        StartCoroutine(RunWaves());
    }

    // ──────────────────────────────────────────────────────────
    // Wave loop
    // ──────────────────────────────────────────────────────────
    private IEnumerator RunWaves()
    {
        for (int i = 0; i < waves.Length; i++)
        {
            currentWaveIndex = i;
            WaveData wave = waves[i];

            // Wait before this wave
            yield return new WaitForSeconds(wave.delayBeforeWave);

            Debug.Log($"[ZombieSpawner] Starting Wave {i + 1} / {waves.Length} — {wave.zombieCount} zombies");

            for (int z = 0; z < wave.zombieCount; z++)
            {
                SpawnZombie();
                yield return new WaitForSeconds(wave.spawnInterval);
            }

            // Wait until all zombies from this wave are dead before next wave
            yield return new WaitUntil(() => activeZombieCount <= 0);
        }

        allWavesComplete = true;
        Debug.Log("[ZombieSpawner] All waves complete!");
        GameManager.Instance?.OnAllWavesComplete();
    }

    // ──────────────────────────────────────────────────────────
    // Spawn a single zombie in a random lane
    // ──────────────────────────────────────────────────────────
    private void SpawnZombie()
    {
        int laneCount = GridManager.Instance.LaneCount;
        if (laneCount == 0)
        {
            Debug.LogWarning("[ZombieSpawner] No lanes found — GridManager may not be ready yet.");
            return;
        }

        int lane = Random.Range(0, laneCount);
        float laneZ = GridManager.Instance.GetLaneZ(lane);
        Vector3 spawnPos = new Vector3(spawnX, spawnY, laneZ);

        GameObject zombie = Instantiate(zombiePrefab, spawnPos, Quaternion.identity);
        zombie.name = $"Zombie_Wave{currentWaveIndex + 1}";

        activeZombies.Add(zombie);
        activeZombieCount++;

        Debug.Log($"[ZombieSpawner] Spawned zombie in lane {lane} at Z={laneZ:F2}");
    }

    // ──────────────────────────────────────────────────────────
    // Called by ZombieHealth when a zombie dies
    // ──────────────────────────────────────────────────────────
    public void OnZombieDied(GameObject zombie)
    {
        activeZombies.Remove(zombie);
        activeZombieCount = Mathf.Max(0, activeZombieCount - 1);
    }

    // ──────────────────────────────────────────────────────────
    // Public queries
    // ──────────────────────────────────────────────────────────
    public bool AllWavesComplete => allWavesComplete;
    public int ActiveZombieCount => activeZombieCount;
    public int CurrentWaveIndex => currentWaveIndex;
    public int TotalWaves => waves.Length;
}
