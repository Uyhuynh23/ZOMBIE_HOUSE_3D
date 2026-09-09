using UnityEngine;
using UnityEngine.AI;
using UnityEditor;
using UnityEditor.AI;

/// <summary>
/// Editor utility: marks all walkable static geometry as NavigationStatic,
/// then bakes the NavMesh for the currently open scene.
/// Run via: Tools → Zombie House → Bake NavMesh
/// </summary>
public static class NavMeshBaker
{
    private static readonly string[] StaticObjectPatterns = new[]
    {
        "Terrain", "Baker_house", "Fence", "Decorations", "Environment", "TreeDecor",
        "Camp_System", "MarketArea", "CampfireArea", "FarmPatch", "ScatteredProps",
        "Decoration_Outside", "Fence_System", "Tree", "Flower_Sys", "Water_Sys"
    };

    private static readonly string[] AlwaysWalkablePatterns = new[]
    {
        "Road", "Point", "SpawnPoint", "PlayerSpawnPoint", "Spawner"
    };

    [MenuItem("Tools/Zombie House/Bake NavMesh")]
    public static void BakeNavMesh()
    {
        Debug.Log("[NavMeshBaker] Starting NavMesh bake for: " +
                  UnityEngine.SceneManagement.SceneManager.GetActiveScene().name);

        int marked = 0;
        int skipped = 0;

        // Iterate every GameObject in the scene
        GameObject[] allObjects = GameObject.FindObjectsOfType<GameObject>();
        foreach (GameObject go in allObjects)
        {
            if (go == null) continue;

            string name = go.name;

            // Skip integration waypoints, spawn points (they're not physical geometry)
            bool isNavHelper = false;
            foreach (var skip in AlwaysWalkablePatterns)
            {
                if (name.Contains(skip)) { isNavHelper = true; break; }
            }
            if (isNavHelper) { skipped++; continue; }

            // Mark objects that should block NavMesh
            bool shouldMark = false;
            foreach (var pattern in StaticObjectPatterns)
            {
                if (name.ToLower().Contains(pattern.ToLower()))
                {
                    shouldMark = true;
                    break;
                }
            }

            // Also mark anything with a non-trigger Collider
            if (!shouldMark)
            {
                var col = go.GetComponent<Collider>();
                if (col != null && !col.isTrigger) shouldMark = true;
            }

            if (shouldMark)
            {
                GameObjectUtility.SetStaticEditorFlags(go,
                    GameObjectUtility.GetStaticEditorFlags(go) | StaticEditorFlags.NavigationStatic);
                marked++;
            }
        }

        Debug.Log($"[NavMeshBaker] Marked {marked} objects as NavigationStatic, skipped {skipped}.");

        // Bake
        UnityEditor.AI.NavMeshBuilder.BuildNavMesh();
        Debug.Log("[NavMeshBaker] NavMesh bake complete!");

        EditorUtility.DisplayDialog("NavMesh Baked",
            $"NavMesh baked successfully!\n\nMarked: {marked} objects\nSkipped: {skipped} objects\n\nZombie NavMesh agent should now work.", "OK");
    }

    [MenuItem("Tools/Zombie House/Clear NavMesh")]
    public static void ClearNavMesh()
    {
        NavMesh.RemoveAllNavMeshData();
        Debug.Log("[NavMeshBaker] NavMesh data cleared.");
    }

    [MenuItem("Tools/Zombie House/Setup Enemy Prefabs (Add NavMeshAgent)")]
    public static void SetupEnemyPrefabs()
    {
        string[] prefabPaths = { "Assets/Prefabs/Zombie.prefab", "Assets/Prefabs/Spider.prefab" };
        int updated = 0;

        foreach (string path in prefabPaths)
        {
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (prefab == null)
            {
                Debug.LogWarning($"[NavMeshBaker] Prefab not found: {path}");
                continue;
            }

            GameObject inst = PrefabUtility.InstantiatePrefab(prefab) as GameObject;

            bool isSpider = path.Contains("Spider");
            bool changed = false;

            // 1. Add NavMeshAgent if missing
            NavMeshAgent agent = inst.GetComponent<NavMeshAgent>();
            if (agent == null)
            {
                agent = inst.AddComponent<NavMeshAgent>();
                changed = true;
            }

            // Configure agent
            if (isSpider)
            {
                agent.radius       = 0.55f;
                agent.height       = 0.9f;
                agent.speed        = 1.6f;
                agent.angularSpeed = 320f;
            }
            else // Zombie
            {
                agent.radius       = 0.45f;
                agent.height       = 2.0f;
                agent.speed        = 1.4f;
                agent.angularSpeed = 300f;
            }
            agent.acceleration          = 8f;
            agent.stoppingDistance       = 1.5f;
            agent.obstacleAvoidanceType = ObstacleAvoidanceType.LowQualityObstacleAvoidance;
            agent.autoBraking           = false;

            // 2. Switch Rigidbody to Kinematic (NavMeshAgent + Dynamic RB = jitter)
            Rigidbody rb = inst.GetComponent<Rigidbody>();
            if (rb != null && !rb.isKinematic)
            {
                rb.isKinematic = true;
                rb.useGravity  = false;
                changed = true;
            }

            // 3. Add EnemyNavAgent if missing
            EnemyNavAgent navScript = inst.GetComponent<EnemyNavAgent>();
            if (navScript == null)
            {
                inst.AddComponent<EnemyNavAgent>();
                changed = true;
            }

            // 4. Add ZombieHealth if missing
            if (inst.GetComponent<ZombieHealth>() == null)
            {
                inst.AddComponent<ZombieHealth>();
                changed = true;
            }

            // 5. Add ZombieAttack if missing
            if (inst.GetComponent<ZombieAttack>() == null)
            {
                inst.AddComponent<ZombieAttack>();
                changed = true;
            }

            // 6. Add ZombieHealthBar if missing
            if (inst.GetComponent<ZombieHealthBar>() == null)
            {
                inst.AddComponent<ZombieHealthBar>();
                changed = true;
            }

            // 7. Add ZombieSway (Zombie only), SpiderAnimSync (Spider only)
            if (isSpider)
            {
                if (inst.GetComponent<SpiderAnimSync>() == null)
                { inst.AddComponent<SpiderAnimSync>(); changed = true; }
            }
            else
            {
                if (inst.GetComponent<ZombieSway>() == null)
                { inst.AddComponent<ZombieSway>(); changed = true; }
            }

            // 8. Set Enemy layer
            int enemyLayer = LayerMask.NameToLayer("Enemy");
            if (enemyLayer >= 0 && inst.layer != enemyLayer)
            {
                inst.layer = enemyLayer;
                changed = true;
            }

            if (changed)
            {
                PrefabUtility.SaveAsPrefabAsset(inst, path);
                Debug.Log($"[NavMeshBaker] Updated prefab: {path}");
                updated++;
            }

            GameObject.DestroyImmediate(inst);
        }

        Debug.Log($"[NavMeshBaker] Setup complete — {updated} prefab(s) updated.");
        EditorUtility.DisplayDialog("Prefab Setup Complete",
            $"Updated {updated} enemy prefab(s) with NavMeshAgent, EnemyNavAgent, and required components.", "OK");
    }
}
