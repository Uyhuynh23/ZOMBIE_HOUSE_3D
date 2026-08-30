using System;
using System.Linq;
using UnityEditor;
using UnityEngine;

/// <summary>
/// Creates the reusable zombie prefab used by every gameplay scene.
/// The prefab only references version-controlled project assets, so it remains
/// intact when another branch merges or cherry-picks the zombie implementation.
/// </summary>
public static class ZombiePrefabBuilder
{
    public const string PrefabPath = "Assets/Prefabs/Zombie.prefab";

    private const string ModelPath = "Assets/ThirdParty/CartoonZombie/Zombie_low.fbx";

    // Optional editor command for intentionally rebuilding the committed prefab.
    [MenuItem("Zombie House/Rebuild Zombie Prefab")]
    public static void RebuildFromMenu()
    {
        GameObject prefab = RebuildPrefab();
        Selection.activeObject = prefab;
        EditorGUIUtility.PingObject(prefab);
        Debug.Log($"[ZombiePrefabBuilder] Zombie prefab ready: {PrefabPath}");
    }

    public static GameObject LoadOrCreatePrefab()
    {
        GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath);
        return prefab != null ? prefab : RebuildPrefab();
    }

    public static GameObject RebuildPrefab()
    {
        EnsureRequiredAssets();

        GameObject root = new GameObject("Zombie");

        try
        {
            root.tag = "Zombie";

            CapsuleCollider bodyCollider = root.AddComponent<CapsuleCollider>();
            bodyCollider.height = 2.1f;
            bodyCollider.radius = 0.48f;
            bodyCollider.center = new Vector3(0f, 1.05f, 0f);

            Rigidbody body = root.AddComponent<Rigidbody>();
            body.useGravity = false;
            body.isKinematic = true;
            body.interpolation = RigidbodyInterpolation.Interpolate;

            ZombieHealth health = root.AddComponent<ZombieHealth>();
            health.maxHealth = 100;
            root.AddComponent<ZombieHealthBar>();
            root.AddComponent<ZombiePrototypeMover>();
            root.AddComponent<ZombieAttack>();

            GameObject modelAsset = AssetDatabase.LoadAssetAtPath<GameObject>(ModelPath);
            GameObject visual = (GameObject)PrefabUtility.InstantiatePrefab(modelAsset);
            visual.name = "Zombie Visual";
            visual.transform.SetParent(root.transform, false);
            visual.transform.localRotation = Quaternion.identity;

            ZombiePrototypeSceneBuilder.ApplyZombieMaterial(visual);
            // Keep the zombie visually comparable to the 1.5x plant models on the map.
            ZombiePrototypeSceneBuilder.FitZombieVisual(visual, 2.1f);

            Animator animator = visual.GetComponent<Animator>();
            if (animator == null)
                animator = visual.AddComponent<Animator>();

            animator.runtimeAnimatorController = ZombiePrototypeSceneBuilder.CreateZombieAnimatorController();
            animator.applyRootMotion = false;

            GameObject prefab = PrefabUtility.SaveAsPrefabAsset(root, PrefabPath, out bool savedSuccessfully);
            if (!savedSuccessfully || prefab == null)
                throw new InvalidOperationException($"Could not save zombie prefab at {PrefabPath}.");

            AssetDatabase.SaveAssets();
            AssetDatabase.ImportAsset(PrefabPath, ImportAssetOptions.ForceUpdate);
            ValidatePrefab(prefab);
            return prefab;
        }
        finally
        {
            UnityEngine.Object.DestroyImmediate(root);
        }
    }

    private static void EnsureRequiredAssets()
    {
        if (AssetDatabase.LoadAssetAtPath<GameObject>(ModelPath) == null)
            throw new InvalidOperationException($"Missing zombie model: {ModelPath}");

        if (!AssetDatabase.IsValidFolder("Assets/Prefabs"))
            AssetDatabase.CreateFolder("Assets", "Prefabs");
    }

    private static void ValidatePrefab(GameObject prefab)
    {
        Type[] requiredComponents =
        {
            typeof(CapsuleCollider),
            typeof(Rigidbody),
            typeof(ZombieHealth),
            typeof(ZombieHealthBar),
            typeof(ZombiePrototypeMover),
            typeof(ZombieAttack),
        };

        Type missingType = requiredComponents.FirstOrDefault(type => prefab.GetComponent(type) == null);
        if (missingType != null)
            throw new InvalidOperationException($"Zombie prefab is missing {missingType.Name}.");

        Animator animator = prefab.GetComponentInChildren<Animator>();
        if (animator == null || animator.runtimeAnimatorController == null)
            throw new InvalidOperationException("Zombie prefab is missing its animator controller.");
    }
}
