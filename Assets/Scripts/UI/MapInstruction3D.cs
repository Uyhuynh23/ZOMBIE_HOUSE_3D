using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Controls the in-game 3D Instruction Board:
/// - Animates decorative 3D elements (e.g. spinning miniature 3D Sun).
/// - Shows a floating in-world 3D prompt when the player is nearby.
/// - Allows inspecting the 3D board in close-up 3D perspective by pressing [H] anywhere or [E] nearby.
/// - Provides subtle 3D mouse parallax while inspecting for an authentic physical 3D experience.
/// </summary>
public class MapInstruction3D : MonoBehaviour
{
    public static MapInstruction3D Instance { get; private set; }

    [Header("Camera & View Anchors")]
    [Tooltip("Target transform where the camera positions itself to frame the 3D board")]
    public Transform inspectCameraAnchor;
    
    [Tooltip("Interpolation speed to smoothly glide into and out of inspect mode")]
    public float transitionSpeed = 6f;

    [Header("3D Parallax Settings")]
    [Tooltip("Enable subtle camera parallax tilt while inspecting")]
    public bool enableParallax = true;
    public float parallaxPitchAmount = 3.5f;
    public float parallaxYawAmount = 5.0f;

    [Header("Proximity Settings")]
    public float proximityRadius = 4.0f;
    public GameObject proximityPrompt;

    [Header("3D Animation")]
    public Transform rotatingSunTopper;
    public float sunRotationSpeed = 45f;

    [Header("State (read-only)")]
    [SerializeField] private bool isInspecting = false;

    private Transform parallaxDummy;
    private PlayerController player;
    private CameraFollow cameraFollow;
    private bool previousCursorLockState = false;
    private readonly System.Collections.Generic.List<Renderer> hiddenPlayerRenderers = new System.Collections.Generic.List<Renderer>();

    public bool IsInspecting => isInspecting;

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else if (Instance != this) { Destroy(gameObject); return; }

        // Create an internal helper transform for parallax blending
        GameObject dummyObj = new GameObject("[ParallaxAnchor]");
        dummyObj.transform.SetParent(transform);
        parallaxDummy = dummyObj.transform;
    }

    private void Start()
    {
        if (inspectCameraAnchor == null)
        {
            // Auto-locate child named "InspectCameraAnchor" or create fallback
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
    }

    private void Update()
    {
        // 1. Animate 3D props (miniature Sun crystal)
        if (rotatingSunTopper != null)
        {
            rotatingSunTopper.Rotate(Vector3.up, sunRotationSpeed * Time.deltaTime, Space.Self);
        }

        // 2. Cache player & camera references
        if (player == null)
        {
            player = Object.FindFirstObjectByType<PlayerController>();
        }
        if (cameraFollow == null)
        {
            cameraFollow = Object.FindFirstObjectByType<CameraFollow>();
        }

        // 3. Proximity detection
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

        // 4. Input listening
        HandleInput(isNear);

        // 5. Apply subtle 3D mouse parallax if inspecting
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

        if (proximityPrompt != null)
        {
            proximityPrompt.SetActive(false);
        }

        if (player == null)
        {
            player = Object.FindFirstObjectByType<PlayerController>();
        }

        if (player != null)
        {
            player.isInputLocked = true;

            // Temporarily hide the player model & gear so they don't block the instruction board
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

        Debug.Log("[MapInstruction3D] Entered 3D Inspect Mode. Press [H] or [ESC] to return.");
    }

    public void ExitInspectMode()
    {
        if (!isInspecting) return;
        isInspecting = false;

        if (player != null)
        {
            player.isInputLocked = false;
        }

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

        Debug.Log("[MapInstruction3D] Exited 3D Inspect Mode.");
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

        // Calculate normalized mouse screen coords (-0.5 to +0.5)
        Vector2 mouseNorm = Vector2.zero;
        if (Mouse.current != null)
        {
            Vector2 mousePos = Mouse.current.position.ReadValue();
            mouseNorm.x = Mathf.Clamp((mousePos.x / Screen.width) - 0.5f, -0.5f, 0.5f);
            mouseNorm.y = Mathf.Clamp((mousePos.y / Screen.height) - 0.5f, -0.5f, 0.5f);
        }

        // Subtly tilt and pan anchor
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
