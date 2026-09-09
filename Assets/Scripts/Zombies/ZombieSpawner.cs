using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

/// <summary>
/// Spawns waves of enemies that navigate to Baker_house via NavMesh.
/// Supports 4 directional spawn zones (East/North/West/South).
///
/// Lane assignment is handled exclusively by LaneEntrance triggers in the scene.
/// This spawner only picks a random spawn point and instantiates the prefab.
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
        [Tooltip("How many enemies spawn in this wave")]
        public int zombieCount;
        [Tooltip("Seconds between individual spawns within the wave")]
        public float spawnInterval;
        [Tooltip("Seconds before this wave starts (after previous wave ends)")]
        public float delayBeforeWave;
    }

    // ──────────────────────────────────────────────────────────
    // Inspector — NavMesh Mode (primary)
    // ──────────────────────────────────────────────────────────
    [Header("Enemy Prefabs")]
    [Tooltip("Primary zombie prefab — must have EnemyNavAgent, ZombieHealth, ZombieAttack.")]
    public GameObject zombiePrefab;
    [Tooltip("All enemy prefabs to pick from randomly (overrides zombiePrefab if filled).")]
    public GameObject[] enemyPrefabs;

    [Header("Spawn Points")]
    [Tooltip("Spawn Transforms scattered around the map perimeter.")]
    public Transform[] spawnPoints;
    [Tooltip("Small XZ random offset at spawn so enemies don't stack exactly.")]
    [Min(0f)] public float spawnJitter = 0.5f;

    [Header("House Target")]
    [Tooltip("Baker_house transform. Auto-found by tag 'HouseTarget' or name 'Baker_house' if null.")]
    public Transform houseTarget;

    [Header("Wave Config (Optional ScriptableObject)")]
    [Tooltip("If assigned, wave data and speed come from this asset.")]
    public MapWaveConfig waveConfig;

    [Header("Waves")]
    public WaveData[] waves = new WaveData[]
    {
        new WaveData { zombieCount = 15, spawnInterval = 1.0f, delayBeforeWave = 5f  },
        new WaveData { zombieCount = 25, spawnInterval = 0.8f, delayBeforeWave = 8f  },
        new WaveData { zombieCount = 35, spawnInterval = 0.6f, delayBeforeWave = 8f  },
        new WaveData { zombieCount = 50, spawnInterval = 0.4f, delayBeforeWave = 10f },
    };

    [Header("Movement Speed")]
    [Min(0.1f)] public float baseEnemySpeed = 1.4f;
    [Min(0f)]   public float speedIncreasePerWave = 0.15f;

    // ──────────────────────────────────────────────────────────
    // Inspector — Legacy Route Mode (test scenes only)
    // ──────────────────────────────────────────────────────────
    [Header("Legacy Route Mode (Test Scenes Only)")]
    public ZombieRoute[] routes;
    [Min(0.1f)] public float routeMoveSpeed = 2.5f;
    [Min(0f)]   public float routeSpawnJitter = 0.35f;

    // ──────────────────────────────────────────────────────────
    // State (read-only inspector display)
    // ──────────────────────────────────────────────────────────
    [Header("State (read-only)")]
    [SerializeField] private int currentWaveIndex;
    [SerializeField] private int activeEnemyCount;
    [SerializeField] private float nextWaveCountdown;
    [SerializeField] private int remainingToSpawn;

    // ──────────────────────────────────────────────────────────
    // Private
    // ──────────────────────────────────────────────────────────
    private List<GameObject> activeEnemies = new List<GameObject>();
    private bool allWavesComplete;
    private int  spawnSequence;

    // ──────────────────────────────────────────────────────────
    // Unity lifecycle
    // ──────────────────────────────────────────────────────────
    private void Awake()
    {
        if (Instance == null) Instance = this;
        else { Destroy(gameObject); return; }

        // Apply ScriptableObject overrides
        if (waveConfig != null)
        {
            if (waveConfig.waves != null && waveConfig.waves.Length > 0)
                waves = waveConfig.waves;
            if (waveConfig.routeMoveSpeed > 0f)
                baseEnemySpeed = waveConfig.routeMoveSpeed;
            if (waveConfig.routeSpawnJitter >= 0f)
                spawnJitter = waveConfig.routeSpawnJitter;
            if (waveConfig.allowedEnemyPrefabs != null && waveConfig.allowedEnemyPrefabs.Length > 0)
                enemyPrefabs = waveConfig.allowedEnemyPrefabs;
        }
    }

    private void Start()
    {
        if (zombiePrefab == null && (enemyPrefabs == null || enemyPrefabs.Length == 0))
        {
            Debug.LogError("[ZombieSpawner] No enemy prefabs assigned!");
            return;
        }

        if (houseTarget == null)
        {
            GameObject h = GameObject.FindWithTag("HouseTarget");
            if (h == null) h = GameObject.Find("Baker_house");
            if (h != null) houseTarget = h.transform;
            else Debug.LogWarning("[ZombieSpawner] Baker_house not found!");
        }

        if (!HasLegacyRoutes())
        {
            var tri = NavMesh.CalculateTriangulation();
            if (tri.indices.Length == 0)
                Debug.LogWarning("[ZombieSpawner] NavMesh not baked!");
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
                if (GameManager.Instance == null ||
                    GameManager.Instance.CurrentState == GameManager.GameState.Playing)
                    nextWaveCountdown -= Time.deltaTime;
                yield return null;
            }

            Debug.Log($"[ZombieSpawner] Wave {i + 1}/{waves.Length} — {wave.zombieCount} enemies, speed={WaveSpeed(i):F2}");
            remainingToSpawn = wave.zombieCount;

            for (int z = 0; z < wave.zombieCount; z++)
            {
                while (GameManager.Instance != null &&
                       GameManager.Instance.CurrentState != GameManager.GameState.Playing)
                    yield return null;

                if (HasLegacyRoutes())
                    SpawnOnLegacyRoute();
                else
                    SpawnNavMeshEnemy(i);

                remainingToSpawn--;
                yield return new WaitForSeconds(wave.spawnInterval);
            }

            yield return new WaitUntil(() => activeEnemyCount <= 0);
        }

        allWavesComplete  = true;
        nextWaveCountdown = 0f;
        remainingToSpawn  = 0;
        Debug.Log("[ZombieSpawner] All waves complete!");
        GameManager.Instance?.OnAllWavesComplete();
    }

    // ──────────────────────────────────────────────────────────
    // NavMesh spawn (primary)
    // ──────────────────────────────────────────────────────────
    private void SpawnNavMeshEnemy(int waveIndex)
    {
        Vector3 spawnPos = GetRandomNavMeshSpawnPosition();

        GameObject prefab = GetRandomEnemyPrefab();
        if (prefab == null) return;

        GameObject enemy = Instantiate(prefab, spawnPos, Quaternion.identity);
        enemy.name  = $"{prefab.name}_W{waveIndex + 1}";
        enemy.layer = LayerMask.NameToLayer("Enemy");

        EnemyNavAgent navAgent = enemy.GetComponent<EnemyNavAgent>();
        if (navAgent != null)
        {
            navAgent.moveSpeed   = WaveSpeed(waveIndex);
            navAgent.houseTarget = houseTarget;
            // Lane assignment happens later via LaneEntrance trigger in the scene.
            // Do NOT call any lane assignment here.
        }

        activeEnemies.Add(enemy);
        activeEnemyCount++;
        Debug.Log($"[ZombieSpawner] Spawned {prefab.name} at {spawnPos}");
    }

    /// <summary>
    /// Picks a random spawn point (with small XZ jitter) and snaps it to the NavMesh.
    /// No lane logic here — zombies move freely until they cross a LaneEntrance trigger.
    /// </summary>
    private Vector3 GetRandomNavMeshSpawnPosition()
    {
        Vector3 candidate = transform.position;

        if (spawnPoints != null && spawnPoints.Length > 0)
        {
            Transform sp = spawnPoints[spawnSequence % spawnPoints.Length];
            spawnSequence++;

            if (sp != null)
            {
                candidate = sp.position;
                // Small XZ jitter so zombies don't stack on the same point
                candidate.x += Random.Range(-spawnJitter, spawnJitter);
                candidate.z += Random.Range(-spawnJitter, spawnJitter);
            }
        }

        // Snap to nearest NavMesh surface
        NavMeshHit hit;
        if (NavMesh.SamplePosition(candidate, out hit, 10f, NavMesh.AllAreas))
            return hit.position;

        return candidate;
    }

    // ──────────────────────────────────────────────────────────
    // Legacy route spawn (test scenes)
    // ──────────────────────────────────────────────────────────
    private void SpawnOnLegacyRoute()
    {
        ZombieRoute route = routes[spawnSequence % routes.Length];
        spawnSequence++;

        Transform sp   = route.SpawnPoint;
        Transform next = route.GetWaypoint(1);
        Vector3 fwd    = next != null ? (next.position - sp.position).normalized : Vector3.forward;
        Vector3 side   = Vector3.Cross(Vector3.up, fwd).normalized;
        Vector3 pos    = sp.position + side * Random.Range(-routeSpawnJitter, routeSpawnJitter);

        GameObject prefab = GetRandomEnemyPrefab();
        if (prefab == null) return;

        GameObject enemy = Instantiate(prefab, pos, Quaternion.identity);
        enemy.name  = $"{prefab.name}_W{currentWaveIndex + 1}_{route.name}";
        enemy.layer = LayerMask.NameToLayer("Enemy");

        ZombiePrototypeMover legacyMover = enemy.GetComponent<ZombiePrototypeMover>();
        if (legacyMover != null)
            legacyMover.ConfigureRoute(enemy.GetComponentInChildren<Animator>(), route, routeMoveSpeed);

        activeEnemies.Add(enemy);
        activeEnemyCount++;
        Debug.Log($"[ZombieSpawner] (Legacy) Spawned {prefab.name} on route {route.name}");
    }

    // ──────────────────────────────────────────────────────────
    // Helpers
    // ──────────────────────────────────────────────────────────
    private float WaveSpeed(int waveIndex) => baseEnemySpeed + waveIndex * speedIncreasePerWave;

    private bool HasLegacyRoutes()
    {
        if (routes == null || routes.Length == 0) return false;
        foreach (var r in routes)
            if (r == null || r.WaypointCount < 2) return false;
        return true;
    }

    private GameObject GetRandomEnemyPrefab()
    {
        if (enemyPrefabs != null && enemyPrefabs.Length > 0)
        {
            var valid = new List<GameObject>();
            foreach (var p in enemyPrefabs)
                if (p != null) valid.Add(p);
            if (valid.Count > 0)
                return valid[Random.Range(0, valid.Count)];
        }
        return zombiePrefab;
    }

    // ──────────────────────────────────────────────────────────
    // Called by ZombieHealth when an enemy dies
    // ──────────────────────────────────────────────────────────
    public void OnZombieDied(GameObject enemy)
    {
        activeEnemies.Remove(enemy);
        activeEnemyCount = Mathf.Max(0, activeEnemyCount - 1);
    }

    // ──────────────────────────────────────────────────────────
    // Public queries
    // ──────────────────────────────────────────────────────────
    public bool  AllWavesComplete  => allWavesComplete;
    public int   ActiveZombieCount => activeEnemyCount;
    public int   CurrentWaveIndex  => currentWaveIndex;
    public int   TotalWaves        => waves.Length;
    public int   CurrentWaveNumber => Mathf.Clamp(currentWaveIndex + 1, 1, Mathf.Max(1, waves.Length));
    public float NextWaveCountdown => Mathf.Max(0f, nextWaveCountdown);
    public int   RemainingToSpawn  => Mathf.Max(0, remainingToSpawn);
}
