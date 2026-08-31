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

    [Header("Map Routes (optional)")]
    [Tooltip("When assigned, zombies round-robin across these waypoint routes instead of the legacy X/Z grid lanes.")]
    public ZombieRoute[] routes;
    [Tooltip("Movement speed used for the larger four-road map.")]
    [Min(0.1f)] public float routeMoveSpeed = 2.5f;
    [Tooltip("Small side offset at spawn so a wave does not look perfectly mechanical.")]
    [Min(0f)] public float routeSpawnJitter = 0.35f;

    [Header("Spawn Position")]
    [Tooltip("X position where zombies spawn (right side of the map).")]
    public float spawnX = 8f;
    [Tooltip("Y (height) at which zombies are spawned.")]
    public float spawnY = 0f;
    [Tooltip("Random Z offset before zombies converge into their selected lane.")]
    public float laneApproachJitter = 0.9f;
    [Tooltip("Point where zombies finish their diagonal approach and walk straight.")]
    public float laneEntryX = 6.2f;
    [Tooltip("X position where zombies stop and attack the house.")]
    public float houseAttackX = -4.75f;

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
    [SerializeField] private float nextWaveCountdown;
    [SerializeField] private int remainingToSpawn;
    private int spawnSequence;

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

        if (!HasUsableRoutes() && GridManager.Instance == null)
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

            nextWaveCountdown = Mathf.Max(0f, wave.delayBeforeWave);
            while (nextWaveCountdown > 0f)
            {
                nextWaveCountdown -= Time.deltaTime;
                yield return null;
            }

            Debug.Log($"[ZombieSpawner] Starting Wave {i + 1} / {waves.Length} — {wave.zombieCount} zombies");
            remainingToSpawn = wave.zombieCount;

            for (int z = 0; z < wave.zombieCount; z++)
            {
                SpawnZombie();
                remainingToSpawn--;
                yield return new WaitForSeconds(wave.spawnInterval);
            }

            // Wait until all zombies from this wave are dead before next wave
            yield return new WaitUntil(() => activeZombieCount <= 0);
        }

        allWavesComplete = true;
        nextWaveCountdown = 0f;
        remainingToSpawn = 0;
        Debug.Log("[ZombieSpawner] All waves complete!");
        GameManager.Instance?.OnAllWavesComplete();
    }

    // ──────────────────────────────────────────────────────────
    // Spawn a single zombie in a random lane
    // ──────────────────────────────────────────────────────────
    private void SpawnZombie()
    {
        if (HasUsableRoutes())
        {
            SpawnZombieOnRoute();
            return;
        }

        int laneCount = GridManager.Instance.LaneCount;
        if (laneCount == 0)
        {
            Debug.LogWarning("[ZombieSpawner] No lanes found — GridManager may not be ready yet.");
            return;
        }

        // Deterministic round-robin keeps every lane represented and makes balancing reproducible.
        int lane = spawnSequence++ % laneCount;
        float laneZ = GridManager.Instance.GetLaneZ(lane);
        Vector3 spawnPos = new Vector3(
            spawnX + Random.Range(0f, 1.2f),
            spawnY,
            laneZ + Random.Range(-laneApproachJitter, laneApproachJitter));

        GameObject zombie = Instantiate(zombiePrefab, spawnPos, Quaternion.identity);
        zombie.name = $"Zombie_Wave{currentWaveIndex + 1}";
        zombie.SetActive(true);

        ZombiePrototypeMover mover = zombie.GetComponent<ZombiePrototypeMover>();
        if (mover != null)
        {
            mover.ConfigureLane(zombie.GetComponentInChildren<Animator>(), Vector3.left, houseAttackX);
            mover.AssignLane(laneZ, laneEntryX);
        }

        activeZombies.Add(zombie);
        activeZombieCount++;

        Debug.Log($"[ZombieSpawner] Spawned zombie in lane {lane} at Z={laneZ:F2}");
    }

    private void SpawnZombieOnRoute()
    {
        ZombieRoute route = routes[spawnSequence++ % routes.Length];
        Transform spawnPoint = route.SpawnPoint;
        Transform nextPoint = route.GetWaypoint(1);

        Vector3 forward = nextPoint != null
            ? (nextPoint.position - spawnPoint.position).normalized
            : Vector3.forward;
        Vector3 side = Vector3.Cross(Vector3.up, forward).normalized;
        Vector3 spawnPosition = spawnPoint.position + side * Random.Range(-routeSpawnJitter, routeSpawnJitter);

        GameObject zombie = Instantiate(zombiePrefab, spawnPosition, Quaternion.identity);
        zombie.name = $"Zombie_Wave{currentWaveIndex + 1}_{route.name}";
        zombie.SetActive(true);

        ZombiePrototypeMover mover = zombie.GetComponent<ZombiePrototypeMover>();
        if (mover != null)
            mover.ConfigureRoute(zombie.GetComponentInChildren<Animator>(), route, routeMoveSpeed);

        activeZombies.Add(zombie);
        activeZombieCount++;
        Debug.Log($"[ZombieSpawner] Spawned zombie on route {route.name}");
    }

    private bool HasUsableRoutes()
    {
        if (routes == null || routes.Length == 0)
            return false;

        foreach (ZombieRoute route in routes)
        {
            if (route == null || route.WaypointCount < 2)
                return false;
        }

        return true;
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
    public int CurrentWaveNumber => Mathf.Clamp(currentWaveIndex + 1, 1, Mathf.Max(1, waves.Length));
    public float NextWaveCountdown => Mathf.Max(0f, nextWaveCountdown);
    public int RemainingToSpawn => Mathf.Max(0, remainingToSpawn);
}
