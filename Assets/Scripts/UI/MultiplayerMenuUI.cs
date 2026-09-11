using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class MultiplayerMenuUI : MonoBehaviour
{
    public Button[] roundButtons;
    public GameObject[] selectionHighlights;
    public string[] roundScenes;

    public Button soloButton;
    public Button hostButton;
    public TMP_InputField roomCodeInput;
    public Button joinButton;
    public TMP_Text roomCodeText;
    public TMP_Text rosterText;
    public Button startButton;
    public Button leaveButton;
    public TMP_Text statusText;

    public Button characterButton;
    public Button backButton;
    public MainMenuManager mainMenuManager;

    private int selectedRoundIndex = 0;

    private void Awake()
    {
        if (mainMenuManager == null)
            mainMenuManager = Object.FindFirstObjectByType<MainMenuManager>();
    }

    private void Start()
    {
        SetupListeners();
        SelectRound(0);
    }

    private void SetupListeners()
    {
        if (roundButtons != null)
        {
            for (int i = 0; i < roundButtons.Length; i++)
            {
                int index = i;
                if (roundButtons[i] != null)
                {
                    roundButtons[i].onClick.RemoveAllListeners();
                    roundButtons[i].onClick.AddListener(() => SelectRound(index));
                }
            }
        }

        if (soloButton != null)
        {
            soloButton.onClick.RemoveAllListeners();
            soloButton.onClick.AddListener(OnSoloClicked);
        }

        if (hostButton != null)
        {
            hostButton.onClick.RemoveAllListeners();
            hostButton.onClick.AddListener(OnHostClicked);
        }

        if (joinButton != null)
        {
            joinButton.onClick.RemoveAllListeners();
            joinButton.onClick.AddListener(OnJoinClicked);
        }

        if (startButton != null)
        {
            startButton.onClick.RemoveAllListeners();
            startButton.onClick.AddListener(OnStartClicked);
        }

        if (leaveButton != null)
        {
            leaveButton.onClick.RemoveAllListeners();
            leaveButton.onClick.AddListener(OnLeaveClicked);
        }

        if (characterButton != null)
        {
            characterButton.onClick.RemoveAllListeners();
            characterButton.onClick.AddListener(OnCharacterClicked);
        }

        if (backButton != null)
        {
            backButton.onClick.RemoveAllListeners();
            backButton.onClick.AddListener(OnBackClicked);
        }
    }

    public void SelectRound(int index)
    {
        selectedRoundIndex = index;
        if (selectionHighlights != null)
        {
            for (int i = 0; i < selectionHighlights.Length; i++)
            {
                if (selectionHighlights[i] != null)
                    selectionHighlights[i].SetActive(i == index);
            }
        }
    }

    private void OnSoloClicked()
    {
        if (mainMenuManager != null)
        {
            mainMenuManager.OnRoundSelected(selectedRoundIndex + 1);
        }
    }

    private void OnHostClicked()
    {
        if (statusText != null)
            statusText.text = "Multiplayer hosting is coming soon!";
    }

    private void OnJoinClicked()
    {
        if (statusText != null)
            statusText.text = "Multiplayer matchmaking is coming soon!";
    }

    private void OnStartClicked()
    {
        OnSoloClicked();
    }

    private void OnLeaveClicked()
    {
        if (statusText != null)
            statusText.text = "Choose solo, host, or join a room.";
        if (roomCodeText != null)
            roomCodeText.text = "—";
        if (rosterText != null)
            rosterText.text = "offline";
    }

    private void OnCharacterClicked()
    {
        if (mainMenuManager != null)
            mainMenuManager.ShowCharacterSetting();
    }

    private void OnBackClicked()
    {
        if (mainMenuManager != null)
            mainMenuManager.ShowMainMenu();
    }
}
