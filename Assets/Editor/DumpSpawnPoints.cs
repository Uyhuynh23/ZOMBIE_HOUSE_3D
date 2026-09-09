using UnityEngine;

public class DumpSpawnPoints
{
    public static void Dump()
    {
        var spawner = Object.FindObjectOfType<ZombieSpawner>();
        if (spawner != null && spawner.spawnPoints != null)
        {
            Debug.Log($"Found {spawner.spawnPoints.Length} spawn points:");
            foreach (var sp in spawner.spawnPoints)
            {
                if (sp != null)
                    Debug.Log($"Spawn: {sp.name} at {sp.position}");
            }
        }
    }
}
