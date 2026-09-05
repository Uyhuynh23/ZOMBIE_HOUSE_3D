using UnityEngine;
using UnityEngine.InputSystem;

public class CameraFollow : MonoBehaviour
{
    [Header("Target Settings")]
    public Transform target;
    public Vector3 targetOffset = new Vector3(0f, 1.5f, 0f);

    [Header("Camera Controls")]
    public float distance = 6f;
    public float minDistance = 1.5f;
    public float maxDistance = 10f;
    
    [Tooltip("Adjust mouse look sensitivity")]
    public float mouseSensitivity = 1f;
    public float smoothTime = 15f;
    public float initialPitch = 34f;
    public bool lockCursorDuringPlay = false;

    [Header("Collision")]
    public float collisionRadius = 0.3f;
    public LayerMask collisionMask = ~0; // Everything

    [Header("Map Boundaries")]
    public bool useBounds = false;
    public float minX = -100f;
    public float maxX = 100f;
    public float minZ = -100f;
    public float maxZ = 100f;

    private float pitch;
    private float yaw = 0f;

    // Pitch limits
    private float minPitch = -10f;
    private float maxPitch = 70f;

    void Start()
    {
        pitch = initialPitch;
        Cursor.lockState = lockCursorDuringPlay ? CursorLockMode.Locked : CursorLockMode.None;
        Cursor.visible = !lockCursorDuringPlay;
        
        if (target != null)
        {
            yaw = target.eulerAngles.y;
        }
    }

    [Header("Override View")]
    [SerializeField] private bool isOverridden = false;
    private Transform overrideTransform;
    private float overrideSpeed = 8f;

    public bool IsOverridden => isOverridden;

    public void SetOverrideView(Transform viewTransform, float speed = 8f)
    {
        isOverridden = true;
        overrideTransform = viewTransform;
        overrideSpeed = speed;
    }

    public void ClearOverrideView()
    {
        isOverridden = false;
        overrideTransform = null;
        if (target != null)
        {
            yaw = transform.eulerAngles.y;
            pitch = initialPitch;
        }
    }

    void LateUpdate()
    {
        if (isOverridden && overrideTransform != null)
        {
            transform.position = Vector3.Lerp(transform.position, overrideTransform.position, overrideSpeed * Time.deltaTime);
            transform.rotation = Quaternion.Slerp(transform.rotation, overrideTransform.rotation, overrideSpeed * Time.deltaTime);
            return;
        }

        if (target == null) return;

        // Unlock cursor if Escape is pressed (for debugging/editor)
        if (Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame)
        {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }
        bool canRotate = Mouse.current != null &&
            (Cursor.lockState == CursorLockMode.Locked || Mouse.current.rightButton.isPressed);
        if (canRotate)
        {
            Vector2 delta = Mouse.current.delta.ReadValue();
            yaw += delta.x * mouseSensitivity * 0.2f;
            pitch -= delta.y * mouseSensitivity * 0.2f;
            pitch = Mathf.Clamp(pitch, minPitch, maxPitch);
        }

        // Desired rotation
        Quaternion rotation = Quaternion.Euler(pitch, yaw, 0f);

        // Calculate desired distance with collision handling
        Vector3 lookAtPos = target.position + targetOffset;
        Vector3 direction = rotation * Vector3.back;
        
        float currentDistance = distance;
        
        // SphereCast from target to desired camera pos
        if (collisionMask.value != 0 && Physics.SphereCast(
            lookAtPos, collisionRadius, direction, out RaycastHit hit, distance,
            collisionMask, QueryTriggerInteraction.Ignore))
        {
            // Filter out hits against the player itself
            if (hit.transform != target && !hit.transform.IsChildOf(target))
            {
                currentDistance = Mathf.Clamp(hit.distance, minDistance, distance);
            }
        }

        // Final calculated position
        Vector3 finalPosition = lookAtPos + direction * currentDistance;
        
        // Clamp camera position to map boundaries if enabled
        if (useBounds)
        {
            finalPosition.x = Mathf.Clamp(finalPosition.x, minX, maxX);
            finalPosition.z = Mathf.Clamp(finalPosition.z, minZ, maxZ);
        }

        // Smoothly interpolate position and rotation
        transform.position = Vector3.Lerp(transform.position, finalPosition, smoothTime * Time.deltaTime);
        transform.rotation = Quaternion.Slerp(transform.rotation, rotation, smoothTime * Time.deltaTime);
    }
}
