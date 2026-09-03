using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Top-level controller for the Main Menu scene.
/// - Round buttons are handled by RoundButtonHandler (attached to each button)
/// - Character button is handled by CharacterButtonHandler
/// - Back button is wired in ShowCharacterSetting()
/// </summary>
public class MainMenuManager : MonoBehaviour
{
    [Header("Panels")]
    public GameObject mainMenuPanel;
    public GameObject characterSettingPanel;
    [Tooltip("Title text shown only on the main menu screen")]
    public GameObject gameTitle;

    [Header("Round Scene Mapping")]
    [Tooltip("Scene names for each round. Index 0 = Round 1, etc.")]
    public string[] roundSceneNames = new string[]
    {
        "Map_Day",
        "Map_Cloudy",
        "Map_Night"
    };

    [Header("Data")]
    public CharacterData[] availableCharacters;
    public EquipmentData[] allEquipment;

    [Header("Preview")]
    public Transform characterPreviewSpot;
    public Camera previewCamera;

    private CharacterSettingUI characterSettingUI;

    void Start()
    {
        // Ensure GameDataCarrier exists
        if (GameDataCarrier.Instance == null)
        {
            GameObject carrier = new GameObject("GameDataCarrier");
            carrier.AddComponent<GameDataCarrier>();
        }

        characterSettingUI = characterSettingPanel.GetComponent<CharacterSettingUI>();

        // Buttons are wired by RoundButtonHandler / CharacterButtonHandler on each button GO.
        ShowMainMenu();
    }

    // ── Panel visibility ──────────────────────────────────────────────────────

    /// <summary>Show the main menu with round buttons.</summary>
    public void ShowMainMenu()
    {
        mainMenuPanel.SetActive(true);
        characterSettingPanel.SetActive(false);
        if (gameTitle != null) gameTitle.SetActive(true);
    }

    /// <summary>Show the character setting panel.</summary>
    public void ShowCharacterSetting()
    {
        mainMenuPanel.SetActive(false);
        characterSettingPanel.SetActive(true);
        if (gameTitle != null) gameTitle.SetActive(false);

        if (characterSettingUI != null)
        {
            characterSettingUI.Initialize(availableCharacters, allEquipment, characterPreviewSpot);

            if (characterSettingUI.backButton != null)
            {
                characterSettingUI.backButton.onClick.RemoveAllListeners();
                characterSettingUI.backButton.onClick.AddListener(OnBackClicked);
            }
        }
    }

    // ── Round loading ─────────────────────────────────────────────────────────

    /// <summary>
    /// Called by RoundButtonHandler on each round button.
    /// roundNumber: 1 = Map_Day, 2 = Map_Cloudy, 3 = Map_Night
    /// </summary>
    public void OnRoundSelected(int roundNumber)
    {
        // Default to first character if none selected yet
        if (GameDataCarrier.Instance != null && !GameDataCarrier.Instance.HasSelection)
        {
            if (availableCharacters != null && availableCharacters.Length > 0)
                GameDataCarrier.Instance.SelectCharacter(availableCharacters[0]);
        }

        int index = roundNumber - 1;
        if (index >= 0 && index < roundSceneNames.Length)
        {
            GameDataCarrier.Instance?.SetRound(roundNumber);
            Debug.Log($"[MainMenuManager] Loading scene: {roundSceneNames[index]}");
            SceneManager.LoadScene(roundSceneNames[index]);
        }
        else
        {
            Debug.LogWarning($"[MainMenuManager] Invalid round number: {roundNumber}");
        }
    }

    // ── Back button (wired at runtime in ShowCharacterSetting) ────────────────

    private void OnBackClicked()
    {
        ShowMainMenu();
    }
}
