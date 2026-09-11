using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Single persistent owner of NGO startup, approval, scene loading and Phase 1
/// player spawning. It exists once in MainMenu and survives gameplay scenes.
/// </summary>
[DisallowMultipleComponent]
[RequireComponent(typeof(NetworkManager), typeof(UnityTransport), typeof(OnlineSessionManager))]
public sealed class NetworkBootstrap : MonoBehaviour
{
    [Serializable]
    public struct CharacterRegistration
    {
        public string stableId;
        public CharacterData character;
        public GameObject prefab;
    }

    public static NetworkBootstrap Instance { get; private set; }
    public static NetworkPlayMode PlayMode { get; private set; }
    public static bool IsNetworkSession => Instance != null && Instance.networkManager != null && Instance.networkManager.IsListening;
    public static bool IsOnlineMatch => PlayMode is NetworkPlayMode.OnlineHost or NetworkPlayMode.OnlineGuest;

    [Header("Phase 1 Prefabs")]
    [SerializeField] private NetworkMatchState matchStatePrefab;
    [SerializeField] private CharacterRegistration[] characters;

    [Header("Scene Flow")]
    [SerializeField] private string gameplayScene = "Map_Day";
    [SerializeField, Min(1)] private int gameplayRound = 1;
    [SerializeField] private string mainMenuScene = "MainMenu";

    private NetworkManager networkManager;
    private UnityTransport transport;
    private OnlineSessionManager onlineSession;
    private readonly Dictionary<ulong, string> approvedCharacters = new();
    private bool returningToMenu;
    private bool sceneTransitionInProgress;
    public string LastConnectionStatus { get; private set; } = "Offline";
    public event Action<string> ConnectionStatusChanged;

    public NetworkManager Manager => networkManager;
    public OnlineSessionManager OnlineSession => onlineSession;
    public bool IsHost => networkManager != null && networkManager.IsHost;
    public bool CanHostStart => IsHost &&
        NetworkMatchState.Instance != null &&
        NetworkMatchState.Instance.Phase.Value == MatchPhase.Lobby &&
        (PlayMode != NetworkPlayMode.OnlineHost ||
         (onlineSession != null && onlineSession.PlayerCount >= OnlineSessionManager.MaxPlayers &&
          networkManager.ConnectedClientsIds.Count >= OnlineSessionManager.MaxPlayers));

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStatics()
    {
        Instance = null;
        PlayMode = NetworkPlayMode.None;
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
        Application.runInBackground = true;
        networkManager = GetComponent<NetworkManager>();
        transport = GetComponent<UnityTransport>();
        onlineSession = GetComponent<OnlineSessionManager>();
        if (Debug.isDebugBuild && GetComponent<NetworkDiagnosticsOverlay>() == null)
            gameObject.AddComponent<NetworkDiagnosticsOverlay>();

        networkManager.NetworkConfig.ConnectionApproval = true;
        networkManager.ConnectionApprovalCallback += ApprovalCheck;
        networkManager.OnServerStarted += OnServerStarted;
        networkManager.OnClientConnectedCallback += OnClientConnected;
        networkManager.OnClientDisconnectCallback += OnClientDisconnected;
        onlineSession.SessionEnded += OnOnlineSessionEnded;
    }

    private async void Start()
    {
        if (!Debug.isDebugBuild) return;
        const string joinPrefix = "--phase1-join=";
        const string profilePrefix = "--phase1-profile=";
        string roomCode = null;
        string profile = "phase1guest";
        foreach (string argument in Environment.GetCommandLineArgs())
        {
            if (argument.StartsWith(joinPrefix, StringComparison.OrdinalIgnoreCase))
                roomCode = argument.Substring(joinPrefix.Length);
            else if (argument.StartsWith(profilePrefix, StringComparison.OrdinalIgnoreCase))
                profile = argument.Substring(profilePrefix.Length);
        }

        if (string.IsNullOrWhiteSpace(roomCode)) return;
        onlineSession.SetInitializationProfile(profile);
        Debug.Log($"[NET][BOOT] Development verification client profile={profile} joining room {roomCode}.");
        await JoinOnlineAsync(roomCode);
    }

