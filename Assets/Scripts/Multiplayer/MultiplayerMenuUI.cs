using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Runtime bindings for the artwork-driven lobby. Visible buttons are baked into
/// immutable sprites; transparent hitboxes and live TMP fields provide behavior.
/// </summary>
public sealed class MultiplayerMenuUI : MonoBehaviour
{
    [Header("Map selection")]
    public Button[] roundButtons;
    public GameObject[] selectionHighlights;
    public string[] roundScenes = { "Map_Day", "Map_Cloudy", "Map_Night" };

    [Header("Multiplayer artwork hitboxes")]
    public Button soloButton;
    public Button hostButton;
    public TMP_InputField roomCodeInput;
    public Button joinButton;
    public TMP_Text roomCodeText;
    public TMP_Text rosterText;
    public Button startButton;
    public Button leaveButton;
    public TMP_Text statusText;

    [Header("Navigation")]
    public Button characterButton;
    public Button backButton;
    public MainMenuManager mainMenuManager;

    public int SelectedRound { get; private set; } = 1;
    public string SelectedScene => roundScenes != null && SelectedRound <= roundScenes.Length
        ? roundScenes[SelectedRound - 1]
        : "Map_Day";

    private NetworkBootstrap bootstrap;
    private OnlineSessionManager session;

    private void Start()
    {
        bootstrap = NetworkBootstrap.Instance;
        if (bootstrap == null)
        {
            SetStatus("Networking bootstrap is missing.");
            return;
        }

        session = bootstrap.OnlineSession;
        if (roundButtons != null)
        {
            for (int i = 0; i < roundButtons.Length; i++)
            {
                int round = i + 1;
                roundButtons[i]?.onClick.AddListener(() => SelectRound(round));
            }
        }

        soloButton?.onClick.AddListener(OnSoloClicked);
        hostButton?.onClick.AddListener(OnHostClicked);
        joinButton?.onClick.AddListener(OnJoinClicked);
        startButton?.onClick.AddListener(OnStartClicked);
        leaveButton?.onClick.AddListener(OnLeaveClicked);
        characterButton?.onClick.AddListener(() => mainMenuManager?.ShowCharacterSetting());
        backButton?.onClick.AddListener(() => mainMenuManager?.ShowMainMenu());
        session.SessionChanged += Refresh;
        session.OperationFailed += SetStatus;
        bootstrap.ConnectionStatusChanged += SetStatus;
        SelectRound(1);
        Refresh();
        if (!string.IsNullOrWhiteSpace(bootstrap.LastConnectionStatus) && bootstrap.LastConnectionStatus != "Offline")
            SetStatus(bootstrap.LastConnectionStatus);
    }

    private void OnDestroy()
    {
        if (session == null) return;
        session.SessionChanged -= Refresh;
        session.OperationFailed -= SetStatus;
        if (bootstrap != null) bootstrap.ConnectionStatusChanged -= SetStatus;
    }

    private void Update()
    {
        if (bootstrap == null) return;
        bool listening = NetworkBootstrap.IsNetworkSession;
        bool busy = session != null && session.IsBusy;

        if (soloButton != null) soloButton.interactable = !listening && !busy;
        if (hostButton != null) hostButton.interactable = !listening && !busy;
        if (joinButton != null)
            joinButton.interactable = !listening && !busy && roomCodeInput != null &&
                !string.IsNullOrWhiteSpace(roomCodeInput.text);
        if (startButton != null)
        {
            startButton.gameObject.SetActive(true);
            startButton.interactable = session != null && session.CurrentSession != null &&
                session.IsHost && bootstrap.CanHostStart;
        }
        if (leaveButton != null)
        {
            leaveButton.gameObject.SetActive(true);
            leaveButton.interactable = listening || session?.CurrentSession != null;
        }
    }

    public void SelectRound(int round)
    {
        if (roundScenes == null || round < 1 || round > roundScenes.Length) return;
        SelectedRound = round;
        GameDataCarrier.Instance?.SetRound(round);
        if (selectionHighlights != null)
        {
            for (int i = 0; i < selectionHighlights.Length; i++)
            {
                bool selected = i == round - 1;
                if (selectionHighlights[i] != null) selectionHighlights[i].SetActive(selected);
                if (roundButtons != null && i < roundButtons.Length && roundButtons[i] != null)
                    roundButtons[i].transform.parent.localScale = Vector3.one * (selected ? 1.04f : 0.94f);
            }
        }
        SetStatus($"Round {round} selected: {SelectedScene}.");
    }

    private void EnsureDefaultCharacter()
    {
        if (GameDataCarrier.Instance != null && !GameDataCarrier.Instance.HasSelection &&
            mainMenuManager != null && mainMenuManager.availableCharacters != null &&
            mainMenuManager.availableCharacters.Length > 0)
            GameDataCarrier.Instance.SelectCharacter(mainMenuManager.availableCharacters[0]);
    }

    private void OnSoloClicked()
    {
        EnsureDefaultCharacter();
        SetStatus($"Starting Round {SelectedRound} solo...");
        if (!bootstrap.StartSolo(SelectedScene, SelectedRound)) SetStatus("Could not start solo mode.");
    }

    private async void OnHostClicked()
    {
        EnsureDefaultCharacter();
        SetStatus("Creating a private Relay room...");
        if (await bootstrap.HostOnlineAsync())
            SetStatus("Room ready. Share the code, then start when your teammate joins.");
    }

    private async void OnJoinClicked()
    {
        EnsureDefaultCharacter();
        SetStatus("Joining room...");
        if (await bootstrap.JoinOnlineAsync(roomCodeInput != null ? roomCodeInput.text : string.Empty))
            SetStatus("Joined. Waiting for the host to start.");
    }

    private void OnStartClicked()
    {
        if (!bootstrap.StartOnlineMatch(SelectedScene, SelectedRound))
            SetStatus("Only the host can start from an open lobby.");
        else
            SetStatus($"Loading {SelectedScene} for all players...");
    }

    private async void OnLeaveClicked()
    {
        SetStatus("Leaving session...");
        await bootstrap.LeaveToMenuAsync();
        Refresh();
    }

    private void Refresh()
    {
        if (session == null) return;
        if (roomCodeText != null)
            roomCodeText.text = string.IsNullOrEmpty(session.RoomCode) ? "—" : session.RoomCode;
        if (rosterText != null)
            rosterText.text = session.CurrentSession == null
                ? "—"
                : $"{session.PlayerCount} / {OnlineSessionManager.MaxPlayers}";
        if (!string.IsNullOrEmpty(session.LastError)) SetStatus(session.LastError);
    }

    private void SetStatus(string message)
    {
        if (statusText != null) statusText.text = message;
    }
}
