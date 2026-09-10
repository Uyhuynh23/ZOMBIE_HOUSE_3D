using System;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Creates a third, independent enemy type from the imported Lizard asset.
/// The prefab inherits the established collision, health, minimap and shared
/// lane-AI setup from Zombie; only visuals, animation, and per-type stats vary.
/// </summary>
public static class LizardMonsterBuilder
{
    private const string BaseEnemyPath = "Assets/Prefabs/Zombie.prefab";
    private const string VisualPath = "Assets/Hatogame_new/Lizard/Perfabs/Lizard_B.prefab";
    private const string SourceMaterialPath = "Assets/Hatogame_new/Lizard/Material/LizardB_Mat.mat";
    private const string OutputPrefabPath = "Assets/Prefabs/LizardMonster.prefab";
    private const string UrpMaterialPath = "Assets/Materials/LizardMonsterURP.mat";

    [MenuItem("Tools/Zombie House/Create Lizard Monster Enemy")]
    public static void CreateLizardMonsterEnemy()
    {
        GameObject baseEnemy = AssetDatabase.LoadAssetAtPath<GameObject>(BaseEnemyPath);
        GameObject lizardVisual = AssetDatabase.LoadAssetAtPath<GameObject>(VisualPath);
        if (baseEnemy == null || lizardVisual == null)
        {
            Debug.LogError("[LizardMonsterBuilder] Could not load the required Zombie or Lizard prefab.");
            return;
        }

        GameObject instance = (GameObject)PrefabUtility.InstantiatePrefab(baseEnemy);
        instance.name = "Lizard Monster";
        PrefabUtility.UnpackPrefabInstance(instance, PrefabUnpackMode.Completely, InteractionMode.AutomatedAction);

        Transform oldVisual = instance.transform.Find("Zombie Visual");
        float targetHeight = GetVisualHeight(oldVisual);
        if (oldVisual != null)
            UnityEngine.Object.DestroyImmediate(oldVisual.gameObject);

        GameObject visual = (GameObject)PrefabUtility.InstantiatePrefab(lizardVisual);
        visual.name = "Lizard Monster Visual";
        visual.transform.SetParent(instance.transform, false);
        visual.transform.localPosition = Vector3.zero;
        visual.transform.localRotation = Quaternion.identity;

        // The imported asset uses Unity's legacy Standard shader.  This project
        // runs URP, where that shader renders magenta, so give this enemy its own
        // URP Lit material instead of modifying the third-party source asset.
        Material urpMaterial = CreateOrUpdateUrpMaterial();
        if (urpMaterial != null)
            ApplyMaterial(visual, urpMaterial);

        float visualHeight = GetVisualHeight(visual.transform);
        if (targetHeight > 0.01f && visualHeight > 0.01f)
        {
            float matchingScale = Mathf.Clamp(targetHeight / visualHeight, 0.1f, 10f);
            visual.transform.localScale *= matchingScale;
        }

        // Type-specific stats. Movement speed remains controlled by the map's
        // existing ZombieSpawner / MapWaveConfig, exactly like other enemies.
        ZombieHealth health = instance.GetComponent<ZombieHealth>();
        if (health != null)
        {
            health.maxHealth = 150;
            health.currentHealth = 150;
            health.deathDelay = 1.0f;
        }

        ZombieAttack attack = instance.GetComponent<ZombieAttack>();
        if (attack != null)
        {
            attack.damagePerAttack = 14;
            attack.attackInterval = 1.05f;
            attack.attackRange = 1.15f;
        }

        // The Lizard has its own animation controller with named states, so
        // the generic Zombie animator hooks must remain empty.
        AssignReference(instance.GetComponent<EnemyNavAgent>(), "animator", null);
        AssignReference(instance.GetComponent<EnemyNavAgent>(), "visualRoot", visual.transform);
        AssignBool(instance.GetComponent<EnemyNavAgent>(), "useSharedMoveAnimation", false);
        AssignFloat(instance.GetComponent<EnemyNavAgent>(), "visualYawOffset", 0f);
        AssignReference(instance.GetComponent<ZombieAttack>(), "animator", null);
        AssignReference(instance.GetComponent<ZombieAttack>(), "visualRoot", visual.transform);
        AssignBool(instance.GetComponent<ZombieAttack>(), "useSharedAttackAnimation", false);
        AssignReference(instance.GetComponent<ZombiePrototypeMover>(), "animator", null);

        ZombieSway sway = instance.GetComponent<ZombieSway>();
        if (sway != null) UnityEngine.Object.DestroyImmediate(sway);

        LizardAnimationDriver driver = instance.GetComponent<LizardAnimationDriver>();
        if (driver == null) driver = instance.AddComponent<LizardAnimationDriver>();
        AssignReference(driver, "animator", visual.GetComponentInChildren<Animator>());

        GameObject prefab = PrefabUtility.SaveAsPrefabAsset(instance, OutputPrefabPath);
        UnityEngine.Object.DestroyImmediate(instance);
        if (prefab == null)
        {
            Debug.LogError("[LizardMonsterBuilder] Could not save LizardMonster.prefab.");
            return;
        }

        AddToWaveConfigs(prefab);
        AddToSceneSpawners(prefab);
        AssetDatabase.SaveAssets();
        Debug.Log("[LizardMonsterBuilder] Created LizardMonster.prefab and registered it in every playable map.");
    }

