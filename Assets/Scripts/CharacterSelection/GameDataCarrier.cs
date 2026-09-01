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
    /// Check if a character has been selected.
    /// </summary>
    public bool HasSelection => selectedCharacter != null;

    private void OnDestroy()
    {
        if (Instance == this)
            Instance = null;
    }
}
