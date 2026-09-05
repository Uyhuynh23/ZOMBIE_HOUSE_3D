using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

/// <summary>
/// Controls the 3D Instruction Table in the Tutorial map.
/// Displays step-by-step instructions, objective checklists, 3D tactile keys,
/// and modifiable Sprite slots for custom AI-generated artwork and UI skins.
/// Also provides smooth close-up 3D camera inspection via [H] or [E].
/// </summary>
public class TutorialTable3D : MonoBehaviour
{
    public static TutorialTable3D Instance { get; private set; }

    [Header("Custom AI Artwork Slots")]
    [Tooltip("Custom AI-generated diagram sprite for Step 1: Movement & Basic Combat")]
    public Sprite checkpoint1Sprite;
    [Tooltip("Custom AI-generated diagram sprite for Step 2: Sun Gathering & Tree Planting")]
    public Sprite checkpoint2Sprite;
    [Tooltip("Custom AI-generated diagram sprite for Step 3: Zombie Wave & House Defense")]
    public Sprite checkpoint3Sprite;

    [Header("Theme & UI Customization Sprites")]
    [Tooltip("Modifiable background frame / parchment skin for the main 3D display")]
    public Sprite panelBackgroundSprite;
    [Tooltip("Modifiable ribbon / banner sprite for title headers")]
    public Sprite headerRibbonSprite;
    [Tooltip("Modifiable button / keycap frame sprite")]
    public Sprite keycapBackgroundSprite;
    [Tooltip("Modifiable checkpoint badge / star icon")]
    public Sprite badgeIconSprite;

    [Header("Internal UI Element References")]
    public Image panelBackgroundImage;
    public Image headerRibbonImage;
    public Text headerTitleText;
    public Image illustrationImage;
    public Text instructionBodyText;
    public Text objectiveChecklistText;
    public GameObject proximityPrompt;

    [Header("3D Keycap Groups on Table")]
    public GameObject movementKeycapGroup;
    public GameObject plantingKeycapGroup;
    public GameObject combatKeycapGroup;

    [Header("3D Camera Inspection")]
    public Transform inspectCameraAnchor;
    public float transitionSpeed = 6f;
    public float proximityRadius = 4.5f;
    public bool enableParallax = true;
    public float parallaxPitchAmount = 3.5f;
    public float parallaxYawAmount = 5.0f;

    [Header("State (read-only)")]
    [SerializeField] private bool isInspecting = false;
    [SerializeField] private int currentPhase = 1;

    private Transform parallaxDummy;
    private PlayerController player;
    private CameraFollow cameraFollow;
    private bool previousCursorLockState = false;
    private readonly List<Renderer> hiddenPlayerRenderers = new List<Renderer>();

    public bool IsInspecting => isInspecting;

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else if (Instance != this) { Destroy(gameObject); return; }

        GameObject dummyObj = new GameObject("[ParallaxAnchor]");
        dummyObj.transform.SetParent(transform);
        parallaxDummy = dummyObj.transform;