    private void OnDestroy()
    {
        if (networkManager != null)
        {
            networkManager.ConnectionApprovalCallback -= ApprovalCheck;
            networkManager.OnServerStarted -= OnServerStarted;
            networkManager.OnClientConnectedCallback -= OnClientConnected;
            networkManager.OnClientDisconnectCallback -= OnClientDisconnected;
            if (networkManager.SceneManager != null)
                networkManager.SceneManager.OnSceneEvent -= OnSceneEvent;
            if (networkManager.IsListening)
                networkManager.Shutdown();
        }
        if (onlineSession != null) onlineSession.SessionEnded -= OnOnlineSessionEnded;
        if (Instance == this) Instance = null;
    }

    public string GetSelectedCharacterId()
    {
        CharacterData selected = GameDataCarrier.Instance != null ? GameDataCarrier.Instance.selectedCharacter : null;
        if (selected != null) return selected.StableId;
        if (characters != null && characters.Length > 0) return characters[0].stableId;
        return string.Empty;
    }

    public async Task<bool> HostOnlineAsync()
    {
        if (networkManager == null || networkManager.IsListening) return false;
        PlayMode = NetworkPlayMode.OnlineHost;
        ConfigureConnectionPayload();
        bool created = await onlineSession.CreateSessionAsync();
        if (!created) PlayMode = NetworkPlayMode.None;
        return created;
    }

    public async Task<bool> JoinOnlineAsync(string roomCode)
    {
        if (networkManager == null || networkManager.IsListening) return false;
        PlayMode = NetworkPlayMode.OnlineGuest;
        ConfigureConnectionPayload();
        bool joined = await onlineSession.JoinSessionAsync(roomCode);
        if (!joined) PlayMode = NetworkPlayMode.None;
        return joined;
    }

    public bool StartSolo(string sceneName = null, int round = 1)
    {
        if (networkManager == null || networkManager.IsListening) return false;
        PlayMode = NetworkPlayMode.Solo;
        gameplayScene = string.IsNullOrWhiteSpace(sceneName) ? gameplayScene : sceneName;
        gameplayRound = Mathf.Max(1, round);
        GameDataCarrier.Instance?.SetRound(gameplayRound);
        SetConnectionStatus("Solo — local gameplay");
        Debug.Log($"[NET][BOOT] Starting browser-safe solo scene={gameplayScene} round={gameplayRound}; no socket or Relay session is created.");
        SceneManager.LoadScene(gameplayScene, LoadSceneMode.Single);
        return true;
    }

    public bool StartOnlineMatch()
    {
        if (!CanHostStart || PlayMode != NetworkPlayMode.OnlineHost) return false;
        return ServerLoadGameplayScene(gameplayScene, gameplayRound);
    }

    public bool StartOnlineMatch(string sceneName, int round)
    {
        if (!CanHostStart || PlayMode != NetworkPlayMode.OnlineHost) return false;
        gameplayScene = string.IsNullOrWhiteSpace(sceneName) ? gameplayScene : sceneName;
        gameplayRound = Mathf.Max(1, round);
        return ServerLoadGameplayScene(gameplayScene, gameplayRound);
    }

    public bool ServerLoadGameplayScene(string sceneName)
    {
        return ServerLoadGameplayScene(sceneName, gameplayRound);
    }

    public bool ServerLoadGameplayScene(string sceneName, int round)
    {
        if (!networkManager.IsServer || NetworkMatchState.Instance == null || sceneTransitionInProgress) return false;
        sceneTransitionInProgress = true;
        gameplayScene = sceneName;
        gameplayRound = Mathf.Max(1, round);
        NetworkMatchState.Instance.ServerSetLoading(sceneName, gameplayRound);
        Debug.Log($"[NET][SCENE] Host starting synchronized load scene={sceneName} clients={networkManager.ConnectedClientsIds.Count}.");
        var status = networkManager.SceneManager.LoadScene(sceneName, LoadSceneMode.Single);
        if (status == SceneEventProgressStatus.Started) return true;

        sceneTransitionInProgress = false;
        Debug.LogError($"[NET][SCENE] NGO could not load {sceneName}: {status}");
        return false;
    }

