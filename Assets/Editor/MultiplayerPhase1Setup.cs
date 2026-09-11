using System.IO;
using System.Linq;
using Unity.Netcode;
using Unity.Netcode.Components;
using Unity.Netcode.Transports.UTP;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

public static class MultiplayerPhase1Setup
{
    private const string MainMenuPath = "Assets/Scenes/GameScenes/MainMenu.unity";
    private const string MatchStatePath = "Assets/Prefabs/Networking/NetworkMatchState.prefab";

    private static readonly string[] CharacterNames = { "Barbarian", "Knight", "Mage", "Rogue" };

    [MenuItem("Tools/Multiplayer/Apply Phase 1 Setup")]
    public static void Apply()
    {
        Directory.CreateDirectory("Assets/Prefabs/Networking");
        NetworkMatchState matchStatePrefab = EnsureMatchStatePrefab();
        GameObject[] characterPrefabs = EnsureCharacterPrefabs();
        CharacterData[] characterData = EnsureCharacterIds();

        EditorSceneManager.OpenScene(MainMenuPath, OpenSceneMode.Single);
        GameObject bootstrapObject = GameObject.Find("NetworkBootstrap");
        if (bootstrapObject == null) bootstrapObject = new GameObject("NetworkBootstrap");

        UnityTransport transport = GetOrAdd<UnityTransport>(bootstrapObject);
        NetworkManager manager = GetOrAdd<NetworkManager>(bootstrapObject);
        GetOrAdd<OnlineSessionManager>(bootstrapObject);
        NetworkBootstrap bootstrap = GetOrAdd<NetworkBootstrap>(bootstrapObject);

        manager.NetworkConfig.NetworkTransport = transport;
        manager.NetworkConfig.EnableSceneManagement = true;
        manager.NetworkConfig.ConnectionApproval = true;
        manager.NetworkConfig.PlayerPrefab = null;

        SerializedObject bootstrapSerialized = new SerializedObject(bootstrap);
        bootstrapSerialized.FindProperty("matchStatePrefab").objectReferenceValue = matchStatePrefab;
        bootstrapSerialized.FindProperty("gameplayScene").stringValue = "Map_Cloudy";
        SerializedProperty registrations = bootstrapSerialized.FindProperty("characters");
        registrations.arraySize = CharacterNames.Length;
        for (int index = 0; index < CharacterNames.Length; index++)
        {
            SerializedProperty entry = registrations.GetArrayElementAtIndex(index);
            entry.FindPropertyRelative("stableId").stringValue = CharacterNames[index].ToLowerInvariant();
            entry.FindPropertyRelative("character").objectReferenceValue = characterData[index];
            entry.FindPropertyRelative("prefab").objectReferenceValue = characterPrefabs[index];
        }
        bootstrapSerialized.ApplyModifiedPropertiesWithoutUndo();

        MainMenuManager menuManager = Object.FindFirstObjectByType<MainMenuManager>();
        if (menuManager != null)
        {
            SerializedObject menuSerialized = new SerializedObject(menuManager);
            SerializedProperty rounds = menuSerialized.FindProperty("roundSceneNames");
            rounds.arraySize = 3;
            rounds.GetArrayElementAtIndex(0).stringValue = "Map_Cloudy";
            rounds.GetArrayElementAtIndex(1).stringValue = "Map_Day";
            rounds.GetArrayElementAtIndex(2).stringValue = "Map_Night";
            menuSerialized.ApplyModifiedPropertiesWithoutUndo();
        }

        EditorUtility.SetDirty(bootstrapObject);
        EditorSceneManager.MarkSceneDirty(bootstrapObject.scene);
        EditorSceneManager.SaveScene(bootstrapObject.scene);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Selection.activeGameObject = bootstrapObject;
        Debug.Log("[MultiplayerPhase1Setup] Phase 1 scene and prefab wiring complete.");
    }

    [MenuItem("Tools/Multiplayer/Build Phase 1 Verification Client")]
    public static void BuildVerificationClient()
    {
        string outputDirectory = Path.Combine(Path.GetTempPath(), "ZombieGardenPhase1");
        Directory.CreateDirectory(outputDirectory);
        string executable = Path.Combine(outputDirectory, "ZombieGardenPhase1.exe");
        string[] scenes = EditorBuildSettings.scenes.Where(scene => scene.enabled).Select(scene => scene.path).ToArray();
        BuildPlayerOptions options = new BuildPlayerOptions
        {
            scenes = scenes,
            locationPathName = executable,
            target = BuildTarget.StandaloneWindows64,
            options = BuildOptions.Development
        };
        BuildReport report = BuildPipeline.BuildPlayer(options);
        if (report.summary.result != UnityEditor.Build.Reporting.BuildResult.Succeeded)
            throw new System.InvalidOperationException($"Phase 1 verification build failed: {report.summary.result}");
        Debug.Log($"[NET][TEST] Verification client built: {executable}");
    }

