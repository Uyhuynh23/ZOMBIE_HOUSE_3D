using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(CharacterController))]
public class PlayerController : MonoBehaviour
{
    [Header("Movement Settings")]
    public float moveSpeed = 5f;
    public float turnSmoothTime = 0.1f;
    private float turnSmoothVelocity;

    [Header("Planting Settings")]
    public GameObject peashooterPrefab;
    public float plantingDuration = 1f;

    [Header("Visual Indicator")]
    public GameObject indicatorPrefab; // Optional: Assign a custom prefab in Inspector
    private GameObject currentIndicator;

    private CharacterController controller;
    private Animator animator;

    private bool isPlanting = false;
    private float plantingTimer = 0f;
    private PlantableSquare currentSquare;

    void Start()
    {
        controller = GetComponent<CharacterController>();
        animator = GetComponentInChildren<Animator>();
        
        // Setup Visual Indicator
        if (indicatorPrefab == null)
        {
            // Create a default indicator (a flat quad slightly above the ground)
            currentIndicator = GameObject.CreatePrimitive(PrimitiveType.Quad);
            currentIndicator.name = "PlantIndicator";
            Destroy(currentIndicator.GetComponent<Collider>()); // Remove physics
            currentIndicator.transform.rotation = Quaternion.Euler(90, 0, 0);
            currentIndicator.transform.localScale = new Vector3(1.8f, 1.8f, 1f);
            
            // Use URP Unlit shader to avoid the Pink material issue
            Shader shader = Shader.Find("Universal Render Pipeline/Unlit");
            if (shader == null) shader = Shader.Find("Standard");
            
            Material mat = new Material(shader);
            if (shader.name.Contains("Universal"))
            {
                mat.SetColor("_BaseColor", new Color(1f, 1f, 0f, 0.5f));
                mat.SetFloat("_Surface", 1); // Transparent
                mat.renderQueue = 3000;
            }
            else
            {
                mat.color = new Color(1f, 1f, 0f, 0.5f);
                mat.SetFloat("_Mode", 3); // Transparent
                mat.SetInt("_ZWrite", 0);
                mat.renderQueue = 3000;
            }
            currentIndicator.GetComponent<MeshRenderer>().material = mat;
        }
        else
        {
            currentIndicator = Instantiate(indicatorPrefab);
        }
        
        currentIndicator.SetActive(false);
    }

    void Update()
    {
        // Don't move or plant again if we are already in the middle of a planting sequence
        if (isPlanting)
        {
            HandlePlantingSequence();
            return;
        }

        HandleMovement();
        CheckCurrentSquare();
        HandlePlantInput();
    }

    void HandleMovement()
    {
        float horizontal = 0f;
        float vertical = 0f;

        if (Keyboard.current != null)
        {
            if (Keyboard.current.wKey.isPressed || Keyboard.current.upArrowKey.isPressed) vertical += 1f;
            if (Keyboard.current.sKey.isPressed || Keyboard.current.downArrowKey.isPressed) vertical -= 1f;
            if (Keyboard.current.aKey.isPressed || Keyboard.current.leftArrowKey.isPressed) horizontal -= 1f;
            if (Keyboard.current.dKey.isPressed || Keyboard.current.rightArrowKey.isPressed) horizontal += 1f;
        }

        Vector3 direction = new Vector3(horizontal, 0f, vertical).normalized;

        if (direction.magnitude >= 0.1f)
        {
            // Calculate movement direction relative to camera rotation if Camera.main exists
            if (Camera.main != null)
            {
                float targetAngle = Mathf.Atan2(direction.x, direction.z) * Mathf.Rad2Deg + Camera.main.transform.eulerAngles.y;
                direction = Quaternion.Euler(0f, targetAngle, 0f) * Vector3.forward;
                
                float smoothedAngle = Mathf.SmoothDampAngle(transform.eulerAngles.y, targetAngle, ref turnSmoothVelocity, turnSmoothTime);
                transform.rotation = Quaternion.Euler(0f, smoothedAngle, 0f);
            }
            else
            {
                float targetAngle = Mathf.Atan2(direction.x, direction.z) * Mathf.Rad2Deg;
                float smoothedAngle = Mathf.SmoothDampAngle(transform.eulerAngles.y, targetAngle, ref turnSmoothVelocity, turnSmoothTime);
                transform.rotation = Quaternion.Euler(0f, smoothedAngle, 0f);
            }

            controller.Move(direction * moveSpeed * Time.deltaTime);

            if (animator != null) animator.SetBool("IsMoving", true);
        }
        else
        {
            if (animator != null) animator.SetBool("IsMoving", false);
        }

        // Apply simple gravity
        if (!controller.isGrounded)
        {
            controller.Move(Vector3.down * 9.81f * Time.deltaTime);
        }
    }

    void CheckCurrentSquare()
    {
        // Raycast down from slightly above the player's feet
        Vector3 origin = transform.position + Vector3.up * 0.5f;
        
        if (Physics.Raycast(origin, Vector3.down, out RaycastHit hit, 2f))
        {
            PlantableSquare square = hit.collider.GetComponent<PlantableSquare>();
            
            if (square != null && !square.isOccupied)
            {
                currentSquare = square;
                currentIndicator.SetActive(true);
                currentIndicator.transform.position = square.transform.position + Vector3.up * 0.06f; 
            }
            else
            {
                currentSquare = null;
                currentIndicator.SetActive(false);
            }
        }
        else
        {
            currentSquare = null;
            currentIndicator.SetActive(false);
        }
    }

    void HandlePlantInput()
    {
        if (currentSquare != null && !currentSquare.isOccupied)
        {
            bool plantPressed = false;
            
            // Check for 'E' key to plant instead of click/space (since those are used for attacking)
            if (Keyboard.current != null && Keyboard.current.eKey.wasPressedThisFrame)
            {
                plantPressed = true;
            }

            if (plantPressed)
            {
                if (peashooterPrefab == null)
                {
                    Debug.LogError("Cannot plant! The 'peashooterPrefab' is not assigned in the PlayerController inspector.");
                    return;
                }

                isPlanting = true;
                plantingTimer = plantingDuration;
                
                // If you have a specific planting animation, use it here. Otherwise, we just wait.
                // if (animator != null) animator.SetTrigger("Plant"); 
                
                currentIndicator.SetActive(false); // Hide indicator while planting
            }
        }
    }

    void HandlePlantingSequence()
    {
        plantingTimer -= Time.deltaTime;
        
        if (plantingTimer <= 0f)
        {
            if (peashooterPrefab != null && currentSquare != null)
            {
                // Plant slightly above the square's center, and apply the exact rotation of the square!
                Instantiate(peashooterPrefab, currentSquare.transform.position + Vector3.up * 0.05f, currentSquare.transform.rotation);
                currentSquare.SetOccupied(true);
            }
            
            isPlanting = false;
        }
    }
}