    public bool ServerRestartCurrentScene()
    {
        if (!IsHost || NetworkMatchState.Instance == null) return false;
        return ServerLoadGameplayScene(SceneManager.GetActiveScene().name, NetworkMatchState.Instance.CurrentRound.Value);
    }

    public bool ServerLoadNextRound()
    {
        if (!IsHost || NetworkMatchState.Instance == null) return false;
        int nextRound = NetworkMatchState.Instance.CurrentRound.Value + 1;
        string[] scenes = { "Map_Day", "Map_Cloudy", "Map_Night" };
        if (nextRound < 1 || nextRound > scenes.Length)
        {
            _ = LeaveToMenuAsync("All rounds complete.");
            return true;
        }
        if (GameDataCarrier.Instance != null) GameDataCarrier.Instance.SetRound(nextRound);
        return ServerLoadGameplayScene(scenes[nextRound - 1], nextRound);
    }

    public async Task LeaveToMenuAsync(string reason = null)
    {
        if (returningToMenu) return;
        returningToMenu = true;
        if (!string.IsNullOrEmpty(reason)) Debug.LogWarning($"[NET][BOOT] {reason}");
        SetConnectionStatus(string.IsNullOrWhiteSpace(reason) ? "Returning to menu" : reason);

        if (onlineSession != null) await onlineSession.LeaveAsync();
        if (networkManager != null && networkManager.IsListening) networkManager.Shutdown();
        approvedCharacters.Clear();
        sceneTransitionInProgress = false;
        PlayMode = NetworkPlayMode.None;
        Time.timeScale = 1f;

        if (SceneManager.GetActiveScene().name != mainMenuScene)
            SceneManager.LoadScene(mainMenuScene, LoadSceneMode.Single);
        Debug.Log("[NET][BOOT] Network/session shutdown complete; returned to menu state.");
        returningToMenu = false;
    }

    private void SetConnectionStatus(string status)
    {
        LastConnectionStatus = status;
        ConnectionStatusChanged?.Invoke(status);
    }

    private void ConfigureConnectionPayload()
    {
        networkManager.NetworkConfig.ConnectionData = Encoding.UTF8.GetBytes(GetSelectedCharacterId());
    }

    private void ApprovalCheck(NetworkManager.ConnectionApprovalRequest request, NetworkManager.ConnectionApprovalResponse response)
    {
        string characterId = Encoding.UTF8.GetString(request.Payload ?? Array.Empty<byte>());
        bool knownCharacter = FindCharacter(characterId).prefab != null;
        bool roomAvailable = approvedCharacters.Count < OnlineSessionManager.MaxPlayers;
        bool lobbyOpen = NetworkMatchState.Instance == null || NetworkMatchState.Instance.Phase.Value == MatchPhase.Lobby;

        response.Approved = knownCharacter && roomAvailable && lobbyOpen;
        response.CreatePlayerObject = false;
        response.Pending = false;
        response.Reason = !knownCharacter ? "Unknown character selection."
            : !roomAvailable ? "Room is full (2/2)."
            : !lobbyOpen ? "The match has already started."
            : string.Empty;

        if (response.Approved) approvedCharacters[request.ClientNetworkId] = characterId;
        Debug.Log($"[NET][BOOT] Approval client={request.ClientNetworkId} character={characterId} approved={response.Approved} reason={response.Reason} roster={approvedCharacters.Count}/{OnlineSessionManager.MaxPlayers}.");
    }

