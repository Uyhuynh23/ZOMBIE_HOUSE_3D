using UnityEngine;

/// <summary>
/// Singleton that persists between scene loads to carry the player's
/// character selection and equipment choices from MainMenu to gameplay.
/// </summary>
public class GameDataCarrier : MonoBehaviour
{
    public static GameDataCarrier Instance { get; private set; }

    [Header("Selection (set by MainMenu)")]
    public CharacterData selectedCharacter;
    public EquipmentData equippedRightHand;
    public EquipmentData equippedLeftHand;

    [Header("Round Tracking")]
    public int currentRound = 1; // 1-3
    public string[] roundSceneNames = new string[]
    {
        "Map_Day",
        "Map_Cloudy",
        "Map_Night"
    };
    public const string MainMenuSceneName = "MainMenu";

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    /// <summary>
    /// Set the selected character and apply default equipment.
    /// </summary>
    public void SelectCharacter(CharacterData character)
    {
        selectedCharacter = character;
        if (character != null)
        {
            equippedRightHand = character.defaultRightHand;
            equippedLeftHand = character.defaultLeftHand;
        }
    }

    /// <summary>
    /// Set the current round (1-based index).
    /// </summary>
    public void SetRound(int round)
    {
        currentRound = Mathf.Clamp(round, 1, roundSceneNames.Length);
    }

    public string GetCurrentRoundScene()
    {
        int index = currentRound - 1;
        if (index >= 0 && index < roundSceneNames.Length)
            return roundSceneNames[index];
        return null;
    }

    public bool HasNextRound => currentRound < roundSceneNames.Length;

    public string GetNextRoundScene()
    {
        if (HasNextRound)
            return roundSceneNames[currentRound]; // Since currentRound is 1-based, index `currentRound` is the next round.
        return null;
    }

    /// <summary>
    /// Check if a character has been selected.
    /// </summary>
    public bool HasSelection => selectedCharacter != null;

    private void OnDestroy()
    {
        if (Instance == this)
            Instance = null;
    }
}
