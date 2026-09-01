using System;
using System.Linq;
using UnityEditor;
using UnityEditor.Animations;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class ZombiePrototypeSceneBuilder
{
    private const string ScenePath = "Assets/Scenes/ZombiePrototype.unity";
    private const string ZombieModelPath = "Assets/ThirdParty/CartoonZombie/Zombie_low.fbx";
    private const string ZombieTexturePath = "Assets/ThirdParty/CartoonZombie/zombie_tex.png";
    private const string ZombieFolder = "Assets/Zombie";
    private const string ZombieAnimationFolder = ZombieFolder + "/Animations";
    private const string ZombieMaterialFolder = ZombieFolder + "/Materials";
    private const string ZombieMaterialPath = ZombieMaterialFolder + "/Zombie_Body_Mat.mat";
    private const string IdleClipPath = ZombieAnimationFolder + "/Zombie_Idle.anim";
    private const string WalkClipPath = ZombieAnimationFolder + "/Zombie_Walk.anim";
    private const string AnimatorControllerPath = ZombieAnimationFolder + "/ZombiePrototype.controller";

    [MenuItem("Zombie House/Build Step 1 - Zombie Prototype")]
    public static void Build()
    {
        BuildScene(null, false);
    }

    [MenuItem("Zombie House/Build Step 2 - Animated Movement")]
    public static void BuildAnimatedMovement()
    {
        RuntimeAnimatorController controller = CreateZombieAnimatorController();
        BuildScene(controller, true);
    }

    private static void BuildScene(RuntimeAnimatorController animatorController, bool addMovement)
    {
        Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

        RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Trilight;
        RenderSettings.ambientSkyColor = new Color(0.18f, 0.24f, 0.3f);
        RenderSettings.ambientEquatorColor = new Color(0.22f, 0.28f, 0.26f);
        RenderSettings.ambientGroundColor = new Color(0.07f, 0.09f, 0.08f);

        CreateGround();
        CreateLighting();
        CreateCamera();
        CreateZombie(animatorController, addMovement);
        CreateStageProps();

        EditorSceneManager.SaveScene(scene, ScenePath);
        AssetDatabase.SaveAssets();
        Debug.Log("Zombie Prototype scene created: " + ScenePath);
    }

    private static void CreateGround()
    {
        GameObject ground = GameObject.CreatePrimitive(PrimitiveType.Plane);
        ground.name = "Test Ground";
        ground.transform.localScale = new Vector3(2.5f, 1f, 2.5f);
        ground.GetComponent<Renderer>().sharedMaterial = CreateMaterial("Ground_Mat", new Color(0.06f, 0.13f, 0.12f));
    }

    private static void CreateLighting()
    {
        GameObject moon = new GameObject("Moon Light");
        Light moonLight = moon.AddComponent<Light>();
        moonLight.type = LightType.Directional;
        moonLight.color = new Color(0.72f, 0.86f, 1f);
        moonLight.intensity = 1.6f;
        moon.transform.rotation = Quaternion.Euler(45f, -35f, 0f);

        CreatePointLight("Front Key Light", new Vector3(0f, 2.2f, -0.4f), new Color(0.72f, 1f, 0.86f), 4.5f, 5f);
        CreatePointLight("Green Rim Light", new Vector3(-3f, 3f, 2.5f), new Color(0.25f, 1f, 0.55f), 4f, 4f);
        CreatePointLight("Red Fill Light", new Vector3(3f, 2f, 1f), new Color(1f, 0.16f, 0.1f), 1.8f, 3f);
    }

    private static void CreateCamera()
    {
        GameObject cameraObject = new GameObject("Prototype Camera");
        Camera camera = cameraObject.AddComponent<Camera>();
        camera.tag = "MainCamera";
        camera.fieldOfView = 40f;
        camera.backgroundColor = new Color(0.015f, 0.025f, 0.04f);
        cameraObject.transform.position = new Vector3(0f, 1.45f, -0.9f);
        cameraObject.transform.LookAt(new Vector3(0f, 0.9f, 1.7f));
    }

    private static void CreateZombie(RuntimeAnimatorController animatorController, bool addMovement)
    {
        GameObject zombie = new GameObject("ZombiePrototype");
        zombie.transform.position = addMovement ? new Vector3(-0.65f, 0f, 1.8f) : new Vector3(0f, 0f, 1.8f);
        CapsuleCollider collider = zombie.AddComponent<CapsuleCollider>();
        collider.height = 1.25f;
        collider.radius = 0.3f;
        collider.center = new Vector3(0f, 0.625f, 0f);

        GameObject model = AssetDatabase.LoadAssetAtPath<GameObject>(ZombieModelPath);
        if (model != null)
        {
            GameObject visual = (GameObject)PrefabUtility.InstantiatePrefab(model);
            visual.name = "Zombie Visual (Cartoon Zombie)";
            visual.transform.SetParent(zombie.transform, false);
            visual.transform.localRotation = Quaternion.identity;
            ApplyZombieMaterial(visual);
            // This legacy FBX reports conservative animated bounds, so a smaller
            // target keeps the posed mesh at roughly human scale in Unity.
            FitZombieVisual(visual, 1.15f);

            if (animatorController != null)
            {
                Animator animator = visual.GetComponent<Animator>();
                if (animator == null)
                    animator = visual.AddComponent<Animator>();

                animator.runtimeAnimatorController = animatorController;
                animator.applyRootMotion = false;

                if (addMovement)
                    AddPrototypeMovement(zombie, animator);
            }
        }
        else
        {
            GameObject fallback = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            fallback.name = "Zombie Visual (Fallback)";
            fallback.transform.SetParent(zombie.transform, false);
            fallback.transform.localPosition = new Vector3(0f, 1f, 0f);
            fallback.GetComponent<Renderer>().sharedMaterial = CreateMaterial("Zombie_Fallback_Mat", new Color(0.2f, 0.72f, 0.35f));
        }

    }

    private static void AddPrototypeMovement(GameObject zombie, Animator animator)
    {
        GameObject path = new GameObject("Zombie Patrol Path");
        GameObject pointA = new GameObject("Patrol Point A");
        GameObject pointB = new GameObject("Patrol Point B");
        pointA.transform.SetParent(path.transform);
        pointB.transform.SetParent(path.transform);
        pointA.transform.position = new Vector3(-0.65f, 0f, 1.8f);
        pointB.transform.position = new Vector3(0.65f, 0f, 1.8f);

        ZombiePrototypeMover mover = zombie.AddComponent<ZombiePrototypeMover>();
        mover.ConfigurePatrol(animator, pointA.transform, pointB.transform);
    }

    internal static RuntimeAnimatorController CreateZombieAnimatorController()
    {
        EnsureFolder("Assets", "Zombie");
        EnsureFolder(ZombieFolder, "Animations");

        AnimatorController existingController = AssetDatabase.LoadAssetAtPath<AnimatorController>(AnimatorControllerPath);
        if (existingController != null)
            return existingController;

        AnimationClip[] sourceClips = AssetDatabase.LoadAllAssetsAtPath(ZombieModelPath)
            .OfType<AnimationClip>()
            .Where(clip => !clip.name.StartsWith("__preview__", StringComparison.Ordinal))
            .ToArray();

        AnimationClip idleSource = Array.Find(sourceClips, clip => clip.name.IndexOf("Idle", StringComparison.OrdinalIgnoreCase) >= 0);
        AnimationClip walkSource = Array.Find(sourceClips, clip => clip.name.IndexOf("Walk", StringComparison.OrdinalIgnoreCase) >= 0);

        if (idleSource == null || walkSource == null)
            throw new InvalidOperationException("The cartoon zombie FBX must contain both Idle and Walk clips.");

        AnimationClip idleClip = CopyLoopingClip(idleSource, IdleClipPath, "Zombie_Idle");
        AnimationClip walkClip = CopyLoopingClip(walkSource, WalkClipPath, "Zombie_Walk");

        AnimatorController controller = AnimatorController.CreateAnimatorControllerAtPath(AnimatorControllerPath);
        controller.AddParameter("MoveSpeed", AnimatorControllerParameterType.Float);

        AnimatorStateMachine stateMachine = controller.layers[0].stateMachine;
        AnimatorState idleState = stateMachine.AddState("Idle");
        idleState.motion = idleClip;
        stateMachine.defaultState = idleState;

        AnimatorState walkState = stateMachine.AddState("Walk");
        walkState.motion = walkClip;

        AnimatorStateTransition idleToWalk = idleState.AddTransition(walkState);
        idleToWalk.hasExitTime = false;
        idleToWalk.duration = 0.16f;
        idleToWalk.AddCondition(AnimatorConditionMode.Greater, 0.1f, "MoveSpeed");

        AnimatorStateTransition walkToIdle = walkState.AddTransition(idleState);
        walkToIdle.hasExitTime = false;
        walkToIdle.duration = 0.16f;
        walkToIdle.AddCondition(AnimatorConditionMode.Less, 0.1f, "MoveSpeed");

        EditorUtility.SetDirty(controller);
        AssetDatabase.SaveAssets();
        return controller;
    }

    private static AnimationClip CopyLoopingClip(AnimationClip source, string assetPath, string clipName)
    {
        AnimationClip destination = AssetDatabase.LoadAssetAtPath<AnimationClip>(assetPath);
        if (destination == null)
        {
            destination = new AnimationClip();
            AssetDatabase.CreateAsset(destination, assetPath);
        }

        EditorUtility.CopySerialized(source, destination);
        destination.name = clipName;
        destination.wrapMode = WrapMode.Loop;
        AnimationClipSettings settings = AnimationUtility.GetAnimationClipSettings(destination);
        settings.loopTime = true;
        AnimationUtility.SetAnimationClipSettings(destination, settings);
        EditorUtility.SetDirty(destination);
        return destination;
    }

    private static void EnsureFolder(string parent, string child)
    {
        string path = parent + "/" + child;
        if (!AssetDatabase.IsValidFolder(path))
            AssetDatabase.CreateFolder(parent, child);
    }

    private static void CreateStageProps()
    {
        Material markerMaterial = CreateMaterial("StageMarker_Mat", new Color(0.12f, 0.9f, 0.55f), true);
        for (int i = -2; i <= 2; i++)
        {
            GameObject marker = GameObject.CreatePrimitive(PrimitiveType.Cube);
            marker.name = "Stage Marker";
            marker.transform.position = new Vector3(i * 1.4f, 0.15f, 4.2f);
            marker.transform.localScale = new Vector3(0.08f, 0.3f, 0.08f);
            marker.GetComponent<Renderer>().sharedMaterial = markerMaterial;
        }
    }

    private static void CreatePointLight(string name, Vector3 position, Color color, float intensity, float range)
    {
        GameObject lightObject = new GameObject(name);
        lightObject.transform.position = position;
        Light pointLight = lightObject.AddComponent<Light>();
        pointLight.type = LightType.Point;
        pointLight.color = color;
        pointLight.intensity = intensity;
        pointLight.range = range;
    }

    internal static void ApplyZombieMaterial(GameObject model)
    {
        Texture2D zombieTexture = AssetDatabase.LoadAssetAtPath<Texture2D>(ZombieTexturePath);
        Material zombieMaterial = LoadOrCreateZombieMaterial();
        if (zombieTexture != null)
        {
            zombieMaterial.mainTexture = zombieTexture;
            if (zombieMaterial.HasProperty("_BaseMap"))
                zombieMaterial.SetTexture("_BaseMap", zombieTexture);
        }

        EditorUtility.SetDirty(zombieMaterial);
        AssetDatabase.SaveAssets();

        foreach (Renderer renderer in model.GetComponentsInChildren<Renderer>())
        {
            renderer.sharedMaterial = zombieMaterial;
        }
    }

    private static Material LoadOrCreateZombieMaterial()
    {
        EnsureFolder("Assets", "Zombie");
        EnsureFolder(ZombieFolder, "Materials");

        Material material = AssetDatabase.LoadAssetAtPath<Material>(ZombieMaterialPath);
        if (material != null)
            return material;

        material = CreateMaterial("Zombie_Body_Mat", Color.white);
        AssetDatabase.CreateAsset(material, ZombieMaterialPath);
        return material;
    }

    internal static void FitZombieVisual(GameObject model, float targetHeight)
    {
        Renderer[] renderers = model.GetComponentsInChildren<Renderer>();
        if (renderers.Length == 0)
            return;

        Bounds bounds = renderers[0].bounds;
        for (int i = 1; i < renderers.Length; i++)
            bounds.Encapsulate(renderers[i].bounds);

        if (bounds.size.y <= Mathf.Epsilon)
            return;

        Vector3 localCenter = model.transform.InverseTransformPoint(bounds.center);
        Vector3 localBottom = model.transform.InverseTransformPoint(new Vector3(bounds.center.x, bounds.min.y, bounds.center.z));
        float scale = targetHeight / bounds.size.y;

        model.transform.localScale = Vector3.one * scale;
        model.transform.localPosition = new Vector3(
            -localCenter.x * scale,
            -localBottom.y * scale,
            -localCenter.z * scale);
    }

    private static Material CreateMaterial(string name, Color color, bool emission = false)
    {
        Shader shader = Shader.Find("Universal Render Pipeline/Lit");
        if (shader == null) shader = Shader.Find("Standard");

        Material material = new Material(shader) { name = name };
        if (material.HasProperty("_BaseColor")) material.SetColor("_BaseColor", color);
        if (material.HasProperty("_Color")) material.color = color;
        if (emission && material.HasProperty("_EmissionColor"))
        {
            material.EnableKeyword("_EMISSION");
            material.SetColor("_EmissionColor", color * 2f);
        }
        return material;
    }
}
