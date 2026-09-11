using System.Collections.Generic;
using Unity.Netcode;
using Unity.Netcode.Components;
using UnityEditor;
using UnityEngine;

/// <summary>One-click, repeatable prefab registration for the Cloudy authority baseline.</summary>
public static class MultiplayerPhase23Setup
{
    private const string PrefabListPath = "Assets/DefaultNetworkPrefabs.asset";
    private static readonly string[] GameplayPrefabs =
    {
        "Assets/Prefabs/Zombie.prefab",
        "Assets/Prefabs/Spider.prefab",
        "Assets/Prefabs/PeaShooter.prefab",
        "Assets/Prefabs/PeaShooterFroze.prefab",
        "Assets/Prefabs/Sunflower.prefab",
        "Assets/Prefabs/Pea_Prefab.prefab",
        "Assets/Prefabs/Sun.prefab"
    };

    [MenuItem("Tools/Multiplayer/Apply Phase 2-3 Cloudy Setup")]
    public static void Apply()
    {
        List<GameObject> configured = new();
        foreach (string path in GameplayPrefabs)
        {
            GameObject root = PrefabUtility.LoadPrefabContents(path);
            if (path.EndsWith("Pea_Prefab.prefab") && root.GetComponent<PeaProjectile>() == null)
                root.AddComponent<PeaProjectile>();
            if (root.GetComponent<NetworkObject>() == null) root.AddComponent<NetworkObject>();
            NetworkTransform transform = root.GetComponent<NetworkTransform>();
            if (transform == null) transform = root.AddComponent<NetworkTransform>();
            transform.AuthorityMode = NetworkTransform.AuthorityModes.Server;
            transform.Interpolate = true;
            transform.SyncScaleX = transform.SyncScaleY = transform.SyncScaleZ = false;
            PrefabUtility.SaveAsPrefabAsset(root, path);
            PrefabUtility.UnloadPrefabContents(root);
            configured.Add(AssetDatabase.LoadAssetAtPath<GameObject>(path));
        }

        NetworkPrefabsList list = AssetDatabase.LoadAssetAtPath<NetworkPrefabsList>(PrefabListPath);
        if (list == null)
        {
            list = ScriptableObject.CreateInstance<NetworkPrefabsList>();
            AssetDatabase.CreateAsset(list, PrefabListPath);
        }
        foreach (GameObject prefab in configured)
        {
            if (prefab != null && !list.Contains(prefab))
                list.Add(new NetworkPrefab { Prefab = prefab });
        }
        EditorUtility.SetDirty(list);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log($"[NET][BOOT] Phase 2-3 Cloudy setup configured and registered {configured.Count} gameplay prefabs.");
    }
}
