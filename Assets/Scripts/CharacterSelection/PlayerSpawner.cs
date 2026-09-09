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

    [Header("Minimap")]
    [Tooltip("The LocationMarker sprite used by the Knight setup in Kha_Minimap.")]
    public Sprite playerMarkerSprite;
    public Color playerMarkerColor = new Color(0.25f, 0.9f, 1f, 1f);
    [Min(0.1f)] public float playerMarkerScale = 2.2f;

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

        SetupMinimapForPlayer();

        Debug.Log($"[PlayerSpawner] Spawned {characterToSpawn.characterName} at {spawnPos}");
    }

    /// <summary>
    /// The player is created at runtime, so a scene reference cannot point to
    /// it beforehand.  Bind every minimap rig after spawning and add the same
    /// marker convention used by the Knight in Kha_Minimap.
    /// </summary>
    private void SetupMinimapForPlayer()
    {
        int markerLayer = LayerMask.NameToLayer("LocationMarker");
        if (markerLayer < 0)
        {
            Debug.LogWarning("[PlayerSpawner] The LocationMarker layer is missing; minimap marker was not created.");
            return;
        }

        MinimapFollow minimapFollow = Object.FindFirstObjectByType<MinimapFollow>();
        if (minimapFollow != null)
        {
            minimapFollow.target = spawnedPlayer.transform;

            Camera minimapCamera = minimapFollow.GetComponent<Camera>();
            if (minimapCamera != null)
                minimapCamera.cullingMask |= 1 << markerLayer;
        }
        else
        {
            Debug.LogWarning("[PlayerSpawner] No MinimapFollow rig was found in this scene.");
        }

        // LocationMarker content must exist only in the RenderTexture camera,
        // never as floating UI in the gameplay camera.
        Camera gameplayCamera = cameraFollow != null ? cameraFollow.GetComponent<Camera>() : Camera.main;
        if (gameplayCamera != null)
            gameplayCamera.cullingMask &= ~(1 << markerLayer);

        Transform existingMarker = spawnedPlayer.transform.Find("PlayerMinimapMarker");
        if (existingMarker != null) return;

        GameObject marker = new GameObject("PlayerMinimapMarker");
        marker.layer = markerLayer;
        marker.tag = "LocationMarker";
        marker.transform.SetParent(spawnedPlayer.transform, false);
        marker.transform.localPosition = new Vector3(0f, 0.12f, 0f);
        marker.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
        marker.transform.localScale = Vector3.one * playerMarkerScale;

        // Match the Knight marker in Kha_Minimap: the marker stays north-up
        // instead of inheriting the player's turning animation.
        LockRotation rotationLock = marker.AddComponent<LockRotation>();
        rotationLock.fixedEulerAngles = new Vector3(90f, 0f, 0f);

        SpriteRenderer renderer = marker.AddComponent<SpriteRenderer>();
        renderer.sprite = playerMarkerSprite != null
            ? playerMarkerSprite
            : Sprite.Create(Texture2D.whiteTexture, new Rect(0f, 0f, 1f, 1f), new Vector2(0.5f, 0.5f));
        renderer.color = playerMarkerColor;
        renderer.sortingOrder = 100;
    }

    /// <summary>
    /// Get reference to the spawned player.
    /// </summary>
    public GameObject SpawnedPlayer => spawnedPlayer;
}