    private void OnServerStarted()
    {
        networkManager.SceneManager.OnSceneEvent -= OnSceneEvent;
        networkManager.SceneManager.OnSceneEvent += OnSceneEvent;

        if (matchStatePrefab == null)
        {
            Debug.LogError("[NET][BOOT] Match state prefab is not configured.");
            return;
        }

        NetworkMatchState state = Instantiate(matchStatePrefab);
        state.GetComponent<NetworkObject>().Spawn(false);
        Debug.Log($"[NET][BOOT] Server started mode={PlayMode} localClient={networkManager.LocalClientId}.");
        SetConnectionStatus(PlayMode == NetworkPlayMode.Solo ? "Solo host running" : "Host connected");
        if (PlayMode == NetworkPlayMode.Solo)
            ServerLoadGameplayScene(gameplayScene, gameplayRound);
    }

    private void OnSceneEvent(SceneEvent sceneEvent)
    {
        Debug.Log($"[NET][SCENE] event={sceneEvent.SceneEventType} scene={sceneEvent.SceneName} client={sceneEvent.ClientId} local={networkManager.LocalClientId}.");
        if (!networkManager.IsServer || sceneEvent.SceneEventType != SceneEventType.LoadEventCompleted) return;
        if (!string.Equals(sceneEvent.SceneName, gameplayScene, StringComparison.Ordinal)) return;

        PlayerSpawner spawner = FindFirstObjectByType<PlayerSpawner>();
        if (spawner == null)
        {
            Debug.LogError("[NET][SCENE] Loaded gameplay scene has no PlayerSpawner.");
            return;
        }

        int spawnIndex = 0;
        foreach (ulong clientId in networkManager.ConnectedClientsIds)
        {
            string characterId = approvedCharacters.TryGetValue(clientId, out string selected)
                ? selected
                : GetSelectedCharacterId();
            CharacterRegistration registration = FindCharacter(characterId);
            spawner.SpawnNetworkPlayer(registration.character, registration.prefab, clientId, spawnIndex++);
        }
        NetworkMatchState.Instance.ServerSetIntro();
        sceneTransitionInProgress = false;
        Debug.Log($"[NET][MATCH] All clients loaded {gameplayScene}; spawned={spawnIndex}; phase=Intro.");
    }

    private void OnClientConnected(ulong clientId)
    {
        SetConnectionStatus(networkManager.IsHost ? "Host connected" : "Connected to host");
        Debug.Log($"[NET][BOOT] Client connected id={clientId} local={networkManager.LocalClientId} connected={networkManager.ConnectedClientsIds.Count}/{OnlineSessionManager.MaxPlayers}.");
    }

    private void OnClientDisconnected(ulong clientId)
    {
        approvedCharacters.Remove(clientId);
        Debug.LogWarning($"[NET][BOOT] Client disconnected id={clientId} local={networkManager.LocalClientId} remaining={(networkManager != null ? networkManager.ConnectedClientsIds.Count : 0)}.");
        if (returningToMenu || PlayMode == NetworkPlayMode.None) return;

        if (networkManager != null && clientId == networkManager.LocalClientId)
        {
            bool localWasHost = PlayMode == NetworkPlayMode.OnlineHost;
            SetConnectionStatus(localWasHost ? "Host network stopped" : "Host disconnected");
            _ = LeaveToMenuAsync(localWasHost
                ? "The online host stopped."
                : "Disconnected from the host.");
        }
        else if (networkManager != null && networkManager.IsServer)
        {
            SetConnectionStatus("Guest disconnected; host continues solo");
            Debug.LogWarning("[NET][CONN] Guest disconnected; authoritative host match continues without rescaling.");
        }
    }

    private void OnOnlineSessionEnded()
    {
        SetConnectionStatus("Session ended by host");
        if (!returningToMenu) _ = LeaveToMenuAsync("The host ended the online session.");
    }

    private CharacterRegistration FindCharacter(string stableId)
    {
        if (characters != null)
        {
            foreach (CharacterRegistration entry in characters)
            {
                string entryId = string.IsNullOrWhiteSpace(entry.stableId) && entry.character != null
                    ? entry.character.StableId
                    : entry.stableId;
                if (string.Equals(entryId, stableId, StringComparison.OrdinalIgnoreCase)) return entry;
            }
        }
        return default;
    }
}