    private static Material CreateOrUpdateUrpMaterial()
    {
        Shader urpLit = Shader.Find("Universal Render Pipeline/Lit");
        if (urpLit == null)
        {
            Debug.LogError("[LizardMonsterBuilder] URP Lit shader was not found.");
            return null;
        }

        Material source = AssetDatabase.LoadAssetAtPath<Material>(SourceMaterialPath);
        if (source == null)
        {
            Debug.LogError("[LizardMonsterBuilder] Could not load the Lizard source material.");
            return null;
        }

        Material material = AssetDatabase.LoadAssetAtPath<Material>(UrpMaterialPath);
        if (material == null)
        {
            material = new Material(urpLit) { name = "LizardMonsterURP" };
            AssetDatabase.CreateAsset(material, UrpMaterialPath);
        }
        else
        {
            material.shader = urpLit;
        }

        Texture albedo = source.GetTexture("_MainTex");
        Texture normal = source.GetTexture("_BumpMap");
        material.SetColor("_BaseColor", Color.white);
        material.SetTexture("_BaseMap", albedo);
        material.SetFloat("_Metallic", 0f);
        material.SetFloat("_Smoothness", Mathf.Clamp01(source.GetFloat("_Glossiness")));
        material.SetTexture("_BumpMap", normal);
        material.SetFloat("_BumpScale", source.GetFloat("_BumpScale"));
        if (normal != null) material.EnableKeyword("_NORMALMAP");
        else material.DisableKeyword("_NORMALMAP");
        EditorUtility.SetDirty(material);
        return material;
    }

    private static void ApplyMaterial(GameObject visual, Material material)
    {
        foreach (Renderer renderer in visual.GetComponentsInChildren<Renderer>(true))
        {
            Material[] materials = renderer.sharedMaterials;
            for (int i = 0; i < materials.Length; i++)
                materials[i] = material;
            renderer.sharedMaterials = materials;
            EditorUtility.SetDirty(renderer);
        }
    }

    private static void AddToWaveConfigs(GameObject lizardPrefab)
    {
        string[] configPaths =
        {
            "Assets/Data/Waves/WaveConfig_Map_Day.asset",
            "Assets/Data/Waves/WaveConfig_Map_Cloudy.asset",
            "Assets/Data/Waves/WaveConfig_Map_Night.asset"
        };

        foreach (string path in configPaths)
        {
            MapWaveConfig config = AssetDatabase.LoadAssetAtPath<MapWaveConfig>(path);
            if (config == null || Contains(config.allowedEnemyPrefabs, lizardPrefab)) continue;
            Array.Resize(ref config.allowedEnemyPrefabs, config.allowedEnemyPrefabs.Length + 1);
            config.allowedEnemyPrefabs[config.allowedEnemyPrefabs.Length - 1] = lizardPrefab;
            EditorUtility.SetDirty(config);
        }
    }

    private static void AddToSceneSpawners(GameObject lizardPrefab)
    {
        string[] scenePaths =
        {
            "Assets/Scenes/GameScenes/Map_Day.unity",
            "Assets/Scenes/GameScenes/Map_Cloudy.unity",
            "Assets/Scenes/GameScenes/Map_Night.unity"
        };

        SceneSetup[] setup = EditorSceneManager.GetSceneManagerSetup();
        try
        {
            foreach (string path in scenePaths)
            {
                Scene scene = SceneManager.GetSceneByPath(path);
                bool wasLoaded = scene.isLoaded;
                if (!wasLoaded) scene = EditorSceneManager.OpenScene(path, OpenSceneMode.Additive);

                bool changed = false;
                foreach (GameObject root in scene.GetRootGameObjects())
                {
                    foreach (ZombieSpawner spawner in root.GetComponentsInChildren<ZombieSpawner>(true))
                    {
                        if (Contains(spawner.enemyPrefabs, lizardPrefab)) continue;
                        Array.Resize(ref spawner.enemyPrefabs, spawner.enemyPrefabs.Length + 1);
                        spawner.enemyPrefabs[spawner.enemyPrefabs.Length - 1] = lizardPrefab;
                        EditorUtility.SetDirty(spawner);
                        changed = true;
                    }
                }

                if (changed) EditorSceneManager.SaveScene(scene);
                if (!wasLoaded) EditorSceneManager.CloseScene(scene, true);
            }
        }
        finally
        {
            EditorSceneManager.RestoreSceneManagerSetup(setup);
        }
    }

    private static bool Contains(GameObject[] prefabs, GameObject item)
    {
        if (prefabs == null) return false;
        foreach (GameObject prefab in prefabs)
            if (prefab == item) return true;
        return false;
    }

    private static float GetVisualHeight(Transform root)
    {
        if (root == null) return 0f;
        Renderer[] renderers = root.GetComponentsInChildren<Renderer>(true);
        if (renderers.Length == 0) return 0f;

        Bounds bounds = renderers[0].bounds;
        for (int i = 1; i < renderers.Length; i++) bounds.Encapsulate(renderers[i].bounds);
        return bounds.size.y;
    }

    private static void AssignReference(Component component, string propertyName, UnityEngine.Object reference)
    {
        if (component == null) return;
        SerializedObject serialized = new SerializedObject(component);
        SerializedProperty property = serialized.FindProperty(propertyName);
        if (property == null) return;
        property.objectReferenceValue = reference;
        serialized.ApplyModifiedPropertiesWithoutUndo();
    }

    private static void AssignFloat(Component component, string propertyName, float value)
    {
        if (component == null) return;
        SerializedObject serialized = new SerializedObject(component);
        SerializedProperty property = serialized.FindProperty(propertyName);
        if (property == null) return;
        property.floatValue = value;
        serialized.ApplyModifiedPropertiesWithoutUndo();
    }

    private static void AssignBool(Component component, string propertyName, bool value)
    {
        if (component == null) return;
        SerializedObject serialized = new SerializedObject(component);
        SerializedProperty property = serialized.FindProperty(propertyName);
        if (property == null) return;
        property.boolValue = value;
        serialized.ApplyModifiedPropertiesWithoutUndo();
    }
}
