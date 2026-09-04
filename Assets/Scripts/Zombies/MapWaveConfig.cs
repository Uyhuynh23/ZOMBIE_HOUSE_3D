using UnityEngine;

/// <summary>
/// ScriptableObject to define wave configurations per map.
/// Allows designers to easily tweak zombie counts, intervals, delays, and speeds
/// for each scene without modifying scene files or code.
/// </summary>
[CreateAssetMenu(fileName = "NewWaveConfig", menuName = "Zombie House/Map Wave Configuration")]
public class MapWaveConfig : ScriptableObject
{
    [Header("Map Info")]
    [Tooltip("Descriptive name of the map or round (e.g. Round 1 - Day).")]
    public string mapName = "New Map";

    [Header("Movement & Spawning")]
    [Tooltip("Movement speed for zombies along the routes on this map.")]
    [Min(0.1f)] public float routeMoveSpeed = 2.55f;

    [Tooltip("Small side offset at spawn so zombies do not walk in an exact single file line.")]
    [Min(0f)] public float routeSpawnJitter = 0.28f;

    [Header("Enemy Prefabs")]
    [Tooltip("Enemy prefabs eligible for spawning on this map (e.g. Zombie, Spider). If left empty, falls back to default zombie.")]
    public GameObject[] allowedEnemyPrefabs;

    [Header("Waves")]
    [Tooltip("List of waves for this map.")]
    public ZombieSpawner.WaveData[] waves = new ZombieSpawner.WaveData[]
    {
        new ZombieSpawner.WaveData { zombieCount = 4, spawnInterval = 1.6f, delayBeforeWave = 3f },
        new ZombieSpawner.WaveData { zombieCount = 8, spawnInterval = 1.2f, delayBeforeWave = 6f },
        new ZombieSpawner.WaveData { zombieCount = 12, spawnInterval = 0.9f, delayBeforeWave = 7f }
    };
}