    private static NetworkMatchState EnsureMatchStatePrefab()
    {
        GameObject existing = AssetDatabase.LoadAssetAtPath<GameObject>(MatchStatePath);
        if (existing != null)
        {
            if (existing.GetComponent<NetworkObject>() != null && existing.GetComponent<NetworkMatchState>() != null)
                return existing.GetComponent<NetworkMatchState>();
        }

        GameObject temporary = new GameObject("NetworkMatchState");
        temporary.AddComponent<NetworkObject>();
        temporary.AddComponent<NetworkMatchState>();
        GameObject saved = PrefabUtility.SaveAsPrefabAsset(temporary, MatchStatePath);
        Object.DestroyImmediate(temporary);
        return saved.GetComponent<NetworkMatchState>();
    }

    private static GameObject[] EnsureCharacterPrefabs()
    {
        var prefabs = new GameObject[CharacterNames.Length];
        for (int index = 0; index < CharacterNames.Length; index++)
        {
            string path = $"Assets/Prefabs/{CharacterNames[index]}.prefab";
            GameObject contents = PrefabUtility.LoadPrefabContents(path);
            GetOrAdd<NetworkObject>(contents);
            NetworkTransform networkTransform = GetOrAdd<NetworkTransform>(contents);
            networkTransform.AuthorityMode = NetworkTransform.AuthorityModes.Owner;
            OwnerNetworkAnimator networkAnimator = GetOrAdd<OwnerNetworkAnimator>(contents);
            networkAnimator.Animator = contents.GetComponentInChildren<Animator>();
            PrefabUtility.SaveAsPrefabAsset(contents, path);
            PrefabUtility.UnloadPrefabContents(contents);
            prefabs[index] = AssetDatabase.LoadAssetAtPath<GameObject>(path);
        }
        return prefabs;
    }

    private static CharacterData[] EnsureCharacterIds()
    {
        var results = new CharacterData[CharacterNames.Length];
        for (int index = 0; index < CharacterNames.Length; index++)
        {
            string path = $"Assets/Data/Characters/{CharacterNames[index]}_Data.asset";
            CharacterData data = AssetDatabase.LoadAssetAtPath<CharacterData>(path);
            SerializedObject serialized = new SerializedObject(data);
            serialized.FindProperty("stableId").stringValue = CharacterNames[index].ToLowerInvariant();
            serialized.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(data);
            results[index] = data;
        }
        return results;
    }

    private static void EnsureLobbyUi()
    {
        // Kept only for source compatibility with older setup revisions.
        // The artwork-driven lobby is built by MultiplayerLobbyUiSetup.
    }

    private static T GetOrAdd<T>(GameObject target) where T : Component
    {
        T component = target.GetComponent<T>();
        return component != null ? component : target.AddComponent<T>();
    }

    private static GameObject UiObject(string name, Transform parent)
    {
        GameObject result = new GameObject(name, typeof(RectTransform));
        result.transform.SetParent(parent, false);
        return result;
    }

    private static Font DefaultFont() => Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

    private static Text MakeText(Transform parent, string name, string value, int fontSize,
        Vector2 position, Vector2 size, TextAnchor alignment)
    {
        GameObject gameObject = UiObject(name, parent);
        Text text = gameObject.AddComponent<Text>();
        text.font = DefaultFont();
        text.text = value;
        text.fontSize = fontSize;
        text.color = Color.white;
        text.alignment = alignment;
        RectTransform rect = gameObject.GetComponent<RectTransform>();
        rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = position;
        rect.sizeDelta = size;
        return text;
    }

    private static Button MakeButton(Transform parent, string name, string label, Vector2 position, Vector2 size)
    {
        GameObject gameObject = UiObject(name, parent);
        Image image = gameObject.AddComponent<Image>();
        image.color = new Color(0.12f, 0.42f, 0.3f, 1f);
        Button button = gameObject.AddComponent<Button>();
        button.targetGraphic = image;
        RectTransform rect = gameObject.GetComponent<RectTransform>();
        rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = position;
        rect.sizeDelta = size;
        MakeText(gameObject.transform, "Label", label, 17, Vector2.zero, size, TextAnchor.MiddleCenter);
        return button;
    }

    private static InputField MakeInput(Transform parent, string name, string placeholderValue, Vector2 position, Vector2 size)
    {
        GameObject gameObject = UiObject(name, parent);
        Image image = gameObject.AddComponent<Image>();
        image.color = new Color(0.12f, 0.15f, 0.17f, 1f);
        RectTransform rect = gameObject.GetComponent<RectTransform>();
        rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = position;
        rect.sizeDelta = size;

        Text inputText = MakeText(gameObject.transform, "Text", string.Empty, 18, Vector2.zero,
            new Vector2(size.x - 20f, size.y), TextAnchor.MiddleLeft);
        Text placeholder = MakeText(gameObject.transform, "Placeholder", placeholderValue, 15, Vector2.zero,
            new Vector2(size.x - 20f, size.y), TextAnchor.MiddleLeft);
        placeholder.color = new Color(1f, 1f, 1f, 0.45f);

        InputField input = gameObject.AddComponent<InputField>();
        input.targetGraphic = image;
        input.textComponent = inputText;
        input.placeholder = placeholder;
        input.characterLimit = 12;
        input.contentType = InputField.ContentType.Alphanumeric;
        input.shouldHideMobileInput = false;
        return input;
    }
}
