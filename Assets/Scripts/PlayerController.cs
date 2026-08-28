using UnityEngine;
using UnityEngine.InputSystem;

public enum PlayerState
{
    Idle,
    Moving,
    Planting
}

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

    private CharacterController controller;
    private Animator animator;

    private PlayerState currentState = PlayerState.Idle;
    private Vector3 targetMovePosition;
    private GridManager.GridNode targetNode;
    private float plantingTimer = 0f;

    private Camera mainCamera;

    void Start()
    {
        controller = GetComponent<CharacterController>();
        animator = GetComponentInChildren<Animator>();
        mainCamera = Camera.main;
    }

    void Update()
    {
        // Simple Gravity
        if (!controller.isGrounded)
        {
            controller.Move(Vector3.down * 9.81f * Time.deltaTime);
        }

        switch (currentState)
        {
            case PlayerState.Idle:
                HandleMouseInput();
                if (animator != null) animator.SetBool("IsMoving", false);
                break;
            case PlayerState.Moving:
                MoveTowardsTarget();
                if (animator != null) animator.SetBool("IsMoving", true);
                break;
            case PlayerState.Planting:
                HandlePlanting();
                if (animator != null) animator.SetBool("IsMoving", false);
                break;
        }
    }

    void HandleMouseInput()
    {
        if (Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame)
        {
            Ray ray = mainCamera.ScreenPointToRay(Mouse.current.position.ReadValue());
            if (Physics.Raycast(ray, out RaycastHit hit))
            {
                if (hit.collider.CompareTag("PlantableNode"))
                {
                    GridManager.GridNode node = GridManager.Instance.GetNodeFromWorldPosition(hit.point);
                    if (node != null && !node.isOccupied)
                    {
                        targetNode = node;
                        targetMovePosition = node.worldPosition;
                        targetMovePosition.y = transform.position.y; // Keep current y
                        currentState = PlayerState.Moving;
                    }
                }
            }
        }
    }

    void MoveTowardsTarget()
    {
        Vector3 direction = (targetMovePosition - transform.position).normalized;
        direction.y = 0; // Ensure movement is strictly horizontal
        
        float distance = Vector3.Distance(new Vector3(transform.position.x, 0, transform.position.z), new Vector3(targetMovePosition.x, 0, targetMovePosition.z));

        if (distance > 0.1f)
        {
            float targetAngle = Mathf.Atan2(direction.x, direction.z) * Mathf.Rad2Deg;
            float angle = Mathf.SmoothDampAngle(transform.eulerAngles.y, targetAngle, ref turnSmoothVelocity, turnSmoothTime);
            transform.rotation = Quaternion.Euler(0f, angle, 0f);

            controller.Move(direction * moveSpeed * Time.deltaTime);
        }
        else
        {
            // Reached target
            currentState = PlayerState.Planting;
            plantingTimer = plantingDuration;
            if (animator != null) animator.SetTrigger("Attack"); // Use attack anim as planting anim for now
        }
    }

    void HandlePlanting()
    {
        plantingTimer -= Time.deltaTime;
        if (plantingTimer <= 0f)
        {
            // Spawn Peashooter
            if (peashooterPrefab != null && targetNode != null)
            {
                Instantiate(peashooterPrefab, targetNode.worldPosition, Quaternion.identity);
                targetNode.isOccupied = true;
            }
            currentState = PlayerState.Idle;
        }
    }
}
