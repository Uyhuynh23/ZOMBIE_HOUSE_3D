using UnityEngine;

/// <summary>
/// Placed in each gameplay scene. Spawns the player's selected character
/// from GameDataCarrier and applies saved equipment.
/// Falls back to a default character if no selection was made.
/// </summary>
public class PlayerSpawner : MonoBehaviour
{
    [Header("Spawn Settings")]
    public Transform spawnPoint;

    [Header("Fallback (if no selection made)")]
    public CharacterData defaultCharacter;

    [Header("Plant Loadout")]
    public PlantData[] plantLoadout;

    [Header("Camera")]
    public CameraFollow cameraFollow;

    private GameObject spawnedPlayer;

    void Start()
    {
        SpawnPlayer();
    }

    void SpawnPlayer()
    {
        CharacterData characterToSpawn = null;
        EquipmentData rightHand = null;
        EquipmentData leftHand = null;

        // Get selection from GameDataCarrier
        if (GameDataCarrier.Instance != null && GameDataCarrier.Instance.HasSelection)
        {
            characterToSpawn = GameDataCarrier.Instance.selectedCharacter;
            rightHand = GameDataCarrier.Instance.equippedRightHand;
            leftHand = GameDataCarrier.Instance.equippedLeftHand;
        }

        // Fallback to default
        if (characterToSpawn == null)
            characterToSpawn = defaultCharacter;

        if (characterToSpawn == null || characterToSpawn.characterPrefab == null)
        {
            Debug.LogError("[PlayerSpawner] No character to spawn! Assign a default character.");
            return;
        }

        // Determine spawn position and rotation
        Vector3 spawnPos = spawnPoint != null ? spawnPoint.position : Vector3.zero;
        Quaternion spawnRot = spawnPoint != null ? spawnPoint.rotation : Quaternion.identity;

        // Instantiate the character
        spawnedPlayer = Instantiate(characterToSpawn.characterPrefab, spawnPos, spawnRot);
        spawnedPlayer.name = characterToSpawn.characterName;

        // Setup Plant Loadout
        PlayerController pc = spawnedPlayer.GetComponent<PlayerController>();
        if (pc != null && plantLoadout != null)
        {
            pc.plants = plantLoadout;
            if (plantLoadout.Length > 0) pc.SelectPlant(0);
        }

        // Apply equipment
        EquipmentManager equipManager = spawnedPlayer.GetComponent<EquipmentManager>();
        if (equipManager == null)
            equipManager = spawnedPlayer.AddComponent<EquipmentManager>();

        // Clear FBX built-in weapons/shields first
        equipManager.ClearBuiltInEquipment();

        if (rightHand != null)
            equipManager.EquipRight(rightHand);
        if (leftHand != null)
            equipManager.EquipLeft(leftHand);

        // Hook up camera
        if (cameraFollow != null)
        {
            cameraFollow.target = spawnedPlayer.transform;
        }
        else
        {
            // Try to find CameraFollow in scene
            CameraFollow cam = Object.FindFirstObjectByType<CameraFollow>();
            if (cam != null)
            {
                cam.target = spawnedPlayer.transform;
            }
        }

        Debug.Log($"[PlayerSpawner] Spawned {characterToSpawn.characterName} at {spawnPos}");
    }

    /// <summary>
    /// Get reference to the spawned player.
    /// </summary>
    public GameObject SpawnedPlayer => spawnedPlayer;
}
