using UnityEngine;
using Unity.Netcode;

/// <summary>
/// Placed in each gameplay scene. Spawns the player's selected character
/// from GameDataCarrier and applies saved equipment.
/// Falls back to a default character if no selection was made.
/// </summary>
public class PlayerSpawner : MonoBehaviour
{
    public static PlayerSpawner Instance { get; private set; }
    [Header("Spawn Settings")]
    public Transform spawnPoint;
    [Tooltip("Default spawn rotation in degrees if spawnPoint is unassigned. Set Y to 180 to face away from the house.")]
    public Vector3 defaultSpawnRotation = new Vector3(0f, 180f, 0f);

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

    private void Awake()
    {
        Instance = this;
    }

    private void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }

    void Start()
    {
        // NGO's server spawns one owned player after every client completes the
        // network scene load. The original local path remains the fallback.
        if (!NetworkBootstrap.IsNetworkSession)
            SpawnPlayer();
    }

    public void SpawnNetworkPlayer(CharacterData character, GameObject prefab, ulong clientId, int spawnIndex)
    {
        NetworkManager manager = NetworkManager.Singleton;
        if (manager == null || !manager.IsServer || prefab == null) return;
        if (manager.ConnectedClients.TryGetValue(clientId, out NetworkClient client) && client.PlayerObject != null)
            return;

        Vector3 basePosition = spawnPoint != null
            ? spawnPoint.position
            : (transform.position != Vector3.zero ? transform.position : new Vector3(0f, 0.1f, -18f));
        Vector3 offset = spawnIndex == 0 ? Vector3.left * 0.9f : Vector3.right * 0.9f;
        Quaternion rotation = spawnPoint != null ? spawnPoint.rotation : Quaternion.Euler(defaultSpawnRotation);

        GameObject player = Instantiate(prefab, basePosition + offset, rotation);
        player.name = character != null ? character.characterName : $"Player {clientId}";
        ConfigurePlayer(player, character, useCarrierEquipment: clientId == manager.LocalClientId);

        NetworkObject networkObject = player.GetComponent<NetworkObject>();
        if (networkObject == null)
        {
            Debug.LogError($"[NET][PLAYER] {prefab.name} is missing NetworkObject.");
            Destroy(player);
            return;
        }
        networkObject.SpawnAsPlayerObject(clientId, true);
        Debug.Log($"[NET][PLAYER] Spawned player={player.name} owner={clientId} networkObject={networkObject.NetworkObjectId} position={player.transform.position}.");
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
        Vector3 spawnPos = spawnPoint != null ? spawnPoint.position : (transform.position != Vector3.zero ? transform.position : new Vector3(0f, 0.1f, -18f));
        Quaternion spawnRot = spawnPoint != null ? spawnPoint.rotation : Quaternion.Euler(defaultSpawnRotation);

        // Instantiate the character
        spawnedPlayer = Instantiate(characterToSpawn.characterPrefab, spawnPos, spawnRot);
        spawnedPlayer.name = characterToSpawn.characterName;

        ConfigurePlayer(spawnedPlayer, characterToSpawn, true, rightHand, leftHand);
        BindLocalPlayer(spawnedPlayer);

        Debug.Log($"[PlayerSpawner] Spawned {characterToSpawn.characterName} at {spawnPos}");
    }

    private void ConfigurePlayer(GameObject player, CharacterData character, bool useCarrierEquipment,
        EquipmentData explicitRight = null, EquipmentData explicitLeft = null)
    {
        PlayerController pc = player.GetComponent<PlayerController>();
        if (pc != null && plantLoadout != null)
        {
            pc.plants = plantLoadout;
            if (plantLoadout.Length > 0) pc.SelectPlant(0);
        }

        EquipmentData right = explicitRight;
        EquipmentData left = explicitLeft;
        if (!useCarrierEquipment || right == null) right = character != null ? character.defaultRightHand : null;
        if (!useCarrierEquipment || left == null) left = character != null ? character.defaultLeftHand : null;

        EquipmentManager equipManager = player.GetComponent<EquipmentManager>();
        if (equipManager == null) equipManager = player.AddComponent<EquipmentManager>();
        equipManager.ClearBuiltInEquipment();
        if (right != null) equipManager.EquipRight(right);
        if (left != null) equipManager.EquipLeft(left);
    }

    public void BindLocalPlayer(GameObject player)
    {
        if (player == null) return;
        spawnedPlayer = player;

        CameraFollow follow = cameraFollow != null ? cameraFollow : Object.FindFirstObjectByType<CameraFollow>();
        if (follow != null) follow.BindTarget(player.transform);

        PlayerController controller = player.GetComponent<PlayerController>();
        Camera gameplayCamera = follow != null ? follow.GetComponent<Camera>() : Camera.main;
        if (controller != null) controller.BindGameplayCamera(gameplayCamera);

        SetupMinimapForPlayer();
        if (GameUIManager.Instance != null) GameUIManager.Instance.BindLocalPlayer(controller);

        MapIntroFlythrough intro = Object.FindFirstObjectByType<MapIntroFlythrough>();
        if (intro != null) intro.BindLocalPlayer(controller, follow);

        NetworkObject localNetworkObject = player.GetComponent<NetworkObject>();
        Debug.Log($"[NET][PLAYER] Bound local player={player.name} owner={(localNetworkObject != null ? localNetworkObject.OwnerClientId : 0)} camera={(follow != null ? follow.name : "none")} minimap={(Object.FindFirstObjectByType<MinimapFollow>() != null ? "bound" : "none")} hud={(GameUIManager.Instance != null ? "bound" : "none")}.");
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
            minimapFollow.BindTarget(spawnedPlayer.transform);

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
