using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// Persistent, scene-independent audio service. It deliberately works even
/// before clips are added, so teammates can integrate code without blockers.
/// Add clips using the paths documented in Assets/Resources/Audio/README.md.
/// </summary>
public sealed class AudioManager : MonoBehaviour
{
    private const string MainMenuSceneName = "MainMenu";
    private const string MenuMusicPath = "Audio/Music/MainMenuMusic";
    private const string GameplayMusicPath = "Audio/Music/GameplayMusic";

    public static AudioManager Instance { get; private set; }

    [Header("Volume")]
    [Range(0f, 1f)] [SerializeField] private float musicVolume = 0.45f;
    [Range(0f, 1f)] [SerializeField] private float sfxVolume = 0.8f;
    [SerializeField] private int simultaneousSfxSources = 4;

    private AudioSource musicSource;
    private AudioSource[] sfxSources;
    private int nextSfxSource;
    private AudioListener fallbackListener;
    private readonly Dictionary<AudioCue, AudioClip> sfxClips = new();
    private readonly HashSet<string> missingClipWarnings = new();

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void CreateIfNeeded()
    {
        if (Instance != null) return;

        GameObject audioRoot = new("AudioManager");
        audioRoot.AddComponent<AudioManager>();
    }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);

        musicSource = gameObject.AddComponent<AudioSource>();
        musicSource.loop = true;
        musicSource.playOnAwake = false;
        musicSource.volume = musicVolume;

        int sourceCount = Mathf.Max(1, simultaneousSfxSources);
        sfxSources = new AudioSource[sourceCount];
        for (int i = 0; i < sourceCount; i++)
        {
            sfxSources[i] = gameObject.AddComponent<AudioSource>();
            sfxSources[i].playOnAwake = false;
            sfxSources[i].volume = sfxVolume;
        }

        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private void Start()
    {
        // sceneLoaded can occur before Start for the initially-opened scene.
        RefreshForScene(SceneManager.GetActiveScene());
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        RefreshForScene(scene);
    }

    private void RefreshForScene(Scene scene)
    {
        EnsureAudioListener();
        PlayMusicForScene(scene.name);
        AddClickSoundsToButtons();
    }

    // Some isolated test scenes do not contain a camera. AudioSources still run
    // there, but Unity cannot output sound without a single AudioListener.
    private void EnsureAudioListener()
    {
        AudioListener[] listeners = FindObjectsByType<AudioListener>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);
        bool externalListenerExists = false;

        foreach (AudioListener listener in listeners)
        {
            if (listener != fallbackListener)
            {
                externalListenerExists = true;
                break;
            }
        }

        if (externalListenerExists)
        {
            if (fallbackListener != null)
            {
                Destroy(fallbackListener);
                fallbackListener = null;
            }
            return;
        }

        if (fallbackListener == null)
            fallbackListener = gameObject.AddComponent<AudioListener>();
    }

    private void PlayMusicForScene(string sceneName)
    {
        string musicPath = sceneName == MainMenuSceneName ? MenuMusicPath : GameplayMusicPath;
        AudioClip requestedClip = Resources.Load<AudioClip>(musicPath);

        if (requestedClip == null)
        {
            WarnOnce(musicPath);
            if (musicSource.isPlaying) musicSource.Stop();
            return;
        }

        if (musicSource.clip == requestedClip && musicSource.isPlaying) return;

        musicSource.clip = requestedClip;
        musicSource.volume = musicVolume;
        musicSource.Play();
    }

    private void AddClickSoundsToButtons()
    {
        Button[] buttons = FindObjectsByType<Button>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        foreach (Button button in buttons)
        {
            if (button.GetComponent<ButtonClickSound>() == null)
                button.gameObject.AddComponent<ButtonClickSound>();
        }
    }

    public static void PlaySfx(AudioCue cue, float volumeScale = 1f)
    {
        if (Instance == null) return;
        Instance.PlaySfxInternal(cue, volumeScale);
    }

    private void PlaySfxInternal(AudioCue cue, float volumeScale)
    {
        if (!sfxClips.TryGetValue(cue, out AudioClip clip) || clip == null)
        {
            clip = Resources.Load<AudioClip>($"Audio/SFX/{cue}");
            sfxClips[cue] = clip;
        }

        if (clip == null)
        {
            WarnOnce($"Audio/SFX/{cue}");
            return;
        }

        AudioSource source = sfxSources[nextSfxSource];
        nextSfxSource = (nextSfxSource + 1) % sfxSources.Length;
        source.PlayOneShot(clip, Mathf.Clamp01(volumeScale) * sfxVolume);
    }

    private void WarnOnce(string resourcePath)
    {
        if (missingClipWarnings.Add(resourcePath))
            Debug.Log($"[AudioManager] Optional audio clip not found at Resources/{resourcePath}.");
    }

    private void OnDestroy()
    {
        if (Instance == this) Instance = null;
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }
}

[RequireComponent(typeof(Button))]
public sealed class ButtonClickSound : MonoBehaviour
{
    private Button button;

    private void Awake()
    {
        button = GetComponent<Button>();
        button.onClick.AddListener(PlayClick);
    }

    private void PlayClick()
    {
        AudioManager.PlaySfx(AudioCue.UiClick);
    }

    private void OnDestroy()
    {
        if (button != null) button.onClick.RemoveListener(PlayClick);
    }
}