        ApplyThemeSprites();
    }

    private void Start()
    {
        if (inspectCameraAnchor == null)
        {
            Transform found = transform.Find("InspectCameraAnchor");
            if (found != null)
            {
                inspectCameraAnchor = found;
            }
            else
            {
                GameObject anchor = new GameObject("InspectCameraAnchor");
                anchor.transform.SetParent(transform);
                anchor.transform.localPosition = new Vector3(0f, 1.45f, -2.4f);
                anchor.transform.localRotation = Quaternion.Euler(6f, 0f, 0f);
                inspectCameraAnchor = anchor.transform;
            }
        }

        parallaxDummy.position = inspectCameraAnchor.position;
        parallaxDummy.rotation = inspectCameraAnchor.rotation;

        if (proximityPrompt != null)
        {
            proximityPrompt.SetActive(false);
        }

        ApplyThemeSprites();
    }

    public void ApplyThemeSprites()
    {
        if (panelBackgroundImage != null && panelBackgroundSprite != null)
        {
            panelBackgroundImage.sprite = panelBackgroundSprite;
            panelBackgroundImage.type = Image.Type.Sliced;
        }

        if (headerRibbonImage != null && headerRibbonSprite != null)
        {
            headerRibbonImage.sprite = headerRibbonSprite;
            headerRibbonImage.type = Image.Type.Sliced;
        }
    }

    private void Update()
    {
        if (player == null)
        {
            player = Object.FindFirstObjectByType<PlayerController>();
        }
        if (cameraFollow == null)
        {
            cameraFollow = Object.FindFirstObjectByType<CameraFollow>();
        }

        bool isNear = false;
        if (player != null && !isInspecting)
        {
            float dist = Vector3.Distance(player.transform.position, transform.position);
            isNear = (dist <= proximityRadius);
        }

        if (proximityPrompt != null && proximityPrompt.activeSelf != isNear && !isInspecting)
        {
            proximityPrompt.SetActive(isNear);
        }

        HandleInput(isNear);

        if (isInspecting && inspectCameraAnchor != null)
        {
            UpdateParallax();
        }
    }

    private void HandleInput(bool isNear)
    {
        if (Keyboard.current == null) return;

        bool togglePressed = Keyboard.current.hKey.wasPressedThisFrame;
        bool interactPressed = isNear && Keyboard.current.eKey.wasPressedThisFrame;
        bool escapePressed = Keyboard.current.escapeKey.wasPressedThisFrame;

        if (isInspecting)
        {
            if (togglePressed || escapePressed || Keyboard.current.eKey.wasPressedThisFrame)
            {
                ExitInspectMode();
            }
        }
        else
        {
            if (togglePressed || interactPressed)
            {
                EnterInspectMode();
            }
        }
    }

    public void EnterInspectMode()
    {
        if (isInspecting) return;
        isInspecting = true;

        if (proximityPrompt != null) proximityPrompt.SetActive(false);

        if (player == null)
        {
            player = Object.FindFirstObjectByType<PlayerController>();
        }

        if (player != null)
        {
            player.isInputLocked = true;

            // Temporarily hide the player model & weapons so they never block the 3D table during inspection
            hiddenPlayerRenderers.Clear();
            Renderer[] rends = player.GetComponentsInChildren<Renderer>(true);
            for (int i = 0; i < rends.Length; i++)
            {
                if (rends[i] != null && rends[i].enabled)
                {
                    rends[i].enabled = false;
                    hiddenPlayerRenderers.Add(rends[i]);
                }
            }
        }

        if (cameraFollow != null && inspectCameraAnchor != null)
        {
            previousCursorLockState = (Cursor.lockState == CursorLockMode.Locked);
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;

            parallaxDummy.position = inspectCameraAnchor.position;
            parallaxDummy.rotation = inspectCameraAnchor.rotation;
            cameraFollow.SetOverrideView(parallaxDummy, transitionSpeed);
        }
    }

    public void ExitInspectMode()
    {
        if (!isInspecting) return;
        isInspecting = false;

        if (player != null) player.isInputLocked = false;

        // Restore player renderers
        for (int i = 0; i < hiddenPlayerRenderers.Count; i++)
        {
            if (hiddenPlayerRenderers[i] != null)
            {
                hiddenPlayerRenderers[i].enabled = true;
            }
        }
        hiddenPlayerRenderers.Clear();

        if (cameraFollow != null)
        {
            cameraFollow.ClearOverrideView();
            if (previousCursorLockState)
            {
                Cursor.lockState = CursorLockMode.Locked;
                Cursor.visible = false;
            }
        }
    }

    private void OnDisable()
    {
        if (isInspecting)
        {
            ExitInspectMode();
        }
    }

    private void UpdateParallax()
    {
        if (!enableParallax || inspectCameraAnchor == null)
        {
            parallaxDummy.position = inspectCameraAnchor.position;
            parallaxDummy.rotation = inspectCameraAnchor.rotation;
            return;
        }

        Vector2 mouseNorm = Vector2.zero;
        if (Mouse.current != null)
        {
            Vector2 mousePos = Mouse.current.position.ReadValue();
            mouseNorm.x = Mathf.Clamp((mousePos.x / Screen.width) - 0.5f, -0.5f, 0.5f);
            mouseNorm.y = Mathf.Clamp((mousePos.y / Screen.height) - 0.5f, -0.5f, 0.5f);
        }

        Quaternion targetRot = inspectCameraAnchor.rotation * Quaternion.Euler(
            -mouseNorm.y * parallaxPitchAmount,
            mouseNorm.x * parallaxYawAmount,
            0f
        );

        Vector3 targetPos = inspectCameraAnchor.position +
            inspectCameraAnchor.right * (mouseNorm.x * 0.15f) +
            inspectCameraAnchor.up * (mouseNorm.y * 0.12f);

        parallaxDummy.position = Vector3.Lerp(parallaxDummy.position, targetPos, 10f * Time.deltaTime);
        parallaxDummy.rotation = Quaternion.Slerp(parallaxDummy.rotation, targetRot, 10f * Time.deltaTime);
    }

    /// <summary>
    /// Updates the 3D table content for the specified checkpoint phase.
    /// Automatically applies custom artwork sprites if provided!
    /// </summary>
    public void SetPhaseDisplay(int phaseIndex, string header, string body, string checklist)
    {
        currentPhase = phaseIndex;

        if (headerTitleText != null) headerTitleText.text = header;
        if (instructionBodyText != null) instructionBodyText.text = body;
        if (objectiveChecklistText != null) objectiveChecklistText.text = checklist;

        // Choose appropriate custom AI sprite
        Sprite chosenSprite = null;
        switch (phaseIndex)
        {
            case 1: chosenSprite = checkpoint1Sprite; break;
            case 2: chosenSprite = checkpoint2Sprite; break;
            case 3: chosenSprite = checkpoint3Sprite; break;
        }

        if (illustrationImage != null)
        {
            if (chosenSprite != null)
            {
                illustrationImage.sprite = chosenSprite;
                illustrationImage.color = Color.white;
                illustrationImage.gameObject.SetActive(true);
            }
            else
            {
                // If no custom sprite provided yet, leave placeholder or stylish tint
                illustrationImage.color = new Color(1f, 1f, 1f, 0.2f);
            }
        }

        // Toggle 3D keycap groups
        if (movementKeycapGroup != null) movementKeycapGroup.SetActive(phaseIndex == 1);
        if (plantingKeycapGroup != null) plantingKeycapGroup.SetActive(phaseIndex == 2);
        if (combatKeycapGroup != null) combatKeycapGroup.SetActive(phaseIndex == 3);
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(transform.position, proximityRadius);

        if (inspectCameraAnchor != null)
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawSphere(inspectCameraAnchor.position, 0.15f);
            Gizmos.DrawRay(inspectCameraAnchor.position, inspectCameraAnchor.forward * 1.5f);
        }
    }
}
